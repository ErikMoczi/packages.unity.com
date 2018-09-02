using System.Collections.Generic;
using UnityEngine.Networking;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Provides raw text from a local or remote URL.
    /// </summary>
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

            internal override TObject ConvertResult(AsyncOperation op)
            {
                var webReq = (op as UnityWebRequestAsyncOperation).webRequest;
                if (string.IsNullOrEmpty(webReq.error))
                    return m_convertFunc(webReq.downloadHandler);
                m_error = new System.Exception(string.Format("RawDataProvider unable to load from url {0}, result='{1}'.", webReq.url, webReq.error));
                return default(TObject);
            }
        }

        /// <summary>
        /// Method to convert the text into the object type requested.  Usually the text contains a JSON formatted serialized object.
        /// </summary>
        /// <typeparam name="TObject">The object type to convert the text to.</typeparam>
        /// <param name="text">The text to be converted.</param>
        /// <returns>The converted object.</returns>
        public abstract TObject Convert<TObject>(DownloadHandler handler) where TObject : class;

        /// <summary>
        /// Provides raw text data from the location.
        /// </summary>
        /// <typeparam name="TObject">Object type.</typeparam>
        /// <param name="location">Location of the data to load.</param>
        /// <param name="loadDependencyOperation">Depency operation.</param>
        /// <returns>Operation to load the raw data.</returns>
        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>();
            return operation.Start(location, loadDependencyOperation, Convert<TObject>);
        }
    }
}