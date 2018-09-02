#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    internal class VirtualAssetBundleProvider : ResourceProviderBase
    {
        VirtualAssetBundleManager m_assetBundleManager;
        string m_providerId;
        public VirtualAssetBundleProvider(VirtualAssetBundleManager abm, string provId)
        {
            m_assetBundleManager = abm;
            m_providerId = provId;
        }

        public override string ProviderId{ get { return m_providerId; } }
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            public VirtualAssetBundleManager assetBundleManager;

            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                Result = null;
                loadDependencyOperation.Completed += (obj) => {
                    assetBundleManager.LoadAsync(location).Completed += (IAsyncOperation<VirtualAssetBundle> operation) => {
                        SetResult(operation.Result as TObject);
                        OnComplete();
                    };
                };

                return base.Start(location);
            }

            public override TObject ConvertResult(AsyncOperation op) { return null; }
        }

        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (loadDependencyOperation == null)
                throw new System.ArgumentNullException("loadDependencyOperation");
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>();
            operation.assetBundleManager = m_assetBundleManager;
            return operation.Start(location, loadDependencyOperation);
        }

        public override bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (asset == null)
                throw new System.ArgumentNullException("asset");
            return m_assetBundleManager.Unload(location);
        }
    }
}
#endif
