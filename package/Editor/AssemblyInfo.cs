using System.Runtime.CompilerServices;
using UnityEditor.UIElements;

[assembly: InternalsVisibleTo("Unity.PackageManagerCaptain.Editor")]
[assembly: InternalsVisibleTo("Unity.PackageManagerUI.EditorTests")]
#if UNITY_2018_3_OR_NEWER
[assembly: UxmlNamespacePrefix("UnityEditor.PackageManager.UI", "upm")]
#endif
