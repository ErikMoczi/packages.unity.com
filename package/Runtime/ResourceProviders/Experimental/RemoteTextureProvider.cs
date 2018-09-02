using System.Collections.Generic;
using UnityEngine.Networking;

namespace UnityEngine.ResourceManagement
{
    public class RemoteTextureProvider : ResourceProviderBase
    {
        public override bool CanProvide<TObject>(IResourceLocation location)
        {
            return base.CanProvide<TObject>(location) && ResourceManagerConfig.IsInstance<TObject, Texture2D>();
        }

        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                Result = null;
                if (loadDependencyOperation != null && location != null)
                {
                    loadDependencyOperation.Completed += (obj) =>
                    {
                        var reqOp = UnityWebRequestTexture.GetTexture(location.InternalId).SendWebRequest();
                        if (reqOp.isDone)
                            DelayedActionManager.AddAction((System.Action<AsyncOperation>)OnComplete, 0, reqOp);
                        else
                            reqOp.completed += OnComplete;
                    };
                }
                return base.Start(location);
            }

            public override TObject ConvertResult(AsyncOperation op)
            {
                return ((op as UnityWebRequestAsyncOperation).webRequest.downloadHandler as DownloadHandlerTexture).texture as TObject;
            }
        }

        public override IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return operation.Start(location, loadDependencyOperation);
        }

        public override bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (asset == null)
                throw new System.ArgumentNullException("asset");
            return true;
        }
    }
}
