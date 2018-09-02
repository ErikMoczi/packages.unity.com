using System;
using System.Collections.Generic;
using Semver;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageInfo : IEquatable<PackageInfo>
    {
        public static bool IsModule(string packageName)
        {
            return packageName.StartsWith("com.unity.modules.");
        }

        public string Name { get; set; }
        public string DisplayName { get; set; }
        private string _PackageId;
        public SemVersion Version { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public PackageState State { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsLatest { get; set; }
        public string Group { get; set; }
        public PackageOrigin Origin { get; set; }
        public List<Error> Errors { get; set; }
        public bool IsRecommended { get; set; }
        public string Author { get; set; }
        
        public PackageManager.PackageInfo Info { get; set; }
        
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
        public string VersionId { get { return string.Format("{0}@{1}", Name.ToLower(), Version); } }
        public string ShortVersionId { get { return string.Format("{0}@{1}", Name.ToLower(), Version.ShortVersion()); } }

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
            return string.Equals(Version.Prerelease, tag, StringComparison.CurrentCultureIgnoreCase);
        }

        public bool HasTag(PackageTag tag)
        {
            return HasTag(tag.ToString());
        }

        public string VersionWithoutTag { get { return Version.VersionOnly(); } }
    }
}
