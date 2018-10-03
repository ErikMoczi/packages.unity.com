using System.Collections.Generic;
using System;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// base class for implemented AsyncOperations, implements the needed interfaces and consolidates redundant code
    /// </summary>
    public abstract class AsyncOperationBase<TObject> : IAsyncOperation<TObject>
    {
        protected TObject m_result;
        protected AsyncOperationStatus m_status;
        protected Exception m_error;
        protected object m_context;
        protected object m_key;
        protected bool m_releaseToCacheOnCompletion = false;
        Action<IAsyncOperation> m_completedAction;
        List<Action<IAsyncOperation<TObject>>> m_completedActionT;

        protected AsyncOperationBase()
        {
            IsValid = true;
        }
        /// <inheritdoc />
        public bool IsValid { get; set; }
        /// <inheritdoc />
        public override string ToString()
        {
            var instId = "";
            var or = m_result as Object;
            if (or != null)
                instId = "(" + or.GetInstanceID().ToString() + ")";
            return base.ToString() + " result = " + m_result + instId + ", status = " + m_status + ", valid = " + IsValid + ", canRelease = " + m_releaseToCacheOnCompletion;
        }
        /// <inheritdoc />
        public virtual void Release()
        {
            Validate();
            m_releaseToCacheOnCompletion = true;
            if (!m_insideCompletionEvent && IsDone)
                AsyncOperationCache.Instance.Release(this);
        }
        /// <inheritdoc />
        public IAsyncOperation<TObject> Retain()
        {
            Validate();
            m_releaseToCacheOnCompletion = false;
            return this;
        }
        /// <inheritdoc />
        public virtual void ResetStatus()
        {
            m_releaseToCacheOnCompletion = true;
            m_status = AsyncOperationStatus.None;
            m_error = null;
            m_result = default(TObject);
            m_context = null;
            m_key = null;
        }
        /// <inheritdoc />
        public bool Validate()
        {
            if (!IsValid)
            {
                Debug.LogError("INVALID OPERATION STATE: " + this);
                return false;
            }
            return true;
        }
        /// <inheritdoc />
        public event Action<IAsyncOperation<TObject>> Completed
        {
            add
            {
                Validate();
                if (IsDone)
                {
                    DelayedActionManager.AddAction(value, 0, this);
                }
                else
                {
                    if (m_completedActionT == null)
                        m_completedActionT = new List<Action<IAsyncOperation<TObject>>>(2);
                    m_completedActionT.Add(value);
                }
            }

            remove
            {
                m_completedActionT.Remove(value);
            }
        }
        /// <inheritdoc />
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
        /// <inheritdoc />
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
        /// <inheritdoc />
        public Exception OperationException
        {
            get
            {
                Validate();
                return m_error;
            }
            protected set
            {
                m_error = value;
            }
        }
        /// <inheritdoc />
        public bool MoveNext()
        {
            Validate();
            return !IsDone;
        }
        /// <inheritdoc />
        public void Reset()
        {
        }
        /// <inheritdoc />
        public object Current
        {
            get
            {
                Validate();
                return Result;
            }
        }
        /// <inheritdoc />
        public TObject Result
        {
            get
            {
                Validate();
                return m_result;
            }
        }
        /// <inheritdoc />
        public virtual bool IsDone
        {
            get
            {
                Validate();
                return Status == AsyncOperationStatus.Failed || Status == AsyncOperationStatus.Succeeded;
            }
        }
        /// <inheritdoc />
        public virtual float PercentComplete
        {
            get
            {
                Validate();
                return IsDone ? 1f : 0f;
            }
        }
        /// <inheritdoc />
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
        /// <inheritdoc />
        public virtual object Key
        {
            get
            {
                Validate();
                return m_key;
            }
            set
            {
                Validate();
                m_key = value;
            }
        }

        bool m_insideCompletionEvent = false;
        /// <inheritdoc />
        public void InvokeCompletionEvent()
        {
            Validate();
            m_insideCompletionEvent = true;

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

            if (m_completedActionT != null)
            {
                for (int i = 0; i < m_completedActionT.Count; i++)
                {
                    try
                    {
                        m_completedActionT[i](this);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        m_error = e;
                        m_status = AsyncOperationStatus.Failed;
                    }
                }
                m_completedActionT.Clear();
            }
            m_insideCompletionEvent = false;
            if (m_releaseToCacheOnCompletion)
                AsyncOperationCache.Instance.Release(this);
        }
        /// <inheritdoc />
        public virtual void SetResult(TObject result)
        {
            Validate();
            m_result = result;
            m_status = (m_result == null) ? AsyncOperationStatus.Failed : AsyncOperationStatus.Succeeded;
        }

    }

    /// <summary>
    /// Wrapper operation for completed results or error cases.
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    public class CompletedOperation<TObject> : AsyncOperationBase<TObject>
    {
         /// <summary>
        /// Starts the operation.
        /// </summary>
        /// <param name="context">Context object.  This is usually set to the IResourceLocation.</param>
        /// <param name="key">Key value.  This is usually set to the address.</param>
        /// <param name="val">Completed result object.  This may be null if error is set.</param>
        /// <param name="error">Optional exception.  This should be set when val is null.</param>       
        public virtual IAsyncOperation<TObject> Start(object context, object key, TObject val, Exception error = null)
        {
            Context = context;
            OperationException = error;
            Key = key;
            SetResult(val);
            Retain();
            DelayedActionManager.AddAction((Action)InvokeCompletionEvent, 0);
            return this;
        }
    }

    /// <summary>
    /// This class can be used to chain operations together in a dependency chain.
    /// </summary>
    /// <typeparam name="TObject">The type of the operation.</typeparam>
    /// <typeparam name="TObjectDependency">The type parameter of the dependency IAsyncOperation.</typeparam>
    public class ChainOperation<TObject, TObjectDependency> : AsyncOperationBase<TObject>
    {
        Func<TObjectDependency, IAsyncOperation<TObject>> m_func;
        IAsyncOperation m_dependencyOperation;
        IAsyncOperation m_dependentOperation;
        /// <summary>
        /// Start the operation.
        /// </summary>
        /// <param name="context">Context object. Usually set to the IResourceLocation.</param>
        /// <param name="key">Key object.  Usually set to the primary key or address.</param>
        /// <param name="dependency">The IAsyncOperation that must complete before invoking the Func that generates the dependent operation that will set the result of this operation.</param>
        /// <param name="func">Function that takes as input the dependency operation and returns a new IAsyncOperation with the results needed by this operation.</param>
        /// <returns></returns>
        public virtual IAsyncOperation<TObject> Start(object context, object key, IAsyncOperation<TObjectDependency> dependency, Func<TObjectDependency, IAsyncOperation<TObject>> func)
        {
            m_func = func;
            Context = context;
            Key = key;
            m_dependencyOperation = dependency;
            m_dependentOperation = null;
            dependency.Completed += OnDependencyCompleted;
            return this;
        }
        /// <inheritdoc />
        public override float PercentComplete
        {
            get
            {
                if (m_dependentOperation == null)
                {
                    if (m_dependencyOperation == null)
                        return 0;
                            
                    return m_dependencyOperation.PercentComplete * .5f;
                }
                    
                return m_dependentOperation.PercentComplete * .5f + .5f;
            }
        }

        private void OnDependencyCompleted(IAsyncOperation<TObjectDependency> op)
        {
            m_dependencyOperation = null;
            var funcOp = m_func(op.Result);
            m_dependentOperation = funcOp;
            Context = funcOp.Context;
            funcOp.Key = Key;
            op.Release();
            funcOp.Completed += OnFuncCompleted;
        }

        private void OnFuncCompleted(IAsyncOperation<TObject> op)
        {
            SetResult(op.Result);
            InvokeCompletionEvent();
        }
        /// <inheritdoc />
        public override object Key
        {
            get
            {
                Validate();
                return m_key;
            }
            set
            {
                Validate();
                m_key = value;
                if (m_dependencyOperation != null)
                    m_dependencyOperation.Key = Key;
            }
        }
    }
    /// <summary>
    /// Class used to combine multiple operations into a single one.
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    public class GroupOperation<TObject> : AsyncOperationBase<IList<TObject>> where TObject : class
    {
        Action<IAsyncOperation<TObject>> m_callback;
        Action<IAsyncOperation<TObject>> m_internalOnComplete;
        List<IAsyncOperation<TObject>> m_operations;
        int m_loadedCount;
        /// <summary>
        /// Construct a new GroupOperation.
        /// </summary>
        public GroupOperation()
        {
            m_internalOnComplete = OnOperationCompleted;
            m_result = new List<TObject>();
        }
        /// <inheritdoc />
        public override void SetResult(IList<TObject> result)
        {
            Validate();
        }
        /// <inheritdoc />
        public override void ResetStatus()
        {
            m_releaseToCacheOnCompletion = true;
            m_status = AsyncOperationStatus.None;
            m_error = null;
            m_context = null;

            Result.Clear();
            m_operations = null;
        }
        /// <inheritdoc />
        public override object Key
        {
            get
            {
                Validate();
                return m_key;
            }
            set
            {
                Validate();
                m_key = value;
                if (m_operations != null)
                {
                    foreach (var op in m_operations)
                        op.Key = Key;
                }
            }
        }
        /// <summary>
        /// Load a list of assets associated with the provided IResourceLocations.
        /// </summary>
        /// <param name="locations">The list of locations.</param>
        /// <param name="callback">Callback methods that will be called when each sub operation is complete.  Order is not guaranteed.</param>
        /// <param name="func">Function to generated each sub operation from the locations</param>
        /// <returns>This object with the results being set to the results of the sub operations.  The result will match the size and order of the locations list.</returns>
        public virtual IAsyncOperation<IList<TObject>> Start(IList<IResourceLocation> locations, Action<IAsyncOperation<TObject>> callback, Func<IResourceLocation, IAsyncOperation<TObject>> func)
        {
            m_context = locations;
            m_callback = callback;
            m_loadedCount = 0;
            m_operations = new List<IAsyncOperation<TObject>>(locations.Count);
            foreach (var o in locations)
            {
                Result.Add(default(TObject));
                var op = func(o);
                op.Key = Key;
                m_operations.Add(op);
                op.Completed += m_internalOnComplete;
            }
            return this;
        }

        /// <summary>
        /// Load a list of assets associated with the provided IResourceLocations.
        /// </summary>
        /// <param name="locations">The list of locations.</param>
        /// <param name="callback">Callback methods that will be called when each sub operation is complete.  Order is not guaranteed.</param>
        /// <param name="func">Function to generated each sub operation from the locations.  This variation allows for a parameter to be passed to this method of type TParam.</param>
        /// <returns>This object with the results being set to the results of the sub operations.  The result will match the size and order of the locations list.</returns>
        public virtual IAsyncOperation<IList<TObject>> Start<TParam>(IList<IResourceLocation> locations, Action<IAsyncOperation<TObject>> callback, Func<IResourceLocation, TParam, IAsyncOperation<TObject>> func, TParam funcParams)
        {
            m_context = locations;
            m_callback = callback;
            m_loadedCount = 0;
            m_operations = new List<IAsyncOperation<TObject>>(locations.Count);
            foreach (var o in locations)
            {
                Result.Add(default(TObject));
                var op = func(o, funcParams);
                op.Key = Key;
                m_operations.Add(op);
                op.Completed += m_internalOnComplete;
            }
            return this;
        }

        /// <summary>
        /// Combines a set of IAsyncOperations into a single operation
        /// </summary>
        /// <param name="context">The context object.</param>
        /// <param name="key">The key object.</param>
        /// <param name="operations">The list of operations to wait on.</param>
        /// <returns></returns>
        public virtual IAsyncOperation<IList<TObject>> Start(object context, object key, List<IAsyncOperation<TObject>> operations)
        {
            m_context = context;
            m_loadedCount = 0;
            m_operations = operations;
            foreach (var op in m_operations)
            {
                Result.Add(default(TObject));
                op.Key = key;
                op.Completed += m_internalOnComplete;
            }
            if (m_operations.Count == 0)
                InvokeCompletionEvent();
            return this;
        }


        /// <inheritdoc />
        public override bool IsDone
        {
            get
            {
                Validate();
                return Result.Count == m_loadedCount;
            }
        }
        /// <inheritdoc />
        public override float PercentComplete
        {
            get
            {
                if (IsDone || m_operations.Count < 1)
                    return 1f;
                float total = 0;
                for (int i = 0; i < m_operations.Count; i++)
                    total += m_operations[i].PercentComplete;
                return total / m_operations.Count;
            }
        }

        private void OnOperationCompleted(IAsyncOperation<TObject> op)
        {
            if (m_callback != null)
            {
                op.Retain();
                m_callback(op);
            }
            m_loadedCount++;
            for (int i = 0; i < m_operations.Count; i++)
            {
                if (m_operations[i] == op)
                {
                    Result[i] = op.Result;
                    if (op.Status != AsyncOperationStatus.Succeeded)
                    {
                        Status = op.Status;
                        m_error = op.OperationException;
                    }
                    break;
                }
            }
            op.Release();
            if (IsDone)
            {
                if (Status != AsyncOperationStatus.Failed)
                    Status = AsyncOperationStatus.Succeeded;
                InvokeCompletionEvent();
            }
        }
    }

}
