#if NET_4_6
using System;
using System.Collections.Generic;

namespace Unity.Properties.Editor.Serialization
{
    public interface IPropertyContainerGenerationBackend
    {
        void Generate(List<PropertyTypeNode> root);
    }
}
#endif // NET_4_6
