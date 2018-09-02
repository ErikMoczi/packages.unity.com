using System;
using System.Collections.Generic;
using System.Linq;
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
        public PackageSource Origin { get; set; }
        public List<Error> Errors { get; set; }
        public bool IsVerified { get; set; }
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
            if (string.IsNullOrEmpty(Version.Prerelease))
                return false;

            return String.Equals(Version.Prerelease.Split('.').First(), tag, StringComparison.CurrentCultureIgnoreCase);
        }

        public bool HasTag(PackageTag tag)
        {
            return HasTag(tag.ToString());
        }

        // Is it a pre-release (alpha/beta/experimental/preview)?
        //        Current logic is any tag is considered pre-release, except recommended
        public bool IsPreRelease
        {
            get { return !string.IsNullOrEmpty(Version.Prerelease) || Version.Major == 0; }
        }

        public bool IsPreview
        {
            get { return HasTag(PackageTag.preview) || Version.Major == 0; }
        }

        // A version is user visible if it has a supported tag (or no tag at all)
        public bool IsUserVisible
        {
            get { return string.IsNullOrEmpty(Version.Prerelease) || HasTag(PackageTag.preview) || IsVerified; }
        }
        
        public string VersionWithoutTag { get { return Version.VersionOnly(); } }
        
        public bool IsVersionLocked
        {
            get
            {
                return Origin == PackageSource.Embedded || Origin == PackageSource.Git ||
                    Origin == PackageSource.BuiltIn || Origin == PackageSource.Local;
            }
        }
    }
}
