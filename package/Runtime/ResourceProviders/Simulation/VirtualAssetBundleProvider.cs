using ResourceManagement.AsyncOperations;
using ResourceManagement;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceManagement.ResourceProviders.Simulation
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

        public override string providerId{ get { return m_providerId; } }
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            public VirtualAssetBundleManager assetBundleManager;

            public override InternalProviderOperation<TObject> Start(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                m_result = null;
                loadDependencyOperation.completed += (obj) => {
                    assetBundleManager.LoadAsync(loc).completed += (IAsyncOperation<VirtualAssetBundle> op) => {
                        SetResult(op.result as TObject);
                        OnComplete();
                    };
                };

                return base.Start(loc, loadDependencyOperation);
            }

            public override TObject ConvertResult(AsyncOperation op) { return null; }
        }

        public override IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            var r = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            r.assetBundleManager = m_assetBundleManager;
            return r.Start(loc, loadDependencyOperation);
        }

        public override bool Release(IResourceLocation loc, object asset)
        {
            return m_assetBundleManager.Unload(loc);
        }
    }
}
