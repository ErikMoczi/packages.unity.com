#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    internal class VirtualBundledAssetProvider : ResourceProviderBase
    {
        public int loadSpeed;
        public VirtualBundledAssetProvider(int speed) { loadSpeed = speed; }
        public override string ProviderId
        {
            get { return typeof(BundledAssetProvider).FullName; }
        }

        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            System.Action<IAsyncOperation<TObject>> onCompleteAction;
            public InternalProviderOperation<TObject> Start(IResourceLocation location, int speed, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                Result = null;
                if (onCompleteAction == null)
                    onCompleteAction = OnComplete;
                loadDependencyOperation.completed += (obj) => 
                {
                    VirtualAssetBundle bundle;
                    if (obj.Result != null && obj.Result.Count > 0 && (bundle = obj.Result[0] as VirtualAssetBundle) != null)
                        bundle.LoadAssetAsync<TObject>(location, speed).completed += onCompleteAction;
                    else
                        OnComplete();
                };

                return base.Start(location);
            }

            void OnComplete(IAsyncOperation<TObject> operation)
            {
                SetResult(operation.Result);
                OnComplete();
            }

            public override TObject ConvertResult(AsyncOperation operation) { return null; }
        }

        public override IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            var r = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return r.Start(location, loadSpeed, loadDependencyOperation);
        }

        public override bool Release(IResourceLocation location, object asset)
        {
            return true;

        }
    }
}
#endif
