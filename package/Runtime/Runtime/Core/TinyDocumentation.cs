

using Unity.Properties;

namespace Unity.Tiny
{
    internal sealed partial class TinyDocumentation : IPropertyContainer
    {
        public IVersionStorage VersionStorage { get; }

        public TinyDocumentation(IVersionStorage versionStorage)
        {
            VersionStorage = versionStorage;
        }
    }
}

