using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal interface IPackageSelection
    {
        void RefreshSelection();
        PackageInfo TargetVersion { get; }
        VisualElement Element { get; }
    }
}
