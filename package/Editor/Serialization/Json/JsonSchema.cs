#if NET_4_6

using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Properties.Serialization;

namespace Unity.Properties.Editor.Serialization
{
    public class JsonSchema
    {
        public static string CurrentVersion { get; } = "0.1.18-preview";

        public static class Keys
        {
            // general
            public const string VersionKey = "Version";
            public const string NamespaceKey = "Namespace";
            public const string TypesKey = "Types";
            public const string UsingAssembliesKey = "UsingAssemblies";
            public const string RequiredAssembliesKey = "RequiredAssemblies";

            // container
            public const string ContainerIsStructKey = "IsStruct";
            public const string ConstructedFromeKey = "ConstructedFrom";
            public const string ContainerNameKey = "Name";
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
        }

        public static string ToJson(JsonSchema schema)
        {
            if (schema?.PropertyTypeNodes == null)
            {
                throw new InvalidOperationException("Invalid property type nodes to be serialized");
            }
            var result = new Dictionary<string, object>
            {
                [Keys.VersionKey] = CurrentVersion,
                [Keys.UsingAssembliesKey] = schema.UsingAssemblies,
                [Keys.TypesKey] = schema.PropertyTypeNodes.Select(PropertyTypeNodeJsonSerializer.SerializeTypeTreeToJson),
                [Keys.RequiredAssembliesKey] = schema.RequiredAssemblies
            };
            return Json.SerializeObject(result);
        }

        public static JsonSchema FromJson(string json)
        {
            object obj;
            if ( ! Json.TryDeserializeObject(json, out obj))
            {
                throw new Exception("Cannot deserialize provided JSON");
            }

            var dd = obj as IDictionary<string, object>;
            if (dd != null)
            {
                return GetJsonSchemaFrom(dd);
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
                    return GetJsonSchemaFrom(d);
                }
            }

            throw new Exception("Cannot deserialize provided JSON");
        }

        public List<string> UsingAssemblies { get; set; } = new List<string>();

        public string Namespace { get; set; }

        public List<string> RequiredAssemblies { get; set;  } = new List<string>();

        public List<PropertyTypeNode> PropertyTypeNodes { get; set; } = new List<PropertyTypeNode>();


        private static JsonSchema GetJsonSchemaFrom(IDictionary<string, object> d)
        {
            var schema = new JsonSchema();

            schema.PropertyTypeNodes.AddRange(ParseContainersSchema(d));

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

            // Post fix
            if (d.ContainsKey(Keys.NamespaceKey) && !string.IsNullOrEmpty(d[Keys.NamespaceKey] as string))
            {
                schema.Namespace = (string)d[Keys.NamespaceKey];

                schema.PropertyTypeNodes = schema.PropertyTypeNodes.Select(definition =>
                {
                    if (string.IsNullOrEmpty(definition.Namespace))
                    {
                        definition.Namespace = schema.Namespace;
                    }
                    return definition;
                }).ToList();
            }

            return schema;
        }

        private static bool IsCompatibleVersion(IDictionary<string, object> d)
        {
            return (d.ContainsKey(Keys.VersionKey) && d[Keys.VersionKey].ToString() == CurrentVersion);
        }

        private static IEnumerable<PropertyTypeNode> ParseContainersSchema(IDictionary<string, object> d)
        {
            var definitions = new List<PropertyTypeNode>();

            if ( ! IsCompatibleVersion(d))
            {
                throw new Exception($"Incompatible schema versions CurrentVersion : {CurrentVersion}");
            }

            if (d.ContainsKey(Keys.TypesKey))
            {
                var types = d[Keys.TypesKey] as IEnumerable;
                if (types == null)
                {
                    return definitions;
                }
                definitions.AddRange(
                    types.OfType<IDictionary<string, object>>().Select(PropertyTypeNodeJsonSerializer.FromJson));
            }

            return definitions;
        }
    }
}

#endif
