using System;
using System.Linq;

namespace UnityEditor.PackageManager.UI
{
    internal static class PackageManagerPrefs
    {
        private const string kSkipRemoveConfirmationPrefs = "PackageManager.SkipRemoveConfirmation";
        private const string kShowPackageDependenciesPrefs = "PackageManager.ShowPackageDependencies";
        private const string kShowPreviewPackagesPrefKeyPrefix = "PackageManager.ShowPreviewPackages_";
        private const string kShowPreviewPackagesWarningPrefs = "PackageManager.ShowPreviewPackagesWarning";
        private const string kLastUsedFilterPrefix = "PackageManager.Filter_";

        private static string GetProjectIdentifier()
        {
            // PlayerSettings.productGUID is already used as LocalProjectID by Analytics, so we use it too
            return PlayerSettings.productGUID.ToString();
        }

        public static bool SkipRemoveConfirmation
        {
            get { return EditorPrefs.GetBool(kSkipRemoveConfirmationPrefs, false); }
            set { EditorPrefs.SetBool(kSkipRemoveConfirmationPrefs, value); }
        }

        public static bool ShowPackageDependencies
        {
            get { return EditorPrefs.GetBool(kShowPackageDependenciesPrefs, false); }
            set { EditorPrefs.SetBool(kShowPackageDependenciesPrefs, value); }
        }

        public static bool ShowPreviewPackages
        {
            get
            {
                var key = kShowPreviewPackagesPrefKeyPrefix + GetProjectIdentifier();

                // If user manually choose to show or not preview packages, use this value
                if (EditorPrefs.HasKey(key))
                    return EditorPrefs.GetBool(key);

                // Returns true if at least one preview package is installed, false otherwise
                return PackageCollection.packages.Values.Any(p => p.Current != null && p.Current.IsPreview);
            }
            set
            {
                EditorPrefs.SetBool(kShowPreviewPackagesPrefKeyPrefix + GetProjectIdentifier(), value);
            }
        }

        public static bool ShowPreviewPackagesWarning
        {
            get { return EditorPrefs.GetBool(kShowPreviewPackagesWarningPrefs, true); }
            set { EditorPrefs.SetBool(kShowPreviewPackagesWarningPrefs, value); }
        }

        public static PackageFilter LastUsedPackageFilter
        {
            get
            {
                return (PackageFilter)Enum.Parse(typeof(PackageFilter), EditorPrefs.GetString(kLastUsedFilterPrefix + GetProjectIdentifier(), PackageFilter.All.ToString()));
            }
            set
            {
                EditorPrefs.SetString(kLastUsedFilterPrefix + GetProjectIdentifier(), value.ToString());
            }
        }
    }
}
