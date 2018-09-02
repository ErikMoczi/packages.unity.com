using System.Collections.Generic;
using UnityEngine;
using ResourceManagement.AsyncOperations;

namespace ResourceManagement.ResourceProviders.Simulation
{
    internal class VirtualBundledAssetProvider : ResourceProviderBase
    {
        public int loadSpeed;
        public VirtualBundledAssetProvider(int speed) { loadSpeed = speed; }
        public override string providerId
        {
            get { return typeof(BundledAssetProvider).FullName; }
        }

        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            System.Action<IAsyncOperation<TObject>> onCompleteAction;
            public InternalProviderOperation<TObject> Start(IResourceLocation loc, int speed, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                m_result = null;
                if (onCompleteAction == null)
                    onCompleteAction = OnComplete;
                loadDependencyOperation.completed += (obj) => 
                {
                    VirtualAssetBundle bundle;
                    if (obj.result != null && obj.result.Count > 0 && (bundle = obj.result[0] as VirtualAssetBundle) != null)
                        bundle.LoadAssetAsync<TObject>(loc, speed).completed += onCompleteAction;
                    else
                        OnComplete();
                };

                return base.Start(loc, loadDependencyOperation);
            }

            void OnComplete(IAsyncOperation<TObject> op)
            {
                SetResult(op.result);
                OnComplete();
            }

            public override TObject ConvertResult(AsyncOperation op) { return null; }
        }

        public override IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            var r = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return r.Start(loc, loadSpeed, loadDependencyOperation);
        }

        public override bool Release(IResourceLocation loc, object asset)
        {
            return true;

        }
    }
}
