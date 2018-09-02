using System;
using System.Collections.Generic;

namespace ResourceManagement.AsyncOperations
{
    public class EmptyGroupOperation<TObject> : LoadGroupOperation<TObject>
        where TObject : class
    {
        public EmptyGroupOperation()
        {
            m_id = "";
            m_result = new TObject[0];
        }

        protected override void SetResult(IList<TObject> result)
        {
            // Do nothing
        }

        public override LoadGroupOperation<TObject> Start(ICollection<IResourceLocation> locations, Func<IResourceLocation, IAsyncOperation<TObject>> loadFunc, Action<IAsyncOperation<TObject>> onComplete)
        {
            return this;
        }

        public override bool isDone
        {
            get
            {
                return true;
            }
        }

        public override float percentComplete
        {
            get
            {
                return 100.0f;
            }
        }
    }
}
