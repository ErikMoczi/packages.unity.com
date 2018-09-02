using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using ResourceManagement.AsyncOperations;
using ResourceManagement.Util;
using System.IO;

namespace ResourceManagement.ResourceProviders
{
    public class RemoteAssetBundleProvider : ResourceProviderBase
    {
        public class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            public override InternalProviderOperation<TObject> Start(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                loadDependencyOperation.completed += (obj) => UnityWebRequest.GetAssetBundle(Path.Combine("file://", Config.ExpandPathWithGlobalVars(loc.id))).SendWebRequest().completed += OnComplete;
                return base.Start(loc, loadDependencyOperation);
            }

            public override TObject ConvertResult(AsyncOperation op)
            {
                return ((op as UnityWebRequestAsyncOperation).webRequest.downloadHandler as DownloadHandlerAssetBundle).assetBundle as TObject;
            }
        }

        public override IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            var r = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return r.Start(loc, loadDependencyOperation);
        }

        public override bool Release(IResourceLocation loc, object asset)
        {
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
