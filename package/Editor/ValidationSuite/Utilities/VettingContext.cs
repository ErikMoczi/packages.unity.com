using System;
using System.IO;
using System.Linq;
using Semver;
using UnityEditor.PackageManager.ValidationSuite;
using UnityEngine;

using UnityEditor.PackageManager;

/// <summary>
/// Class containing package data required for vetting.
/// </summary>
internal class VettingContext
{
    public enum ContextType
    {
        PackageManager,
        AssetStore
    }

    internal class ManifestData
    {
        public string path = "";
        public string name = "";
        public string description = "";
        public string unity = "";
        public string version;
    }

    public ManifestData ProjectPackageInfo { get; set; }
    public ManifestData PublishPackageInfo { get; set; }
    public ManifestData PreviousPackageInfo { get; set; }

    public ContextType Type { get; set; }

    public static VettingContext CreatePackmanContext(string packagePath)
    {
        VettingContext context = new VettingContext();

        // First, get the manifest for the default build
        context.ProjectPackageInfo = GetManifest(packagePath);

        // Then, publish the package locally to get an actual snapshot of what we will publish
        var publishPackagePath = PublishPackage(packagePath);
        context.PublishPackageInfo = GetManifest(publishPackagePath);

        // Get Previous Version of the package
        // var previousPackagePath = GetPreviousPackage(this.ProjectPackageInfo);
        context.PreviousPackageInfo = null;
        // if (!string.IsNullOrEmpty(previousPackagePath))
        //     this.PublishPackageInfo = GetManifest(previousPackagePath);

        return context;
    }

    public static VettingContext CreateAssetStoreContext(string packagePath, string previousPackagePath)
    {
        VettingContext context = new VettingContext();

        return context;
    }


    private static ManifestData GetManifest(string packagePath)
    {
        // Start by parsing the package's manifest data.
        var manifestPath = Path.Combine(packagePath, Utilities.PackageJsonFilename);

        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException(manifestPath);
        }

        // Read manifest json data, and convert it.
        var textManifestData = File.ReadAllText(manifestPath);
        var manifest = JsonUtility.FromJson<ManifestData>(textManifestData);
        manifest.path = packagePath;

        return manifest;
    }

    private static string PublishPackage(string projectPackagePath)
    {
        // ***** HACK ****** until upm has an api to pack a folder, we will do it ourselves.
        var tempPath = System.IO.Path.GetTempPath();

        var packageName = Utilities.CreatePackage(projectPackagePath, tempPath);
        var packageInfo = Utilities.GetDataFromJson<ManifestData>(Path.Combine(projectPackagePath, Utilities.PackageJsonFilename));

        return Utilities.ExtractPackage(packageName, tempPath, packageInfo.name);
    }

    private string GetPreviousPackage(ManifestData projectPackageInfo)
    {
        // List out available versions for a package.
        var request = Client.Search(projectPackageInfo.path);
        while (!request.IsCompleted)
        {
            System.Threading.Thread.Sleep(100);
        }

        // If it exists, get the last one from that list.
        if (request.Result.Length > 0)
        {
            var packageInfo = request.Result.Last();

            // Create Temp Dir
            var tempPath = System.IO.Path.GetTempPath();
            var PreviousPackagePath = Path.Combine(tempPath, projectPackageInfo.name + "-" + packageInfo.version);
            Client.Add(packageInfo.packageId);
        }

        return string.Empty;
    }
}
