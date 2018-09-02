using System;
using System.Collections.Generic;
using UnityEngine;
using ResourceManagement.Util;

namespace ResourceManagement.AsyncOperations
{
    public abstract class InternalProviderOperation<TObject> : AsyncOperationBase<TObject>
        where TObject : class
    {
        IResourceLocation m_location;
        int startFrame;

        protected InternalProviderOperation() : base("") {}

        public IResourceLocation ResourceLocation
        {
            get { return m_location; }
        }

        public virtual InternalProviderOperation<TObject> Start(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            startFrame = Time.frameCount;
            m_result = null;
            m_location = loc;
            m_id = loc.id;

            return this;
        }

        protected virtual void OnComplete(AsyncOperation op)
        {
            m_result = ConvertResult(op);
            OnComplete();
        }

        protected virtual void OnComplete()
        {
            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncCompletion, m_location, Time.frameCount - startFrame);
            InvokeCompletionEvent(this);
            AsyncOperationCache.Instance.Release<TObject>(this);
        }

        public abstract TObject ConvertResult(AsyncOperation op);
    }
}
