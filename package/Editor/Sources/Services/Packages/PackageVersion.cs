namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// Combines a package and a version
    /// </summary>
    internal class PackageVersion
    {
        public Package Package;
        public PackageInfo Version;

        public PackageVersion(Package package, PackageInfo packageInfo)
        {
            Package = package;
            Version = packageInfo;
        }
    }
}
