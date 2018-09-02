using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public class BundledAssetProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
           where TObject : class
        {
            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                Result = null;
                if (loadDependencyOperation != null)
                {
                    loadDependencyOperation.Completed += (obj) =>
                    {
                        AssetBundle bundle = obj.Result[0] as AssetBundle;
                        var reqOp = bundle.LoadAssetAsync<TObject>(location.InternalId);
                        if (reqOp.isDone)
                            DelayedActionManager.AddAction((System.Action<AsyncOperation>)OnComplete, 0, reqOp);
                        else
                            reqOp.completed += OnComplete;
                    };
                }
                return base.Start(location);
            }

            public override TObject ConvertResult(AsyncOperation op)
            {
                return (op as AssetBundleRequest).asset as TObject;
            }
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
            return true;
        }
    }
}
