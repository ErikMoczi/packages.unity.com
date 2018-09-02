using System;

namespace UnityEngine.ResourceManagement
{
    public class UnknownResourceProviderException : Exception
    {
        public IResourceLocation Location { get; private set; }
        public UnknownResourceProviderException(IResourceLocation location)
        {
            Location = location;
        }
        public override string Message
        {
            get
            {
                return base.Message + ", Location=" + Location;
            }
        }
    }

    public class ResourceProviderFailedException : Exception
    {
        public IResourceLocation Location { get; private set; }
        public IResourceProvider Provider { get; private set; }
        public ResourceProviderFailedException(IResourceProvider provider, IResourceLocation location)
        {
            Provider = provider;
            Location = location;
        }
        public override string Message
        {
            get
            {
                return base.Message + ", Provider=" + Provider + ", Location=" + Location;
            }
        }
    }
}