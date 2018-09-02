using UnityEngine.ResourceManagement.Diagnostics;
using System;

namespace UnityEngine.ResourceManagement
{
    internal abstract class InternalProviderOperation<TObject> : AsyncOperationBase<TObject>
        where TObject : class
    {
        int startFrame;

        internal virtual InternalProviderOperation<TObject> Start(IResourceLocation location)
        {
            Validate();
            if (location == null)
                m_error = new ArgumentNullException("location");
            startFrame = Time.frameCount;
            Context = location;
            return this;
        }

        protected virtual void OnComplete(IAsyncOperation<TObject> op)
        {
            Validate();
            if (op.Status != AsyncOperationStatus.Succeeded)
                m_error = op.OperationException;

            SetResult(op.Result);
            OnComplete();
        }

        protected virtual void OnComplete(AsyncOperation op)
        {
            Validate();
            TObject res = default(TObject);
            try
            {
                res = ConvertResult(op);
            }
            catch (Exception ex)
            {
                m_error = ex;
            }
            SetResult(res);
            OnComplete();
        }

        protected virtual void OnComplete()
        {
            Validate();
            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncCompletion, Context, Time.frameCount - startFrame);
            InvokeCompletionEvent();
        }

        internal virtual TObject ConvertResult(AsyncOperation op)
        {
            return default(TObject);
        }
    }
}
