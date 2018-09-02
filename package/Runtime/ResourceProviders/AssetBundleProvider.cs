using System.Collections.Generic;
using UnityEngine.Networking;

namespace UnityEngine.ResourceManagement
{
    public class AssetBundleProvider : ResourceProviderBase
    {
        [System.Serializable]
        public class CacheInfo
        {
            public string m_hash;
            public uint m_crc;
        }

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
                            var cacheInfo = (Context as IResourceLocation).Data as CacheInfo;
                            if(cacheInfo != null && !string.IsNullOrEmpty(cacheInfo.m_hash) && cacheInfo.m_crc != 0)
                                m_requestOperation = UnityWebRequestAssetBundle.GetAssetBundle(path, Hash128.Parse(cacheInfo.m_hash), cacheInfo.m_crc).SendWebRequest();
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

            public override TObject ConvertResult(AsyncOperation op)
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

        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>();
            return operation.Start(location, loadDependencyOperation);
        }

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
