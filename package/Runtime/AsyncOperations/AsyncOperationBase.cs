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
        protected string m_id;
        protected T m_result;
        event Action<IAsyncOperation<T>> m_completed;

        public AsyncOperationBase(string id) { m_id = id; }
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
        public string id { get { return m_id; } }
        public virtual T result { get { return m_result; } }
        public virtual bool isDone { get { return !(EqualityComparer<T>.Default.Equals(result, default(T))); } }
        public virtual float percentComplete { get { return isDone ? 1f : 0f; } }

        public void InvokeCompletionEvent(IAsyncOperation<T> op)
        {
            SetResult(op.result);
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
