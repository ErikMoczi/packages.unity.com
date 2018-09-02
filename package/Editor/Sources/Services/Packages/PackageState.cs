using System;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal enum PackageState {
        UpToDate,
        Outdated,
        InProgress,
        Error
    }
}