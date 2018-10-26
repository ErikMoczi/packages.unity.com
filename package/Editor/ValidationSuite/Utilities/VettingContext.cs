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

public class PackageDependencyInfo
{
    public string DependencyVersion { get; set; }

    public string ParentName { get; set; }

    public string ParentVersion { get; set; }

    public bool ParentIsVerified { get; set; }

    public bool ParentIsPreview { get; set; }
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
        public string type = "";
        public string gitHead = "";
        public Dictionary<string, string> repository = new Dictionary<string, string>();
        public Dictionary<string, string> dependencies = new Dictionary<string, string>();

        public bool IsPreview
        {
            get { return version.ToLower().Contains("-preview"); }
        }

        public bool IsProjectTemplate
        {
            get { return type.Equals("template", StringComparison.InvariantCultureIgnoreCase); }
        }

        public string Id
        {
            get { return GetPackageId(name, version); }
        }

        public static string GetPackageId(string name, string version)
        {
            return name + "@" + version;
        }
    }

    public ManifestData ProjectPackageInfo { get; set; }
    public ManifestData PublishPackageInfo { get; set; }
    public ManifestData PreviousPackageInfo { get; set; }
    public Dictionary<string, List<PackageDependencyInfo>> PackageCoDependencies { get; set; }

    public string PreviousPackageBinaryDirectory { get; set; }
    public ValidationType ValidationType { get; set; }
    public const string PreviousVersionBinaryPath = "Temp/ApiValidationBinaries";

    public static VettingContext CreatePackmanContext(string packagePath, bool publishLocally)
    {
        VettingContext context = new VettingContext();

        // First, get the manifest for the default build
        context.ProjectPackageInfo = GetManifest(packagePath);

        // Then, publish the package locally to get an actual snapshot of what we will publish
        if (publishLocally)
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

#if UNITY_2018_2_OR_NEWER
        context.PackageCoDependencies = BuildPackageDependencyTree(context.ProjectPackageInfo);
#endif

        context.ValidationType = ValidationType.PackageManager;
        return context;
    }

    public static VettingContext CreateAssetStoreContext(string packageName, string packageVersion, string packagePath, string previousPackagePath)
    {
        VettingContext context = new VettingContext();
        context.ProjectPackageInfo = new ManifestData () { path = packagePath, name = packageName, version = packageVersion};
        context.PublishPackageInfo = new ManifestData () { path = packagePath, name = packageName, version = packageVersion };
        context.PreviousPackageInfo = string.IsNullOrEmpty(previousPackagePath) ? null : new ManifestData () { path = previousPackagePath, name = packageName, version = "Previous" };
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

            throw new ArgumentException("Previous version number " + PreviousPackageInfo.version + " is the same major/minor/patch version as the current package " + ProjectPackageInfo.version);
        }
    }

    public static ManifestData GetManifest(string packagePath)
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
        manifest.repository = ParseDictionary(textManifestData, "repository");

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

        var packageInfo = Utilities.GetDataFromJson<ManifestData>(Path.Combine(projectPackagePath, Utilities.PackageJsonFilename));
        var packageName = string.Empty;
        if (packageInfo.IsProjectTemplate)
        {
            var installedPackages = Utilities.UpmListOffline(packageInfo.name);
            if (installedPackages.Length > 0 && installedPackages[0].source == PackageSource.Embedded)
            {
                var projectPath = Path.Combine(projectPackagePath, "../../");
                // re-direct the package path to the folder where the converted project template will be
                projectPackagePath = Path.Combine(tempPath, "converted-" + packageInfo.Id);
                ProjectTemplateUtils.ConvertProjectToTemplate(projectPath, projectPackagePath);
            }
        }
        packageName = Utilities.CreatePackage(projectPackagePath, tempPath);
        var publishPackagePath = Path.Combine(tempPath, "publish-" + packageInfo.Id);
        return Utilities.ExtractPackage(packageName, tempPath, publishPackagePath, packageInfo.name);
    }

    private static string GetPreviousPackage(ManifestData projectPackageInfo)
    {
        #if UNITY_2018_1_OR_NEWER

        // List out available versions for a package.
        var foundPackages =  Utilities.UpmSearch(projectPackageInfo.name);

        // If it exists, get the last one from that list.
        if (foundPackages != null && foundPackages.Length > 0)
        {
            var packageInfo = foundPackages[0];
            var version = SemVersion.Parse(projectPackageInfo.version);
            var previousVersions = packageInfo.versions.compatible.Where(v =>
            {
                var prevVersion = SemVersion.Parse(v);
                // ignore pre-release and build tags when finding previous version
                return prevVersion < version && !(prevVersion.Major == version.Major && prevVersion.Minor == version.Minor && prevVersion.Patch == version.Patch);
            });

            // Find the last version on Production
            string previousVersion = null;
            previousVersions = previousVersions.Reverse();
            foreach (var prevVersion in previousVersions)
            {
                if (Utilities.PackageExistsOnProduction(packageInfo.name + "@" + prevVersion))
                {
                    previousVersion = prevVersion;
                    break;
                }
            }

            if (previousVersion != null)
            {
                try
                {
                    var previousPackageId = ManifestData.GetPackageId(projectPackageInfo.name, previousVersion);
                    var tempPath = Path.GetTempPath();
                    var previousPackagePath = Path.Combine(tempPath, "previous-" + previousPackageId);
                    var packageFileName = Utilities.DownloadPackage(previousPackageId, tempPath);
                    Utilities.ExtractPackage(packageFileName, tempPath, previousPackagePath, projectPackageInfo.name);
                    return previousPackagePath;
                }
                catch (Exception exception)
                {
                    // Failing to fetch when there is no prior version, which is an accepted case.
                    if ((string)exception.Data["reason"] == "fetchFailed")
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
        request.timeout = 60; // 60 seconds time out
        request.downloadHandler = new DownloadHandlerFile(zipPath);
        var operation = request.SendWebRequest();
        while (!operation.isDone)
            Thread.Sleep(1);

        if (request.isHttpError || request.isNetworkError || !PackageBinaryZipping.Unzip(zipPath, PreviousVersionBinaryPath))
        {
            Debug.Log(String.Format("Could not download binary assemblies for previous package version from {0}. {1}", uri, request.responseCode));
            PreviousPackageBinaryDirectory = null;
        }
        else
            PreviousPackageBinaryDirectory = PreviousVersionBinaryPath;
#endif
    }

#if UNITY_2018_2_OR_NEWER
    private static Dictionary<string, List<PackageDependencyInfo>> BuildPackageDependencyTree(ManifestData manifestData)
    {
        var packageCoDependencies = new Dictionary<string, List<PackageDependencyInfo>>();

        var foundPackages =  Utilities.UpmSearch(string.Empty, true);

        // Fill in the dictionary
        if (foundPackages != null && foundPackages.Length > 0)
        {
            packageCoDependencies[manifestData.name] = new List<PackageDependencyInfo>();
            if (manifestData.dependencies != null)
            {
                foreach (var dependency in manifestData.dependencies)
                {
                    packageCoDependencies[dependency.Key] = new List<PackageDependencyInfo>();
                }
            }

            foreach (var packageInfo in foundPackages)
            {
                // Check each of the packages dependencies against all other dependencies.
                foreach (var dependency in packageCoDependencies)
                {
                    var dependencyInfo = packageInfo.dependencies.SingleOrDefault(d => d.name == dependency.Key);
                    if (!string.IsNullOrEmpty(dependencyInfo.name) && Utilities.PackageExistsOnProduction(packageInfo.packageId))
                    {
                        packageCoDependencies[dependency.Key].Add(CreateDependencyInfo(dependencyInfo.version, packageInfo));
                    }
                }

                // Is there a verified version?  If so, if it is different than the returned version, let's make a call to retrieve it's data.
                if (!string.IsNullOrEmpty(packageInfo.versions.recommended) && packageInfo.versions.recommended != packageInfo.version)
                {
                    var foundVerifiedPackages = Utilities.UpmSearch(packageInfo.name + "@" + packageInfo.versions.recommended);
                    if (foundVerifiedPackages.Length > 0)
                    {
                        foreach (var dependency in packageCoDependencies)
                        {
                            var dependencyInfo = foundVerifiedPackages[0].dependencies.SingleOrDefault(d => d.name == dependency.Key);
                            if (!string.IsNullOrEmpty(dependencyInfo.name))
                            {
                                packageCoDependencies[dependency.Key].Add(CreateDependencyInfo(dependencyInfo.version, foundVerifiedPackages[0]));
                            }
                        }
                    }
                }
            }
        }

        return packageCoDependencies;
    }

    private static PackageDependencyInfo CreateDependencyInfo(string version, UnityEditor.PackageManager.PackageInfo packageInfo)
    {
        var semVer = SemVersion.Parse(packageInfo.version);

        return new PackageDependencyInfo()
        {
            DependencyVersion = version,
            ParentName = packageInfo.name,
            ParentVersion = packageInfo.version,
            ParentIsVerified = packageInfo.versions.recommended == packageInfo.version,
            ParentIsPreview = semVer.Prerelease.Contains("preview") || semVer.Major == 0
        };
    }
#endif
}
