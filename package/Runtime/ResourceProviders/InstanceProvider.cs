using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.Diagnostics;

namespace UnityEngine.ResourceManagement
{
    public class InstanceProvider : IInstanceProvider
    {
        internal class InternalOp<TObject> : AsyncOperationBase<TObject>
            where TObject : UnityEngine.Object
        {
            TObject prefabResult;
            int m_startFrame;
            Action<IAsyncOperation<TObject>> m_completeAction;
            InstantiationParameters m_instParams;

            public InternalOp() 
            {
                m_completeAction = OnComplete;
            }

            public InternalOp<TObject> Start(IAsyncOperation<TObject> loadOperation, IResourceLocation location, InstantiationParameters instantiateParameters)
            {
                prefabResult = null;
                Result = null;
                m_context = location;
                m_instParams = instantiateParameters;
                m_startFrame = Time.frameCount;
                loadOperation.completed += m_completeAction;
                return this;
            }

            void OnComplete(IAsyncOperation<TObject> operation)
            {
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.InstantiateAsyncCompletion, m_context, Time.frameCount - m_startFrame);
                prefabResult = operation.Result;
                if (prefabResult == null)
                {
                    Debug.Log("Unable to load asset to instantiate: " + m_context);
                }
                else if (Result == null)
                {
                    Result = m_instParams.Instantiate(prefabResult);
                }
                InvokeCompletionEvent();
                AsyncOperationCache.Instance.Release<TObject>(this);
            }
        }

        public bool CanProvideInstance<TObject>(IResourceProvider loadProvider, IResourceLocation location)
            where TObject : Object
        {
            return loadProvider.CanProvide<TObject>(location) && ResourceManagerConfig.IsInstance<TObject, GameObject>();
        }

        public IAsyncOperation<TObject> ProvideInstanceAsync<TObject>(IResourceProvider loadProvider, IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation, InstantiationParameters instantiateParameters)
            where TObject : Object
        {
            var depOp = loadProvider.ProvideAsync<TObject>(location, loadDependencyOperation);

            var r = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return r.Start(depOp, location, instantiateParameters);
        }

        public bool ReleaseInstance(IResourceProvider loadProvider, IResourceLocation location, UnityEngine.Object asset)
        {
            Object.Destroy(asset);
            return loadProvider.Release(location, null);
        }
    }
}
