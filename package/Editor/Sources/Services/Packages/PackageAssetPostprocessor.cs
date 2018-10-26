using System.Linq;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageAssetPostprocessor : AssetPostprocessor
    {
        static bool IsPackageJsonAsset(string path)
        {
            var pathComponents = (path ?? "").Split('/');
            return pathComponents.Length == 3 && pathComponents[0] == "Packages" && pathComponents[2] == "package.json";
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!importedAssets.Any(IsPackageJsonAsset) && !deletedAssets.Any(IsPackageJsonAsset) && !movedAssets.Any(IsPackageJsonAsset)) 
                return;
            
            PackageManagerWindow.FetchListOfflineCacheForAllWindows();
        }
    }
}