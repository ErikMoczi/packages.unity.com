using System.Collections.Generic;
using ResourceManagement.ResourceProviders;

namespace ResourceManagement.ResourceLocations
{
    public struct LegacyResourcesLocation : IResourceLocation<string>
    {
        string m_address;
        public string address { get { return m_address; } }
        public LegacyResourcesLocation(string a) { m_address = a; }
        public string id { get { return m_address; } }
        public string providerId { get { return typeof(LegacyResourcesProvider).FullName; } }
        public IList<IResourceLocation> dependencies { get { return null; } }
    }
}
