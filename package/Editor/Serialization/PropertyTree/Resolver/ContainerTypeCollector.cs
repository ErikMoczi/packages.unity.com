#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unity.Properties.Editor.Serialization
{
    public class ContainerTypeCollector : IContainerTypeTreeVisitor
    {
        public Dictionary<string, PropertyTypeNode.TypeTag> BuiltinTypes { get; internal set; } 
            = new Dictionary<string, PropertyTypeNode.TypeTag>();
        
        public void VisitNestedContainer(ContainerTypeTreePath path, PropertyTypeNode container)
        {
            BuiltinTypes.Add(path.FullPath, container.Tag);
        }
        
        public void VisitContainer(ContainerTypeTreePath path, PropertyTypeNode container)
        {
            BuiltinTypes.Add(path.FullPath, container.Tag);
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
