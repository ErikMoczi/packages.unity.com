using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    // History of a single package
    internal class Package : IEquatable<Package>
    {
        internal const string packageManagerUIName = "com.unity.package-manager-ui";
        private readonly string packageName;
        private readonly IEnumerable<PackageInfo> source;

        public Package(string packageName, IEnumerable<PackageInfo> infos)
        {
            if (string.IsNullOrEmpty(packageName))
                throw new ArgumentException("Cannot be empty or null", "packageName");

            if (!infos.Any())
                throw new ArgumentException("Cannot be empty", "infos");
            
            this.packageName = packageName;
            source = infos;
        }

        public PackageInfo Current { get { return Versions.FirstOrDefault(package => package.IsCurrent); } }
        public PackageInfo Latest { get { return Versions.LastOrDefault(package => package.IsLatest) ?? Versions.LastOrDefault(); } }
                
        public IEnumerable<PackageInfo> Versions { get { return source.OrderBy(package => package.Version); } }
        public string Name { get { return packageName; } }

        public bool IsPackageManagerUI
        {
            get { return Name == packageManagerUIName; }
        }
        
        public bool Equals(Package other)
        {
            if (other == null) 
                return false;
            
            return packageName == other.packageName;
        }

        public override int GetHashCode()
        {
            return packageName.GetHashCode();
        }
        
        [SerializeField]
        public readonly OperationSignal<IAddOperation> AddSignal = new OperationSignal<IAddOperation>();

        private Action<PackageInfo> OnAddOperationSuccessEvent;
        private Action OnAddOperationFinalizedEvent;
        
        public void Add(PackageInfo packageInfo)
        {
            if (packageInfo == Current)
                return;
            
            var operation = OperationFactory.Instance.CreateAddOperation();
            OnAddOperationSuccessEvent = p => 
            {
                PackageCollection.Instance.Reset();
            };
            OnAddOperationFinalizedEvent = () =>
            {
                AddSignal.Operation = null;
                operation.OnOperationSuccess -= OnAddOperationSuccessEvent;
                operation.OnOperationFinalized -= OnAddOperationFinalizedEvent;
            };

            operation.OnOperationSuccess += OnAddOperationSuccessEvent;
            operation.OnOperationFinalized += OnAddOperationFinalizedEvent;

            AddSignal.SetOperation(operation);
            operation.AddPackageAsync(packageInfo);
        }

        public void Update()
        {
            Add(Latest);
        }

        [SerializeField]
        public readonly OperationSignal<IRemoveOperation> RemoveSignal = new OperationSignal<IRemoveOperation>();

        private Action OnRemoveOperationSuccessEvent;
        private Action OnRemoveOperationFinalizedEvent;

        public void Remove()
        {
            var operation = OperationFactory.Instance.CreateRemoveOperation();
            OnRemoveOperationSuccessEvent = () =>
            {
                PackageCollection.Instance.RefreshPackages();
            };
            OnRemoveOperationFinalizedEvent = () =>
            {
                RemoveSignal.Operation = null;
                operation.OnOperationSuccess -= OnRemoveOperationSuccessEvent;
                operation.OnOperationFinalized -= OnRemoveOperationFinalizedEvent;
            };

            operation.OnOperationSuccess += OnRemoveOperationSuccessEvent;
            operation.OnOperationFinalized += OnRemoveOperationFinalizedEvent;
            RemoveSignal.SetOperation(operation);

            operation.RemovePackageAsync(Current);
        }
    }
}
