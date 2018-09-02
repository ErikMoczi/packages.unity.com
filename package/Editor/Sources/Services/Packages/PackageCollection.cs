using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageCollection
    {
        private static readonly PackageCollection instance = new PackageCollection();
        public static PackageCollection Instance { get { return instance; } }
        
        public event Action<Package> OnPackageAdded = delegate { };
        public event Action<IEnumerable<Package>> OnPackagesChanged = delegate { };
        public event Action<PackageFilter> OnFilterChanged = delegate { };

        [SerializeField]
        private List<PackageInfo> packageInfos;
        [SerializeField] 
        private Dictionary<string, Package> Packages;

        private PackageFilter filter;
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
        }

        // Return Packages from internal cache.
        public IEnumerable<PackageInfo> PackageInfos
        {
            get { return packageInfos; }
        }

        public void SetFilter(PackageFilter filter, bool refresh = true)
        {
            if (filter == Filter) 
                return;
            
            Filter = filter;
            if (refresh)
                RefreshPackages();
        }

        // Force a re-init
        public void Reset()
        {
            Filter = PackageFilter.Local;
            RefreshPackages();
        }
        
        public void RefreshPackages()
        {
            switch (Filter)
            {
                case PackageFilter.All:
                    SearchPackages();
                    break;
                case PackageFilter.None:
                case PackageFilter.Local:
                    ListPackages();
                    break;
                default:
                    Debug.LogError("Unknown package filter.");
                    break;
            }
        }

        private void ListPackages()
        {
            var operation = OperationFactory.Instance.CreateListOperation();
            operation.GetPackageListAsync(SetPackageInfos);            
        }

        private void SearchPackages()
        {
            var operation = OperationFactory.Instance.CreateSearchOperation();
            operation.GetAllPackageAsync(AddSearchPackageInfos); 
        }

        private void AddSearchPackageInfos(IEnumerable<PackageInfo> searchPackageInfos)
        {
            var copyPackageInfo = new List<PackageInfo>(packageInfos);
            copyPackageInfo.AddRange(searchPackageInfos.Where(pi => !Packages.ContainsKey(pi.Name) || Packages[pi.Name].Current == null || Packages[pi.Name].Current.Version != pi.Version));
            SetPackageInfos(copyPackageInfo);
        }

        public void SetPackageInfos(IEnumerable<PackageInfo> packageInfos)
        {
            ClearPackagesInternal();
            AddPackageInfos(packageInfos);
        }

        public void AddPackageInfo(PackageInfo packageInfo)
        {
            AddPackageInfoInternal(packageInfo);
            OnPackagesChanged(Packages.Values.AsEnumerable());
        }

        public void AddPackageInfos(IEnumerable<PackageInfo> packageInfos)
        {
            if (packageInfos == null)
                packageInfos = Enumerable.Empty<PackageInfo>();

            foreach (var packageInfo in packageInfos.OrderBy(p => p.DisplayName))
            {
                AddPackageInfoInternal(packageInfo);
            }
            
            OnPackagesChanged(Packages.Values.AsEnumerable());
        }

        private void AddPackageInfoInternal(PackageInfo packageInfo)
        {
            packageInfos.Add(packageInfo);

            if (Packages.ContainsKey(packageInfo.Name)) 
                return;

            var packageQuery = from pkg in packageInfos where pkg.Name == packageInfo.Name select pkg;
            var package = new Package(packageInfo.Name, packageQuery);
            Packages[packageInfo.Name] = package;

            OnPackageAdded(package);
        }

        public void ClearPackages()
        {
            ClearPackagesInternal();
            OnPackagesChanged(Packages.Values.AsEnumerable());
        }

        private void ClearPackagesInternal()
        {
            packageInfos.Clear();
            Packages.Clear();
        }

        public Package GetPackageByName(string name)
        {
            Package package;
            Packages.TryGetValue(name, out package);
            return package;
        }
        
    }
}
