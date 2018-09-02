#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    internal class VirtualBundledAssetProvider : ResourceProviderBase
    {
        public override string ProviderId
        {
            get { return typeof(BundledAssetProvider).FullName; }
        }

        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            System.Action<IAsyncOperation<TObject>> onCompleteAction;
            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                Result = null;
                if (onCompleteAction == null)
                    onCompleteAction = OnComplete;
                loadDependencyOperation.Completed += (obj) => 
                {
                    VirtualAssetBundle bundle;
                    if (obj.Result != null && obj.Result.Count > 0 && (bundle = obj.Result[0] as VirtualAssetBundle) != null)
                        bundle.LoadAssetAsync<TObject>(location).Completed += onCompleteAction;
                    else
                        OnComplete();
                };

                return base.Start(location);
            }

            public override TObject ConvertResult(AsyncOperation operation) { return null; }
        }

        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (loadDependencyOperation == null)
                throw new System.ArgumentNullException("loadDependencyOperation");
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>();
            return operation.Start(location, loadDependencyOperation);
        }

        public override bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (asset == null)
                throw new System.ArgumentNullException("asset");
            return true;

        }
    }
}
#endif
