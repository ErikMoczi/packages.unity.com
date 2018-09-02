namespace UnityEngine.ResourceManagement
{
    public class LegacyResourcesLocator : IResourceLocator<string>
    {
        public IResourceLocation Locate(string key)
        {
            return new LegacyResourcesLocation(key);
        }
    }
}
