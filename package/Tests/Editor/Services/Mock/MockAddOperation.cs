using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Tests
{
    internal class MockAddOperation : MockOperation, IAddOperation
    {
        public new event Action<Error> OnOperationError = delegate { };
        public new event Action OnOperationFinalized = delegate { };
        public event Action<PackageInfo> OnOperationSuccess = delegate { };

        public PackageInfo PackageInfo { get; set; }

        public MockAddOperation(MockOperationFactory factory, PackageInfo packageInfo = null) : base(factory)
        {
            PackageInfo = packageInfo;
        }

        public void AddPackageAsync(PackageInfo packageInfo, Action<PackageInfo> doneCallbackAction = null, 
                                    Action<Error> errorCallbackAction = null)
        {
            if (ForceError != null)
            {
                if (errorCallbackAction != null)
                    errorCallbackAction(ForceError);
                
                OnOperationError(ForceError);
            }
            else
            {
                // Note: TestAddOperation should instead have a list of possible packages to add, and fail if requested package is not in that list
                if (PackageInfo.PackageId != packageInfo.PackageId)
                    Debug.LogError(string.Format("Add operation is adding a different package: {0} -- {1}",
                        packageInfo.PackageId,
                        PackageInfo.PackageId));

                if (doneCallbackAction != null)
                    doneCallbackAction(PackageInfo);

                OnOperationSuccess(PackageInfo);
            }

            OnOperationFinalized();
        }
        
        internal void ResetEvents()
        {
            OnOperationError = delegate { };
            OnOperationFinalized = delegate { };
            OnOperationSuccess = delegate { };
        }
    }
}
