#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public class VirtualBundledAssetProvider : ResourceProviderBase
    {
        public VirtualBundledAssetProvider()
        {
            m_providerId = typeof(BundledAssetProvider).FullName; 
        }

        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            System.Action<IAsyncOperation<TObject>> onCompleteAction;
            IAsyncOperation m_dependencyOperation;
            IAsyncOperation<TObject> m_requestOperation;

            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                m_result = null;
                if (onCompleteAction == null)
                    onCompleteAction = OnComplete;
                m_requestOperation = null;
                m_dependencyOperation = loadDependencyOperation;
                loadDependencyOperation.Completed += (obj) =>
                {
                    VirtualAssetBundle bundle;
                    if (obj.Result != null && obj.Result.Count > 0 && (bundle = obj.Result[0] as VirtualAssetBundle) != null)
                    {
                        (m_requestOperation = bundle.LoadAssetAsync<TObject>(location)).Completed += onCompleteAction;
                    }
                    else
                        OnComplete();
                };

                return base.Start(location);
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
            internal override TObject ConvertResult(AsyncOperation operation) { return null; }
        }

        /// <inheritdoc/>
        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (loadDependencyOperation == null)
                return new CompletedOperation<TObject>().Start(location, location, default(TObject), new System.ArgumentNullException("IAsyncOperation<IList<object>> loadDependencyOperation"));
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>();
            return operation.Start(location, loadDependencyOperation);
        }

        /// <inheritdoc/>
        public override bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            return true;

        }
    }
}
#endif
