using System.Collections.Generic;
using UnityEngine.Networking;

namespace UnityEngine.ResourceManagement
{
    public abstract class RawDataProvider : ResourceProviderBase
    {
        internal class InternalOp<TObject> : InternalProviderOperation<TObject> where TObject : class
        {
            System.Action<IAsyncOperation<IList<object>>> action;
            System.Func<DownloadHandler, TObject> m_convertFunc;
            public InternalOp()
            {
                action = (op) => 
                {
                    var url = ResourceManagerConfig.ExpandPathWithGlobalVariables((Context as IResourceLocation).InternalId);
                    var reqOp = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET, new DownloadHandlerBuffer(), null).SendWebRequest();
                    if (reqOp.isDone)
                        DelayedActionManager.AddAction((System.Action<AsyncOperation>)OnComplete, 0, reqOp);
                    else
                        reqOp.completed += OnComplete;
                };
            }

            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation, System.Func<DownloadHandler, TObject> convertFunc)
            {
                Result = null;
                m_convertFunc = convertFunc;
                Context = location;
                loadDependencyOperation.Completed += action;
                return base.Start(location);
            }

            public override TObject ConvertResult(AsyncOperation op)
            {
                var webReq = (op as UnityWebRequestAsyncOperation).webRequest;
                if (string.IsNullOrEmpty(webReq.error))
                    return m_convertFunc(webReq.downloadHandler);
                m_error = new System.Exception(string.Format("RawDataProvider unable to load from url {0}, result='{1}'.", webReq.url, webReq.error));
                return default(TObject);
            }
        }

        public virtual TObject Convert<TObject>(DownloadHandler handler) where TObject : class
        {
            return default(TObject);
        }

        public override IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (loadDependencyOperation == null)
                throw new System.ArgumentNullException("loadDependencyOperation");
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return operation.Start(location, loadDependencyOperation, Convert<TObject>);
        }
    }
}