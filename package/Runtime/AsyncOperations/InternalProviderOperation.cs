using System;
using System.Collections.Generic;
using UnityEngine;
using ResourceManagement.Util;

namespace ResourceManagement.AsyncOperations
{
    public abstract class InternalProviderOperation<TObject> : AsyncOperationBase<TObject>
        where TObject : class
    {
        int startFrame;

        public virtual InternalProviderOperation<TObject> Start(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
        {
            startFrame = Time.frameCount;
            m_context = loc;
            return this;
        }

        protected virtual void OnComplete(AsyncOperation op)
        {
            SetResult(ConvertResult(op));
            OnComplete();
        }

        protected virtual void OnComplete()
        {
            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncCompletion, m_context, Time.frameCount - startFrame);
            InvokeCompletionEvent();
            AsyncOperationCache.Instance.Release<TObject>(this);
        }

        public abstract TObject ConvertResult(AsyncOperation op);
    }
}
