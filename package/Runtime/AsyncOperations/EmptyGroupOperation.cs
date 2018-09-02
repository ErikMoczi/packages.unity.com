using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    internal class EmptyGroupOperation<TObject> : LoadGroupOperation<TObject>
        where TObject : class
    {
        public EmptyGroupOperation()
        {
            Result = new TObject[0];
        }

        public override LoadGroupOperation<TObject> Start(IList<IResourceLocation> locations, Func<IResourceLocation, IAsyncOperation<TObject>> loadFunc, Action<IAsyncOperation<TObject>> onComplete)
        {
            return this;
        }

        public override bool IsDone
        {
            get
            {
                return true;
            }
        }

        public override float PercentComplete
        {
            get
            {
                return 100.0f;
            }
        }
    }
}
