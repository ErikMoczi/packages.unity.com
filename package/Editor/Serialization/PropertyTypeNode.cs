#if NET_4_6

using System.Collections.Generic;
using System.Collections;
using System.Linq;

using Unity.Properties.Serialization;
using System;

namespace Unity.Properties.Editor.Serialization
{
    public class PropertyConstructor
    {
        public List<KeyValuePair<string, string>> ParameterTypes { get; set; } = new List<KeyValuePair<string, string>>();
    }

    public class PropertyTypeNode
    {
        public enum TypeTag
        {
            Unknown,
            Enum,
            Class,
            Struct,
            List,
            Primitive
        }

        public static bool IsCompositeType(TypeTag t)
        {
            return IsAggregateType(t) || IsEnumerableType(t);
        }

        public static bool IsAggregateType(TypeTag t)
        {
            return t == TypeTag.Struct || t == TypeTag.Class;
        }

        public static bool IsValueType(TypeTag t)
        {
            return t == TypeTag.Struct || t == TypeTag.Primitive;
        }

        public static bool IsEnumerableType(TypeTag t)
        {
            return t == TypeTag.List;
        }

        public bool IsAbstractClass { get; set; } = false;

        public bool IsReadonly { get; set; } = false;

        public string OverrideDefaultBaseClass { get; set; } = string.Empty;

        public UserHookFlags UserHooks { get; set; } = UserHookFlags.None;

        public string Namespace { get; set; } = string.Empty;

        private string m_typename = string.Empty;
        public string TypeName
        {
            get { return m_typename; }
            set
            {
                // @TODO mmmh messy
                if (value == "list")
                {
                    m_typename = "List";
                }
                else
                {
                    m_typename = value;
                }
            }
        }

        public string FullName => string.IsNullOrEmpty(Namespace) ? _name : Namespace + "." + _name;

        // Property name (including optional nested class names separated by '/')
        private string _name = string.Empty;
        public string Name
        {
            get
            {
                return _name.Split('/').Last();
            }
            set
            {
                _name = value;
            }
        }

        public TypeTag Tag { get; set; } = TypeTag.Unknown;

        // @TODO stored as a string for now, should be better handled
        public string DefaultValue { get; set; } = string.Empty;

        public string PropertyBackingAccessor { get; set; } = string.Empty;

        public List<string> ContainsPropertyContainerDefinitions { get; set; } = new List<string>();

        // when enumerable type
        public PropertyTypeNode Of { get; set; }

        public PropertyConstructor Constructor = new PropertyConstructor();

        public List<PropertyTypeNode> Children = new List<PropertyTypeNode>();

        public List<PropertyTypeNode> ChildContainers = new List<PropertyTypeNode>();

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
                definitions = FromJson(e);
            }
            else
            {
                var d = obj as IDictionary<string, object>;
                if (d != null)
                {
                    definitions.Add(FromJson(d));
                }
            }

            return definitions;
        }

        public static PropertyTypeNode FromJson(IDictionary<string, object> d)
        {
            return ParseContainer(string.Empty, d);
        }

        public static List<PropertyTypeNode> FromJson(IEnumerable e)
        {
            var definitions = new List<PropertyTypeNode>();
            foreach (var item in e)
            {
                var d = item as IDictionary<string, object>;
                if (d != null)
                {
                    definitions.Add(FromJson(d));
                }
            }
            return definitions;
        }

        public static string ToJson(PropertyTypeNode node)
        {
            return Json.SerializeObject(SerializeTypeTreeToJson(node));
        }

        public static string ToJson(IEnumerable<PropertyTypeNode> nodes)
        {
            return Json.SerializeObject(nodes.Select(node => SerializeTypeTreeToJson(node)).ToList());
        }



        private static readonly Dictionary<string, PropertyTypeNode.TypeTag> TypeQualiersMap =
            new Dictionary<string, PropertyTypeNode.TypeTag>()
        {
            { "enum", PropertyTypeNode.TypeTag.Enum },
            { "class", PropertyTypeNode.TypeTag.Class },
            { "struct", PropertyTypeNode.TypeTag.Struct },
        };

        private static void FetchTagAndTypeNameFromFullyQualifiedName(
            string fullyQualifiedPropertyTypeName,
            out string propertyTypeName,
            out PropertyTypeNode.TypeTag typeTag,
            PropertyTypeNode.TypeTag defaultTypeTag)
        {
            if (fullyQualifiedPropertyTypeName.ToLower() == "list")
            {
                typeTag = PropertyTypeNode.TypeTag.List;
                propertyTypeName = "List";
                return;
            }

            var propertyTypeTokens = fullyQualifiedPropertyTypeName.Split(' ');

            bool isCompound = propertyTypeTokens.Length > 1;

            propertyTypeName = string.Empty;

            typeTag = defaultTypeTag;

            if (isCompound)
            {
                var qualifier = propertyTypeTokens[0];
                if (!TypeQualiersMap.ContainsKey(qualifier))
                {
                    throw new Exception($"Invalid type qualifier '{qualifier}'");
                }

                typeTag = TypeQualiersMap[propertyTypeTokens[0]];
                propertyTypeName = propertyTypeTokens[1];
            }
            else
            {
                propertyTypeName = fullyQualifiedPropertyTypeName;
            }
        }

        private static string SafeGetKeyValue(IDictionary<string, object> d, string k)
        {
            return d.ContainsKey(k) ? (d[k] as string) : string.Empty;
        }

        private static PropertyTypeNode ParseProperty(
            string propertyName,
            IDictionary<string, object> rawProperty)
        {
            if (!rawProperty.ContainsKey(JsonSchema.Keys.PropertyTypeKey))
                return null;

            var propertyQualifiedTypeName = SafeGetKeyValue(rawProperty, JsonSchema.Keys.PropertyTypeKey);

            string propertyTypeName = string.Empty;
            PropertyTypeNode.TypeTag propertyTypeTag = PropertyTypeNode.TypeTag.Unknown;
            FetchTagAndTypeNameFromFullyQualifiedName(
                propertyQualifiedTypeName,
                out propertyTypeName,
                out propertyTypeTag,
                PropertyTypeNode.TypeTag.Primitive);

            var defaultValue = SafeGetKeyValue(rawProperty, JsonSchema.Keys.PropertyDefaultValueKey);
            var propertyItemType = SafeGetKeyValue(rawProperty, JsonSchema.Keys.PropertyItemTypeIdKey);
            var propertyBackingAccessor = SafeGetKeyValue(rawProperty, JsonSchema.Keys.PropertyDelegateMemberToKey);

            bool isReadonlyProperty;
            if (!Boolean.TryParse(
                SafeGetKeyValue(rawProperty, JsonSchema.Keys.IsReadonlyPropertyKey),
                out isReadonlyProperty))
            {
                isReadonlyProperty = false;
            }

            if (propertyTypeTag == PropertyTypeNode.TypeTag.List &&
                string.IsNullOrEmpty(propertyItemType))
            {
                throw new Exception($"Property {propertyName} has 'list' type but not item type specifier");
            }

            // @TODO too simple should support recursive typedefs

            PropertyTypeNode subPropertyItem = null;
            if (!string.IsNullOrEmpty(propertyItemType))
            {
                var listItemTypeTag = PropertyTypeNode.TypeTag.Unknown;
                string listItemTypeName = string.Empty;

                FetchTagAndTypeNameFromFullyQualifiedName(
                    propertyItemType,
                    out listItemTypeName,
                    out listItemTypeTag,
                    PropertyTypeNode.TypeTag.Primitive);

                subPropertyItem = new PropertyTypeNode()
                {
                    TypeName = listItemTypeName,
                    Tag = listItemTypeTag
                };
            }

            return new PropertyTypeNode()
            {
                Name = propertyName,
                TypeName = propertyTypeName,
                Tag = propertyTypeTag,
                DefaultValue = defaultValue,
                PropertyBackingAccessor = propertyBackingAccessor,
                Of = subPropertyItem,
                IsReadonly = isReadonlyProperty
            };
        }

        private static List<PropertyTypeNode> ParseProperties(IDictionary<string, object> d)
        {
            List<PropertyTypeNode> properties = new List<PropertyTypeNode>();
            if (d != null && d.ContainsKey(JsonSchema.Keys.PropertiesListKey))
            {
                // Empty props if not
                var propertiesAsJson = d[JsonSchema.Keys.PropertiesListKey] as IDictionary<string, object>;
                if (properties == null)
                {
                    return properties;
                }

                foreach (var k in propertiesAsJson.Keys)
                {
                    var propertyName = k;

                    var property = propertiesAsJson[propertyName] as IDictionary<string, object>;

                    properties.Add(ParseProperty(propertyName, property));
                }
            }
            return properties;
        }

        private static PropertyTypeNode ParseContainer(
            string ns,
            IDictionary<string, object> t)
        {
            var containerQualifiedTypeName = SafeGetKeyValue(t, JsonSchema.Keys.ContainerNameKey);

            var generatedUserHooks =
                SafeGetKeyValue(t, JsonSchema.Keys.GeneratedUserHooksKey);

            var overrideDefaultBaseClass =
                SafeGetKeyValue(t, JsonSchema.Keys.OverrideDefaultBaseClassKey);

            bool isAbstractClass;
            if (!Boolean.TryParse(
                    SafeGetKeyValue(t, JsonSchema.Keys.IsAbstractClassKey),
                    out isAbstractClass))
            {
                isAbstractClass = false;
            }

            string containerTypeName = string.Empty;
            PropertyTypeNode.TypeTag containerTypeTag = PropertyTypeNode.TypeTag.Unknown;

            FetchTagAndTypeNameFromFullyQualifiedName(
                containerQualifiedTypeName,
                out containerTypeName,
                out containerTypeTag,
                PropertyTypeNode.TypeTag.Class);

            var n = new PropertyTypeNode
            {
                Namespace = ns,
                Name = containerTypeName,
                TypeName = containerTypeName,
                Tag = containerTypeTag,
                UserHooks = Serialization.UserHooks.From(generatedUserHooks),
                OverrideDefaultBaseClass = overrideDefaultBaseClass,
                IsAbstractClass = isAbstractClass,
                Children = ParseProperties(t)
            };

            if (t.ContainsKey(JsonSchema.Keys.ConstructedFromeKey))
            {
                var constructorParams = t[JsonSchema.Keys.ConstructedFromeKey] as IEnumerable;

                var paramTypes = new List<KeyValuePair<string, string>>();

                if (constructorParams != null)
                {
                    paramTypes.AddRange(from object p in constructorParams
                                        select p as IDictionary<string, object>
                                        into dp
                                        let paramType = dp.ContainsKey(JsonSchema.Keys.PropertyTypeKey)
                                            ? (dp[JsonSchema.Keys.PropertyTypeKey] as string)
                                            : ""
                                        let paramName = dp.ContainsKey(JsonSchema.Keys.PropertyNameKey)
                                            ? (dp[JsonSchema.Keys.PropertyNameKey] as string)
                                            : ""
                                        where !string.IsNullOrEmpty(paramName) && !string.IsNullOrEmpty(paramType)
                                        select new KeyValuePair<string, string>(paramType, paramName));
                }

                n.Constructor.ParameterTypes = paramTypes;
            }

            if (t.ContainsKey(JsonSchema.Keys.NestedPropertyContainersKey))
            {
                var nestedContainers = t[JsonSchema.Keys.NestedPropertyContainersKey] as IEnumerable;
                if (nestedContainers != null)
                {
                    foreach (var nestedContainerEnumerable in nestedContainers)
                    {
                        var nestedContainer = nestedContainerEnumerable as IDictionary<string, object>;
                        if (nestedContainer != null)
                            n.ChildContainers.Add(ParseContainer(ns, nestedContainer));
                    }
                }
            }

            return n;
        }

        private static Dictionary<string, object>
            SerializePropertyFieldsForType(PropertyTypeNode type)
        {
            var properties = new Dictionary<string, object>();
            foreach (var property in type.Children)
            {
                var propertyDescriptors = new Dictionary<string, object>
                {
                    [JsonSchema.Keys.PropertyTypeKey] = property.TypeName,
                    [JsonSchema.Keys.PropertyItemTypeIdKey] = property.Of != null ? property.Of.TypeName : string.Empty,
                    [JsonSchema.Keys.IsReadonlyPropertyKey] = property.IsReadonly,
                };
                
                if (!string.IsNullOrEmpty(property.Name))
                {
                    properties[property.Name] = propertyDescriptors;
                }
            }

            return properties;
        }

        // post order traversal
        private static Dictionary<string, object> SerializeTypeTreeToJson(PropertyTypeNode node)
        {
            var o = new List<Dictionary<string, object>>();

            foreach (var child in node.ChildContainers)
            {
                o.Add(SerializeTypeTreeToJson(child));
            }

            var serializedContainer = SerializePropertyContainerToJson(node);

            serializedContainer[JsonSchema.Keys.NestedPropertyContainersKey] = o;

            return serializedContainer;
        }

        private static Dictionary<string, object>
            SerializePropertyContainerToJson(PropertyTypeNode type)
        {
            var serializedContainer = new Dictionary<string, object>();

            var ns = type.Namespace;

            var containerTypeQualifier = string.Empty;
            if (type.Tag == TypeTag.Class)
            {
                containerTypeQualifier = "class ";
            }
            else if (type.Tag == TypeTag.Struct)
            {
                containerTypeQualifier = "struct ";
            }

            serializedContainer[JsonSchema.Keys.ContainerNameKey] = containerTypeQualifier + type.Name;
            serializedContainer[JsonSchema.Keys.IsAbstractClassKey] = type.IsAbstractClass;
            serializedContainer[JsonSchema.Keys.OverrideDefaultBaseClassKey] = type.OverrideDefaultBaseClass;
            serializedContainer[JsonSchema.Keys.PropertiesListKey] = SerializePropertyFieldsForType(type);

            if (type.UserHooks.HasFlag(UserHookFlags.OnPropertyBagConstructed))
            {
                serializedContainer[JsonSchema.Keys.GeneratedUserHooksKey] = UserHookFlags.OnPropertyBagConstructed.ToString();
            }

            return serializedContainer;
        }
    }
}

#endif // NET_4_6
