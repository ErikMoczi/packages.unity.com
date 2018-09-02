using System.Collections.Generic;
using UnityEngine.ResourceManagement.Diagnostics;

namespace UnityEngine.ResourceManagement
{
    internal abstract class InternalProviderOperation<TObject> : AsyncOperationBase<TObject>
        where TObject : class
    {
        int startFrame;

        public virtual InternalProviderOperation<TObject> Start(IResourceLocation location)
        {
            startFrame = Time.frameCount;
            m_context = location;
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
