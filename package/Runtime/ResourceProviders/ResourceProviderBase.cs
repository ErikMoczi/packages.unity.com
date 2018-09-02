using System.Collections.Generic;

namespace ResourceManagement.ResourceProviders
{
    public abstract class ResourceProviderBase : IResourceProvider
    {
        protected ResourceProviderBase() {}

        public virtual string providerId
        {
            get { return GetType().FullName; }
        }

        public virtual bool CanProvide<TObject>(IResourceLocation loc)
            where TObject : class
        {
            return providerId.Equals(loc.providerId, System.StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return string.Format("[{0}]", providerId);
        }

        public abstract IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
        where TObject : class;

        public abstract bool Release(IResourceLocation loc, object asset);
    }
}
