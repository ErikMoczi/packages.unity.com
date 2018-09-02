using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Base class for IResourceProvider.
    /// </summary>
    public abstract class ResourceProviderBase : IResourceProvider
    {
        protected ResourceProviderBase() { }

        /// <inheritdoc/>
        public virtual string ProviderId
        {
            get { return GetType().FullName; }
        }

        /// <inheritdoc/>
        public virtual bool CanProvide<TObject>(IResourceLocation location)
            where TObject : class
        {
            if (location == null)
                throw new ArgumentException("IResourceLocation location cannot be null.");
            return ProviderId.Equals(location.ProviderId, System.StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("[{0}]", ProviderId);
        }

        /// <inheritdoc/>
        public abstract IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        where TObject : class;

        /// <inheritdoc/>
        public virtual bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            return true;
        }
    }
}
