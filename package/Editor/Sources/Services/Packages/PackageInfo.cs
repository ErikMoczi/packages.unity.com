using System;
using System.Collections.Generic;
using Semver;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PackageInfo : IEquatable<PackageInfo>
    {
        public static bool IsModule(string packageName)
        {
            return packageName.StartsWith("com.unity.modules.");
        }

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
        public PackageOrigin Origin;
        [SerializeField] 
        public List<Error> Errors;
        [SerializeField] 
        public bool IsRecommended;
        
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

        // This will always be <name>@<version>, even for an embedded package.
        public string VersionId {get { return string.Format("{0}@{1}", Name.ToLower(), Version); }}
        public string ShortVersionId {get { return string.Format("{0}@{1}", Name.ToLower(), Version.ShortVersion()); }}

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

        public bool HasTag(string tag)
        {
            return String.Equals(Version.Prerelease, tag, StringComparison.CurrentCultureIgnoreCase);
        }

        public bool HasTag(PackageTag tag)
        {
            return HasTag(tag.ToString());
        }

        public string VersionWithoutTag { get { return Version.VersionOnly(); } }
    }
}
