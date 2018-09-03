#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Unity.Properties.Editor.Serialization
{
    public class TypeResolver
    {
        public static bool IsPrimitiveType(string typeName)
        {
            return PrimitiveTypes.ContainsKey(typeName)
                   || PrimitiveTypes.Values.Any(v => v.Name == typeName || v.FullName == typeName);
        }

        private static readonly Dictionary<string, Type> PrimitiveTypes = new Dictionary<string, Type>()
        {
            { "void", typeof(void) } ,
            { "bool", typeof(bool) },
            { "char", typeof(char) },
            { "int", typeof(int) },
            { "uint", typeof(uint) },
            { "long", typeof(long) },
            { "ulong", typeof(ulong) },
            { "byte", typeof(byte) },
            { "sbyte", typeof(sbyte) },
            { "short", typeof(short) },
            { "ushort", typeof(ushort) },
            { "float", typeof(float) },
            { "double", typeof(double) },
            { "string", typeof(string) },
            { "object", typeof(object) }
        };

        public TypeResolver()
        {}

        public TypeResolver(
            List<string> referenceAssemblyNames,
            Dictionary<string, PropertyTypeNode.TypeTag> builtinSymbols)
        {
            if (referenceAssemblyNames != null)
            {
                _referenceAssemblyNames = referenceAssemblyNames;
                _referenceAssemblyNames.Reverse();
            }

            if (builtinSymbols != null)
            {
                _builtinSymbols = builtinSymbols;
            }
        }

        public TypeResolver WithReferenceAssemblyNames(List<string> referenceAssemblyNames)
        {
            if (referenceAssemblyNames != null)
            {
                _referenceAssemblyNames = referenceAssemblyNames;
                _referenceAssemblyNames.Reverse();
            }

            return this;
        }

        public TypeResolver WithBuiltinSymbols(Dictionary<string, PropertyTypeNode.TypeTag> builtinSymbols)
        {
            if (builtinSymbols != null)
            {
                _builtinSymbols = builtinSymbols;
            }

            return this;
        }

        private List<string> _referenceAssemblyNames = new List<string>();
        private Dictionary<string, PropertyTypeNode.TypeTag> _builtinSymbols =
            new Dictionary<string, PropertyTypeNode.TypeTag>();

        // @TODO when typeName is a qualified type name (contains namespace etc)
        private PropertyTypeNode.TypeTag ResolveWithBuiltinSymbols(
            ContainerTypeTreePath context, string typeName)
        {
            var tag = PropertyTypeNode.TypeTag.Unknown;

            if (context == null)
            {
                return tag;
            }

            var typeTreePath = new ContainerTypeTreePath(context);

            while (typeTreePath.TypePath.Count != 0)
            {
                var currentFullyQualifiedTypeName = typeTreePath.WithNestedTypeName(typeName).FullPath;

                if (_builtinSymbols.ContainsKey(currentFullyQualifiedTypeName))
                {
                    return _builtinSymbols[currentFullyQualifiedTypeName];
                }
                typeTreePath.TypePath.Pop();
            }

            // top level context

            if ( ! string.IsNullOrWhiteSpace(context.Namespace))
            {
                var typePath = context.WithRootTypeName(typeName).FullPath;
                if (_builtinSymbols.ContainsKey(typePath))
                {
                    return _builtinSymbols[typePath];
                }
            }

            // Fallback to global namespace

            return _builtinSymbols.ContainsKey(typeName) ? _builtinSymbols[typeName] : tag;
        }

        public PropertyTypeNode.TypeTag Resolve(
            ContainerTypeTreePath context, string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return PropertyTypeNode.TypeTag.Unknown;
            }

            // Handle trivial cases first

            if (typeName.ToLower() == "list")
            {
                return PropertyTypeNode.TypeTag.List;
            }

            if (IsPrimitiveType(typeName))
            {
                return PropertyTypeNode.TypeTag.Primitive;
            }

            // Look up in the current known resolver's symbols

            var tag = ResolveWithBuiltinSymbols(context, typeName);
            if (tag != PropertyTypeNode.TypeTag.Unknown)
            {
                // Found it as an internal symbol
                return tag;
            }

            // Look up the assemblies

            if (_referenceAssemblyNames == null)
            {
                return PropertyTypeNode.TypeTag.Unknown;
            }

            var t = ResolveType(typeName);

            if (t == null)
            {
                return PropertyTypeNode.TypeTag.Unknown;
            }

            if (t.IsClass)
            {
                return PropertyTypeNode.TypeTag.Class;
            }
            if (t.IsEnum)
            {
                return PropertyTypeNode.TypeTag.Enum;
            }
            if (t.IsValueType && t.IsLayoutSequential)
            {
                return PropertyTypeNode.TypeTag.Struct;
            }

            return PropertyTypeNode.TypeTag.Primitive;
        }

        public Type ResolveType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            if (typeName.ToLower() == "list")
            {
                return typeof(List<>);
            }

            if (IsPrimitiveType(typeName))
            {
                return typeof(object).Assembly.GetType(typeName);
            }

            if (_referenceAssemblyNames == null)
            {
                return null;
            }

            foreach (var assemblyName in _referenceAssemblyNames)
            {
                var assembly = Assembly.Load(new AssemblyName(assemblyName));
                if (assembly != null)
                {
                    var type = assembly.GetType(typeName);

                    if (type == null)
                    {
                        continue;
                    }

                    return type;
                }
            }

            return null;
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
