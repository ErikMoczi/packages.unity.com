using System.Collections.Generic;

namespace ResourceManagement
{
    public class ResourceLocationBase<T> : IResourceLocation<T>
    {
        T m_key;
        string m_id;
        string m_providerId;
        List<IResourceLocation> m_dependencies;
        public T key { get { return m_key; } }
        public string id { get { return m_id; } }
        public string providerId { get { return m_providerId; } }
        public IList<IResourceLocation> dependencies { get { return m_dependencies; } set { m_dependencies = new List<IResourceLocation>(); m_dependencies.AddRange(value); } }

        public override string ToString()
        {
            return m_key.ToString();
        }

        public ResourceLocationBase(T key, string id, string providerId, params IResourceLocation[] deps)
        {
            m_key = key;
            m_id = id;
            m_providerId = providerId;
            dependencies = new List<IResourceLocation>(deps);
        }
    }
}
