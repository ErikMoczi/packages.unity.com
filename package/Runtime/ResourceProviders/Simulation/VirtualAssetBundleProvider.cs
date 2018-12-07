#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Simulates the loading behavior of an asset bundle.  Internally it uses the AssetDatabase API.  This provider will only work in the editor.
    /// </summary>
    public class VirtualAssetBundleProvider : ResourceProviderBase
    {
        VirtualAssetBundleManager m_Manager;
        /// <summary>
        /// Construct a new VirtualAssetBundleProvider object;
        /// </summary>
        public VirtualAssetBundleProvider()
        {
            m_ProviderId = typeof(AssetBundleProvider).FullName;
        }

        /// <summary>
        /// Construct a new VirtualAssetBundleProvider object with a specific manager and id
        /// </summary>
        /// <param name="mgr">The VirtualAssetBundleManager to use.</param>
        /// <param name="providerId">The provider id.</param>
        public VirtualAssetBundleProvider(VirtualAssetBundleManager mgr, string providerId)
        {
            m_ProviderId = providerId;
            m_Manager = mgr;
        }

        class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            Action<IAsyncOperation<IList<object>>> m_Action;
            IAsyncOperation m_DependencyOperation;
            IAsyncOperation<VirtualAssetBundle> m_RequestOperation;
            VirtualAssetBundleManager m_Manager;
            public InternalOp()
            {
                m_Action = op =>
                {
                    if (op == null || op.Status == AsyncOperationStatus.Succeeded)
                    {
                        (m_RequestOperation = m_Manager.LoadAsync(Context as IResourceLocation)).Completed += bundleOp =>
                        {
                            SetResult(bundleOp.Result as TObject);
                            OnComplete();
                        };
                    }
                    else
                    {
                        m_Error = op.OperationException;
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

                    float reqPer = m_RequestOperation == null ? 0 : m_RequestOperation.PercentComplete;
                    if (m_DependencyOperation == null)
                        return reqPer;
                    return reqPer * .25f + m_DependencyOperation.PercentComplete * .75f;
                }
            }

            public InternalProviderOperation<TObject> Start(VirtualAssetBundleManager mgr, IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                Context = location;
                m_Manager = mgr;
                m_Result = null;
                m_RequestOperation = null;
                m_DependencyOperation = loadDependencyOperation;
                if (loadDependencyOperation == null)
                    m_Action(null);
                else
                    loadDependencyOperation.Completed += m_Action;
                return base.Start(location);
            }

            internal override TObject ConvertResult(AsyncOperation op) { return null; }
        }

        VirtualAssetBundleManager GetManager()
        {
            if (m_Manager == null)
                m_Manager = VirtualAssetBundleManager.CreateManager(ResourceManager.OnResolveInternalId);
            return m_Manager;
        }

        /// <inheritdoc/>
        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new ArgumentNullException("location");
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>();
            return operation.Start(GetManager(), location, loadDependencyOperation);
        }

        /// <inheritdoc/>
        public override bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new ArgumentNullException("location");
            if (asset == null)
            {
                Debug.LogWarningFormat("Releasing null asset bundle from location {0}.  This is an indication that the bundle failed to load.", location);
                return false;
            }
            return GetManager().Unload(location);
        }
    }
}
#endif
