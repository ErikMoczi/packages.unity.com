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
        event Action<IAsyncOperation<T>> m_completed;
        protected object m_context;

        public AsyncOperationBase() { }
        public event Action<IAsyncOperation<T>> completed
        {
            add
            {
                if (isDone)
                    value(this);
                else
                    m_completed += value;
            }

            remove
            {
                m_completed -= value;
            }
        }

        public bool MoveNext() { return !isDone; }
        public void Reset() {}
        public object Current { get { return result; } }
        object IAsyncOperation.result { get { return result; } }
        public virtual T result { get { return m_result; } }
        public virtual bool isDone { get { return !(EqualityComparer<T>.Default.Equals(result, default(T))); } }
        public virtual float percentComplete { get { return isDone ? 1f : 0f; } }
        public object context { get { return m_context; } }
        public void InvokeCompletionEvent()
        {
            SetResult(result);
            if (m_completed != null)
            {
                var tmpEvent = m_completed;
                m_completed = null;
                tmpEvent(this);
            }
        }

        protected virtual void SetResult(T result)
        {
            m_result = result;
        }
    }
}
