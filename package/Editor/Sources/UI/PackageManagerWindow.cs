using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageManagerWindow : EditorWindow
    {
        public const string PackagePath = "Packages/com.unity.package-manager-ui/";
        public const string ResourcesPath = PackagePath + "Editor/Resources/";
        private const string TemplatePath = ResourcesPath + "Templates/PackageManagerWindow.uxml";
        private const string DarkStylePath = ResourcesPath + "Styles/Main_Dark.uss";
        private const string LightStylePath = ResourcesPath + "Styles/Main_Light.uss";

        private const double targetVersionNumber = 2018.1;

#if UNITY_2018_1_OR_NEWER

        public void OnEnable()
        {
            this.GetRootVisualContainer().AddStyleSheetPath(EditorGUIUtility.isProSkin ? DarkStylePath : LightStylePath);

            var windowResource = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TemplatePath);
            if (windowResource != null)
            {
                var template = windowResource.CloneTree(null);
                this.GetRootVisualContainer().Add(template);
                template.StretchToParentSize();

                // Disable filter while fetching first results
                if (!PackageCollection.Instance.HasFetchedPackageList())
                    PackageSearchFilterTabs.SetEnabled(false);

                PackageList.OnSelected += OnPackageSelected;
                PackageList.OnLoaded += OnPackagesLoaded;
            }
        }

        public void OnDisable()
        {
            // Package list item may not be valid here.
            if (PackageList != null)
            {
                PackageList.OnSelected -= OnPackageSelected;
                PackageList.OnLoaded -= OnPackagesLoaded;
            }
        }

        private void OnPackageSelected(Package package)
        {
            PackageDetails.SetPackage(package);
        }

        private void OnPackagesLoaded()
        {
            PackageSearchFilterTabs.SetEnabled(true);
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
        
        internal Alert ErrorBanner { get { return this.GetRootVisualContainer().Q<Alert>("errorBanner"); } }
        
#endif

        [MenuItem("Window/Package Manager", priority = 1500)]
        internal static void ShowPackageManagerWindow()
        {
#if UNITY_2018_1_OR_NEWER
            var window = GetWindow<PackageManagerWindow>(false, "Packages", true);
            window.minSize = new Vector2(700, 250);
            window.Show();
#else
            EditorUtility.DisplayDialog("Unsupported Unity Version", string.Format("The Package Manager requires Unity Version {0} or higher to operate.", targetVersionNumber), "Ok");
#endif
        }
    }
}
