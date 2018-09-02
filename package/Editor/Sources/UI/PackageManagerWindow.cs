using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageManagerWindow : EditorWindow
    {
        private const double targetVersionNumber = 2018.1;
        
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

            PackageList.OnSelected += OnPackageSelected;
        }

        private void OnPackageSelected(Package package)
        {
            PackageDetails.SetPackage(package, PackageSearchFilterTabs.CurrentFilter);
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
        public static void ShowPackageManagerWindow()
        {
            // Check that we are using the right Unity version before we proceed.
            // Eventually, we could launch different functionality here based on version.
            var version = Application.unityVersion;

            // version's format will be something like 2017.3.0b4.
            // we need a numerical representation of the major.minor.
            double versionNumber;
            
            if (double.TryParse(version.Substring(0, version.LastIndexOf(".")), out versionNumber) && versionNumber < targetVersionNumber)
            {
                EditorUtility.DisplayDialog("Unsupported Unity Version", string.Format("The Package Manager requires Unity Version {0} or higher to operate.", targetVersionNumber), "Ok");
                return;
            }

            var window = GetWindow<PackageManagerWindow>(true, "Package Manager", true);
            window.minSize = new Vector2(850, 450);
            window.maxSize = window.minSize;
            window.Show();
        }
    }
}
