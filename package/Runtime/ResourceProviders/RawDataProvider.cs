using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Provides raw text from a local or remote URL.
    /// </summary>
    public abstract class RawDataProvider : ResourceProviderBase
    {
        class InternalOp<TObject> : InternalProviderOperation<TObject> where TObject : class
        {
            Action<IAsyncOperation<IList<object>>> m_Action;
            Func<string, TObject> m_ConvertFunc;
            IAsyncOperation m_DependencyOperation;
            UnityWebRequestAsyncOperation m_RequestOperation;
            public InternalOp()
            {
                m_Action = op =>
                {
                    if ( (op == null || op.Status == AsyncOperationStatus.Succeeded) && Context is IResourceLocation)
                    {
                        var loc = Context as IResourceLocation;
                        var path = loc.InternalId;
                        if (File.Exists(path))
                        {
                            var text = File.ReadAllText(path);
                            SetResult(m_ConvertFunc(text));
                            DelayedActionManager.AddAction((Action)OnComplete);
                        }
                        else if (path.Contains("://"))
                        {
                            m_RequestOperation = new UnityWebRequest(path, UnityWebRequest.kHttpVerbGET, new DownloadHandlerBuffer(), null).SendWebRequest();
                            if (m_RequestOperation.isDone)
                                DelayedActionManager.AddAction((Action<AsyncOperation>)OnComplete, 0, m_RequestOperation);
                            else
                                m_RequestOperation.completed += OnComplete;
                        }
                        else
                        {
                            OperationException = new Exception(string.Format("Invalid path in RawDataProvider: '{0}'.", path));
                            SetResult(default(TObject));
                            OnComplete();
                        }
                    }
                    else
                    {
                        if(op != null)
                            m_Error = op.OperationException;
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
                    if (m_RequestOperation != null)
                        reqPer = m_RequestOperation.progress;

                    if (m_DependencyOperation == null)
                        return reqPer;

                    return reqPer * .25f + m_DependencyOperation.PercentComplete * .75f;
                }
            }
            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation, Func<string, TObject> convertFunc)
            {
                m_Result = null;
                m_ConvertFunc = convertFunc;
                Context = location;
                m_DependencyOperation = loadDependencyOperation;
                if (loadDependencyOperation == null)
                    m_Action(null);
                else
                    loadDependencyOperation.Completed += m_Action;
                return base.Start(location);
            }

            internal override TObject ConvertResult(AsyncOperation op)
            {
                var webOp = op as UnityWebRequestAsyncOperation;
                if (webOp != null)
                {
                    var webReq = webOp.webRequest;
                    if (string.IsNullOrEmpty(webReq.error))
                        return m_ConvertFunc(webReq.downloadHandler.text);
                    
                    OperationException = new Exception(string.Format("RawDataProvider unable to load from url {0}, result='{1}'.", webReq.url, webReq.error));
                }
                else
                {
                    OperationException = new Exception("RawDataProvider unable to load from unknown url.");
                }
                return default(TObject);

            }
        }

        /// <summary>
        /// Method to convert the text into the object type requested.  Usually the text contains a JSON formatted serialized object.
        /// </summary>
        /// <typeparam name="TObject">The object type to convert the text to.</typeparam>
        /// <param name="text">The text to be converted.</param>
        /// <returns>The converted object.</returns>
        public abstract TObject Convert<TObject>(string text) where TObject : class;

        /// <summary>
        /// If true, the data is loaded as text for the handler
        /// </summary>
        public virtual bool LoadAsText { get { return true; } }

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
                throw new ArgumentNullException("location");
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>();
            return operation.Start(location, loadDependencyOperation, Convert<TObject>);
        }
    }
}