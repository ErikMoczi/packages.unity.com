#if (NET_4_6 || NET_STANDARD_2_0)

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

        public List<Dictionary<string, object>> SerializedNodes { get; set; }

        public PropertyTypeNodeJsonSerializer(TypeResolver typeResolver)
        {
            _typeResolver = typeResolver;
        }

        private readonly TypeResolver _typeResolver;

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
                definitions.Add(ParseContainer(d, new ContainerTypeTreePath()));
                return definitions;
            }

            var e = obj as IEnumerable;
            if (e != null)
            {
                definitions = e.OfType<IDictionary<string, object>>().Select(
                    dj => ParseContainer(dj, new ContainerTypeTreePath())
                    ).ToList();
            }
            return definitions;
        }

        public static List<PropertyTypeNode> FromJson(string json, TypeResolver resolver)
        {
            var serializer = new PropertyTypeNodeJsonSerializer(resolver) { Json = json};
            return serializer.Deserialize();
        }

        public static PropertyTypeNode FromJson(IDictionary<string, object> d, TypeResolver resolver)
        {
            return new PropertyTypeNodeJsonSerializer(resolver).ParseContainer(
                d, new ContainerTypeTreePath());
        }

        public static List<PropertyTypeNode> FromJson(IEnumerable propertyNodes, TypeResolver resolver)
        {
            if (propertyNodes == null)
            {
                throw new Exception("Invalid property node list");
            }

            return propertyNodes.OfType<IDictionary<string, object>>().Select(
                dictPropertyNode =>
                    new PropertyTypeNodeJsonSerializer(resolver).ParseContainer(
                        dictPropertyNode, new ContainerTypeTreePath()
                    )
                ).ToList();
        }

        public static string ToJson(List<PropertyTypeNode> nodes, TypeResolver resolver)
        {
            var serializer = new PropertyTypeNodeJsonSerializer(resolver);
            serializer.Serialize(nodes);
            return Properties.Serialization.Json.SerializeObject(serializer.SerializedNodes);
        }

        private static Dictionary<string, object> SerializeTypeTreeToJson(PropertyTypeNode node)
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

        private PropertyTypeNode.TypeTag GetTypeTagFromPropertyName(
            string propertyTypeName,
            ContainerTypeTreePath context,
            PropertyTypeNode.TypeTag defaultTypeTag = PropertyTypeNode.TypeTag.Class)
        {
            var propertyTypeTag = defaultTypeTag;
            if (TypeResolver.IsPrimitiveType(propertyTypeName))
            {
                propertyTypeTag = PropertyTypeNode.TypeTag.Primitive;
            }
            else if (_typeResolver != null)
            {
                propertyTypeTag = _typeResolver.Resolve(context, propertyTypeName);
            }
            return propertyTypeTag;
        }

        private static string SafeGetKeyValue(IDictionary<string, object> d, string k)
        {
            return d.ContainsKey(k) ? d[k].ToString() : string.Empty;
        }

        private PropertyTypeNode ParseProperty(
            string propertyName,
            IDictionary<string, object> rawProperty,
            ContainerTypeTreePath context)
        {
            if ( ! rawProperty.ContainsKey(JsonSchema.Keys.PropertyTypeKey))
            {
                return null;
            }

            var propertyTypeName = SafeGetKeyValue(rawProperty, JsonSchema.Keys.PropertyTypeKey);

            var propertyTypeTag = GetTypeTagFromPropertyName(propertyTypeName, context);

            bool isPublicProperty;
            if (!bool.TryParse(
                SafeGetKeyValue(rawProperty, JsonSchema.Keys.PropertyIsPublicKey),
                out isPublicProperty))
            {
                isPublicProperty = PropertyTypeNode.Defaults.IsPublicProperty;
            }

            var defaultValue = SafeGetKeyValue(rawProperty, JsonSchema.Keys.PropertyDefaultValueKey);
            var propertyItemTypeName = SafeGetKeyValue(rawProperty, JsonSchema.Keys.PropertyItemTypeKey);

            var propertyBackingAccessor = SafeGetKeyValue(rawProperty, JsonSchema.Keys.PropertyDelegateMemberToKey);

            bool isReadonlyProperty;
            if (!bool.TryParse(
                SafeGetKeyValue(rawProperty, JsonSchema.Keys.IsReadonlyPropertyKey),
                out isReadonlyProperty))
            {
                isReadonlyProperty = PropertyTypeNode.Defaults.IsReadonly;
            }

            if (propertyTypeTag == PropertyTypeNode.TypeTag.List &&
                string.IsNullOrEmpty(propertyItemTypeName))
            {
                throw new Exception($"Property {propertyName} has 'list' type but not item type specifier");
            }

            PropertyTypeNode subPropertyItem = null;
            if ( !string.IsNullOrEmpty(propertyItemTypeName))
            {
                subPropertyItem = new PropertyTypeNode
                {
                    TypeName = propertyItemTypeName,
                    Tag = GetTypeTagFromPropertyName(propertyItemTypeName, context, PropertyTypeNode.TypeTag.Primitive),
                    NativeType = TryExtractPropertyNativeType(propertyItemTypeName)
                };
            }

            bool isCustomProperty;
            if (!bool.TryParse(
                SafeGetKeyValue(rawProperty, JsonSchema.Keys.IsCustomPropertyKey),
                out isCustomProperty))
            {
                isCustomProperty = PropertyTypeNode.Defaults.IsCustomProperty;
            }
            
            bool dontInitializeBackingField;
            if (!bool.TryParse(
                SafeGetKeyValue(rawProperty, JsonSchema.Keys.DontInitializeBackingFieldKey),
                out dontInitializeBackingField))
            {
                dontInitializeBackingField = PropertyTypeNode.Defaults.DontInitializeBackingField;
            }

            return new PropertyTypeNode
            {
                PropertyName = propertyName,
                TypeName = propertyTypeName,
                Tag = propertyTypeTag,
                DefaultValue = defaultValue,
                PropertyBackingAccessor = propertyBackingAccessor,
                Of = subPropertyItem,
                IsReadonly = isReadonlyProperty,
                IsPublicProperty = isPublicProperty,
                IsCustomProperty = isCustomProperty,
                NativeType = TryExtractPropertyNativeType(propertyTypeName),
                DontInitializeBackingField = dontInitializeBackingField
            };
        }

        private List<PropertyTypeNode> ParseProperties(
            IDictionary<string, object> d, ContainerTypeTreePath context)
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
                            $"Invalid property description found (expecting a JSON dictionary) for container {context.FullPath}.");
                    }

                    var propertyName = property[JsonSchema.Keys.PropertyNameKey] as string;

                    if (string.IsNullOrEmpty(propertyName))
                    {
                        throw new Exception(
                            $"Invalid property name (empty) found in container {context.FullPath}.");
                    }

                    properties.Add(ParseProperty(propertyName, property, context));
                }
            }

            return properties;
        }

        private Type TryExtractPropertyNativeType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName) || _typeResolver == null)
            {
                return null;
            }
            return _typeResolver.ResolveType(typeName);
        }

        private PropertyTypeNode ParseContainer(
            IDictionary<string, object> t,
            ContainerTypeTreePath context,
            bool ignoreNamespace = false)
        {
            var containerTypeName = SafeGetKeyValue(t, JsonSchema.Keys.ContainerNameKey);

            if (string.IsNullOrWhiteSpace(containerTypeName))
            {
                throw new Exception(
                    $"Invalid container type (empty) while parsing json (path: '{context.FullPath}')");
            }

            if ( ! ignoreNamespace)
            {
                context.Namespace = SafeGetKeyValue(t, JsonSchema.Keys.NamespaceKey);
            }

            var generatedUserHooks =
                SafeGetKeyValue(t, JsonSchema.Keys.GeneratedUserHooksKey);

            var overrideDefaultBaseClass =
                SafeGetKeyValue(t, JsonSchema.Keys.OverrideDefaultBaseClassKey);

            bool isAbstractClass;
            if (!bool.TryParse(
                SafeGetKeyValue(t, JsonSchema.Keys.IsAbstractClassKey),
                out isAbstractClass))
            {
                isAbstractClass = PropertyTypeNode.Defaults.IsAbstractClass;
            }

            bool noDefaultImplementation;
            if (!bool.TryParse(
                SafeGetKeyValue(t, JsonSchema.Keys.NoDefaultImplementationKey),
                out noDefaultImplementation))
            {
                noDefaultImplementation = PropertyTypeNode.Defaults.NoDefaultImplementation;
            }
            
            var n = new PropertyTypeNode
            {
                TypePath = new ContainerTypeTreePath(context),
                TypeName = containerTypeName,
                Tag = GetTypeTagFromPropertyName(containerTypeName, context),
                UserHooks = UserHooks.From(generatedUserHooks),
                OverrideDefaultBaseClass = overrideDefaultBaseClass,
                IsAbstractClass = isAbstractClass,
                Properties = ParseProperties(t, context),
                NativeType = TryExtractPropertyNativeType(containerTypeName),
                NoDefaultImplementation = noDefaultImplementation,
            };

            if (t.ContainsKey(JsonSchema.Keys.ConstructedFromKey))
            {
                var constructorParams = t[JsonSchema.Keys.ConstructedFromKey] as IEnumerable;

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

            if ( ! t.ContainsKey(JsonSchema.Keys.NestedTypesKey) ||
                 ! (t[JsonSchema.Keys.NestedTypesKey] is IEnumerable))
            {
                return n;
            }

            var nestedContainers = (IEnumerable) t[JsonSchema.Keys.NestedTypesKey];

            context.TypePath.Push(containerTypeName);

            foreach (var nestedContainerEnumerable in nestedContainers)
            {
                var nestedContainer = nestedContainerEnumerable as IDictionary<string, object>;
                if (nestedContainer == null)
                {
                    continue;
                }

                n.NestedContainers.Add(
                    ParseContainer(
                        nestedContainer,
                        context,
                        true
                        )
                    );
            }

            context.TypePath.Pop();

            return n;
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
                        [JsonSchema.Keys.PropertyNameKey] = property.PropertyName,
                        [JsonSchema.Keys.PropertyTypeKey] = property.TypeName
                    };

                if (property.Of != null)
                {
                    propertyFielsMap[JsonSchema.Keys.PropertyItemTypeKey] = property.Of != null ? property.Of.TypeName : string.Empty;
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

                if (property.DontInitializeBackingField != PropertyTypeNode.Defaults.DontInitializeBackingField)
                {
                    propertyFielsMap[JsonSchema.Keys.DontInitializeBackingFieldKey] = property.DontInitializeBackingField;
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
                [JsonSchema.Keys.ContainerNameKey] = type.TypeName,
                [JsonSchema.Keys.ContainerIsStructKey] = type.Tag == PropertyTypeNode.TypeTag.Struct,
                [JsonSchema.Keys.PropertiesListKey] = SerializePropertyFieldsForType(type)
            };

            if (!string.IsNullOrEmpty(type.TypePath.Namespace))
            {
                serializedContainer[JsonSchema.Keys.NamespaceKey] = type.TypePath.Namespace;
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

#endif // (NET_4_6 || NET_STANDARD_2_0)
