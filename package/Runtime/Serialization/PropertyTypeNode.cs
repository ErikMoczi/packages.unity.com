using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace Unity.Properties.Serialization
{
    public class PropertyConstructor
    {
        public List<KeyValuePair<string, string>> ParameterTypes { get; set; } =
            new List<KeyValuePair<string, string>>();
    }

    public class PropertyTypeNode
    {
        public string TypeName { get; set; }

        public PropertyType.TypeTag Tag { get; set; }

        public IDictionary<string, object> raw_node;

        public PropertyConstructor constructor;

        // Very linear for now, not a type tree
        public Dictionary<string, PropertyType> children = new Dictionary<string, PropertyType>();
    }
}
