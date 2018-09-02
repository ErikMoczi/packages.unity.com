using System.Collections.Generic;

namespace ResourceManagement
{
    public class ResourceLocationBase<T> : IResourceLocation<T>
    {
        T m_address;
        string m_id;
        string m_providerId;
        List<IResourceLocation> m_dependencies;
        public T address { get { return m_address; } }
        public string id { get { return m_id; } }
        public string providerId { get { return m_providerId; } }
        public IList<IResourceLocation> dependencies { get { return m_dependencies; } set { m_dependencies = new List<IResourceLocation>(); m_dependencies.AddRange(value); } }

        public override string ToString()
        {
            return m_address.ToString();
        }

        public ResourceLocationBase(T address, string id, string providerId, params IResourceLocation[] deps)
        {
            m_address = address;
            m_id = id;
            m_providerId = providerId;
            dependencies = new List<IResourceLocation>(deps);
        }
    }
}
