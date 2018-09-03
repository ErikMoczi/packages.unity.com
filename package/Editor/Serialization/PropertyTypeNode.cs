#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

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

        public string IsInheritedFrom { get; set; } = string.Empty;

        public bool IsAbstractClass { get; set; } = false;

        public bool IsReadonly { get; set; } = false;

        public string OverrideDefaultBaseClass { get; set; } = string.Empty;

        public UserHookFlags UserHooks { get; set; } = UserHookFlags.None;

        public string Namespace { get; set; } = string.Empty;

        public string TypeName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public TypeTag Tag { get; set; } = TypeTag.Unknown;

        // stored as a string for now, should be better handled
        public string DefaultValue { get; set; } = string.Empty;

        public string PropertyBackingAccessor { get; set; } = string.Empty;
        
        // when enumerable type
        public PropertyTypeNode Of { get; set; }

        public IDictionary<string, object> RawNode;

        public PropertyConstructor Constructor = new PropertyConstructor();

        public List<PropertyTypeNode> Children = new List<PropertyTypeNode>();
    }
}
#endif // NET_4_6
