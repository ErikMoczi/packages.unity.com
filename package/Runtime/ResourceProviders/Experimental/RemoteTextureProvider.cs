using System.Collections.Generic;
using UnityEngine.Networking;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Provides a Texture2d object from a remote url using UnityWebRequestTexture.GetTexture.
    /// </summary>
    public class RemoteTextureProvider : ResourceProviderBase
    {
        /// <inheritdoc/>
        public override bool CanProvide<TObject>(IResourceLocation location)
        {
            return base.CanProvide<TObject>(location) && ResourceManagerConfig.IsInstance<TObject, Texture2D>();
        }

        internal class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            IAsyncOperation m_dependencyOperation;
            AsyncOperation m_requestOperation;
            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                m_result = null;
                m_dependencyOperation = loadDependencyOperation;
                m_requestOperation = null;
                if (loadDependencyOperation != null && location != null)
                {
                    loadDependencyOperation.Completed += (obj) =>
                    {
                        m_requestOperation = UnityWebRequestTexture.GetTexture(location.InternalId).SendWebRequest();
                        if (m_requestOperation.isDone)
                            DelayedActionManager.AddAction((System.Action<AsyncOperation>)OnComplete, 0, m_requestOperation);
                        else
                            m_requestOperation.completed += OnComplete;
                    };
                }
                return base.Start(location);
            }
            public override float PercentComplete
            {
                get
                {
                    if (IsDone)
                        return 1;

                    float reqPer = m_requestOperation == null ? 0 : m_requestOperation.progress;
                    if (m_dependencyOperation == null)
                        return reqPer;

                    return reqPer * .25f + m_dependencyOperation.PercentComplete * .75f;
                }
            }
            internal override TObject ConvertResult(AsyncOperation op)
            {
                return ((op as UnityWebRequestAsyncOperation).webRequest.downloadHandler as DownloadHandlerTexture).texture as TObject;
            }
        }

        /// <inheritdoc/>
        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>();
            return operation.Start(location, loadDependencyOperation);
        }

        /// <inheritdoc/>
        public override bool Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            return true;
        }
    }
}
