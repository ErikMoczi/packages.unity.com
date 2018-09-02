namespace UnityEngine.ResourceManagement
{
    public class ResourceLocationLocator : IResourceLocator<IResourceLocation>
    {
        public IResourceLocation Locate(IResourceLocation key)
        {
            return key;
        }
    }
}
