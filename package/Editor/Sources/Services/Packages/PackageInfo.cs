using System;
using System.Collections.Generic;
using Semver;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PackageInfo : IEquatable<PackageInfo>
    {
        public static bool IsModule(string packageName)
        {
            return packageName.StartsWith("com.unity.modules.");
        }

        private const string previewTag = "preview";

        [SerializeField]
        public string Name;
        [SerializeField]
        public string DisplayName;
        [SerializeField]
        private string _PackageId;
        [SerializeField]
        public SemVersion Version;
        [SerializeField]
        public string Description;
        [SerializeField]
        public string Category;
        [SerializeField]
        public PackageState State;
        [SerializeField]
        public bool IsCurrent;
        [SerializeField]
        public bool IsLatest;
        [SerializeField]
        public string Group;
        [SerializeField] 
        public OriginType OriginType;
        [SerializeField] 
        public List<Error> Errors;
        
        public string PackageId {
            get
            {
                if (_PackageId != null) 
                    return _PackageId;
                return string.Format("{0}@{1}", Name.ToLower(), Version);
            }
            set
            {
                _PackageId = value;
            }
        }

        public string ModuleName { get { return IsModule(Name) ? DisplayName : ""; } }

        public bool Equals(PackageInfo other)
        {
            if (other == null) 
                return false;
            if (other == this)
                return true;
            
            return Name == other.Name && Version == other.Version;
        }

        public override int GetHashCode()
        {
            return PackageId.GetHashCode();
        }

        public bool IsInPreview { get { return Version.Major == 0 || Version.Prerelease.ToLower() == previewTag; } }

        public string VersionWithoutTag { get { return Version.VersionOnly(); } }
    }
}
