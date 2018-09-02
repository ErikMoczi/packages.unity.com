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
                m_result = null;
                loadDependencyOperation.Completed += (op) =>
                {
                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        AssetBundle bundle = op.Result[0] as AssetBundle;
                        var reqOp = bundle.LoadAssetAsync<TObject>(location.InternalId);
                        if (reqOp.isDone)
                            DelayedActionManager.AddAction((System.Action<AsyncOperation>)OnComplete, 0, reqOp);
                        else
                            reqOp.completed += OnComplete;
                    }
                    else
                    {
                        m_error = op.OperationException;
                        SetResult(default(TObject));
                        OnComplete();
                    }
                };
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
                return new EmptyOperation<TObject>().Start(location, default(TObject), new System.ArgumentNullException("IAsyncOperation<IList<object>> loadDependencyOperation"));

            return AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>().Start(location, loadDependencyOperation);
        }

        public override bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            return true;
        }
    }
}
