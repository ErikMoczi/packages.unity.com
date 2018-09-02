using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using ResourceManagement.AsyncOperations;
using ResourceManagement.Diagnostics;

namespace ResourceManagement.ResourceProviders
{
    public class InstanceProvider : IInstanceProvider
    {
        public class InternalOp<TObject> : AsyncOperationBase<TObject>
            where TObject : class
        {
            IResourceLocation m_location;
            TObject prefabResult;
            int m_startFrame;
            public InternalOp() : base("") {}

            public InternalOp<TObject> Start(IAsyncOperation<TObject> loadOp, IResourceLocation loc, TObject val = null)
            {
                prefabResult = null;
                m_result = val;
                m_location = loc;
                m_startFrame = Time.frameCount;
                loadOp.completed += OnComplete;
                return this;
            }

            void OnComplete(IAsyncOperation<TObject> op)
            {
                ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.InstantiateAsyncCompletion, m_location, Time.frameCount - m_startFrame);
                prefabResult = op.result;
                if (prefabResult == null)
                {
                    Debug.Log("NULL prefab on instantiate: " + m_location);
                    InvokeCompletionEvent(this);
                    AsyncOperationCache.Instance.Release<TObject>(this);
                }
                else
                {
                    if (m_result == null)
                        m_result = Object.Instantiate(prefabResult as GameObject) as TObject;
                    InvokeCompletionEvent(this);
                    AsyncOperationCache.Instance.Release<TObject>(this);
                }
            }
        }

        public bool CanProvideInstance<TObject>(IResourceProvider loadProvider, IResourceLocation loc)
            where TObject : Object
        {
            return loadProvider.CanProvide<TObject>(loc) && typeof(TObject).IsAssignableFrom(typeof(GameObject));
        }

        public IAsyncOperation<TObject> ProvideInstanceAsync<TObject>(IResourceProvider loadProvider, IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
            where TObject : Object
        {
            var depOp = loadProvider.ProvideAsync<TObject>(loc, loadDependencyOperation);

            var r = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>();
            return r.Start(depOp, loc);
        }

        public bool ReleaseInstance(IResourceProvider loadProvider, IResourceLocation loc, UnityEngine.Object asset)
        {
            Object.Destroy(asset);
            return loadProvider.Release(loc, null);
        }
    }
}
