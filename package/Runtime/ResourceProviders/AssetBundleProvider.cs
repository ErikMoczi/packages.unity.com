using System.Collections.Generic;
using UnityEngine.Networking;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Contains cache information to be used by the AssetBundleProvider
    /// </summary>
    [System.Serializable]
    public class AssetBundleRequestOptions
    {
        [SerializeField]
        string m_hash = "";
        /// <summary>
        /// Hash value of the asset bundle.
        /// </summary>
        public string Hash { get { return m_hash; } set { m_hash = value; } }
        [SerializeField]
        uint m_crc = 0;
        /// <summary>
        /// CRC value of the bundle.
        /// </summary>
        public uint Crc { get { return m_crc; } set { m_crc = value; } }
        [SerializeField]
        int m_timeout = 0;
        /// <summary>
        /// Sets UnityWebRequest to attempt to abort after the number of seconds in timeout have passed.
        /// </summary>
        public int Timeout { get { return m_timeout; } set { m_timeout = value; } }
        [SerializeField]
        bool m_chunkedTransfer = false;
        /// <summary>
        /// Indicates whether the UnityWebRequest system should employ the HTTP/1.1 chunked-transfer encoding method.
        /// </summary>
        public bool ChunkedTransfer { get { return m_chunkedTransfer; } set { m_chunkedTransfer = value; } }
        [SerializeField]
        int m_redirectLimit = -1;
        /// <summary>
        /// Indicates the number of redirects which this UnityWebRequest will follow before halting with a “Redirect Limit Exceeded” system error.
        /// </summary>
        public int RedirectLimit { get { return m_redirectLimit; } set { m_redirectLimit = value; } }
        [SerializeField]
        int m_retryCount = 0;
        /// <summary>
        /// Indicates the number of times the request will be retried.  
        /// </summary>
        public int RetryCount { get { return m_retryCount; } set { m_retryCount = value; } }
    }

    /// <summary>
    /// IResourceProvider for asset bundles.  Loads bundles via UnityWebRequestAssetBundle API if the internalId contains "://".  If not, it will load the bundle via AssetBundle.LoadFromFileAsync.
    /// </summary>
    public class AssetBundleProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            System.Action<IAsyncOperation<IList<object>>> m_dependencyLoadAction;
            IAsyncOperation<IList<object>> m_dependencyOperation;
            AsyncOperation m_requestOperation;
            int m_retries = 0;
            int m_maxRetries = 0;
            public InternalOp()
            {
                m_dependencyLoadAction = (op) =>
                {
                    if (op == null || op.Status == AsyncOperationStatus.Succeeded)
                    {
                        var loc = Context as IResourceLocation;
                        if (loc.InternalId.Contains("://"))
                            m_requestOperation = CreateWebRequest(loc).SendWebRequest();
                        else
                            m_requestOperation = AssetBundle.LoadFromFileAsync(loc.InternalId);

                        if (m_requestOperation.isDone)
                            DelayedActionManager.AddAction((System.Action<AsyncOperation>)OnComplete, 0, m_requestOperation);
                        else
                            m_requestOperation.completed += OnComplete;
                    }
                    else
                    {
                        OperationException = op.OperationException;
                        SetResult(default(TObject));
                        OnComplete();
                    }
                };
            }

            protected override void OnComplete(AsyncOperation op)
            {
                Validate();
                TObject res = default(TObject);
                try
                {
                    var localReq = op as AssetBundleCreateRequest;
                    if (localReq != null)
                    {
                        res = localReq.assetBundle as TObject;
                    }
                    else
                    {
                        var remoteReq = op as UnityWebRequestAsyncOperation;
                        if (remoteReq != null)
                        {
                            var webReq = remoteReq.webRequest;
                            if (string.IsNullOrEmpty(webReq.error))
                            {
                                res = (webReq.downloadHandler as DownloadHandlerAssetBundle).assetBundle as TObject;
                            }
                            else
                            {
                                if (m_retries < m_maxRetries)
                                {
                                    m_retries++;
                                    Debug.Log("Web Request failed, retrying...");
                                    m_dependencyLoadAction(m_dependencyOperation);
                                    return;
                                }
                                else
                                {
                                    OperationException = new System.Exception(string.Format("RemoteAssetBundleProvider unable to load from url {0}, result='{1}'.", webReq.url, webReq.error));
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    OperationException = ex;
                }
                SetResult(res);
                OnComplete();
            }

            UnityWebRequest CreateWebRequest(IResourceLocation loc)
            {
                var options = loc.Data as AssetBundleRequestOptions;
                if (options == null)
                    return UnityWebRequestAssetBundle.GetAssetBundle(loc.InternalId);

                var webRequest = (!string.IsNullOrEmpty(options.Hash) && options.Crc != 0) ? 
                    UnityWebRequestAssetBundle.GetAssetBundle(loc.InternalId, Hash128.Parse(options.Hash), options.Crc) :
                    UnityWebRequestAssetBundle.GetAssetBundle(loc.InternalId);

                if(options.Timeout > 0)
                    webRequest.timeout = options.Timeout;
                if (options.RedirectLimit > 0)
                    webRequest.redirectLimit = options.RedirectLimit;
                webRequest.chunkedTransfer = options.ChunkedTransfer;
                m_maxRetries = options.RetryCount;
                return webRequest;
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
                    m_dependencyLoadAction(null);
                else
                    loadDependencyOperation.Completed += m_dependencyLoadAction;

                return base.Start(location);
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
