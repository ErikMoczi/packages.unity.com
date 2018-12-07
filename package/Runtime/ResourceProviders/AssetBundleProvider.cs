using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Contains cache information to be used by the AssetBundleProvider
    /// </summary>
    [Serializable]
    public class AssetBundleRequestOptions
    {
        [FormerlySerializedAs("m_hash")]
        [SerializeField]
        string m_Hash = "";
        /// <summary>
        /// Hash value of the asset bundle.
        /// </summary>
        public string Hash { get { return m_Hash; } set { m_Hash = value; } }
        [FormerlySerializedAs("m_crc")]
        [SerializeField]
        uint m_Crc;
        /// <summary>
        /// CRC value of the bundle.
        /// </summary>
        public uint Crc { get { return m_Crc; } set { m_Crc = value; } }
        [FormerlySerializedAs("m_timeout")]
        [SerializeField]
        int m_Timeout;
        /// <summary>
        /// Sets UnityWebRequest to attempt to abort after the number of seconds in timeout have passed.
        /// </summary>
        public int Timeout { get { return m_Timeout; } set { m_Timeout = value; } }
        [FormerlySerializedAs("m_chunkedTransfer")]
        [SerializeField]
        bool m_ChunkedTransfer;
        /// <summary>
        /// Indicates whether the UnityWebRequest system should employ the HTTP/1.1 chunked-transfer encoding method.
        /// </summary>
        public bool ChunkedTransfer { get { return m_ChunkedTransfer; } set { m_ChunkedTransfer = value; } }
        [FormerlySerializedAs("m_redirectLimit")]
        [SerializeField]
        int m_RedirectLimit = -1;
        /// <summary>
        /// Indicates the number of redirects which this UnityWebRequest will follow before halting with a “Redirect Limit Exceeded” system error.
        /// </summary>
        public int RedirectLimit { get { return m_RedirectLimit; } set { m_RedirectLimit = value; } }
        [FormerlySerializedAs("m_retryCount")]
        [SerializeField]
        int m_RetryCount;
        /// <summary>
        /// Indicates the number of times the request will be retried.  
        /// </summary>
        public int RetryCount { get { return m_RetryCount; } set { m_RetryCount = value; } }
    }

    /// <summary>
    /// IResourceProvider for asset bundles.  Loads bundles via UnityWebRequestAssetBundle API if the internalId contains "://".  If not, it will load the bundle via AssetBundle.LoadFromFileAsync.
    /// </summary>
    public class AssetBundleProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            Action<IAsyncOperation<IList<object>>> m_DependencyLoadAction;
            IAsyncOperation<IList<object>> m_DependencyOperation;
            AsyncOperation m_RequestOperation;
            int m_Retries;
            int m_MaxRetries;
            public InternalOp()
            {
                m_DependencyLoadAction = op =>
                {
                    if ((op == null || op.Status == AsyncOperationStatus.Succeeded) && Context is IResourceLocation)
                    {
                        var loc = Context as IResourceLocation;
                        var path = loc.InternalId;
                        if (File.Exists(path))
                        {
                            var options = loc.Data as AssetBundleRequestOptions;
                            m_RequestOperation = AssetBundle.LoadFromFileAsync(path, options == null ? 0 : options.Crc);
                        }
                        else if (path.Contains("://"))
                        {
                            m_RequestOperation = CreateWebRequest(loc).SendWebRequest();
                        }
                        else
                        {
                            m_RequestOperation = null;
                            OperationException = new Exception(string.Format("Invalid path in AssetBundleProvider: '{0}'.", path));
                            SetResult(default(TObject));
                            OnComplete();
                        }
                        if (m_RequestOperation != null)
                        {
                            if (m_RequestOperation.isDone)
                                DelayedActionManager.AddAction((Action<AsyncOperation>)OnComplete, 0, m_RequestOperation);
                            else
                                m_RequestOperation.completed += OnComplete;
                        }
                    }
                    else
                    {
                        m_Error = op == null ? null : op.OperationException;
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
                                if (typeof(TObject) == typeof(AssetBundle) && webReq.downloadHandler is DownloadHandlerAssetBundle)
                                    res = (webReq.downloadHandler as DownloadHandlerAssetBundle).assetBundle as TObject;
                                else
                                    res = webReq.downloadHandler as TObject;
                            }
                            else
                            {
                                if (m_Retries < m_MaxRetries)
                                {
                                    m_Retries++;
                                    Debug.LogFormat("Web request failed, retrying ({0}/{1})...", m_Retries, m_MaxRetries);
                                    m_DependencyLoadAction(m_DependencyOperation);
                                    return;
                                }

                                OperationException = new Exception(string.Format("RemoteAssetBundleProvider unable to load from url {0}, result='{1}'.", webReq.url, webReq.error));
                            }
                        }
                    }
                }
                catch (Exception ex)
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

                var webRequest = !string.IsNullOrEmpty(options.Hash) ? 
                    UnityWebRequestAssetBundle.GetAssetBundle(loc.InternalId, Hash128.Parse(options.Hash), options.Crc) : 
                    UnityWebRequestAssetBundle.GetAssetBundle(loc.InternalId, options.Crc);

                if(options.Timeout > 0)
                    webRequest.timeout = options.Timeout;
                if (options.RedirectLimit > 0)
                    webRequest.redirectLimit = options.RedirectLimit;
                webRequest.chunkedTransfer = options.ChunkedTransfer;
                m_MaxRetries = options.RetryCount;
                return webRequest;
            }


            public override float PercentComplete
            {
                get
                {
                    if (IsDone)
                        return 1;

                    float reqPer = m_RequestOperation == null ? 0 : m_RequestOperation.progress;
                    if (m_DependencyOperation == null)
                        return reqPer;

                    return reqPer * .25f + m_DependencyOperation.PercentComplete * .75f;
                }
            }

            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                Context = location;
                m_Result = null;
                m_RequestOperation = null;
                m_DependencyOperation = loadDependencyOperation;
                if (loadDependencyOperation == null)
                    m_DependencyLoadAction(null);
                else
                    loadDependencyOperation.Completed += m_DependencyLoadAction;

                return base.Start(location);
            }
        }
        /// <inheritdoc/>
        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new ArgumentNullException("location");
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
                throw new ArgumentNullException("location");
            if (asset == null)
            {
                Debug.LogWarningFormat("Releasing null asset bundle from location {0}.  This is an indication that the bundle failed to load.", location);
                return false;
            }
            var bundle = asset as AssetBundle;
            if (bundle != null)
            {
                bundle.Unload(true);
                return true;
            }
            var dhHandler = asset as DownloadHandlerAssetBundle;
            if (dhHandler != null)
            {
                dhHandler.Dispose();
                return true;
            }
            return false;
        }
    }
}
