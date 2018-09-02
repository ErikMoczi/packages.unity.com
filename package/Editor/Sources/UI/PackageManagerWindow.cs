using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageManagerWindow : EditorWindow
    {
        private const double targetVersionNumber = 2018.1;

#if UNITY_2018_1_OR_NEWER
        // When object is created
        public void OnEnable()
        {
            if (EditorGUIUtility.isProSkin)
                this.GetRootVisualContainer().AddStyleSheetPath("Styles/Main_Dark");
            else
                this.GetRootVisualContainer().AddStyleSheetPath("Styles/Main_Light");

            var template = Resources.Load<VisualTreeAsset>("Templates/PackageManagerWindow").CloneTree(null);
            this.GetRootVisualContainer().Add(template);
            template.StretchToParentSize();
            this.GetRootVisualContainer().StretchToParentSize();

            PackageSearchFilterTabs.visible = false;

            PackageList.OnSelected += OnPackageSelected;
            PackageList.OnLoaded += OnPackagesLoaded;
        }

        private void OnPackageSelected(Package package)
        {
            PackageDetails.SetPackage(package, PackageSearchFilterTabs.CurrentFilter);
        }

        private void OnPackagesLoaded()
        {
            PackageSearchFilterTabs.visible = true;
        }

        private PackageList PackageList
        {
            get {return this.GetRootVisualContainer().Q<PackageList>("packageList");}
        }

        private PackageDetails PackageDetails
        {
            get {return this.GetRootVisualContainer().Q<PackageDetails>("detailsGroup");}
        }

        private PackageSearchFilterTabs PackageSearchFilterTabs
        {
            get {return this.GetRootVisualContainer().Q<PackageSearchFilterTabs>("tabsGroup");}
        }

        [MenuItem("Project/Packages/Manage")]
        internal static void ShowPackageManagerWindow()
        {
            var window = GetWindow<PackageManagerWindow>(true, "Packages", true);
            window.minSize = new Vector2(850, 450);
            window.maxSize = window.minSize;
            window.Show();
        }
#else
        [MenuItem("Project/Packages/Manage")]
        internal static void ShowPackageManagerWindow()
        {
            EditorUtility.DisplayDialog("Unsupported Unity Version", string.Format("The Package Manager requires Unity Version {0} or higher to operate.", targetVersionNumber), "Ok");
        }
#endif
    }
}
