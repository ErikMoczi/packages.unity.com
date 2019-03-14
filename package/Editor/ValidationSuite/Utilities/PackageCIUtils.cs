using System.Collections.Generic;

namespace UnityEditor.PackageManager.ValidationSuite
{
    internal class PackageCIUtils : CIUtils
    {
        internal static List<string> Pack(string path, string destinationPath)
        {
            return _Pack("package", path, destinationPath);
        }
    }
}
