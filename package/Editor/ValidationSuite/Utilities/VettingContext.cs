using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text.RegularExpressions;
using System.Threading;
using Semver;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;

using UnityEditor.PackageManager.ValidationSuite;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;

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
        public Dictionary<string, string> dependencies = new Dictionary<string, string>();

        public bool IsPreview
        {
            get { return version.ToLower().Contains("-preview"); }
        }
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
#if UNITY_2018_1_OR_NEWER
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
		context.PreviousPackageInfo = string.IsNullOrEmpty(previousPackagePath) ? null : new ManifestData () { path = previousPackagePath };
        context.ValidationType = ValidationType.AssetStore;
        return context;
    }

    public VersionChangeType VersionChangeType
    {
        get
        {
            if (PreviousPackageInfo == null || PreviousPackageInfo.version == null ||
                PreviousPackageInfo == null || PreviousPackageInfo.version == null)
            {
                return VersionChangeType.Unknown;
            }
            var prevVersion = SemVersion.Parse(PreviousPackageInfo.version);
            var curVersion = SemVersion.Parse(ProjectPackageInfo.version);

            if (curVersion.CompareByPrecedence(prevVersion) < 0)
                throw new ArgumentException("Previous version number comes after current version number");

            if (curVersion.Major > prevVersion.Major)
                return VersionChangeType.Major;
            if (curVersion.Minor > prevVersion.Minor)
                return VersionChangeType.Minor;
            if (curVersion.Patch > prevVersion.Patch)
                return VersionChangeType.Patch;

            throw new ArgumentException("Previous version number" + PreviousPackageInfo.version + " is the same major/minor/patch version as the current package " + ProjectPackageInfo.version);
        }
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
        manifest.dependencies = ParseDictionary(textManifestData, "dependencies");

        return manifest;
    }

    private static Dictionary<string, string> ParseDictionary(string json, string key)
    {
        string minified = new Regex("[\"\\s]").Replace(json, "");
        var regex = new Regex(key + ":{(.*?)}");
        MatchCollection matches = regex.Matches(minified);
        if (matches.Count == 0)
            return new Dictionary<string, string>();

        string match = matches[0].Groups[1].Value;    // Group 0 is full match, group 1 is capture group
        if (match.Length == 0)                        // Found empty dictionary {}
            return new Dictionary<string, string>();

        string[] keyValuePairs = match.Split(',');
        return keyValuePairs.Select(kvp => kvp.Split(':')).ToDictionary(k => k[0], v => v[1]);
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
                catch (Exception exception)
                {
                    // Failing to fetch when there is no prior version, which is an accepted case.
                    if (exception.Message != "Fetching package failed.")
                        EditorUtility.DisplayDialog("Data: " + exception.Message, "Failed", "ok");
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
            Debug.Log(String.Format("Could not download binaries for previous package version from {0}. {1}", uri, request.responseCode));
            PreviousPackageBinaryDirectory = null;
        }
        else
            PreviousPackageBinaryDirectory = PreviousVersionBinaryPath;
#endif
    }
}
