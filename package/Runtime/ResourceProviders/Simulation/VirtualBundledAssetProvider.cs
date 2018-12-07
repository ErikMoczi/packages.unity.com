#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Provides assets from virtual asset bundles.  Actual loads are done through the AssetDatabase API.
    /// </summary>
    public class VirtualBundledAssetProvider : ResourceProviderBase
    {
        /// <summary>
        /// Default copnstructor.
        /// </summary>
        public VirtualBundledAssetProvider()
        {
            m_ProviderId = typeof(BundledAssetProvider).FullName; 
        }

        class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            Action<IAsyncOperation<TObject>> m_OnCompleteAction;
            IAsyncOperation m_DependencyOperation;
            IAsyncOperation<TObject> m_RequestOperation;

            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                m_Result = null;
                if (m_OnCompleteAction == null)
                    m_OnCompleteAction = OnComplete;
                m_RequestOperation = null;
                m_DependencyOperation = loadDependencyOperation;
                loadDependencyOperation.Completed += obj =>
                {
                    VirtualAssetBundle bundle;
                    if (obj.Result != null && obj.Result.Count > 0 && (bundle = obj.Result[0] as VirtualAssetBundle) != null)
                    {
                        (m_RequestOperation = bundle.LoadAssetAsync<TObject>(location)).Completed += m_OnCompleteAction;
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

                    float reqPer = m_RequestOperation == null ? 0 : m_RequestOperation.PercentComplete;
                    if (m_DependencyOperation == null)
                        return reqPer;
                    return reqPer * .25f + m_DependencyOperation.PercentComplete * .75f;
                }
            }
            internal override TObject ConvertResult(AsyncOperation operation) { return null; }
        }

        /// <inheritdoc/>
        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new ArgumentNullException("location");
            if (loadDependencyOperation == null)
                return new CompletedOperation<TObject>().Start(location, location, default(TObject), new ArgumentNullException("IAsyncOperation<IList<object>> loadDependencyOperation"));
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>();
            return operation.Start(location, loadDependencyOperation);
        }

        /// <inheritdoc/>
        public override bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new ArgumentNullException("location");
            return true;

        }
    }
}
#endif
