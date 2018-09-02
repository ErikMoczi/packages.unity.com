using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    internal class LoadGroupOperation<TObject> : AsyncOperationBase<IList<TObject>>
        where TObject : class
    {
        int m_loadedCount;
        Action<IAsyncOperation<TObject>> m_internalOnComplete;
        Action<IAsyncOperation<TObject>> m_action;
        List<IAsyncOperation<TObject>> m_operations = new List<IAsyncOperation<TObject>>();
        public LoadGroupOperation() 
        {
            m_internalOnComplete = LoadGroupOperation_completed;
        }

        public override void ResetStatus()
        {
            base.ResetStatus();
            Result = null;
            m_operations.Clear();
        }

        public virtual LoadGroupOperation<TObject> Start(IList<IResourceLocation> locations, Func<IResourceLocation, IAsyncOperation<TObject>> loadFunc, Action<IAsyncOperation<TObject>> onComplete)
        {
            Validate();
            Debug.Assert(locations != null, "Null location list passed into LoadGroupOperation");
            Debug.Assert(loadFunc != null, "Null loadFunc passed into LoadGroupOperation");
            m_loadedCount = 0;
            Context = locations;
            m_action = onComplete;
            Result = new List<TObject>(locations.Count);
            m_operations.Clear();
            for (int i = 0; i < locations.Count; i++)
            {
                var op = loadFunc(locations[i]);
                op.Completed += m_internalOnComplete;
                Result.Add(default(TObject));
                m_operations.Add(op);
            }
            return this;
        }

        public override bool IsDone
        {
            get
            {
                Validate();
                return Result != null && Result.Count == m_loadedCount;
            }
        }

        void LoadGroupOperation_completed(IAsyncOperation<TObject> op)
        {
            Validate();
            if (m_action != null)
                m_action(op);

            m_loadedCount++;
            for (int i = 0; i < m_operations.Count; i++)
            {
                if (m_operations[i] == op)
                {
                    Result[i] = op.Result;
                    break;
                }
            }

            if (IsDone)
                InvokeCompletionEvent();
        }
    }
}
