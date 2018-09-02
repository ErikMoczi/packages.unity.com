using System;
using System.Reflection;
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

            var windowResource = Resources.Load<VisualTreeAsset>("Templates/PackageManagerWindow");
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
            PackageDetails.SetPackage(package, PackageSearchFilterTabs.CurrentFilter);
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
            // Make sure we are not registered on callback anymore
            AssemblyReloadEvents.beforeAssemblyReload -= ShowPackageManagerWindow;

            var window = GetWindow<PackageManagerWindow>(false, "Package Manager", true);
            window.minSize = new Vector2(700, 250);
            window.maxSize = new Vector2(1400, 1400);
            window.Show();
#else
            EditorUtility.DisplayDialog("Unsupported Unity Version", string.Format("The Package Manager requires Unity Version {0} or higher to operate.", targetVersionNumber), "Ok");
#endif
        }
    }
}
