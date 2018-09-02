using UnityEngine.ResourceManagement.Diagnostics;
using System;

namespace UnityEngine.ResourceManagement
{
    internal abstract class InternalProviderOperation<TObject> : AsyncOperationBase<TObject>
        where TObject : class
    {
        int startFrame;

        public virtual InternalProviderOperation<TObject> Start(IResourceLocation location)
        {
            Validate();
            if (location == null)
                throw new ArgumentNullException("location");
            startFrame = Time.frameCount;
            Context = location;
            return this;
        }

        protected virtual void OnComplete(IAsyncOperation<TObject> op)
        {
            Validate();
            SetResult(op.Result);
            OnComplete();
        }

        protected virtual void OnComplete(AsyncOperation op)
        {
            Validate();
            SetResult(ConvertResult(op));
            OnComplete();
        }

        protected virtual void OnComplete()
        {
            Validate();
            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncCompletion, Context, Time.frameCount - startFrame);
            InvokeCompletionEvent();
        }

        public virtual TObject ConvertResult(AsyncOperation op)
        {
            return default(TObject);
        }
    }
}
