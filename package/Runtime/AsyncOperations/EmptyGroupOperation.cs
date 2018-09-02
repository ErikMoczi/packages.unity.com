using System;
using System.Collections.Generic;

namespace ResourceManagement.AsyncOperations
{
    internal class EmptyGroupOperation<TObject> : LoadGroupOperation<TObject>
        where TObject : class
    {
        public EmptyGroupOperation()
        {
            m_result = new TObject[0];
        }

        public override void SetResult(IList<TObject> result)
        {
            // Do nothing
        }

        public override LoadGroupOperation<TObject> Start(IList<IResourceLocation> locations, Func<IResourceLocation, IAsyncOperation<TObject>> loadFunc, Action<IAsyncOperation<TObject>> onComplete)
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
