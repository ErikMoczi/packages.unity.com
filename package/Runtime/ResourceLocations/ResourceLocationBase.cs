using System.Collections.Generic;
using System;

namespace UnityEngine.ResourceManagement
{
    public class ResourceLocationBase<TKey> : IResourceLocation<TKey>
    {
        TKey m_key;
        string m_id;
        string m_providerId;
        List<IResourceLocation> m_dependencies;
        public TKey Key { get { return m_key; } }
        public string InternalId { get { return m_id; } }
        public string ProviderId { get { return m_providerId; } }
        public IList<IResourceLocation> Dependencies { get { return m_dependencies; } }

        public override string ToString()
        {
            return m_key.ToString();
        }

        public ResourceLocationBase(TKey key, string id, string providerId, params IResourceLocation[] dependencies)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(id);
            if (string.IsNullOrEmpty(providerId))
                throw new ArgumentNullException(providerId);
            m_key = key;
            m_id = id;
            m_providerId = providerId;
            m_dependencies = new List<IResourceLocation>(dependencies);
        }
    }

}
