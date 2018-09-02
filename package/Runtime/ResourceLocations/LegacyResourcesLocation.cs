using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public struct LegacyResourcesLocation : IResourceLocation<string>
    {
        string m_key;
        public string Key { get { return m_key; } }
        public LegacyResourcesLocation(string key) { m_key = key; }
        public string InternalId { get { return m_key; } }
        public string ProviderId { get { return typeof(LegacyResourcesProvider).FullName; } }
        public IList<IResourceLocation> Dependencies { get { return null; } }
    }
}
