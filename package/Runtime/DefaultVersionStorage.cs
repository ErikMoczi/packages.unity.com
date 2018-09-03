using System.Collections.Generic;

namespace Unity.Properties
{
    public class DefaultVersionStorage : IVersionStorage
    {
        public static readonly DefaultVersionStorage Instance = new DefaultVersionStorage();

        private readonly Dictionary<IProperty, Dictionary<int, int>> m_Versions;

        private DefaultVersionStorage()
        {
            m_Versions = new Dictionary<IProperty, Dictionary<int, int>>();
        }

        public int GetVersion<TContainer>(IProperty property, ref TContainer container)
            where TContainer : IPropertyContainer
        {
            Dictionary<int, int> containerVersions;
            if (!m_Versions.TryGetValue(property, out containerVersions))
            {
                return -1;
            }

            int v;
            if (containerVersions.TryGetValue(container.GetHashCode(), out v))
            {
                return v;
            }

            return -1;
        }

        public void IncrementVersion<TContainer>(IProperty property, ref TContainer container)
            where TContainer : IPropertyContainer
        {
            var key = container.GetHashCode();
            
            Dictionary<int, int> containerVersions;
            if (!m_Versions.TryGetValue(property, out containerVersions))
            {
                m_Versions[property] = containerVersions = new Dictionary<int, int>();
                containerVersions[key] = 1;
                return;
            }
            int v;
            if (!containerVersions.TryGetValue(key, out v))
            {
                containerVersions[key] = 1;
                return;
            }
            ++containerVersions[key];
        }
    }
}