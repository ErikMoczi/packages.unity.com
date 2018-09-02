using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public abstract class ResourceProviderBase : IResourceProvider
    {
        protected ResourceProviderBase() {}

        public virtual string ProviderId
        {
            get { return GetType().FullName; }
        }

        public virtual bool CanProvide<TObject>(IResourceLocation location)
            where TObject : class
        {
            return ProviderId.Equals(location.ProviderId, System.StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return string.Format("[{0}]", ProviderId);
        }

        public abstract IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        where TObject : class;

        public virtual bool Release(IResourceLocation location, object asset) { return true; }
    }
}
