using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Assertions;

namespace Unity.Properties.Serialization
{
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
            return IsAggregateType(t) || IsEnumerableType(t);
        }

        public static bool IsAggregateType(TypeTag t)
        {
            return t == TypeTag.Struct || t == TypeTag.Class;
        }

        public static bool IsValueType(TypeTag t)
        {
            return t == TypeTag.Struct || t == TypeTag.Other;
        }

        public static bool IsEnumerableType(TypeTag t)
        {
            return t == TypeTag.List || t == TypeTag.Array;
        }

        public PropertyType(string type_name,
            TypeTag t,
            string default_value,
            string property_backing_accessor,
            PropertyType of = null)
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
}
