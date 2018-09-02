using System.Collections.Generic;
using System;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// base class for implemented AsyncOperations, implements the needed interfaces and consolidates redundant code
    /// </summary>
    public abstract class AsyncOperationBase<TObject> : IAsyncOperation<TObject>
    {
        TObject m_result;
        AsyncOperationStatus m_status;
        Exception m_error;
        object m_context;
        bool m_releaseToCacheOnCompletion = false;
        event Action<IAsyncOperation> m_completedAction;
        event Action<IAsyncOperation<TObject>> m_completedActionT;
        protected AsyncOperationBase()
        {
            IsValid = true;
        }

        public bool IsValid { get; set; }

        public override string ToString()
        {
            var instId = "";
            var or = m_result as Object;
            if (or != null)
                instId = "(" + or.GetInstanceID().ToString() + ")";
            return base.ToString() +  " result = " + m_result + instId + ", status = " + m_status + ", Valid = " + IsValid + ", canRelease = " + m_releaseToCacheOnCompletion;
        }

        public virtual void Release()
        {
            Validate();
            m_releaseToCacheOnCompletion = true;
            if (IsDone)
                AsyncOperationCache.Instance.Release<TObject>(this);
        }

        public IAsyncOperation<TObject> Acquire()
        {
            Validate();
            m_releaseToCacheOnCompletion = false;
            return this;
        }

        public virtual void ResetStatus()
        {
            m_releaseToCacheOnCompletion = true;
            m_status = AsyncOperationStatus.None;
            m_error = null;
            m_result = default(TObject);
            m_context = null;
        }

        public bool Validate()
        {
            if (!IsValid)
            {
                Debug.LogError("INVALID OPERATION STATE: " + this);
                return false;
            }
            return true;
        }

        public event Action<IAsyncOperation<TObject>> Completed
        {
            add
            {
                Validate();
                if (IsDone)
                    DelayedActionManager.AddAction(value, 0, this);
                else
                    m_completedActionT += value;
            }

            remove
            {
                m_completedActionT -= value;
            }
        }
		
		event Action<IAsyncOperation> IAsyncOperation.Completed
		{
			add
			{
                Validate();
                if (IsDone)
                    DelayedActionManager.AddAction(value, 0, this);
                else
                    m_completedAction += value;
            }

            remove
			{
				m_completedAction -= value;
			}
		}

        object IAsyncOperation.Result
        {
            get
            {
                Validate();
                return m_result;
            }
        }

        public AsyncOperationStatus Status
        {
            get
            {
                Validate();
                return m_status;
            }
            protected set
            {
                Validate();
                m_status = value;
            }
        }

        public Exception OperationException
        {
            get
            {
                Validate();
                return m_error;
            }
        }

        public bool MoveNext()
        {
            Validate();
            return !IsDone;
        }

        public void Reset()
        {
        }

        public object Current
        {
            get
            {
                Validate();
                return Result;
            }
        }
        public TObject Result
        {
            get
            {
                Validate();
                return m_result;
            }
            set
            {
                Validate();
                m_result = value;
            }
        }
        public virtual bool IsDone
        {
            get
            {
                Validate();
                return !(EqualityComparer<TObject>.Default.Equals(Result, default(TObject)));
            }
        }
        public virtual float PercentComplete
        {
            get
            {
                Validate();
                return IsDone ? 1f : 0f;
            }
        }
        public object Context
        {
            get
            {
                Validate();
                return m_context;
            }
            protected set
            {
                Validate();
                m_context = value;
            }
        }

        public void InvokeCompletionEvent()
        {
            Validate();
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
            if (m_releaseToCacheOnCompletion)
                AsyncOperationCache.Instance.Release<TObject>(this);
        }

        public virtual void SetResult(TObject result)
        {
            Validate();
            m_result = result;
            m_status = (m_result == null) ? AsyncOperationStatus.Failed : AsyncOperationStatus.Succeeded;
        }

    }
}
