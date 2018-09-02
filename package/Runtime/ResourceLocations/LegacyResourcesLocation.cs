using System.Collections.Generic;
using ResourceManagement.ResourceProviders;

namespace ResourceManagement.ResourceLocations
{
    public struct LegacyResourcesLocation : IResourceLocation<string>
    {
        string m_key;
        public string key { get { return m_key; } }
        public LegacyResourcesLocation(string a) { m_key = a; }
        public string id { get { return m_key; } }
        public string providerId { get { return typeof(LegacyResourcesProvider).FullName; } }
        public IList<IResourceLocation> dependencies { get { return null; } }
    }
}
