using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    internal class LoadGroupOperation<TObject> : AsyncOperationBase<IList<TObject>>
        where TObject : class
    {
        protected int totalToLoad;
        Action<IAsyncOperation<TObject>> m_internalOnComplete;
        Action<IAsyncOperation<TObject>> m_action;

        public LoadGroupOperation() 
        {
            m_internalOnComplete = LoadGroupOperation_completed;
        }

        public override void ResetStatus()
        {
            base.ResetStatus();
            Result = new List<TObject>();
        }

        public override void SetResult(IList<TObject> result)
        {
            Validate();
            if (result != null)
            {
                foreach (var r in result)
                    Result.Add(r);
            }
            Status = (result.Count != totalToLoad) ? AsyncOperationStatus.Failed : AsyncOperationStatus.Succeeded;
        }


        public virtual LoadGroupOperation<TObject> Start(IList<IResourceLocation> locations, Func<IResourceLocation, IAsyncOperation<TObject>> loadFunc, Action<IAsyncOperation<TObject>> onComplete)
        {
            Validate();
            Debug.Assert(locations != null, "Null location list passed into LoadGroupOperation");
            Debug.Assert(loadFunc != null, "Null loadFunc passed into LoadGroupOperation");
            totalToLoad = locations.Count;
            Context = locations;
            m_action = onComplete;
            for (int i = 0; i < locations.Count; i++)
                loadFunc(locations[i]).Completed += m_internalOnComplete;
            return this;
        }

        public override bool IsDone
        {
            get
            {
                Validate();
                return Result.Count == totalToLoad;
            }
        }
        void LoadGroupOperation_completed(IAsyncOperation<TObject> obj)
        {
            Validate();
            if (m_action != null)
                m_action(obj);

            Result.Add(obj.Result);

            if (IsDone)
                InvokeCompletionEvent();
        }
    }
}
