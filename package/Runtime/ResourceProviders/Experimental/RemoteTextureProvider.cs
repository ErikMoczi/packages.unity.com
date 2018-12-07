using System;
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

        class InternalOp<TObject> : InternalProviderOperation<TObject>
            where TObject : class
        {
            IAsyncOperation m_DependencyOperation;
            AsyncOperation m_RequestOperation;
            public InternalProviderOperation<TObject> Start(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            {
                m_Result = null;
                m_DependencyOperation = loadDependencyOperation;
                m_RequestOperation = null;
                if (loadDependencyOperation != null && location != null)
                {
                    loadDependencyOperation.Completed += obj =>
                    {

                        m_RequestOperation = UnityWebRequestTexture.GetTexture(location.InternalId).SendWebRequest();
                        if (m_RequestOperation.isDone)
                            DelayedActionManager.AddAction((Action<AsyncOperation>)OnComplete, 0, m_RequestOperation);
                        else
                            m_RequestOperation.completed += OnComplete;
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

                    float reqPer = m_RequestOperation == null ? 0 : m_RequestOperation.progress;
                    if (m_DependencyOperation == null)
                        return reqPer;

                    return reqPer * .25f + m_DependencyOperation.PercentComplete * .75f;
                }
            }
            internal override TObject ConvertResult(AsyncOperation op)
            {
                if (op is UnityWebRequestAsyncOperation)
                {
                    var textureHandler = ((op as UnityWebRequestAsyncOperation).webRequest.downloadHandler as DownloadHandlerTexture);
                    if(textureHandler != null)
                        return textureHandler.texture as TObject;
                }

                return null;
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
                throw new ArgumentNullException("location");
            return true;
        }
    }
}
