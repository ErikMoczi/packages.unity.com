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

        public override string ProviderId { get { return m_providerId; } }
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            public VirtualAssetBundleManager assetBundleManager;
            System.Action<IAsyncOperation<IList<object>>> action;
            IAsyncOperation m_dependencyOperation;
            IAsyncOperation<VirtualAssetBundle> m_requestOperation;
            public InternalOp()
            {
                action = (op) =>
                {
                    if (op == null || op.Status == AsyncOperationStatus.Succeeded)
                    {
                        (m_requestOperation = assetBundleManager.LoadAsync(Context as IResourceLocation)).Completed += (bundleOp) =>
                        {
                            SetResult(bundleOp.Result as TObject);
                            OnComplete();
                        };
                    }
                    else
                    {
                        m_error = op.OperationException;
                        SetResult(default(TObject));
                        OnComplete();
                    }

                };
            }

            public override float PercentComplete
            {
                get
                {
                    if (IsDone)
                        return 1;

                    float reqPer = m_requestOperation == null ? 0 : m_requestOperation.PercentComplete;
                    if (m_dependencyOperation == null)
                        return reqPer;
                    return reqPer * .25f + m_dependencyOperation.PercentComplete * .75f;
                }
            }

            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                Context = location;
                m_result = null;
                m_requestOperation = null;
                m_dependencyOperation = loadDependencyOperation;
                if (loadDependencyOperation == null)
                    action(null);
                else
                    loadDependencyOperation.Completed += action;
                return base.Start(location);
            }

            internal override TObject ConvertResult(AsyncOperation op) { return null; }
        }

        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
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
