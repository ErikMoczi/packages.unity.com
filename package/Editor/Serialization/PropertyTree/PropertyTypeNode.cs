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

        public static class Defaults
        {
            public static bool IsAbstractClass { get; } = false;
            public static bool IsReadonly { get; } = true;
            public static bool IsPublicProperty { get; } = false;
            public static bool NoDefaultImplementation { get; } = false;
            public static bool IsCustomProperty { get; } = false;
        }


        // 
        // Property definition related values
        // 

        // Property name (including optional nested class names separated by '/')
        private string _name = string.Empty;
        public string Name
        {
            get
            {
                // @TODO cleanup semantics
                return _name.Split('/').Last().Split('.').Last();
            }
            set
            {
                _name = value;
            }
        }

        public string TypeName
        {
            get { return m_typename; }
            set
            {
                // @TODO remove string mess
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
        private string m_typename = string.Empty;

        public string FullName => string.IsNullOrEmpty(Namespace) ? _name : Namespace + "." + _name;

        public string Namespace { get; set; } = string.Empty;

        public TypeTag Tag { get; set; } = TypeTag.Unknown;

        public Type NativeType { get; set; } = null;


        // 
        // Container definition related values
        // 

        public bool IsAbstractClass { get; set; } = Defaults.IsAbstractClass;

        public bool NoDefaultImplementation { get; set; } = Defaults.NoDefaultImplementation;

        public string OverrideDefaultBaseClass { get; set; } = string.Empty;

        public UserHookFlags UserHooks { get; set; } = UserHookFlags.None;

        // @TODO split the container vs property simplification used now
        public List<PropertyTypeNode> Properties = new List<PropertyTypeNode>();

        public List<PropertyTypeNode> NestedContainers = new List<PropertyTypeNode>();


        // 
        // Property definition related values
        // 

        // @TODO stored as a string for now, should be better handled
        public string DefaultValue { get; set; } = string.Empty;

        public string PropertyBackingAccessor { get; set; } = string.Empty;
        
        public PropertyTypeNode Of { get; set; }

        public bool IsReadonly { get; set; } = Defaults.IsReadonly;

        public bool IsPublicProperty { get; set; } = false;

        public bool IsCustomProperty { get; set; } = Defaults.IsCustomProperty;

        public PropertyConstructor Constructor = new PropertyConstructor();
    }
}

#endif // NET_4_6
