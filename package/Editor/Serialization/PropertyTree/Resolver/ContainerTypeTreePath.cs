#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Properties.Editor.Serialization
{
    public class ContainerTypeTreePath
    {
        public static readonly string TypeNameSeparator = ".";

        public static readonly string NestedTypeNameSeparator = "/";

        public ContainerTypeTreePath()
        {}

        public ContainerTypeTreePath(ContainerTypeTreePath other)
        {
            Namespace = other.Namespace;
            TypePath = new Stack<string>(other.TypePath.Reverse());
        }

        public static ContainerTypeTreePath CreateFromString(string fullpath)
        {
            if (string.IsNullOrEmpty(fullpath))
            {
                return new ContainerTypeTreePath();
            }

            // The type path for the leaf type here:
            // namespace my.namespace {
            //   class roottype {
            //     class nested {
            //       class types {}
            //     }
            //   }
            // }
            // is:
            //  "my.namespace.roottype/nested/types"
            // for which the traversal path should be
            //  "my.namespace" > "roottype" > "nested" > "types"

            var paths = fullpath.Split(NestedTypeNameSeparator[0]);

            var namespaceAndRootTypename = paths[0];
            var nestedTypeNames = paths.Reverse().Take(paths.Length - 1).Reverse().ToList();

            var rootTypeName = string.Empty;
            var nameSpace = string.Empty;
            {
                var topLevelPathParts =
                    namespaceAndRootTypename.Split(TypeNameSeparator[0]);

                rootTypeName = topLevelPathParts.Last();

                if (topLevelPathParts.Length > 1)
                {
                    nameSpace = string.Join(
                        TypeNameSeparator
                        , topLevelPathParts.Take(topLevelPathParts.Length - 1));
                }
            }

            var ttPath = new ContainerTypeTreePath()
            {
                Namespace = nameSpace,
                TypePath = new Stack<string>(new [] { rootTypeName })
            };

            foreach (var nestedTypeName in nestedTypeNames)
            {
                ttPath.TypePath.Push(nestedTypeName);
            }

            return ttPath;
        }

        public ContainerTypeTreePath WithNestedTypeName(string nestedTypename)
        {
            var n = CreateFromString(FullPath);
            n.TypePath.Push(nestedTypename);
            return CreateFromString(n.FullPath);
        }

        public ContainerTypeTreePath WithRootTypeName(string typeName)
        {
            return new ContainerTypeTreePath()
            {
                Namespace = this.Namespace,
                TypePath = new Stack<string> (new [] { typeName })
            };
        }

        public string Namespace { get; set; } = string.Empty;

        public Stack<string> TypePath { get; set; } = new Stack<string>();

        public string TypeName => TypePath.Count > 0 ? TypePath.Peek() : string.Empty;

        public string FullPath
        {
            get
            {
                if (TypePath.Count == 0)
                {
                    return Namespace;
                }

                var prefix = string.IsNullOrWhiteSpace(Namespace)
                    ? string.Empty : Namespace + TypeNameSeparator;

                // @TODO cleanup

                var paths = new List<string>();
                var s = new Stack<string>(TypePath);
                while (s.Count != 0)
                {
                    paths.Add(s.Pop());
                }

                return prefix + string.Join(NestedTypeNameSeparator, paths);
            }
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)