using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public class BundledAssetProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
           where TObject : class
        {
            IAsyncOperation m_dependencyOperation;
            AssetBundleRequest m_requestOperation;
            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                m_result = null;
                m_requestOperation = null;
                m_dependencyOperation = loadDependencyOperation;
                loadDependencyOperation.Completed += (op) =>
                {
                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        AssetBundle bundle = op.Result[0] as AssetBundle;
                        m_requestOperation = bundle.LoadAssetAsync<TObject>(location.InternalId);
                        if (m_requestOperation.isDone)
                            DelayedActionManager.AddAction((System.Action<AsyncOperation>)OnComplete, 0, m_requestOperation);
                        else
                            m_requestOperation.completed += OnComplete;
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

            public override float PercentComplete
            {
                get
                {
                    if (IsDone)
                        return 1;

                    float reqPer = m_requestOperation == null ? 0 : m_requestOperation.progress;
                    if (m_dependencyOperation == null)
                        return reqPer;
                    return reqPer * .25f + m_dependencyOperation.PercentComplete * .75f;
                }
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
                return new EmptyOperation<TObject>().Start(location, location, default(TObject), new System.ArgumentNullException("IAsyncOperation<IList<object>> loadDependencyOperation"));

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
