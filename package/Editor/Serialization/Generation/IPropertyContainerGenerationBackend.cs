#if NET_4_6
using System;
using System.Collections.Generic;

namespace Unity.Properties.Editor.Serialization
{
    public interface IPropertyContainerGenerationBackend
    {
        void Generate(List<PropertyTypeNode> root);

        void GenerateProperty(PropertyTypeNode property,
            string containerName,
            PropertyTypeNode.TypeTag containerTypeTag);

        void GeneratePropertyContainer(PropertyTypeNode property);
    }
}
#endif // NET_4_6
