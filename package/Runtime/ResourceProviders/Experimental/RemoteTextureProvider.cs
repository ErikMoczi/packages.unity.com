using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using ResourceManagement.AsyncOperations;

namespace ResourceManagement.ResourceProviders.Experimental
{
    public class RemoteTextureProvider : ResourceProviderBase
    {
        public override bool CanProvide<TObject>(IResourceLocation loc)
        {
            return base.CanProvide<TObject>(loc) && typeof(Texture2D).IsAssignableFrom(typeof(TObject));
        }

        public class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            public override InternalProviderOperation<TObject> Start(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                loadDependencyOperation.completed += (obj) => UnityWebRequestTexture.GetTexture(loc.id).SendWebRequest().completed += OnComplete;
                return base.Start(loc, loadDependencyOperation);
            }

            public override TObject ConvertResult(AsyncOperation op)
            {
                return ((op as UnityWebRequestAsyncOperation).webRequest.downloadHandler as DownloadHandlerTexture).texture as TObject;
            }
        }

        public override IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            var r = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return r.Start(loc, loadDependencyOperation);
        }

        public override bool Release(IResourceLocation loc, object asset)
        {
            return true;
        }
    }
}
