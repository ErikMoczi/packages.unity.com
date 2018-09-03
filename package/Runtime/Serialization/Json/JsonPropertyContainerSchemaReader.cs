using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Properties.Serialization
{
    public static class JsonPropertyContainerSchemaReader
    {
        private static class SerializedKeys
        {
            // @TODO no cases
            public const string NamespaceKey = "Namespace";
            public const string TypesKey = "Types";
            public const string IsValueTypeKey = "IsValueType";
            public const string ContainerNameKey = "Name";
            public const string ConstructedFromeKey = "ConstructedFrom";
            public const string PropertiesListKey = "Properties";
            public const string PropertyTypeKey = "TypeId";
            public const string PropertyItemTypeIdKey = "ItemTypeId";
            public const string PropertyDelegateMemberToKey = "BackingField";
            public const string PropertyDelegateTargetKey = "Target";
            public const string PropertyDefaultValueKey = "DefaultValue";
            public const string PropertyNameKey = "Name";
            public const string LegacyPropertyTypeKey = "FieldType";
        }

        public static List<PropertyContainerType> Read(string json)
        {
            List<PropertyContainerType> definitions = new List<PropertyContainerType>();

            object obj;
            if (!SimpleJson.TryDeserializeObject(json, out obj))
            {
                return definitions;
            }

            var e = obj as IEnumerable;

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

            return definitions;
        }

        static private Dictionary<string, PropertyType.TypeTag> TypesMap = new Dictionary<string, PropertyType.TypeTag>()
        {
            { "List", PropertyType.TypeTag.List },
            { "Array", PropertyType.TypeTag.Array }
        };

        // TODO Fix me : expand for composite types
        private static PropertyType.TypeTag TypeTagForSymbol(IDictionary<string, object> node)
        {
            if (!node.ContainsKey(SerializedKeys.IsValueTypeKey))
            {
                if (node.ContainsKey(SerializedKeys.PropertyTypeKey))
                {
                    var type_name = node[SerializedKeys.PropertyTypeKey] as string;
                    if (TypesMap.ContainsKey(type_name))
                    {
                        return TypesMap[type_name];
                    }
                }

                return PropertyType.TypeTag.Other;
            }

            var is_value_type = false;
            if (Boolean.TryParse(node[SerializedKeys.IsValueTypeKey] as string, out is_value_type))
            {
                return is_value_type ? PropertyType.TypeTag.Struct : PropertyType.TypeTag.Class;
            }
            return PropertyType.TypeTag.Unknown;
        }

        private static string ParsePropertyType(object t)
        {
            var d = t as IDictionary<string, object>;
            if (d != null)
            {
                return d.ContainsKey("Name") ? (d["Name"] as string) : "";
            }
            return string.Empty;
        }

        // TODO BAD check for ref type & constructors etc.
        static private string[] EnumerableTypes = (new List<string> { "List", "Array" }).ToArray();

        private static PropertyType ParseProperty(
            IDictionary<string, object> raw_property,
            Dictionary<string, PropertyTypeNode> symbols)
        {
            if (!raw_property.ContainsKey(SerializedKeys.PropertyTypeKey))
                return null;

            var default_value = raw_property.ContainsKey(SerializedKeys.PropertyDefaultValueKey) ? (raw_property[SerializedKeys.PropertyDefaultValueKey] as string) : string.Empty;
            var property_item_type = raw_property.ContainsKey(SerializedKeys.PropertyItemTypeIdKey) ? (raw_property[SerializedKeys.PropertyItemTypeIdKey] as string) : string.Empty;
            var property_backing_accessor = raw_property.ContainsKey(SerializedKeys.PropertyDelegateMemberToKey) ? (raw_property[SerializedKeys.PropertyDelegateMemberToKey] as string) : string.Empty;

            var property_type = raw_property[SerializedKeys.PropertyTypeKey] as string;

            if (EnumerableTypes.Contains(property_type) && string.IsNullOrEmpty(property_item_type))
                return null;

            // @TODO too simple should support recursive typedefs

            PropertyType sub_property_item = null;
            if (!string.IsNullOrEmpty(property_item_type))
            {
                var property_item_type_tag = symbols.ContainsKey(property_item_type) ?
                    symbols[property_item_type].Tag
                    : PropertyType.TypeTag.Unknown;

                sub_property_item = new PropertyType(
                    property_item_type,
                    property_item_type_tag,
                    string.Empty,
                    string.Empty);
            }

            return new PropertyType(
                property_type,
                TypeTagForSymbol(raw_property),
                default_value,
                property_backing_accessor,
                sub_property_item);
        }

        private static void ParsePropertyContainer(PropertyTypeNode s, Dictionary<string, PropertyTypeNode> symbols_table)
        {
            var d = s.raw_node;

            var container_name = s.TypeName;

            if (d.ContainsKey(SerializedKeys.ConstructedFromeKey))
            {
                var constructor_params = d[SerializedKeys.ConstructedFromeKey] as IEnumerable;

                var param_types = new List<KeyValuePair<string, string>>();

                foreach (var p in constructor_params)
                {
                    // TODO keep symbol table

                    var dp = p as IDictionary<string, object>;

                    var param_type = dp.ContainsKey(SerializedKeys.PropertyTypeKey) ? (dp[SerializedKeys.PropertyTypeKey] as string) : "";
                    var param_name = dp.ContainsKey(SerializedKeys.PropertyNameKey) ? (dp[SerializedKeys.PropertyNameKey] as string) : "";

                    if (!string.IsNullOrEmpty(param_name) && !string.IsNullOrEmpty(param_type))
                    {
                        param_types.Add(new KeyValuePair<string, string>(param_type, param_name));
                    }
                }

                s.constructor.ParameterTypes = param_types;
            }

            if (d.ContainsKey(SerializedKeys.PropertiesListKey))
            {
                // Empty props if not
                var properties = d[SerializedKeys.PropertiesListKey] as IDictionary<string, object>;
                foreach (var k in properties.Keys)
                {
                    var property_name = k;

                    var property = properties[property_name] as IDictionary<string, object>;
    
                    s.children[property_name] = ParseProperty(property, symbols_table);
                }
            }
        }

        private static void ParseContainersSchema(IDictionary<string, object> d, List<PropertyContainerType> definitions)
        {
            Assert.IsNotNull(definitions);

            var ns = d.ContainsKey(SerializedKeys.NamespaceKey) ? (d[SerializedKeys.NamespaceKey] as string) : "";

            var symbols_table = new Dictionary<string, PropertyTypeNode>();

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
                foreach (var type in types)
                {
                    var t = type as IDictionary<string, object>;
                    if (t != null)
                    {
                        var n = new PropertyTypeNode();

                        n.TypeName = t.ContainsKey(SerializedKeys.ContainerNameKey)
                            ? (t[SerializedKeys.ContainerNameKey] as string) : "";

                        n.raw_node = t as IDictionary<string, object>;

                        n.Tag = TypeTagForSymbol(t);

                        symbols_table[n.TypeName] = n;
                    }
                }

                // 2. Second pass

                foreach (var symbol in symbols_table)
                {
                    var node = symbol.Value;

                    PropertyContainerType container = new PropertyContainerType();
                    container.Namespace = ns;
                    container.PropertyTypeNode = node;

                    ParsePropertyContainer(container.PropertyTypeNode, symbols_table);

                    definitions.Add(container);
                }
            }
        }
    }
}
