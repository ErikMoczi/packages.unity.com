using System.Collections.Generic;
using System;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// base class for implemented AsyncOperations, implements the needed interfaces and consolidates redundant code
    /// </summary>
    public abstract class AsyncOperationBase<T> : IAsyncOperation<T>
    {
        protected T m_result;
        protected AsyncOperationStatus m_status;
        protected Exception m_error;
        protected object m_context;
        event Action<IAsyncOperation> m_completedAction;
        event Action<IAsyncOperation<T>> m_completedActionT;
        protected AsyncOperationBase() { }

        public event Action<IAsyncOperation<T>> completed
        {
            add
            {
                if (IsDone)
                {
                    try
                    {
                        if(value != null)
                            value(this);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        m_error = e;
                        m_status = AsyncOperationStatus.Failed;
                    }
                }
                else
                    m_completedActionT += value;
            }

            remove
            {
                m_completedActionT -= value;
            }
        }
		
		event Action<IAsyncOperation> IAsyncOperation.completed
		{
			add
			{
                if (IsDone)
                {
                    try
                    {
                        if (value != null)
                            value(this);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        m_error = e;
                        m_status = AsyncOperationStatus.Failed;
                    }
                }
                else
                    m_completedAction += value;
			}

			remove
			{
				m_completedAction -= value;
			}
		}

        object IAsyncOperation.Result { get { return m_result; } }
        public AsyncOperationStatus Status { get { return m_status; } }
        public Exception OperationException { get { return m_error; } }
        public bool MoveNext() { return !IsDone; }
        public void Reset() { }
        public object Current { get { return Result; } }
        public virtual T Result { get { return m_result; } set { m_result = value; } }
        public virtual bool IsDone { get { return !(EqualityComparer<T>.Default.Equals(Result, default(T))); } }
        public virtual float PercentComplete { get { return IsDone ? 1f : 0f; } }
        public object Context { get { return m_context; } }

        public void InvokeCompletionEvent()
        {
            if (m_completedActionT != null)
            {
                var tmpEvent = m_completedActionT;
                m_completedActionT = null;
                try
                {
                    tmpEvent(this);
                }
                catch (Exception e)
                {
					Debug.LogException(e);
					m_error = e;
                    m_status = AsyncOperationStatus.Failed;
                }
            }
			
            if (m_completedAction != null)
            {
                var tmpEvent = m_completedAction;
				m_completedAction = null;
                try
                {
                    tmpEvent(this);
                }
                catch (Exception e)
                {
					Debug.LogException(e);
                    m_error = e;
                    m_status = AsyncOperationStatus.Failed;
                }
            }
        }

        public virtual void SetResult(T result)
        {
            m_result = result;
            m_status = (m_result == null) ? AsyncOperationStatus.Failed : AsyncOperationStatus.Succeeded;
        }

        public virtual void ResetStatus()
        {
            m_status = AsyncOperationStatus.None;
            m_error = null;
        }

    }
}
