using System;

namespace UnityEditor.PackageManager.UI
{
    internal interface IRemoveOperation : IBaseOperation
    {
        event Action OnOperationSuccess;

        void RemovePackageAsync(PackageInfo package, Action doneCallbackAction = null,  Action<Error> errorCallbackAction = null);
    }
}
