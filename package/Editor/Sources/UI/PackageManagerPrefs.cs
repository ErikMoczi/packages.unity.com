using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UnityEditor.PackageManager.UI
{
    internal static class PackageManagerPrefs
    {
        private const string showPreviewPackagesPrefs = "PackageManager.ShowPreviewPackages";
        private const string showPreviewPackagesWarningPrefs = "PackageManager.ShowPreviewPackagesWarning";
        private const string lastUsedFilterPrefix = "PackageManager.Filter_";

        public static bool ShowPreviewPackages
        {
            get { return EditorPrefs.GetBool(showPreviewPackagesPrefs, false); }
            set { EditorPrefs.SetBool(showPreviewPackagesPrefs, value); }
        }

        public static bool ShowPreviewPackagesWarning
        {
            get { return EditorPrefs.GetBool(showPreviewPackagesWarningPrefs, true); }
            set { EditorPrefs.SetBool(showPreviewPackagesWarningPrefs, value); }
        }

        private static string GetHascodeHexString(string str)
        {
            var bytes = new SHA1Managed().ComputeHash(Encoding.UTF8.GetBytes(Directory.GetCurrentDirectory()));
            return BitConverter.ToString(bytes);            
        }

        public static PackageFilter GetLastUsedPackageFilter(string str)
        {
            return (PackageFilter)Enum.Parse(typeof(PackageFilter),
                EditorPrefs.GetString(lastUsedFilterPrefix + GetHascodeHexString(str), PackageFilter.All.ToString()));
        }

        public static void SetLastUsedPackageFilter(string str, PackageFilter filter)
        {
            EditorPrefs.SetString(lastUsedFilterPrefix + GetHascodeHexString(str), filter.ToString());
        }
    }
}
