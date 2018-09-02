using System.Collections.Generic;
using System;

namespace UnityEngine.ResourceManagement
{
    public class ResourceLocationBase : IResourceLocation
    {
        string m_name;
        string m_id;
        string m_providerId;
        List<IResourceLocation> m_dependencies;
        public string InternalId { get { return m_id; } }
        public string ProviderId { get { return m_providerId; } }
        public IList<IResourceLocation> Dependencies { get { return m_dependencies; } }
        public bool HasDependencies { get { return m_dependencies != null && m_dependencies.Count > 0; } }
        public object Data { get { return null; } }

        public override string ToString()
        {
            return m_name;
        }

        public ResourceLocationBase(string name, string id, string providerId, params IResourceLocation[] dependencies)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(id);
            if (string.IsNullOrEmpty(providerId))
                throw new ArgumentNullException(providerId);
            m_name = name;
            m_id = id;
            m_providerId = providerId;
            m_dependencies = new List<IResourceLocation>(dependencies);
        }
    }

}
