using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.Diagnostics;

namespace UnityEngine.ResourceManagement
{
    public class InstanceProvider : IInstanceProvider
    {
        internal class InternalOp<TObject> : AsyncOperationBase<TObject>
            where TObject : Object
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
                Validate();
                prefabResult = null;
                Result = null;
                Context = location;
                m_instParams = instantiateParameters;
                m_startFrame = Time.frameCount;
                loadOperation.Completed += m_completeAction;
                return this;
            }

            void OnComplete(IAsyncOperation<TObject> operation)
            {
                Validate();
                Debug.Assert(operation != null);
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.InstantiateAsyncCompletion, Context, Time.frameCount - m_startFrame);
                prefabResult = operation.Result;
                if (prefabResult == null)
                {
                    Debug.LogWarningFormat("Unable to load asset to instantiate from location {0}", Context);
                }
                else if (Result == null)
                {
                    Result = m_instParams.Instantiate(prefabResult);
                }
                InvokeCompletionEvent();
            }
        }

        public bool CanProvideInstance<TObject>(IResourceProvider loadProvider, IResourceLocation location)
            where TObject : Object
        {
            if (loadProvider == null)
                return false;
            return loadProvider.CanProvide<TObject>(location) && ResourceManagerConfig.IsInstance<TObject, GameObject>();
        }

        public IAsyncOperation<TObject> ProvideInstanceAsync<TObject>(IResourceProvider loadProvider, IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation, InstantiationParameters instantiateParameters)
            where TObject : Object
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (loadDependencyOperation == null)
                throw new System.ArgumentNullException("loadDependencyOperation");
            if (loadProvider == null)
                throw new ArgumentNullException("loadProvider");

            var depOp = loadProvider.Provide<TObject>(location, loadDependencyOperation);

            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>();
            return operation.Start(depOp, location, instantiateParameters);
        }

        public bool ReleaseInstance(IResourceProvider loadProvider, IResourceLocation location, Object instance)
        {
            if (loadProvider == null)
                throw new ArgumentException("IResourceProvider loadProvider cannot be null.");
            Object.Destroy(instance);
            return true;
        }
    }
}
