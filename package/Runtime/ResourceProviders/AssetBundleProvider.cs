using System.Collections.Generic;
using UnityEngine.Networking;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Contains cache information to be used by the AssetBundleProvider
    /// </summary>
    [System.Serializable]
    public class AssetBundleCacheInfo
    {
        [SerializeField]
        string m_hash;
        /// <summary>
        /// Hash value of the asset bundle.
        /// </summary>
        public string Hash { get { return m_hash; } set { m_hash = value; } }
        [SerializeField]
        uint m_crc;
        /// <summary>
        /// CRC value of the bundle.
        /// </summary>
        public uint Crc { get { return m_crc; } set { m_crc = value; } }
    }

    /// <summary>
    /// IResourceProvider for asset bundles.  Loads bundles via UnityWebRequestAssetBundle API if the internalId contains "://".  If not, it will load the bundle via AssetBundle.LoadFromFileAsync.
    /// </summary>
    public class AssetBundleProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            System.Action<IAsyncOperation<IList<object>>> action;
            IAsyncOperation m_dependencyOperation;
            AsyncOperation m_requestOperation;
            public InternalOp()
            {
                action = (op) =>
                {
                    if (op == null || op.Status == AsyncOperationStatus.Succeeded)
                    {
                        var path = (Context as IResourceLocation).InternalId;
                        if (path.Contains("://"))
                        {
                            var cacheInfo = (Context as IResourceLocation).Data as AssetBundleCacheInfo;
                            if (cacheInfo != null && !string.IsNullOrEmpty(cacheInfo.Hash) && cacheInfo.Crc != 0)
                                m_requestOperation = UnityWebRequestAssetBundle.GetAssetBundle(path, Hash128.Parse(cacheInfo.Hash), cacheInfo.Crc).SendWebRequest();
                            else
                                m_requestOperation = UnityWebRequestAssetBundle.GetAssetBundle(path).SendWebRequest();
                        }
                        else
                        {
                            m_requestOperation = AssetBundle.LoadFromFileAsync(path);
                        }

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

            internal override TObject ConvertResult(AsyncOperation op)
            {
                var localReq = op as AssetBundleCreateRequest;
                if (localReq != null)
                    return localReq.assetBundle as TObject;

                var remoteReq = op as UnityWebRequestAsyncOperation;
                if (remoteReq != null)
                {
                    var webReq = remoteReq.webRequest;
                    if (string.IsNullOrEmpty(webReq.error))
                        return (webReq.downloadHandler as DownloadHandlerAssetBundle).assetBundle as TObject;
                    m_error = new System.Exception(string.Format("RemoteAssetBundleProvider unable to load from url {0}, result='{1}'.", webReq.url, webReq.error));
                }
                return default(TObject);
            }
        }
        /// <inheritdoc/>
        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>();
            return operation.Start(location, loadDependencyOperation);
        }

        /// <summary>
        /// Releases the asset bundle via AssetBundle.Unload(true).
        /// </summary>
        /// <param name="location"></param>
        /// <param name="asset"></param>
        /// <returns></returns>
        public override bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (asset == null)
                throw new System.ArgumentNullException("asset");
            var bundle = asset as AssetBundle;
            if (bundle != null)
            {
                bundle.Unload(true);
                return true;
            }

            return false;
        }
    }
}
