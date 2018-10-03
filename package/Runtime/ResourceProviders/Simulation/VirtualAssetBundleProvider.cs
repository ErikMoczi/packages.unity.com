#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Simulates the loading behavior of an asset bundle.  Internally it uses the AssetDatabase API.  This provider will only work in the editor.
    /// </summary>
    public class VirtualAssetBundleProvider : ResourceProviderBase
    {
        VirtualAssetBundleManager m_manager;
        /// <summary>
        /// Construct a new VirtualAssetBundleProvider object;
        /// </summary>
        public VirtualAssetBundleProvider()
        {
            m_providerId = typeof(AssetBundleProvider).FullName;
        }

        /// <summary>
        /// Construct a new VirtualAssetBundleProvider object with a specific manager and id
        /// </summary>
        /// <param name="mgr">The VirtualAssetBundleManager to use.</param>
        /// <param name="providerId">The provider id.</param>
        public VirtualAssetBundleProvider(VirtualAssetBundleManager mgr, string providerId)
        {
            m_providerId = providerId;
            m_manager = mgr;
        }

        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            System.Action<IAsyncOperation<IList<object>>> action;
            IAsyncOperation m_dependencyOperation;
            IAsyncOperation<VirtualAssetBundle> m_requestOperation;
            VirtualAssetBundleManager m_manager;
            public InternalOp()
            {
                action = (op) =>
                {
                    if (op == null || op.Status == AsyncOperationStatus.Succeeded)
                    {
                        (m_requestOperation = m_manager.LoadAsync(Context as IResourceLocation)).Completed += (bundleOp) =>
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

            public InternalProviderOperation<TObject> Start(VirtualAssetBundleManager mgr, IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                Context = location;
                m_manager = mgr;
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

        VirtualAssetBundleManager GetManager()
        {
            if (m_manager == null)
                m_manager = VirtualAssetBundleManager.CreateManager(ResourceManager.OnResolveInternalId);
            return m_manager;
        }

        /// <inheritdoc/>
        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>();
            return operation.Start(GetManager(), location, loadDependencyOperation);
        }

        /// <inheritdoc/>
        public override bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (asset == null)
                throw new System.ArgumentNullException("asset");
            return GetManager().Unload(location);
        }
    }
}
#endif
