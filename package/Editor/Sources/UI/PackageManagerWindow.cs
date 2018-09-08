using System.Linq;
using System.IO;
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

#if UNITY_2018_1_OR_NEWER

        [SerializeField]
        internal PackageCollection Collection;
        [SerializeField]
        private PackageSearchFilter SearchFilter;

        private VisualElement root;

        public void OnEnable()
        {
            var collectionWasNull = Collection == null;
            if (Collection == null)
                Collection = new PackageCollection();

            if (SearchFilter == null)
                SearchFilter = new PackageSearchFilter();
            
            this.GetRootVisualContainer().AddStyleSheetPath(EditorGUIUtility.isProSkin ? DarkStylePath : LightStylePath);
            // Temporarly fix for case 1075335 (UIElements)
            this.GetRootVisualContainer().style.positionLeft = 2;
            this.GetRootVisualContainer().style.positionTop = 22;
            this.GetRootVisualContainer().style.positionRight = 2;
            this.GetRootVisualContainer().style.positionBottom = 2;
            this.GetRootVisualContainer().style.flexGrow = 1;

            var windowResource = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TemplatePath);
            if (windowResource != null)
            {
                root = windowResource.CloneTree(null);
                this.GetRootVisualContainer().Add(root);
                root.StretchToParentSize();

                Collection.SetFilter(PackageManagerPrefs.GetLastUsedPackageFilter(Directory.GetCurrentDirectory()));

                SetupPackageDetails();
                SetupPackageList();
                SetupSearchToolbar();
                SetupToolbar();
                SetupStatusbar();
                SetupCollection();
                
                // Disable filter while fetching first results
                if (!Collection.LatestListPackages.Any())
                    PackageManagerToolbar.SetEnabled(false);
                else
                    PackageList.SelectLastSelection(Collection.SelectedPackage);
                
                Collection.FetchListOfflineCache(Collection.listOperationOfflineOngoing);
                Collection.FetchListCache(Collection.listOperationOngoing);
                Collection.FetchSearchCache(Collection.searchOperationOngoing);
                
                if (!collectionWasNull)
                    Collection.UpdatePackageCollection(true);
            }
        }

        public void OnDisable()
        {
            PackageManagerPrefs.SetLastUsedPackageFilter(Directory.GetCurrentDirectory(), Collection.Filter);
        }

        private void SetupCollection()
        {
            Collection.OnPackagesChanged += PackageList.SetPackages;
            Collection.OnUpdateTimeChange += PackageStatusbar.SetDefaultMessage;
            Collection.ListSignal.WhenOperation(PackageStatusbar.OnListOrSearchOperation);
            Collection.SearchSignal.WhenOperation(PackageStatusbar.OnListOrSearchOperation);
        }

        private void SetupStatusbar()
        {
            PackageStatusbar.OnCheckInternetReachability += OnCheckInternetReachability;
        }

        private void SetupToolbar()
        {
            PackageManagerToolbar.OnFilterChange += OnFilterChange;
            PackageManagerToolbar.OnTogglePreviewChange += OnTogglePreviewChange;
            PackageManagerToolbar.SetFilter(Collection.Filter);
        }

        private void SetupSearchToolbar()
        {
            PackageManagerToolbar.SearchToolbar.OnSearchChange += OnSearchChange;
            PackageManagerToolbar.SearchToolbar.OnFocusChange += OnToolbarFocusChange;
            PackageManagerToolbar.SearchToolbar.SetSearchText(SearchFilter.SearchText);
        }

        private void SetupPackageList()
        {
            PackageList.OnSelected += OnPackageSelected;
            PackageList.OnLoaded += OnPackagesLoaded;
            PackageList.OnFocusChange += OnListFocusChange;
            PackageList.OnReload += OnReload;
            PackageList.SetSearchFilter(SearchFilter);
        }

        private void SetupPackageDetails()
        {
            PackageDetails.OnCloseError += OnCloseError;
            PackageDetails.OnOperationError += OnOperationError;
        }

        private void OnCloseError(Package package)
        {
            Collection.RemovePackageErrors(package);
            Collection.UpdatePackageCollection();
        }

        private void OnOperationError(Package package, Error error)
        {
            Collection.AddPackageError(package, error);
            Collection.UpdatePackageCollection();
        }

        private void OnTogglePreviewChange()
        {
            Collection.UpdatePackageCollection(true);
        }

        private void OnFilterChange(PackageFilter filter)
        {
            Collection.SetFilter(filter);
        }

        private void OnCheckInternetReachability()
        {
            Collection.FetchSearchCache(true);
            Collection.FetchListCache(true);
        }

        private void OnListFocusChange()
        {
            PackageManagerToolbar.GrabFocus();
        }

        private void OnToolbarFocusChange()
        {
            PackageList.GrabFocus();
        }

        private void OnSearchChange(string searchText)
        {
            SearchFilter.SearchText = searchText;
            PackageList.SetSearchFilter(SearchFilter);
            PackageFiltering.FilterPackageList(PackageList);
        }

        private void OnReload()
        {
            // Force a re-init to initial condition
            Collection.UpdatePackageCollection();
            PackageList.SelectLastSelection(Collection.SelectedPackage);
        }

        private void OnPackageSelected(Package package)
        {
            Collection.SelectedPackage = package == null ? null : package.Name;
            PackageDetails.SetPackage(package);
        }

        private void OnPackagesLoaded()
        {
            PackageManagerToolbar.SetEnabled(true);
        }

        private PackageList _packageList;
        private PackageList PackageList
        {
            get { return _packageList ?? (_packageList = root.Q<PackageList>("packageList")); }
        }

        private PackageDetails _packageDetails;
        private PackageDetails PackageDetails
        {
            get { return _packageDetails ?? (_packageDetails = root.Q<PackageDetails>("detailsGroup")); }
        }

        private PackageManagerToolbar _packageManagerToolbar;
        private PackageManagerToolbar PackageManagerToolbar
        {
            get {return _packageManagerToolbar ?? (_packageManagerToolbar = root.Q<PackageManagerToolbar>("toolbarContainer"));}
        }

        private PackageStatusBar _packageStatusbar;
        private PackageStatusBar PackageStatusbar
        {
            get {return _packageStatusbar ?? (_packageStatusbar = root.Q<PackageStatusBar>("packageStatusBar"));}
        }

        internal static void FetchListOfflineCacheForAllWindows()
        {
            var windows = UnityEngine.Resources.FindObjectsOfTypeAll<PackageManagerWindow>();
            if (windows == null || windows.Length <= 0) 
                return;
            
            foreach (var window in windows)
            {
                if (window.Collection != null)
                    window.Collection.FetchListOfflineCache(true);
            }
        }
#endif

        [MenuItem("Window/Package Manager", priority = 1500)]
        internal static void ShowPackageManagerWindow()
        {
#if UNITY_2018_1_OR_NEWER
            var window = GetWindow<PackageManagerWindow>(false, "Packages", true);
            window.minSize = new Vector2(700, 250);
            window.Show();
#else
            const double targetVersionNumber = 2018.1;
            EditorUtility.DisplayDialog("Unsupported Unity Version", string.Format("The Package Manager requires Unity Version {0} or higher to operate.", targetVersionNumber), "Ok");
#endif
        }
    }
}
