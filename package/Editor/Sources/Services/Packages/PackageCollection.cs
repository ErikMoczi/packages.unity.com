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

        private IBaseOperation currentOperation;
        private IEnumerable<PackageInfo> LastListPackages = null;
        private IEnumerable<PackageInfo> LastSearchPackages = null;

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

        public void UpdatePackageCollection(bool reset = false)
        {
            if (reset)
            {
                LastListPackages = null;
                LastSearchPackages = null;
            }

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

        private void ListPackages()
        {
            if (currentOperation != null)
            {
                currentOperation.Cancel();
                currentOperation = null;
            }

            if (LastListPackages != null)
            {
                ClearPackagesInternal();
                AddPackageInfos(LastListPackages);
            }
            else
            {
                var operation = OperationFactory.Instance.CreateListOperation();
                currentOperation = operation;
                operation.GetPackageListAsync(SetListPackageInfos, error => { ClearPackages(); });
            }

        }

        private void SearchPackages()
        {
            if (currentOperation != null)
            {
                currentOperation.Cancel();
                currentOperation = null;
            }

            if (LastSearchPackages != null)
            {
                ClearPackagesInternal();
                AddPackageInfos(LastSearchPackages);
            }
            else
            {
                var operation = OperationFactory.Instance.CreateSearchOperation();
                currentOperation = operation;
                operation.GetAllPackageAsync(SetSearchPackageInfos, error => { ClearPackages(); });
            }
        }

        public void SetSearchPackageInfos(IEnumerable<PackageInfo> searchPackageInfos)
        {
            currentOperation = null;
            var currentPackageInfos = packageInfos.Where(p => !searchPackageInfos.Any(p2 => p2.PackageId == p.PackageId));
            var newPackageInfos = new List<PackageInfo>(searchPackageInfos);
            newPackageInfos.AddRange(currentPackageInfos);

            LastSearchPackages = newPackageInfos;
            ClearPackagesInternal();
            AddPackageInfos(newPackageInfos);
        }

        public void SetListPackageInfos(IEnumerable<PackageInfo> packageInfos)
        {
            currentOperation = null;

            LastListPackages = packageInfos;
            ClearPackagesInternal();
            AddPackageInfos(packageInfos);
        }

        public void AddPackageInfo(PackageInfo packageInfo)
        {
            AddPackageInfoInternal(packageInfo);
            OnPackagesChanged(Packages.Values.AsEnumerable());
        }

        public void ClearPackages()
        {
            currentOperation = null;
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

        private void AddPackageInfos(IEnumerable<PackageInfo> packageInfos)
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
            {
                Packages[packageInfo.Name].source = from pkg in packageInfos where pkg.Name == packageInfo.Name select pkg;
                return;
            }

            var packageQuery = from pkg in packageInfos where pkg.Name == packageInfo.Name select pkg;
            var package = new Package(packageInfo.Name, packageQuery);
            Packages[packageInfo.Name] = package;

            OnPackageAdded(package);
        }

    }
}
