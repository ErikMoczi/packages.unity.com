using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResourceManagement.AsyncOperations
{
    /// <summary>
    /// base class for implemented AsyncOperations, implements the needed interfaces and consolidates redundant code
    /// </summary>
    public class AsyncOperationBase<T> : IAsyncOperation<T>
    {
        protected T m_result;
        event Action<IAsyncOperation> m_completedAction;
        event Action<IAsyncOperation<T>> m_completedActionT;
        public object m_context;
		object IAsyncOperation.result { get { return m_result; } }
        public AsyncOperationBase() { }
        public event Action<IAsyncOperation<T>> completed
        {
            add
            {
                if (isDone)
                {
                    try
                    {
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
                if (isDone)
                {
                    try
                    {
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

		protected AsyncOperationStatus m_status;
        protected Exception m_error;
        public AsyncOperationStatus status { get { return m_status; } }
        public Exception error { get { return m_error; } }
        public bool MoveNext() { return !isDone; }
        public void Reset() { }
        public object Current { get { return result; } }
        public virtual T result { get { return m_result; } }
        public virtual bool isDone { get { return !(EqualityComparer<T>.Default.Equals(result, default(T))); } }
        public virtual float percentComplete { get { return isDone ? 1f : 0f; } }
        public object context { get { return m_context; } }

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
