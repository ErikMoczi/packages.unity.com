using System;

namespace UnityEditor.PackageManager.UI
{
    internal interface IBaseOperation
    {
        event Action<Error> OnOperationError;
        event Action OnOperationFinalized;

        void Cancel();
    }
}
