using System;
using System.IO;
using System.Linq;
using UnityEditor.PackageManager.ValidationSuite;
using UnityEngine;

using UnityEditor.PackageManager;

/// <summary>
/// Class containing package data required for vetting.
/// </summary>
internal class VettingContext
{
    internal class ManifestData
    {
        public string Path = "";
        public string name = "";
        public string description = "";
        public string version = "";
        public string unity = "";
    }

    public ManifestData ProjectPackageInfo { get; set; }
    public ManifestData PublishPackageInfo { get; set; }
    public ManifestData PreviousPackageInfo { get; set; }

    private ManifestData GetManifest(string packagePath)
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
        manifest.Path = packagePath;

        return manifest;
    }

    public void Initialize(string packagePath)
    {
        // First, get the manifest for the default build
        this.ProjectPackageInfo = GetManifest(packagePath);

        // Then, publish the package locally to get an actual snapshot of what we will publish
        var publishPackagePath = PublishPackage(packagePath);
        this.PublishPackageInfo = GetManifest(publishPackagePath);

        // Get Previous Version of the package
        // var previousPackagePath = GetPreviousPackage(this.ProjectPackageInfo);
        // if (!string.IsNullOrEmpty(previousPackagePath))
        //     this.PublishPackageInfo = GetManifest(previousPackagePath);

    }

    private string PublishPackage(string projectPackagePath)
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
        var request = Client.Search(projectPackageInfo.Path);
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
