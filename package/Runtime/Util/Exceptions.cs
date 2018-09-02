using System;
using System.Runtime.Serialization;

namespace UnityEngine.ResourceManagement
{
    public class UnknownResourceProviderException : ResourceManagerException
    {
        public IResourceLocation Location { get; private set; }
        public UnknownResourceProviderException(IResourceLocation location)
        {
            Location = location;
        }
        public UnknownResourceProviderException() { }
        public UnknownResourceProviderException(string message) : base(message) { }
        public UnknownResourceProviderException(string message, Exception innerException) : base(message, innerException) { }
        protected UnknownResourceProviderException(SerializationInfo message, StreamingContext context) : base(message, context) { }


        public override string Message
        {
            get
            {
                return base.Message + ", Location=" + Location;
            }
        }
    }

    public class ResourceManagerException : Exception
    {
        public ResourceManagerException() { }
        public ResourceManagerException(string message) : base(message) { }
        public ResourceManagerException(string message, Exception innerException) : base(message, innerException) { }
        protected ResourceManagerException(SerializationInfo message, StreamingContext context) : base(message, context) { }
    }

    public class ResourceProviderFailedException : ResourceManagerException
    {
        public IResourceLocation Location { get; private set; }
        public IResourceProvider Provider { get; private set; }
        public ResourceProviderFailedException(IResourceProvider provider, IResourceLocation location)
        {
            Provider = provider;
            Location = location;
        }
        public ResourceProviderFailedException() { }
        public ResourceProviderFailedException(string message) : base(message) { }
        public ResourceProviderFailedException(string message, Exception innerException) : base(message, innerException) { }
        protected ResourceProviderFailedException(SerializationInfo message, StreamingContext context) : base(message, context) { }

        public override string Message
        {
            get
            {
                return base.Message + ", Provider=" + Provider + ", Location=" + Location;
            }
        }
    }
}