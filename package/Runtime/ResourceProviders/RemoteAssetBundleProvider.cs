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
                action = (op) => { UnityWebRequestAssetBundle.GetAssetBundle(ResourceManagerConfig.ExpandPathWithGlobalVariables((Context as IResourceLocation).InternalId)).SendWebRequest().completed += OnComplete; };
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
                return ((operation as UnityWebRequestAsyncOperation).webRequest.downloadHandler as DownloadHandlerAssetBundle).assetBundle as TObject;
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
