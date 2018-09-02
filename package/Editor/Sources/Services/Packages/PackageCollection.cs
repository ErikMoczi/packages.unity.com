using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageCollection
    {
        private static readonly PackageCollection instance = new PackageCollection();
        public static PackageCollection Instance { get { return instance; } }
        
        public event Action<Package> OnPackageAdded = delegate { };
        public event Action<Package> OnPackageUpdated = delegate { };
        public event Action<IEnumerable<Package>> OnPackagesChanged = delegate { };
        public event Action<PackageFilter> OnFilterChanged = delegate { };

        private List<PackageInfo> packageInfos;
        private Dictionary<string, Package> Packages;
        private PackageFilter filter;

        private IEnumerable<PackageInfo> LastListOfflinePackages = null;
        private IEnumerable<PackageInfo> LastListPackages = null;
        private IEnumerable<PackageInfo> LastSearchPackages = null;
        public ISearchOperation searchOperation;
        public IListOperation listOperation;
        private IListOperation listOperationOffline;

        public PackageFilter Filter
        {
            get { return filter; }
            
            // For public usage, use SetFilter() instead
            private set
            {
                var changed = value != filter;
                filter = value;
                
                if (changed)
                    OnFilterChanged(filter);
            }
        }
        
        private PackageCollection()
        {
            packageInfos = new List<PackageInfo>();
            Packages = new Dictionary<string, Package>();
            
            Filter = PackageFilter.Local;

            FetchAllCaches();
        }

        // Return Packages from internal cache.
        public IEnumerable<PackageInfo> PackageInfos
        {
            get { return packageInfos; }
        }

        public bool SetFilter(PackageFilter filter, bool refresh = true)
        {
            if (filter == Filter) 
                return false;
            
            Filter = filter;
            if (refresh)
            {
                UpdatePackageCollection();
            }

            return true;
        }

        private void Reset()
        {
            LastListOfflinePackages = null;
            LastListPackages = null;
            LastSearchPackages = null;

            listOperation = null;
            listOperationOffline = null;
            searchOperation = null;

            ClearPackagesInternal();
            FetchAllCaches();            
        }
        
        public void UpdatePackageCollection(bool reset = false)
        {
            if (reset)
                Reset();

            switch (Filter)
            {
                case PackageFilter.All:
                    SearchPackages();
                    break;
                case PackageFilter.None:
                case PackageFilter.Local:
                    ListPackages();
                    break;
            }
        }

        public bool HasFetchedPackageList()
        {
            return LastListPackages != null || LastListOfflinePackages != null;
        }

        public bool HasFetchedSearchPackages()
        {
            return LastSearchPackages != null;
        }

        private void FetchListOfflineCache()
        {
            if (listOperationOffline == null && LastListOfflinePackages == null)
            {
                listOperationOffline = OperationFactory.Instance.CreateListOperation(true);
                listOperationOffline.GetPackageListAsync(infos => { LastListOfflinePackages = infos; }, error => { ClearPackages(); });
            }
        }

        private void FetchListCache()
        {
            if (listOperation == null && LastListPackages == null)
            {
                var operation = OperationFactory.Instance.CreateListOperation();
                listOperation = operation;
                operation.GetPackageListAsync(infos => { LastListPackages = infos; });
            }
        }

        private void FetchSearchCache()
        {
            if (searchOperation == null && LastSearchPackages == null)
            {
                var operation = OperationFactory.Instance.CreateSearchOperation();
                searchOperation = operation;
                operation.GetAllPackageAsync(infos => { LastSearchPackages = infos; });
            }
        }

        private void FetchAllCaches()
        {
            FetchListOfflineCache();
            FetchListCache();
            FetchSearchCache();
        }

        private void ListPackagesOffline()
        {
            if (LastListPackages != null)
            {
                SetListPackageInfos(LastListPackages);
                return;
            }

            if (listOperationOffline == null)
                FetchListOfflineCache();

            if (LastListOfflinePackages == null)
            {
                listOperationOffline.OnOperationFinalized -= OnListOperationOfflineFinalized;    // Make sure we cancel previous listeners 
                listOperationOffline.OnOperationFinalized += OnListOperationOfflineFinalized;
            }
            else
            {
                SetListPackageInfos(LastListOfflinePackages);
            }
        }

        private void OnListOperationOfflineFinalized()
        {
            SetListPackageInfos(LastListOfflinePackages);
        }

        private void ListPackagesOnline()
        {
            if (listOperation == null)
                FetchListCache();

            if (LastListPackages == null)
            {
                listOperation.OnOperationFinalized -= OnListOperationFinalized;  // Make sure we cancel previous listeners
                listOperation.OnOperationFinalized += OnListOperationFinalized;
            }
            else
            {
                SetListPackageInfos(LastListPackages);
            }
        }

        private void OnListOperationFinalized()
        {
            listOperation = null;
            if (LastListPackages != null)
            {
                SetListPackageInfos(LastListPackages);
            }
        }

        private void CancelListOffline()
        {
            if (listOperationOffline != null)
            {
                listOperationOffline.Cancel();
                listOperationOffline = null;
            }  
        }

        private void ListPackages()
        {
            ListPackagesOffline();
            ListPackagesOnline();
        }
        
        private void SearchPackages()
        {
            if (searchOperation == null)
                FetchSearchCache();

            if (LastSearchPackages == null)
            {
                searchOperation.OnOperationFinalized -= OnSearchOperationFinalized; // Make sure we cancel previous listeners
                searchOperation.OnOperationFinalized += OnSearchOperationFinalized;
            }
            else
            {
                SetSearchPackageInfos(LastSearchPackages);
            }            
        }

        private void OnSearchOperationFinalized()
        {
            if (LastSearchPackages != null)
            {
                SetSearchPackageInfos(LastSearchPackages);
            }
        }

        private void SetSearchPackageInfos(IEnumerable<PackageInfo> searchPackageInfos)
        {
            searchOperation = null;
            var copyPackageInfo = new List<PackageInfo>(packageInfos);
            copyPackageInfo.AddRange(searchPackageInfos.Where(pi => !Packages.ContainsKey(pi.Name) || Packages[pi.Name].Versions.All(v => v.Version != pi.Version)));

            LastSearchPackages = copyPackageInfo;

            // Don't update the current list if the filter changed since the operation started 
            if (Filter == PackageFilter.All)
            {
                ClearPackageInfosInternal();
                AddPackageInfos(LastSearchPackages);
            }
        }

        public void SetListPackageInfos(IEnumerable<PackageInfo> packageInfos)
        {
            // Don't update the current list if the filter changed since the operation started 
            if (Filter == PackageFilter.Local)
            {
                CancelListOffline();
                ClearPackageInfosInternal();
                AddPackageInfos(packageInfos);
            }
        }

        private IEnumerable<Package> OrderedPackages()
        {
            return Packages.Values.OrderBy(pkg => pkg.Versions.LastOrDefault() == null ? pkg.Name : pkg.Versions.Last().DisplayName).AsEnumerable();
        }

        public void AddPackageInfo(PackageInfo packageInfo)
        {
            AddPackageInfoInternal(packageInfo);
            OnPackagesChanged(OrderedPackages());
        }

        
        public void ClearPackages()
        {
            listOperation = null;
            listOperationOffline = null;
            searchOperation = null;
            
            ClearPackagesInternal();
            OnPackagesChanged(OrderedPackages());
        }

        private void ClearPackageInfosInternal()
        {
            packageInfos.Clear();
        }

        private void ClearPackagesInternal()
        {
            ClearPackageInfosInternal();
            Packages.Clear();
        }

        public Package GetPackageByName(string name)
        {
            Package package;
            Packages.TryGetValue(name, out package);
            return package;
        }

        private void AddPackageInfos(IEnumerable<PackageInfo> packageInfos)
        {
            if (packageInfos == null)
                packageInfos = Enumerable.Empty<PackageInfo>();

            foreach (var packageInfo in packageInfos.OrderBy(p => p.DisplayName))
            {
                AddPackageInfoInternal(packageInfo);
            }

            OnPackagesChanged(OrderedPackages());
        }

        private void AddPackageInfoInternal(PackageInfo packageInfo)
        {
            packageInfos.Add(packageInfo);

            if (Packages.ContainsKey(packageInfo.Name))
            {
                Packages[packageInfo.Name].UpdateSource(from pkg in packageInfos where pkg.Name == packageInfo.Name select pkg);
                return;
            }

            var packageQuery = from pkg in packageInfos where pkg.Name == packageInfo.Name select pkg;
            var package = new Package(packageInfo.Name, packageQuery);
            Packages[packageInfo.Name] = package;

            OnPackageAdded(package);
        }

    }
}
