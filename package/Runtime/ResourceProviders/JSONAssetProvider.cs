using System.Collections.Generic;
using UnityEngine.Networking;

namespace UnityEngine.ResourceManagement
{
    public class JsonAssetProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject> where TObject : class
        {
            System.Action<IAsyncOperation<IList<object>>> action;
            public InternalOp()
            {
                action = (op) => 
                {
                    var m_webRequest = new UnityWebRequest(ResourceManagerConfig.ExpandPathWithGlobalVariables((Context as IResourceLocation).InternalId), UnityWebRequest.kHttpVerbGET, new DownloadHandlerBuffer(), null);
                    m_webRequest.SendWebRequest().completed += OnComplete;
                };
            }
            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                Result = null;
                Context = location;
                loadDependencyOperation.Completed += action;
                return base.Start(location);
            }

            public override TObject ConvertResult(AsyncOperation op)
            {
                return JsonUtility.FromJson<TObject>((op as UnityWebRequestAsyncOperation).webRequest.downloadHandler.text);
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
    }
}