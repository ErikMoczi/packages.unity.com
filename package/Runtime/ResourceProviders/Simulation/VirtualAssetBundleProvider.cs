using System;
using System.Collections.Generic;
using UnityEngine;
using ResourceManagement.AsyncOperations;
using ResourceManagement.ResourceProviders;

namespace ResourceManagement.ResourceProviders.Simulation
{
    internal class VirtualAssetBundleProvider : ResourceProviderBase
    {
        VirtualAssetBundleManager m_assetBundleManager;
        bool m_allowSynchronous;

        public VirtualAssetBundleProvider(VirtualAssetBundleManager abm, bool allowSynchronous = true)
        {
            m_assetBundleManager = abm;
            m_allowSynchronous = allowSynchronous;
        }

        public override string providerId
        {
            get { return m_allowSynchronous ? typeof(LocalAssetBundleProvider).FullName : typeof(RemoteAssetBundleProvider).FullName; }
        }

        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            public VirtualAssetBundleManager assetBundleManager;

            public override InternalProviderOperation<TObject> Start(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                loadDependencyOperation.completed += (obj) => {
                        assetBundleManager.LoadAsync(loc.id).completed += (IAsyncOperation<VirtualAssetBundle> op) => {
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
            m_assetBundleManager.Unload(loc.id);
            return true;
        }
    }
}
