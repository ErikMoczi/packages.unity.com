using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unity.Properties.Serialization
{
    public class PropertyContainerType
    {
        public string Namespace
        {
            get { return namespace_name; }
            set { namespace_name = value; }
        }
        private string namespace_name;

        public PropertyTypeNode PropertyTypeNode
        {
            get { return container_type_tree; }
            set { container_type_tree = value; }
        }
        private PropertyTypeNode container_type_tree = new PropertyTypeNode();
    }
}
