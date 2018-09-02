using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using ResourceManagement.AsyncOperations;
using ResourceManagement.Util;

namespace ResourceManagement.ResourceProviders
{
    public class InstanceProvider : IInstanceProvider
    {
        internal class InternalOp<TObject> : AsyncOperationBase<TObject>
            where TObject : UnityEngine.Object
        {
            TObject prefabResult;
            int m_startFrame;
            Action<IAsyncOperation<TObject>> m_completeAction;
            InstantiationParams m_instParams;

            public InternalOp() 
            {
                m_completeAction = OnComplete;
            }

            public InternalOp<TObject> Start(IAsyncOperation<TObject> loadOp, IResourceLocation loc, InstantiationParams instParams)
            {
                prefabResult = null;
                m_result = null;
                m_context = loc;
                m_instParams = instParams;
                m_startFrame = Time.frameCount;
                loadOp.completed += m_completeAction;
                return this;
            }

            void OnComplete(IAsyncOperation<TObject> op)
            {
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.InstantiateAsyncCompletion, m_context, Time.frameCount - m_startFrame);
                prefabResult = op.result;
                if (prefabResult == null)
                {
                    Debug.Log("Unable to load asset to instantiate: " + m_context);
                }
                else if (m_result == null)
                {
                    m_result = m_instParams.Instantiate(prefabResult);
                }
                InvokeCompletionEvent();
                AsyncOperationCache.Instance.Release<TObject>(this);
            }
        }

        public bool CanProvideInstance<TObject>(IResourceProvider loadProvider, IResourceLocation loc)
            where TObject : Object
        {
            return loadProvider.CanProvide<TObject>(loc) && Config.IsInstance<TObject, GameObject>();
        }

        public IAsyncOperation<TObject> ProvideInstanceAsync<TObject>(IResourceProvider loadProvider, IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation, InstantiationParams instParams)
            where TObject : Object
        {
            var depOp = loadProvider.ProvideAsync<TObject>(loc, loadDependencyOperation);

            var r = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return r.Start(depOp, loc, instParams);
        }

        public bool ReleaseInstance(IResourceProvider loadProvider, IResourceLocation loc, UnityEngine.Object asset)
        {
            Object.Destroy(asset);
            return loadProvider.Release(loc, null);
        }
    }
}
