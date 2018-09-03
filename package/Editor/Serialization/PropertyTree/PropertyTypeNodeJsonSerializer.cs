#if NET_4_6

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Properties.Serialization;
using UnityEngine;

namespace Unity.Properties.Editor.Serialization
{
    public class PropertyTypeNodeJsonSerializer : IPropertyTypeNodeDeserializer, IPropertyTypeNodeSerializer
    {
        public string Json { get; set; } = string.Empty;

        public List<Dictionary<string, object>> SerializedNodes { get; set; } = null;

        public void Serialize(List<PropertyTypeNode> nodes)
        {
            if (nodes == null)
            {
                throw new Exception("Invalid nodes list");
            }

            SerializedNodes = nodes.Select(SerializeTypeTreeToJson).ToList();
        }

        public List<PropertyTypeNode> Deserialize()
        {
            var definitions = new List<PropertyTypeNode>();

            if (string.IsNullOrEmpty(Json))
            {
                return definitions;
            }

            object obj;
            if ( ! Properties.Serialization.Json.TryDeserializeObject(Json, out obj))
            {
                return definitions;
            }

            var d = obj as IDictionary<string, object>;
            if (d != null)
            {
                definitions.Add(ParseContainer(d));
                return definitions;
            }

            var e = obj as IEnumerable;
            if (e != null)
            {
                definitions = FromJson(e);
            }
            return definitions;
        }

        public static List<PropertyTypeNode> FromJson(string json)
        {
            var serializer = new PropertyTypeNodeJsonSerializer {Json = json};
            return serializer.Deserialize();
        }

        public static PropertyTypeNode FromJson(IDictionary<string, object> d)
        {
            return ParseContainer(d);
        }

        public static List<PropertyTypeNode> FromJson(IEnumerable propertyNodes)
        {
            if (propertyNodes == null)
            {
                throw new Exception("Invalid property node list");
            }
            return propertyNodes.OfType<IDictionary<string, object>>().Select(dictPropertyNode => ParseContainer(dictPropertyNode)).ToList();
        }

        public static string ToJson(List<PropertyTypeNode> nodes)
        {
            var serializer = new PropertyTypeNodeJsonSerializer();
            serializer.Serialize(nodes);
            return Properties.Serialization.Json.SerializeObject(serializer.SerializedNodes);
        }

        public static Dictionary<string, object> SerializeTypeTreeToJson(PropertyTypeNode node)
        {
            var o = new List<Dictionary<string, object>>();

            foreach (var child in node.NestedContainers)
            {
                o.Add(SerializeTypeTreeToJson(child));
            }

            var serializedContainer = SerializePropertyContainerToJson(node);

            serializedContainer[JsonSchema.Keys.NestedTypesKey] = o;

            return serializedContainer;
        }

        private static readonly Dictionary<string, PropertyTypeNode.TypeTag> TypeQualiersMap =
            new Dictionary<string, PropertyTypeNode.TypeTag>()
            {
                {"enum", PropertyTypeNode.TypeTag.Enum},
                {"class", PropertyTypeNode.TypeTag.Class},
                {"struct", PropertyTypeNode.TypeTag.Struct},
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
            return d.ContainsKey(k) ? d[k].ToString() : string.Empty;
        }

        private static PropertyTypeNode ParseProperty(
            string propertyName,
            IDictionary<string, object> rawProperty)
        {
            if (!rawProperty.ContainsKey(JsonSchema.Keys.PropertyTypeKey))
                return null;

            var propertyQualifiedTypeName = SafeGetKeyValue(rawProperty, JsonSchema.Keys.PropertyTypeKey);

            string propertyTypeName;
            PropertyTypeNode.TypeTag propertyTypeTag;

            FetchTagAndTypeNameFromFullyQualifiedName(
                propertyQualifiedTypeName,
                out propertyTypeName,
                out propertyTypeTag,
                PropertyTypeNode.TypeTag.Primitive);

            var defaultValue = SafeGetKeyValue(rawProperty, JsonSchema.Keys.PropertyDefaultValueKey);
            var propertyItemType = SafeGetKeyValue(rawProperty, JsonSchema.Keys.PropertyItemTypeKey);

            bool isPublicProperty;
            if (!bool.TryParse(
                SafeGetKeyValue(rawProperty, JsonSchema.Keys.PropertyIsPublicKey),
                out isPublicProperty))
            {
                isPublicProperty = PropertyTypeNode.Defaults.IsPublicProperty;
            }

            var propertyBackingAccessor = SafeGetKeyValue(rawProperty, JsonSchema.Keys.PropertyDelegateMemberToKey);

            bool isReadonlyProperty;
            if (!bool.TryParse(
                SafeGetKeyValue(rawProperty, JsonSchema.Keys.IsReadonlyPropertyKey),
                out isReadonlyProperty))
            {
                isReadonlyProperty = PropertyTypeNode.Defaults.IsReadonly;
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
                PropertyTypeNode.TypeTag listItemTypeTag;
                string listItemTypeName;

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

            bool isCustomProperty;
            if (!bool.TryParse(
                SafeGetKeyValue(rawProperty, JsonSchema.Keys.IsCustomPropertyKey),
                out isCustomProperty))
            {
                isCustomProperty = false;
            }

            return new PropertyTypeNode()
            {
                Name = propertyName,
                TypeName = propertyTypeName,
                Tag = propertyTypeTag,
                DefaultValue = defaultValue,
                PropertyBackingAccessor = propertyBackingAccessor,
                Of = subPropertyItem,
                IsReadonly = isReadonlyProperty,
                IsPublicProperty = isPublicProperty,
                IsCustomProperty = isCustomProperty
            };
        }

        private static List<PropertyTypeNode> ParseProperties(
            string containerTypeName, IDictionary<string, object> d)
        {
            var properties = new List<PropertyTypeNode>();
            if (d != null && d.ContainsKey(JsonSchema.Keys.PropertiesListKey))
            {
                // Empty props if not
                var propertiesAsJson = d[JsonSchema.Keys.PropertiesListKey] as IEnumerable;
                if (propertiesAsJson == null)
                {
                    return properties;
                }

                foreach (var p in propertiesAsJson)
                {
                    var property = p as IDictionary<string, object>;
                    if (property == null)
                    {
                        throw new Exception(
                            $"Invalid property description found (expecting a JSON dictionary) for container {containerTypeName}.");
                    }

                    var propertyName = property[JsonSchema.Keys.PropertyNameKey] as string;

                    if (string.IsNullOrEmpty(propertyName))
                    {
                        throw new Exception(
                            $"Invalid property name (empty) found in container {containerTypeName}.");
                    }

                    properties.Add(ParseProperty(propertyName, property));
                }
            }

            return properties;
        }

        private static PropertyTypeNode ParseContainer(
            IDictionary<string, object> t)
        {
            var containerTypeName = SafeGetKeyValue(t, JsonSchema.Keys.ContainerNameKey);

            var nameSpace =
                SafeGetKeyValue(t, JsonSchema.Keys.NamespaceKey);

            var generatedUserHooks =
                SafeGetKeyValue(t, JsonSchema.Keys.GeneratedUserHooksKey);

            var overrideDefaultBaseClass =
                SafeGetKeyValue(t, JsonSchema.Keys.OverrideDefaultBaseClassKey);

            var containerTypeTag = PropertyTypeNode.TypeTag.Class;
            if (SafeGetKeyValue(t, JsonSchema.Keys.ContainerIsStructKey).ToLower() == "true")
            {
                containerTypeTag = PropertyTypeNode.TypeTag.Struct;
            }

            bool isAbstractClass;
            if (!Boolean.TryParse(
                SafeGetKeyValue(t, JsonSchema.Keys.IsAbstractClassKey),
                out isAbstractClass))
            {
                isAbstractClass = PropertyTypeNode.Defaults.IsAbstractClass;
            }

            bool noDefaultImplementation;
            if (!Boolean.TryParse(
                SafeGetKeyValue(t, JsonSchema.Keys.NoDefaultImplementationKey),
                out noDefaultImplementation))
            {
                noDefaultImplementation = PropertyTypeNode.Defaults.NoDefaultImplementation;
            }

            var n = new PropertyTypeNode
            {
                Namespace = nameSpace,
                Name = containerTypeName,
                TypeName = containerTypeName,
                Tag = containerTypeTag,
                UserHooks = Serialization.UserHooks.From(generatedUserHooks),
                OverrideDefaultBaseClass = overrideDefaultBaseClass,
                IsAbstractClass = isAbstractClass,
                Properties = ParseProperties(containerTypeName, t),
                NoDefaultImplementation = noDefaultImplementation
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

            if (t.ContainsKey(JsonSchema.Keys.NestedTypesKey))
            {
                var nestedContainers = t[JsonSchema.Keys.NestedTypesKey] as IEnumerable;
                if (nestedContainers != null)
                {
                    foreach (var nestedContainerEnumerable in nestedContainers)
                    {
                        var nestedContainer = nestedContainerEnumerable as IDictionary<string, object>;
                        if (nestedContainer != null)
                            n.NestedContainers.Add(ParseContainer(nestedContainer));
                    }
                }
            }

            return n;
        }

        private static string PropertyTypeNameFrom(PropertyTypeNode.TypeTag tag, string typeName)
        {
            switch (tag)
            {
                case PropertyTypeNode.TypeTag.Struct:
                    return $"struct {typeName}";
                case PropertyTypeNode.TypeTag.Class:
                    return $"class {typeName}";
            }

            return typeName;
        }

        private static List<object>
            SerializePropertyFieldsForType(PropertyTypeNode type)
        {
            var properties = new List<object>();

            foreach (var property in type.Properties)
            {
                var propertyFielsMap =
                    new Dictionary<string, object>
                    {
                        [JsonSchema.Keys.PropertyNameKey] = property.Name,
                        [JsonSchema.Keys.PropertyTypeKey] = PropertyTypeNameFrom(property.Tag, property.TypeName)
                    };

                if (property.Of != null)
                {
                    propertyFielsMap[JsonSchema.Keys.PropertyItemTypeKey] =
                        PropertyTypeNameFrom(
                            property.Of.Tag, property.Of.TypeName);
                }

                if (property.IsReadonly != PropertyTypeNode.Defaults.IsReadonly)
                {
                    propertyFielsMap[JsonSchema.Keys.IsReadonlyPropertyKey] = property.IsReadonly;
                }

                if (property.IsCustomProperty != PropertyTypeNode.Defaults.IsCustomProperty)
                {
                    propertyFielsMap[JsonSchema.Keys.IsCustomPropertyKey] = property.IsCustomProperty;
                }

                if (property.IsPublicProperty != PropertyTypeNode.Defaults.IsPublicProperty)
                {
                    propertyFielsMap[JsonSchema.Keys.PropertyIsPublicKey] = property.IsPublicProperty;
                }

                properties.Add(propertyFielsMap);
            }

            return properties;
        }

        private static Dictionary<string, object>
            SerializePropertyContainerToJson(PropertyTypeNode type)
        {
            var serializedContainer = new Dictionary<string, object>
            {
                [JsonSchema.Keys.ContainerNameKey] = type.Name,
                [JsonSchema.Keys.ContainerIsStructKey] = type.Tag == PropertyTypeNode.TypeTag.Struct,
                [JsonSchema.Keys.PropertiesListKey] = SerializePropertyFieldsForType(type)
            };

            if (!string.IsNullOrEmpty(type.Namespace))
            {
                serializedContainer[JsonSchema.Keys.NamespaceKey] = type.Namespace;
            }

            if (type.IsAbstractClass != PropertyTypeNode.Defaults.IsAbstractClass)
            {
                serializedContainer[JsonSchema.Keys.IsAbstractClassKey] = type.IsAbstractClass;
            }

            if (type.NoDefaultImplementation != PropertyTypeNode.Defaults.NoDefaultImplementation)
            {
                serializedContainer[JsonSchema.Keys.NoDefaultImplementationKey] = type.NoDefaultImplementation;
            }

            if (!string.IsNullOrEmpty(type.OverrideDefaultBaseClass))
            {
                serializedContainer[JsonSchema.Keys.OverrideDefaultBaseClassKey] = type.OverrideDefaultBaseClass;
            }

            if (type.UserHooks.HasFlag(UserHookFlags.OnPropertyBagConstructed))
            {
                serializedContainer[JsonSchema.Keys.GeneratedUserHooksKey] =
                    UserHookFlags.OnPropertyBagConstructed.ToString();
            }

            return serializedContainer;
        }
    }
}

#endif // NET_4_6
