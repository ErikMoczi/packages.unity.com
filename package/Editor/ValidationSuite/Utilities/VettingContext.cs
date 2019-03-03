using System;
using System.Collections.Generic;
using System.ComponentModel;
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


public class PackageDependencyInfo
{
    public string DependencyVersion { get; set; }

    public string ParentName { get; set; }

    public string ParentVersion { get; set; }

    public bool ParentIsVerified { get; set; }

    public bool ParentIsPreview { get; set; }
}

public class RelatedPackage
{
    public string Name;
    public string Version;
    public string Path;

    public RelatedPackage(string name, string version, string path)
    {
        Name = name;
        Version = version;
        Path = path;
    }
}


/// <summary>
/// Class containing package data required for vetting.
/// </summary>
internal class VettingContext
{
    [Serializable]
    internal class SampleData
    {
        public string displayName = "";
        public string description = "";
        public string path = "";
    }

    internal class ManifestData
    {
        public string path = "";
        public string name = "";
        public string displayName = "";
        public string description = "";
        public string unity = "";
        public string version = "";
        public string type = "";
        public List<SampleData> samples = new List<SampleData>();
        public Dictionary<string, string> repository = new Dictionary<string, string>();
        public Dictionary<string, string> dependencies = new Dictionary<string, string>();
        public Dictionary<string, string> relatedPackages = new Dictionary<string, string>();

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

    public bool IsCore { get; set; }

    public ManifestData ProjectPackageInfo { get; set; }
    public ManifestData PublishPackageInfo { get; set; }
    public ManifestData PreviousPackageInfo { get; set; }
    public Dictionary<string, List<PackageDependencyInfo>> PackageCoDependencies { get; set; }

    public string PreviousPackageBinaryDirectory { get; set; }
    public ValidationType ValidationType { get; set; }
    public const string PreviousVersionBinaryPath = "Temp/ApiValidationBinaries";
    public List<RelatedPackage> relatedPackages = new List<RelatedPackage>();

    public static VettingContext CreatePackmanContext(string packageId, ValidationType validationType)
    {
        VettingContext context = new VettingContext();
        var packageParts = packageId.Split('@');
        var packageInfo = Utilities.UpmListOffline().SingleOrDefault(p => p.name == packageParts[0] && p.version == packageParts[1]);

        if (packageInfo == null)
        {
            throw new ArgumentException("Package Id " + packageId + " is not part of this project.");
        }

#if UNITY_2019_1_OR_NEWER
        context.IsCore = packageInfo.source == PackageSource.BuiltIn && packageInfo.type != "module";
#else
        context.IsCore = false; // there are no core packages before 2019.1
#endif
        context.ValidationType = validationType;
        context.ProjectPackageInfo = GetManifest(packageInfo.resolvedPath);

        if (context.ValidationType == ValidationType.LocalDevelopment)
        {
            var publishPackagePath = PublishPackage(context);
            context.PublishPackageInfo = GetManifest(publishPackagePath);
        }
        else
        {
            context.PublishPackageInfo = GetManifest(packageInfo.resolvedPath);
        }


#if UNITY_2019_1_OR_NEWER
        foreach (var relatedPackage in context.PublishPackageInfo.relatedPackages)
        {
            // Check to see if the package is available locally
            // We are only focusing on local packages to avoid validation suite failures in CI
            // when the situation arises where network connection is impaired
            var foundRelatedPackage = Utilities.UpmListOffline().Where(p => p.name.Equals(relatedPackage.Key));
            var relatedPackageInfo = foundRelatedPackage.ToList();
            if (!relatedPackageInfo.Any())
            {
                Debug.Log(String.Format("Cannot find the relatedPackage {0} ", relatedPackage.Key));
                continue;
            }
            context.relatedPackages.Add(new RelatedPackage(relatedPackage.Key, relatedPackage.Value,
                relatedPackageInfo.First().resolvedPath));
        }
#endif

#if UNITY_2018_1_OR_NEWER
        // No need to compare against the previous version of the package if we're testing out the verified set.
        if (context.ValidationType == ValidationType.VerifiedSet)
        {
            var previousPackagePath = GetPreviousPackage(context.ProjectPackageInfo);
            if (!string.IsNullOrEmpty(previousPackagePath))
            {
                context.PreviousPackageInfo = GetManifest(previousPackagePath);
                context.DownloadAssembliesForPreviousVersion();
            }

#if UNITY_2018_2_OR_NEWER
            context.PackageCoDependencies = BuildPackageDependencyTree(context.ProjectPackageInfo);
#endif
        }
        else
        {
            context.PreviousPackageInfo = null;
            context.PackageCoDependencies = new Dictionary<string, List<PackageDependencyInfo>>();
        }
#else
        context.PreviousPackageInfo = null;
#endif
        return context;
    }

    public static VettingContext CreateAssetStoreContext(string packageName, string packageVersion, string packagePath, string previousPackagePath)
    {
        VettingContext context = new VettingContext();
        context.ProjectPackageInfo = new ManifestData() { path = packagePath, name = packageName, version = packageVersion};
        context.PublishPackageInfo = new ManifestData() { path = packagePath, name = packageName, version = packageVersion };
        context.PreviousPackageInfo = string.IsNullOrEmpty(previousPackagePath) ? null : new ManifestData() { path = previousPackagePath, name = packageName, version = "Previous" };
        context.ValidationType = ValidationType.AssetStore;
        return context;
    }

    internal VersionChangeType VersionChangeType
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
        manifest.relatedPackages = ParseDictionary(textManifestData, "relatedPackages");
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

    private static string PublishPackage(VettingContext context)
    {
        var packagePath = context.ProjectPackageInfo.path;
        if (context.ProjectPackageInfo.IsProjectTemplate)
        {
            return packagePath;
        }
        else
        {
            var tempPath = System.IO.Path.GetTempPath();
            string packageName = context.ProjectPackageInfo.Id.Replace("@", "-") + ".tgz";

            //Use upm-template-tools package-ci
            PackageCIUtils.Pack(packagePath, tempPath);

            // Create a NodeLauncher object that will handle the installation of the
            // package to validate
            NodeLauncher launcher;

            if (GetDependencyPackages(context.ProjectPackageInfo.dependencies, packagePath, tempPath, false))
            {
                //Create a new launcher without an npmPrefix to use installed dependencies
                launcher = new NodeLauncher(tempPath, npmPrefix: "", npmRegistry: "");
            }
            else
            {
                //Create the launcher with an npmPrefix so that modules are installed correctly
                launcher = new NodeLauncher(tempPath, npmPrefix: ".");
            }

            var publishPackagePath = Path.Combine(tempPath, "publish-" + context.ProjectPackageInfo.Id);
            return Utilities.ExtractPackage(packageName, tempPath, publishPackagePath, context.ProjectPackageInfo.name, launcher);
        }
    }

    private static bool GetDependencyPackages(Dictionary<string, string> packages, string packagePath, string workingDirectory = "", bool runValidation = false)
    {
        NodeLauncher launcher = new NodeLauncher(workingDirectory);
        bool createdLocalDependencies = false;

        foreach (var package in packages)
        {
            var relatedPackagePath = "";
            var offlineFoundPackages = Utilities.UpmListOffline(package.Key);
            if (offlineFoundPackages.Any())
            {
                if (offlineFoundPackages[0].source == PackageSource.Embedded)
                {
                    string packageId = offlineFoundPackages[0].name + "@" + offlineFoundPackages[0].version;
                    //Create the context of the package, this also publishes (packages) the package
                    VettingContext packageContext = CreatePackmanContext(packageId, ValidationType.LocalDevelopment);

                    if (runValidation)
                    {
                        if (!ValidationSuite.ValidatePackage(packageContext, ValidationType.LocalDevelopment))
                        {
                            //Package is invalid.
                            throw new ApplicationException("Validation check failed for dependent package " + packageId);
                        }
                    }

                    // Since the packge is local, we need to install it.
                    string packageFilePath = Path.Combine(workingDirectory, packageId.Replace("@", "-") + ".tgz");
                    launcher.NpmInstall(packageFilePath);
                    createdLocalDependencies = true;
                    continue;
                }
                else
                {
                    continue;
                }
            }

            if (Utilities.PackageExistsOnProduction(package.Key))
            {
                var tempPath = Path.GetTempPath();
                var packageFileName = Utilities.DownloadPackage(package.Key, tempPath);
                var packagesPath = Path.Combine(Directory.GetParent(packagePath).ToString(), package.Key);
                relatedPackagePath = Utilities.ExtractPackage(packageFileName, tempPath, packagesPath, package.Key);
            }

            if (relatedPackagePath.Equals(""))
            {
                Debug.Log("Cannot find the package " + package.Key + " locally or remotely");
            }
        }

        return createdLocalDependencies;
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
            var previousVersions = packageInfo.versions.all.Where(v =>
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
