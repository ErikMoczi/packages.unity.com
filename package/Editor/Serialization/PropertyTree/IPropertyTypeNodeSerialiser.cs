#if NET_4_6

using System.Collections.Generic;

namespace Unity.Properties.Editor.Serialization
{
    public interface IPropertyTypeNodeSerializer
    {
        void Serialize(List<PropertyTypeNode> nodes);
    }
}

#endif // NET_4_6
