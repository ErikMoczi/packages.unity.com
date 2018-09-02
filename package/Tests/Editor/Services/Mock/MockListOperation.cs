using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Tests
{
    internal class MockListOperation : MockOperation, IListOperation
    {
        public bool OfflineMode { get; set; }

        public MockListOperation(MockOperationFactory factory) : base(factory)
        {
        }

        public void GetPackageListAsync(Action<IEnumerable<PackageInfo>> doneCallbackAction,
            Action<Error> errorCallbackAction = null)
        {
            if (ForceError != null)
            {
                if (errorCallbackAction != null)
                    errorCallbackAction(ForceError);
            }
            else
            {
                doneCallbackAction(Factory.Packages);
            }
        }
    }
}