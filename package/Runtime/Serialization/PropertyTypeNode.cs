using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace Unity.Properties.Serialization
{
    // TODO smells
    public interface RawPropertyTypeNode : IDictionary<string, object>
    { }
    
    public class PropertyType
    {
        public enum TypeTag
        {
            Unknown,
            Class,
            Struct,
            List,
            Array,
            Other
        }

        public static bool IsCompositeType(TypeTag t)
        {
            return t == TypeTag.List || t == TypeTag.Array || t == TypeTag.Struct || t == TypeTag.Class;
        }

        public static bool IsValueType(TypeTag t)
        {
            return t == TypeTag.Struct || t == TypeTag.Other;
        }

        public static bool IsEnumerableType(TypeTag t)
        {
            return t == TypeTag.List || t == TypeTag.Array;
        }

        public PropertyType(string type_name, TypeTag t, string default_value, string property_backing_accessor, PropertyType of = null)
        {
            _type_name = type_name;
            _tag = t;
            _default_value = default_value;
            _property_backing_accessor = property_backing_accessor;

            if (IsEnumerableType(t))
            {
                Assert.IsNotNull(of);
            }
            // @TODO -> validate
            _of = of;
        }

        // @TODO
        public string ToString()
        {
            return string.Empty;
        }

        public string Name
        {
            get { return _type_name; }
        }
        private string _type_name;

        public TypeTag Tag
        {
            get { return _tag; }
        }
        private TypeTag _tag;

        // stored as a string for now, should be better handled
        public string DefaultValue
        {
            get { return _default_value; }
        }
        private string _default_value;

        public string PropertyBackingAccessor
        {
            get { return _property_backing_accessor; }
        }
        private string _property_backing_accessor;

        // when enumerable type
        public PropertyType Of
        {
            get { return _of; }
        }
        private PropertyType _of;
    }

    public class PropertyConstructor
    {
        public List<KeyValuePair<string, string>> ParameterTypes
        {
            get { return parameter_types; }
            set { parameter_types = value; }
        }
        private List<KeyValuePair<string, string>> parameter_types = new List<KeyValuePair<string, string>>();
    }

    public class PropertyTypeNode
    {
        public string type_name;
        public PropertyType.TypeTag tag;
        public IDictionary<string, object> raw_node;
        public PropertyConstructor constructor;

        public string TypeName => type_name; //.Split('.').Last();

        // Very linear for now, not a type tree
        public Dictionary<string, PropertyType> children = new Dictionary<string, PropertyType>();
    }
}
