using System;
using System.IO;
using System.Threading;
using UnityEditor.PackageManager.ValidationSuite;
using UnityEngine;
using UnityEngine.Networking;

public enum ValidationType
{
    PackageManager,
    AssetStore
}

/// <summary>
/// Class containing package data required for vetting.
/// </summary>
internal class VettingContext
{
    internal class ManifestData
    {
        public string path = "";
        public string name = "";
        public string displayName = "";
        public string description = "";
        public string unity = "";
        public string version = "";
    }

    public ManifestData ProjectPackageInfo { get; set; }
    public ManifestData PublishPackageInfo { get; set; }
    public ManifestData PreviousPackageInfo { get; set; }
    public string PreviousPackageBinaryDirectory { get; set; }
    public ValidationType ValidationType { get; set; }
    public const string PreviousVersionBinaryPath = "Temp/ApiValidationBinaries";

    public static VettingContext CreatePackmanContext(string packagePath, bool isEmbedded)
    {
        VettingContext context = new VettingContext();

        // First, get the manifest for the default build
        context.ProjectPackageInfo = GetManifest(packagePath);

        // Then, publish the package locally to get an actual snapshot of what we will publish
		if (isEmbedded)
        {
            var publishPackagePath = PublishPackage(packagePath);
            context.PublishPackageInfo = GetManifest(publishPackagePath);
        }
        else
        {
            context.PublishPackageInfo = context.ProjectPackageInfo;
        }

        // Get Previous Version of the package
#if UNITY_2018_2_OR_NEWER
        var previousPackagePath = GetPreviousPackage(context.ProjectPackageInfo);
        if (!string.IsNullOrEmpty(previousPackagePath))
        {
            context.PreviousPackageInfo = GetManifest(previousPackagePath);
            context.DownloadAssembliesForPreviousVersion();
        }
#else
        context.PreviousPackageInfo = null;
#endif

        context.ValidationType = ValidationType.PackageManager;
        return context;
    }

    public static VettingContext CreateAssetStoreContext(string packagePath, string previousPackagePath)
    {
        VettingContext context = new VettingContext();
		context.ProjectPackageInfo = new ManifestData () { path = packagePath };
		context.PublishPackageInfo = new ManifestData () { path = packagePath };
		context.PreviousPackageInfo = new ManifestData () { path = previousPackagePath };
        context.ValidationType = ValidationType.AssetStore;
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

        var packageId = packageInfo.name + "@" + packageInfo.version;
        var publishPackagePath = Path.Combine(tempPath, "publish-" + packageId);
        return Utilities.ExtractPackage(packageName, tempPath, publishPackagePath, packageInfo.name);
    }

    private static string GetPreviousPackage(ManifestData projectPackageInfo)
    {
		#if UNITY_2018_1_OR_NEWER

        // List out available versions for a package.
        var request = Client.Search(projectPackageInfo.name);
        while (!request.IsCompleted)
        {
            System.Threading.Thread.Sleep(100);
        }

        // If it exists, get the last one from that list.
        if (request.Result != null && request.Result.Length > 0)
        {
            var packageInfo = request.Result[0];
            var previousVersion = packageInfo.versions.compatible.LastOrDefault(v => SemVersion.Parse(v) < SemVersion.Parse(projectPackageInfo.version));

            if (previousVersion != null)
            {
                try
                {
                    var tempPath = Path.GetTempPath();
                    var packageId = projectPackageInfo.name + "@" + previousVersion;
                    var previousPackagePath = Path.Combine(tempPath, "previous-" + packageId);
                    var packageName = Utilities.DownloadPackage("https://packages.unity.com/", packageId, tempPath);
                    Utilities.ExtractPackage(packageName, tempPath, previousPackagePath, projectPackageInfo.name);
                    return previousPackagePath;
                }
                catch (Exception)
                {
                    EditorUtility.DisplayDialog("Data", "Failed", "ok");
                }
            }
        }
		#endif
        return string.Empty;
    }

    private void DownloadAssembliesForPreviousVersion()
    {
        if (Directory.Exists(PreviousVersionBinaryPath))
            Directory.Delete(PreviousVersionBinaryPath, true);

        Directory.CreateDirectory(PreviousVersionBinaryPath);

#if UNITY_2018_1_OR_NEWER

        var packageDataZipFilename = PackageBinaryZipping.PackageDataZipFilename(PreviousPackageInfo.name, PreviousPackageInfo.version);
        var zipPath = Path.Combine(PreviousVersionBinaryPath, packageDataZipFilename);
        var uri = Path.Combine("https://artifactory.eu-cph-1.unityops.net/pkg-api-validation", packageDataZipFilename);

        UnityWebRequest request = new UnityWebRequest(uri);
        request.downloadHandler = new DownloadHandlerFile(zipPath);
        var operation = request.SendWebRequest();
        while (!operation.isDone)
            Thread.Sleep(1);

        if (request.isHttpError || request.isNetworkError || !PackageBinaryZipping.Unzip(zipPath, PreviousVersionBinaryPath))
        {
            Debug.Log(String.Format("Could not download binaries for previous package version from {0}. {1}", uri, request.error));
            PreviousPackageBinaryDirectory = null;
        }
        else
            PreviousPackageBinaryDirectory = PreviousVersionBinaryPath;
#endif
    }
}
