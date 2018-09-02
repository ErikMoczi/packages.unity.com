using System;

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
