using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface IListOperation : IBaseOperation
    {
        void GetPackageListAsync(Action<IEnumerable<PackageInfo>> doneCallbackAction, Action<Error> errorCallbackAction = null);
    }
}
