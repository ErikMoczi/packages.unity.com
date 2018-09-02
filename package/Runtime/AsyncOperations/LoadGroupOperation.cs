using System;
using System.Collections.Generic;

namespace ResourceManagement.AsyncOperations
{
    public class LoadGroupOperation<TObject> : AsyncOperationBase<IList<TObject>>
        where TObject : class
    {
        protected int totalToLoad;
        int loadCount;
        bool allStarted;
        Action<IAsyncOperation<TObject>> m_action;
        List<IAsyncOperation<TObject>> m_ops;

        protected override void SetResult(IList<TObject> result)
        {
            foreach (var op in m_ops)
                m_result.Add(op.result);
        }

        public LoadGroupOperation() : base("")
        {
            m_ops = new List<IAsyncOperation<TObject>>();
            m_result = new List<TObject>();
        }

        public virtual LoadGroupOperation<TObject> Start(ICollection<IResourceLocation> locations, Func<IResourceLocation, IAsyncOperation<TObject>> loadFunc, Action<IAsyncOperation<TObject>> onComplete)
        {
            totalToLoad = locations.Count;
            loadCount = 0;
            allStarted = false;
            m_action = onComplete;
            m_result.Clear();
            m_ops.Clear();

            if (locations != null)
            {
                foreach (var loc in locations)
                {
                    var op = loadFunc(loc);
                    m_ops.Add(op);
                    op.completed += LoadGroupOperation_completed;
                }

                allStarted = true;

                if (isDone)
                    InvokeCompletionEvent(this);
            }
            else
            {
                allStarted = true;
                InvokeCompletionEvent(this);
            }

            return this;
        }

        public override bool isDone { get { return allStarted && loadCount == totalToLoad; } }

        void LoadGroupOperation_completed(IAsyncOperation<TObject> obj)
        {
            if (m_action != null)
                m_action(obj);

            loadCount++;

            if (isDone)
                InvokeCompletionEvent(this);
        }
    }
}
