#if NET_4_6

using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

using Unity.Properties.Serialization;

namespace Unity.Properties.Editor.Serialization
{
    public static class JsonSchema
    {
        public static class Keys
        {
            public const string NamespaceKey = "Namespace";
            public const string TypesKey = "Types";
            public const string ContainerNameKey = "Name";
            public const string ConstructedFromeKey = "ConstructedFrom";
            public const string PropertiesListKey = "Properties";
            public const string PropertyTypeKey = "TypeId";
            public const string PropertyItemTypeIdKey = "ItemTypeId";
            public const string PropertyDelegateMemberToKey = "BackingField";
            public const string PropertyDefaultValueKey = "DefaultValue";
            public const string PropertyNameKey = "Name";
            public const string GeneratedUserHooksKey = "GeneratedUserHooks";
            public const string OverrideDefaultBaseClassKey = "OverrideDefaultBaseClass";
            public const string IsAbstractClassKey = "IsAbstractClass";
            public const string IsReadonlyPropertyKey = "IsReadonlyProperty";
            public const string NestedPropertyContainersKey = "NestedPropertyContainers";
        }

        public static string ToJson(PropertyTypeNode node)
        {
            throw new NotImplementedException("Not implemented");
        }

        public static string ToJson(IEnumerable<PropertyTypeNode> nodes)
        {
            throw new NotImplementedException("Not implemented");
        }

        public static List<PropertyTypeNode> FromJson(string json)
        {
            var definitions = new List<PropertyTypeNode>();

            object obj;
            if (!Json.TryDeserializeObject(json, out obj))
            {
                return definitions;
            }

            var e = obj as IEnumerable;
            if (e != null)
            {
                foreach (var item in e)
                {
                    var d = item as IDictionary<string, object>;
                    if (d != null)
                    {
                        definitions.AddRange(ParseContainersSchema(d));

                        // Expect one ... 
                        break;
                    }
                }
            }
            else
            {
                var d = obj as IDictionary<string, object>;
                if (d != null)
                {
                    definitions.AddRange(ParseContainersSchema(d));
                }
            }

            return definitions;
        }
        
        private static List<PropertyTypeNode> ParseContainersSchema(IDictionary<string, object> d)
        {
            List<PropertyTypeNode> definitions = new List<PropertyTypeNode>();

            // var ns = d.ContainsKey(Keys.NamespaceKey) ? (d[Keys.NamespaceKey] as string) : "";

            if (d.ContainsKey(Keys.TypesKey))
            {
                var types = d[Keys.TypesKey] as IEnumerable;
                if (types != null)
                {
                    foreach (var type in types)
                    {
                        var t = type as IDictionary<string, object>;
                        if (t != null)
                        {
                            definitions.Add(PropertyTypeNode.FromJson(t));
                        }
                    }
                }
            }

            return definitions;
        }
    }
}

#endif
