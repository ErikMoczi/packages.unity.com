using System.Collections.Generic;
using UnityEngine;
using ResourceManagement.AsyncOperations;
using UnityEngine.Networking;

namespace ResourceManagement.ResourceProviders
{
    public class JSONAssetProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject> where TObject : class
        {
            System.Action<IAsyncOperation<IList<object>>> action;
            public InternalOp()
            {
                action = (op) => 
                {
                    var m_webRequest = new UnityWebRequest((m_context as IResourceLocation).id, UnityWebRequest.kHttpVerbGET, new DownloadHandlerBuffer(), null);
                    m_webRequest.SendWebRequest().completed += OnComplete;
                };
            }
            public override InternalProviderOperation<TObject> Start(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                m_result = null;
                m_context = loc;
                loadDependencyOperation.completed += action;
                return base.Start(loc, loadDependencyOperation);
            }

            public override TObject ConvertResult(AsyncOperation op)
            {
                return JsonUtility.FromJson<TObject>((op as UnityWebRequestAsyncOperation).webRequest.downloadHandler.text);
            }
        }

        public override IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            var r = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return r.Start(loc, loadDependencyOperation);
        }

        /*
         * 			return CreateProvideAsyncOperation(loc, loadDependencyOperation,
                        (deps) => 
                        {
                            var m_webRequest = new UnityWebRequest(loc.id);
                            m_webRequest.downloadHandler = new DownloadHandlerBuffer();
                            return m_webRequest.SendWebRequest();
                        },
                        (op) => JsonUtility.FromJson<TObject>((op as UnityWebRequestAsyncOperation).webRequest.downloadHandler.text));

         */
    }
}