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
        public string PackageId;
        [SerializeField]
        public SemVersion Version;
        [SerializeField]
        public string Tag;
        [SerializeField]
        public string Description;
        [SerializeField]
        public string Category;
        [SerializeField]
        public PackageState State;
        [SerializeField]
        public bool IsCurrent;        
        [SerializeField]
        public string Group;
        [SerializeField] 
        public OriginType OriginType;
        [SerializeField] 
        public List<Error> Errors;

        public string ModuleName { get { return IsModule(Name) ? DisplayName : ""; } }

        public bool Equals(PackageInfo other)
        {
            if (other == null) 
                return false;
            
            return PackageId == other.PackageId;
        }

        public override int GetHashCode()
        {
            return PackageId.GetHashCode();
        }

        public bool IsInPreview { get { return Version.Major == 0 || Version.Prerelease.ToLower() == previewTag; } }

        public string VersionWithoutTag { get { return Version.VersionOnly(); } }
    }
}
