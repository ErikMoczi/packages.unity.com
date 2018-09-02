using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public class LegacyResourcesProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                Result = null;
                loadDependencyOperation.Completed += (obj) =>
                {
                    var reqOp = Resources.LoadAsync<Object>(location.InternalId);
                    if (reqOp.isDone)
                        DelayedActionManager.AddAction((System.Action<AsyncOperation>)OnComplete, 0, reqOp);
                    else
                        reqOp.completed += OnComplete;
                };
                return base.Start(location);
            }

            public override TObject ConvertResult(AsyncOperation op)
            {
                return (op as ResourceRequest).asset as TObject;
            }
        }

        public override IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (loadDependencyOperation == null)
                throw new System.ArgumentNullException("loadDependencyOperation");
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return operation.Start(location, loadDependencyOperation);
        }

        public override bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");

            var obj = asset as Object;
            if (obj != null)
            {
                Resources.UnloadAsset(obj);
                return true;
            }

            return false;
        }
    }
}
