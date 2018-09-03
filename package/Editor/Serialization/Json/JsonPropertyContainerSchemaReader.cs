#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Properties.Serialization;

namespace Unity.Properties.Editor.Serialization
{
    public static class JsonPropertyContainerSchemaReader
    {
        private static class SerializedKeys
        {
            public const string NamespaceKey = "Namespace";
            public const string TypesKey = "Types";
            public const string IsValueTypeKey = "IsValueType";
            public const string ContainerNameKey = "Name";
            public const string ConstructedFromeKey = "ConstructedFrom";
            public const string PropertiesListKey = "Properties";
            public const string PropertyTypeKey = "TypeId";
            public const string PropertyItemTypeIdKey = "ItemTypeId";
            public const string PropertyDelegateMemberToKey = "BackingField";
            public const string PropertyDefaultValueKey = "DefaultValue";
            public const string PropertyNameKey = "Name";
            public const string IsEnumKey = "IsEnum";
            public const string GeneratedUserHooksKey = "GeneratedUserHooks";
            public const string OverrideDefaultBaseClassKey = "OverrideDefaultBaseClass";
            public const string IsAbstractClassKey = "IsAbstractClass";
            public const string InheritedPropertyFromKey = "InheritedPropertyFrom";
            public const string IsReadonlyPropertyKey = "IsReadonlyProperty";
        }

        public static List<PropertyTypeNode> Read(string json)
        {
            List<PropertyTypeNode> definitions = new List<PropertyTypeNode>();

            object obj;
            if (!Unity.Properties.Serialization.Json.TryDeserializeObject(json, out obj))
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
                        ParseContainersSchema(d, definitions);

                        // expect only one for now
                        break;
                    }
                }
            }

            return definitions;
        }

        private static readonly Dictionary<string, PropertyTypeNode.TypeTag> TypesMap = new Dictionary<string, PropertyTypeNode.TypeTag>()
        {
            { "List", PropertyTypeNode.TypeTag.List },
            { "Array", PropertyTypeNode.TypeTag.Array }
        };

        // TODO Fix me : expand for composite types
        private static PropertyTypeNode.TypeTag TypeTagForSymbol(IDictionary<string, object> node)
        {
            if (node.ContainsKey(SerializedKeys.IsEnumKey))
            {
                var isEnum = false;
                if (Boolean.TryParse(node[SerializedKeys.IsEnumKey] as string, out isEnum))
                {
                    if (isEnum)
                    {
                        return PropertyTypeNode.TypeTag.Enum;
                    }
                }
            }

            if (!node.ContainsKey(SerializedKeys.IsValueTypeKey))
            {
                if (node.ContainsKey(SerializedKeys.PropertyTypeKey))
                {
                    var typeName = node[SerializedKeys.PropertyTypeKey] as string;
                    if (typeName != null && TypesMap.ContainsKey(typeName))
                    {
                        return TypesMap[typeName];
                    }
                }
                return PropertyTypeNode.TypeTag.Other;
            }

            var isValueType = false;
            if (Boolean.TryParse(node[SerializedKeys.IsValueTypeKey] as string, out isValueType))
            {
                return isValueType ? PropertyTypeNode.TypeTag.Struct : PropertyTypeNode.TypeTag.Class;
            }
            return PropertyTypeNode.TypeTag.Unknown;
        }

        // TODO BAD check for ref type & constructors etc.
        private static readonly string[] EnumerableTypes = (new List<string> { "List", "Array" }).ToArray();

        private static string SafeGetKeyValue(IDictionary<string, object> d, string k)
        {
            return d.ContainsKey(k) ? (d[k] as string) : string.Empty;
        }

        private static PropertyTypeNode ParseProperty(
            string propertyName,
            IDictionary<string, object> rawProperty,
            Dictionary<string, PropertyTypeNode> symbols)
        {
            if (!rawProperty.ContainsKey(SerializedKeys.PropertyTypeKey))
                return null;

            var defaultValue = SafeGetKeyValue(rawProperty, SerializedKeys.PropertyDefaultValueKey);

            var propertyItemType = SafeGetKeyValue(rawProperty, SerializedKeys.PropertyItemTypeIdKey);

            var propertyBackingAccessor = SafeGetKeyValue(rawProperty, SerializedKeys.PropertyDelegateMemberToKey);

            var propertyType = SafeGetKeyValue(rawProperty, SerializedKeys.PropertyTypeKey);

            var isInheritedFrom = SafeGetKeyValue(rawProperty, SerializedKeys.InheritedPropertyFromKey);

            bool isReadonlyProperty;
            if (!Boolean.TryParse(
                SafeGetKeyValue(rawProperty, SerializedKeys.IsReadonlyPropertyKey),
                out isReadonlyProperty))
            {
                isReadonlyProperty = false;
            }

            if (EnumerableTypes.Contains(propertyType) && string.IsNullOrEmpty(propertyItemType))
                return null;

            // @TODO too simple should support recursive typedefs

            PropertyTypeNode subPropertyItem = null;
            if (!string.IsNullOrEmpty(propertyItemType))
            {
                // Check if it is a known property
                var propertyItemTypeTag = symbols.ContainsKey(propertyItemType) ?
                    symbols[propertyItemType].Tag
                    : PropertyTypeNode.TypeTag.Unknown;

                subPropertyItem = new PropertyTypeNode()
                {
                    TypeName = propertyItemType,
                    Tag = propertyItemTypeTag
                };
            }

            return new PropertyTypeNode()
            {
                Name = propertyName,
                TypeName = propertyType,
                Tag = TypeTagForSymbol(rawProperty),
                DefaultValue = defaultValue,
                PropertyBackingAccessor = propertyBackingAccessor,
                Of = subPropertyItem,
                IsInheritedFrom = isInheritedFrom,
                IsReadonly = isReadonlyProperty
            };
        }

        private static void ParsePropertyContainer(
            PropertyTypeNode s,
            Dictionary<string, PropertyTypeNode> symbolsTable)
        {
            if (s == null || s.RawNode == null)
            {
                return;
            }

            var d = s.RawNode;

            var containerName = s.Name;

            if (d.ContainsKey(SerializedKeys.ConstructedFromeKey))
            {
                var constructorParams = d[SerializedKeys.ConstructedFromeKey] as IEnumerable;

                var paramTypes = new List<KeyValuePair<string, string>>();

                if (constructorParams != null)
                {
                    paramTypes.AddRange(from object p in constructorParams
                        select p as IDictionary<string, object>
                        into dp
                        let paramType = dp.ContainsKey(SerializedKeys.PropertyTypeKey)
                            ? (dp[SerializedKeys.PropertyTypeKey] as string)
                            : ""
                        let paramName = dp.ContainsKey(SerializedKeys.PropertyNameKey)
                            ? (dp[SerializedKeys.PropertyNameKey] as string)
                            : ""
                        where !string.IsNullOrEmpty(paramName) && !string.IsNullOrEmpty(paramType)
                        select new KeyValuePair<string, string>(paramType, paramName));
                }

                s.Constructor.ParameterTypes = paramTypes;
            }

            if (d.ContainsKey(SerializedKeys.PropertiesListKey))
            {
                // Empty props if not
                var properties = d[SerializedKeys.PropertiesListKey] as IDictionary<string, object>;
                if (properties == null)
                {
                    return;
                }

                foreach (var k in properties.Keys)
                {
                    var propertyName = k;

                    var property = properties[propertyName] as IDictionary<string, object>;
    
                    s.Children.Add(ParseProperty(propertyName, property, symbolsTable));
                }
            }
        }

        private static void ParseContainersSchema(IDictionary<string, object> d, List<PropertyTypeNode> definitions)
        {
            Assert.IsNotNull(definitions);

            var ns = d.ContainsKey(SerializedKeys.NamespaceKey) ? (d[SerializedKeys.NamespaceKey] as string) : "";

            var symbolsTable = new Dictionary<string, PropertyTypeNode>();

            if (d.ContainsKey(SerializedKeys.TypesKey))
            {
                // 1. First pass

                // Here we just check the meta info for the types
                // to fill some sort of a symbol table. Those meta info
                // will then useful in the case of
                // types cross referencing other types in that schema, so that
                // we know what that user defined type is (value type).

                // We could add am optional { "IsValueType": false } to all the subtypes
                // but it would be redundant (every time  a type could refer that another
                // it would duplicate that info ..).

                var types = d[SerializedKeys.TypesKey] as IEnumerable;
                if (types != null)
                {
                    foreach (var type in types)
                    {
                        var t = type as IDictionary<string, object>;
                        if (t != null)
                        {
                            var containerTypeName =
                                SafeGetKeyValue(t, SerializedKeys.ContainerNameKey);

                            var generatedUserHooks =
                                SafeGetKeyValue(t, SerializedKeys.GeneratedUserHooksKey);

                            var overrideDefaultBaseClass =
                                SafeGetKeyValue(t, SerializedKeys.OverrideDefaultBaseClassKey);

                            bool isAbstractClass;
                            if (!Boolean.TryParse(
                                    SafeGetKeyValue(t, SerializedKeys.IsAbstractClassKey),
                                    out isAbstractClass))
                            {
                                isAbstractClass = false;
                            }

                            var n = new PropertyTypeNode
                            {
                                Namespace = ns,
                                Name = containerTypeName,
                                TypeName = containerTypeName,
                                RawNode = t,
                                Tag = TypeTagForSymbol(t),
                                UserHooks = UserHooks.From(generatedUserHooks),
                                OverrideDefaultBaseClass = overrideDefaultBaseClass,
                                IsAbstractClass = isAbstractClass
                            };

                            if (n.Name != null) symbolsTable[n.Name] = n;
                        }
                    }
                }

                // 2. Second pass

                foreach (var symbol in symbolsTable)
                {
                    var node = symbol.Value;

                    ParsePropertyContainer(node, symbolsTable);

                    definitions.Add(node);
                }
            }
        }
    }
}
#endif // NET_4_6
