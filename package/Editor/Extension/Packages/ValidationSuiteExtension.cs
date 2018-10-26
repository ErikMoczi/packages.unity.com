#define NEW_PACKMAN

using UnityEditor.PackageManager.UI;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.PackageManager.ValidationSuite.UI
{
    [InitializeOnLoad]
#if UNITY_2018_2_OR_NEWER
    internal class ValidationSuiteExtension : IPackageManagerExtension
#else
    internal class ValidationSuiteExtension
#endif

    {
        private PackageInfo packageInfo;
        private ValidationSuiteExtensionUI ui;

        public ValidationSuiteExtension()
        {
        }

        public VisualElement CreateExtensionUI()
        {
            return ui ?? (ui = new ValidationSuiteExtensionUI());
        }

        public void OnPackageSelectionChange(PackageInfo packageInfo)
        {
            if (packageInfo == this.packageInfo)
                return;
            
            this.packageInfo = packageInfo;
            ui.OnPackageSelectionChange(this.packageInfo);
        }

        public void OnPackageAddedOrUpdated(PackageInfo packageInfo)
        {
        }

        public void OnPackageRemoved(PackageInfo packageInfo)
        {
        }

        static ValidationSuiteExtension()
        {
#if UNITY_2018_2_OR_NEWER
            PackageManagerExtensions.RegisterExtension(new ValidationSuiteExtension());
#endif
        }
    }
}
