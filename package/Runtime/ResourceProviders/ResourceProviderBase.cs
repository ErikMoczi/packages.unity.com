using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Base class for IResourceProvider.
    /// </summary>
    public abstract class ResourceProviderBase : IResourceProvider
    {
        protected string m_ProviderId;

        /// <inheritdoc/>
        public virtual string ProviderId
        {
            get
            {
                if(string.IsNullOrEmpty(m_ProviderId))
                    m_ProviderId = GetType().FullName;

                return m_ProviderId;
            }
        }

        /// <inheritdoc/>
        public virtual bool Initialize(string id, string data)
        {
            m_ProviderId = id;
            return !string.IsNullOrEmpty(m_ProviderId);
        }

        /// <inheritdoc/>
        public virtual bool CanProvide<TObject>(IResourceLocation location)
            where TObject : class
        {
            if (location == null)
                throw new ArgumentException("IResourceLocation location cannot be null.");
            return ProviderId.Equals(location.ProviderId, StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ProviderId;
        }

        /// <inheritdoc/>
        public abstract IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        where TObject : class;

        public virtual bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new ArgumentNullException("location");
            return true;
        }
    }
}
