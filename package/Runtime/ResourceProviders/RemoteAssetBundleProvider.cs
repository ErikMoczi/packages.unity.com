using System.Collections.Generic;
using UnityEngine.Networking;

namespace UnityEngine.ResourceManagement
{
    public class RemoteAssetBundleProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            System.Action<IAsyncOperation<IList<object>>> action;
            public InternalOp()
            {
                action = (op) => 
                {
                    var bundleURL = ResourceManagerConfig.ExpandPathWithGlobalVariables((Context as IResourceLocation).InternalId);
                    var reqOp = UnityWebRequestAssetBundle.GetAssetBundle(bundleURL).SendWebRequest();
                    if (reqOp.isDone)
                        DelayedActionManager.AddAction((System.Action<AsyncOperation>)OnComplete, 0, reqOp);
                    else
                        reqOp.completed += OnComplete;
                };
            }

            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                Result = null;
                Context = location;
                loadDependencyOperation.Completed += action;
                return base.Start(location);
            }

            public override TObject ConvertResult(AsyncOperation operation)
            {
                var webReq = (operation as UnityWebRequestAsyncOperation).webRequest;
                if (string.IsNullOrEmpty(webReq.error))
                    return ((operation as UnityWebRequestAsyncOperation).webRequest.downloadHandler as DownloadHandlerAssetBundle).assetBundle as TObject;
                m_error = new System.Exception(string.Format("RemoteAssetBundleProvider unable to load from url {0}, result='{1}'.", webReq.url, webReq.error));
                return default(TObject);
            }
        }

        public override IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (loadDependencyOperation == null)
                throw new System.ArgumentNullException("loadDependencyOperation");

            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
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
