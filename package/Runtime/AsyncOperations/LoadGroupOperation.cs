using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    internal class LoadGroupOperation<TObject> : AsyncOperationBase<IList<TObject>>
        where TObject : class
    {
        protected int totalToLoad;
        int loadCount;
        bool allStarted;
        Action<IAsyncOperation<TObject>> m_internalOnComplete;
        Action<IAsyncOperation<TObject>> m_action;
        List<IAsyncOperation<TObject>> m_ops;
        public override void SetResult(IList<TObject> ignored)
        {
            foreach (var op in m_ops)
                Result.Add(op.Result);
        }

        public LoadGroupOperation() 
        {
            m_internalOnComplete = LoadGroupOperation_completed;
        }

        public virtual LoadGroupOperation<TObject> Start(IList<IResourceLocation> locations, Func<IResourceLocation, IAsyncOperation<TObject>> loadFunc, Action<IAsyncOperation<TObject>> onComplete)
        {
            UnityEngine.Debug.Assert(locations != null, "Null location list passed into LoadGroupOperation");
            totalToLoad = locations.Count;
            m_context = locations;
            loadCount = 0;
            allStarted = false;
            m_action = onComplete;
            if(Result == null)
                Result = new List<TObject>(locations.Count);
            else
                Result.Clear();

            if(m_ops == null)
                m_ops = new List<IAsyncOperation<TObject>>(locations.Count);
            else
                m_ops.Clear();

            for(int i = 0; i < locations.Count; i++)
            {
                var op = loadFunc(locations[i]);
                m_ops.Add(op);
                op.completed += m_internalOnComplete;
            }

            allStarted = true;

            if (IsDone)
            {
                SetResult(Result);
                InvokeCompletionEvent();
            }

            return this;
        }

        public override bool IsDone { get { return allStarted && loadCount == totalToLoad; } }

        void LoadGroupOperation_completed(IAsyncOperation<TObject> obj)
        {
            if (m_action != null)
                m_action(obj);

            loadCount++;

            if (IsDone)
            {
                SetResult(Result);
                InvokeCompletionEvent();
            }
        }
    }
}
