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
            IAsyncOperation m_dependencyOperation;
            UnityWebRequestAsyncOperation m_requestOperation;
            public InternalOp()
            {
                action = (op) => 
                {
                    if (op == null || op.Status == AsyncOperationStatus.Succeeded)
                    {
                        var path = (Context as IResourceLocation).InternalId;
                        //this is necessary because this provider can only load via UnityWebRequest
                        if (!path.Contains("://"))
                            path = "file://" + path;
                        m_requestOperation = new UnityWebRequest(path, UnityWebRequest.kHttpVerbGET, new DownloadHandlerBuffer(), null).SendWebRequest();
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

                    float reqPer = 0;
                    if (m_requestOperation != null)
                        reqPer = m_requestOperation.progress;

                    if (m_dependencyOperation == null)
                        return reqPer;

                    return reqPer * .25f + m_dependencyOperation.PercentComplete * .75f;
                }
            }
            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation, System.Func<DownloadHandler, TObject> convertFunc)
            {
                m_result = null;
                m_convertFunc = convertFunc;
                Context = location;
                m_dependencyOperation = loadDependencyOperation;
                if (loadDependencyOperation == null)
                    action(null);
                else
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

        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>();
            return operation.Start(location, loadDependencyOperation, Convert<TObject>);
        }
    }
}