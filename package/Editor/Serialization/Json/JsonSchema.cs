#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

using Unity.Properties.Serialization;

namespace Unity.Properties.Editor.Serialization
{
    public class JsonSchema
    {
        // @TODO "configure.in" this
        public static string CurrentVersion { get; } = "0.1.18-preview";

        public static class Keys
        {
            // general
            public const string VersionKey = "Version";
            public const string NamespaceKey = "Namespace";
            public const string TypesKey = "Types";
            public const string ContainerNameKey = "Name";
            public const string UsingAssembliesKey = "UsingAssemblies";
            public const string RequiredAssembliesKey = "RequiredAssemblies";

            // container
            public const string ContainerIsStructKey = "IsStruct";
            public const string ContainerNamespaceKey = "Namespace";
            public const string ConstructedFromKey = "ConstructedFrom";
            public const string PropertiesListKey = "Properties";
            public const string GeneratedUserHooksKey = "GeneratedUserHooks";
            public const string OverrideDefaultBaseClassKey = "OverrideDefaultBaseClass";
            public const string IsAbstractClassKey = "IsAbstractClass";
            public const string NestedTypesKey = "NestedTypes";
            public const string NoDefaultImplementationKey = "NoDefaultImplementation";

            // properties
            public const string PropertyTypeKey = "Type";
            public const string PropertyItemTypeKey = "ItemType";
            public const string PropertyDelegateMemberToKey = "BackingField";
            public const string PropertyDefaultValueKey = "DefaultValue";
            public const string PropertyNameKey = "Name";
            public const string PropertyIsPublicKey = "IsPublic";
            public const string IsCustomPropertyKey = "IsCustom";
            public const string IsReadonlyPropertyKey = "IsReadonlyProperty";
            public const string DontInitializeBackingFieldKey = "DontInitializeBackingField";
        }

        public static string ToJson(JsonSchema schema)
        {
            if (schema?.PropertyTypeNodes == null)
            {
                throw new InvalidOperationException("Invalid property type nodes to be serialized");
            }

            var nodesList = schema.PropertyTypeNodes.ToList();

            // Collect all the dependancies (assemblies and defined symbols)

            var assemblyDependancies = new List<string>();

            var builtinSymbols = new Dictionary<string, PropertyTypeNode.TypeTag>();

            foreach (var node in nodesList)
            {
                assemblyDependancies.AddRange(node.DependantAssemblyNames());

                var collector = new ContainerTypeCollector();
                node.VisitRoot(collector);

                collector.BuiltinTypes.ToList().ForEach(kv => builtinSymbols.Add(kv.Key, kv.Value));
            }

            // Serialize the property container tree

            var serializer = new PropertyTypeNodeJsonSerializer(new TypeResolver(assemblyDependancies, builtinSymbols));
            serializer.Serialize(nodesList);

            var result = new Dictionary<string, object>
            {
                [Keys.VersionKey] = CurrentVersion,
                [Keys.UsingAssembliesKey] = schema.UsingAssemblies,
                [Keys.TypesKey] = serializer.SerializedNodes,
                [Keys.RequiredAssembliesKey] = schema.RequiredAssemblies
            };

            return Json.SerializeObject(result);
        }

        public static JsonSchema FromJson(
            string json, Dictionary<string, PropertyTypeNode.TypeTag> injectBuiltinTypes = null)
        {
            object obj;
            if ( ! Json.TryDeserializeObject(json, out obj))
            {
                throw new Exception("Cannot deserialize provided JSON");
            }

            var dd = obj as IDictionary<string, object>;
            if (dd != null)
            {
                return GetJsonSchemaFrom(dd, injectBuiltinTypes);
            }

            var e = obj as IEnumerable;
            if (e == null)
            {
                throw new Exception("Cannot deserialize provided JSON");
            }

            foreach (var item in e)
            {
                var d = item as IDictionary<string, object>;
                if (d != null)
                {
                    return GetJsonSchemaFrom(d, injectBuiltinTypes);
                }
            }

            throw new Exception("Cannot deserialize provided JSON");
        }

        public List<string> UsingAssemblies { get; set; } = new List<string>();

        public string Namespace { get; set; }

        public List<string> RequiredAssemblies { get; set;  } = new List<string>();

        public List<PropertyTypeNode> PropertyTypeNodes { get; set; } = new List<PropertyTypeNode>();


        private static JsonSchema GetJsonSchemaFrom(IDictionary<string, object> d, Dictionary<string, PropertyTypeNode.TypeTag> injectBuiltinTypes)
        {
            var schema = new JsonSchema();

            schema.PropertyTypeNodes.AddRange(ParseContainersSchema(d, injectBuiltinTypes));

            if (d.ContainsKey(Keys.UsingAssembliesKey) &&
                d[Keys.UsingAssembliesKey] is IList)
            {
                var usings = d[Keys.UsingAssembliesKey] as IList;

                schema.UsingAssemblies = usings.OfType<string>().Select(u => u as string).ToList();
            }

            if (d.ContainsKey(Keys.RequiredAssembliesKey) &&
                d[Keys.RequiredAssembliesKey] is IList)
            {
                var requiredAssemblies = d[Keys.RequiredAssembliesKey] as IList;

                schema.RequiredAssemblies = requiredAssemblies.OfType<string>().Select(u => u as string).ToList();
            }

            return schema;
        }

        private static bool IsCompatibleVersion(IDictionary<string, object> d)
        {
            return d.ContainsKey(Keys.VersionKey) && d[Keys.VersionKey].ToString() == CurrentVersion;
        }

        // @TODO extract as a visitor/tree iterator

        private static void CollectTypesFromTypeTree(
            IEnumerable types,
            Dictionary<string, PropertyTypeNode.TypeTag> builtinSymbols,
            ContainerTypeTreePath path = null)
        {
            foreach (var type in types.OfType<IDictionary<string, object>>())
            {
                if (!type.ContainsKey(Keys.ContainerNameKey)
                    || string.IsNullOrEmpty(type[Keys.ContainerNameKey] as string))
                {
                    continue;
                }

                var currentPath = path ?? new ContainerTypeTreePath();

                currentPath.TypePath.Push((string) type[Keys.ContainerNameKey]);

                if (path != null)
                {
                    currentPath.Namespace = type.ContainsKey(Keys.NamespaceKey)
                        ? type[Keys.NamespaceKey] as string
                        : string.Empty;
                }

                var tag = PropertyTypeNode.TypeTag.Class;

                if (type.ContainsKey(Keys.ContainerIsStructKey)
                    && type[Keys.ContainerIsStructKey] is bool
                    && (bool) type[Keys.ContainerIsStructKey])
                {
                    tag = PropertyTypeNode.TypeTag.Struct;
                }

                builtinSymbols[currentPath.FullPath] = tag;

                // recurse

                if (!type.ContainsKey(Keys.NestedTypesKey) ||
                    !(type[Keys.NestedTypesKey] is IEnumerable))
                {
                    continue;
                }

                CollectTypesFromTypeTree(
                    (IEnumerable) type[Keys.NestedTypesKey],
                    builtinSymbols,
                    currentPath);
            }
        }

        private static IEnumerable<PropertyTypeNode> ParseContainersSchema(
            IDictionary<string, object> d, Dictionary<string, PropertyTypeNode.TypeTag> injectBuiltinTypes)
        {
            var definitions = new List<PropertyTypeNode>();

            if ( ! IsCompatibleVersion(d))
            {
                throw new Exception($"Incompatible schema versions CurrentVersion : {CurrentVersion}");
            }

            var referenceAssemblyNames = new List<string>();
            if (d.ContainsKey(Keys.RequiredAssembliesKey))
            {
                var assemblyNames = d[Keys.RequiredAssembliesKey] as IEnumerable;
                if (assemblyNames != null)
                {
                    referenceAssemblyNames.AddRange(assemblyNames.OfType<string>().ToList());
                }
            }

            if (d.ContainsKey(Keys.TypesKey))
            {
                var types = d[Keys.TypesKey] as IEnumerable;
                if (types == null)
                {
                    return definitions;
                }

                // 1. Do a first pass to collect the list of symbols

                var builtinSymbols = new Dictionary<string, PropertyTypeNode.TypeTag>();

                if (injectBuiltinTypes != null)
                {
                    builtinSymbols = builtinSymbols.Union(injectBuiltinTypes)
                        .ToDictionary(k => k.Key, v => v.Value);
                }

                CollectTypesFromTypeTree(types, builtinSymbols);
               
                // 2. Actual parsing

                var resolver = new TypeResolver(referenceAssemblyNames, builtinSymbols);
                definitions.AddRange(
                    types.OfType<IDictionary<string, object>>().Select(
                        e => PropertyTypeNodeJsonSerializer.FromJson(e, resolver)
                        )
                    );
            }

            // Post fix
            if (d.ContainsKey(Keys.NamespaceKey) && ! string.IsNullOrEmpty(d[Keys.NamespaceKey] as string))
            {
                var globalNamespace = d[Keys.NamespaceKey] as string;

                definitions = definitions.Select(definition =>
                {
                    if (string.IsNullOrEmpty(definition.TypePath.Namespace))
                    {
                        definition.TypePath.Namespace = globalNamespace;
                    }

                    return definition;
                }).ToList();
            }

            return definitions;
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
