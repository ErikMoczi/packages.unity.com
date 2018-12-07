using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using Semver;
using UnityEditor.Sprites;
using UnityEngine;

#if UNITY_2018_1_OR_NEWER
using UnityEditor.Compilation;
#endif

namespace UnityEditor.PackageManager.ValidationSuite
{
    internal static class Utilities
    {
        internal const string PackageJsonFilename = "package.json";
        internal const string ChangeLogFilename = "CHANGELOG.md";
        internal const string EditorAssemblyDefintionSuffix = ".Editor.asmdef";
        internal const string EditorTestsAssemblyDefintionSuffix = ".EditorTests.asmdef";
        internal const string RuntimeAssemblyDefintionSuffix = ".Runtime.asmdef";
        internal const string RuntimeTestsAssemblyDefintionSuffix = ".RuntimeTests.asmdef";

        public static bool NetworkNotReachable{ get { return Application.internetReachability == NetworkReachability.NotReachable; } }

        public static string CreatePackageId(string name, string version)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
                throw new ArgumentNullException("Both name and version must be specified.");

            return string.Format("{0}@{1}", name, version);
        }

        public static bool IsPreviewVersion(string version)
        {
            var semVer = SemVersion.Parse(version);
            return semVer.Prerelease.Contains("preview") || semVer.Major == 0;
        }

        internal static T GetDataFromJson<T>(string jsonFile)
        {
            return JsonUtility.FromJson<T>(File.ReadAllText(jsonFile));
        }

        internal static string CreatePackage(string path, string workingDirectory)
        {
            //No Need to delete the file, npm pack always overwrite: https://docs.npmjs.com/cli/pack
            var packagePath =  Path.Combine(Path.Combine(Application.dataPath, ".."), path);

            var launcher = new NodeLauncher();
            launcher.WorkingDirectory = workingDirectory;
            launcher.NpmPack(packagePath);

            var packageName = launcher.OutputLog.ToString().Trim();
            return packageName;
        }

        internal static PackageManager.PackageInfo[] UpmSearch(string packageIdOrName = null, bool throwOnRequestFailure = false)
        {
            var request = string.IsNullOrEmpty(packageIdOrName) ? Client.SearchAll() : Client.Search(packageIdOrName);
            while (!request.IsCompleted)
            {
                if (Utilities.NetworkNotReachable)
                    throw new Exception("Failed to fetch package infomation: network not reachable");
                System.Threading.Thread.Sleep(100);
            }
            if (throwOnRequestFailure && request.Status == StatusCode.Failure)
                throw new Exception("Failed to fetch package infomation.  Error details: " + request.Error.errorCode + " " + request.Error.message);
            return request.Result;
        }

        internal static PackageManager.PackageInfo[] UpmListOffline(string packageIdOrName = null)
        {
            var request = Client.List(true);
            while (!request.IsCompleted)
                System.Threading.Thread.Sleep(100);
            var result = new List<PackageManager.PackageInfo>();
            foreach (var upmPackage in request.Result)
            {
                if (!string.IsNullOrEmpty(packageIdOrName) && !(upmPackage.name == packageIdOrName || upmPackage.packageId == packageIdOrName))
                    continue;
                result.Add(upmPackage);
            }
            return result.ToArray();
        }

        internal static string DownloadPackage(string packageId, string workingDirectory)
        {
            //No Need to delete the file, npm pack always overwrite: https://docs.npmjs.com/cli/pack
            var launcher = new NodeLauncher();
            launcher.WorkingDirectory = workingDirectory;
            launcher.NpmRegistry = NodeLauncher.ProductionRepositoryUrl;

            try
            {
                launcher.NpmPack(packageId);
            }
            catch (ApplicationException exception)
            {
                exception.Data["code"] = "fetchFailed";
                throw exception;
            }

            var packageName = launcher.OutputLog.ToString().Trim();
            return packageName;
        }

        internal static bool PackageExistsOnProduction(string packageId)
        {
            var launcher = new NodeLauncher();
            launcher.NpmRegistry = NodeLauncher.ProductionRepositoryUrl;

            try
            {
                launcher.NpmView(packageId);
            }
            catch (ApplicationException exception)
            {
                if (exception.Message.Contains("npm ERR! code E404") && exception.Message.Contains("is not in the npm registry."))
                    return false;
                exception.Data["code"] = "fetchFailed";
                throw exception;
            }

            var packageData = launcher.OutputLog.ToString().Trim();
            return !string.IsNullOrEmpty(packageData);
        }

        internal static string ExtractPackage(string packageFileName, string workingPath, string outputDirectory, string packageName)
        {
            //verify if package exists
            if(!packageFileName.EndsWith(".tgz"))
                throw new ArgumentException("Package should be a .tgz file");

            var fullPackagePath = Path.Combine(workingPath, packageFileName);
            var modulePath = Path.Combine(workingPath, "node_modules");
            if (!File.Exists(fullPackagePath))
                throw new FileNotFoundException(fullPackagePath + " was not found.");

            //Clean node_modules if it exists
            if (File.Exists(modulePath))
            {
                try{
                    Directory.Delete(modulePath, true);
                } catch(Exception e) {
                    throw e;
                }
            }
            
            var launcher = new NodeLauncher();
            launcher.NpmLogLevel = "error";
            launcher.NpmRegistry = NodeLauncher.ProductionRepositoryUrl;
            launcher.WorkingDirectory = workingPath;
            launcher.NpmInstall(packageFileName);
            
            var extractedPackagePath = Path.Combine(modulePath, packageName);
            if(!Directory.Exists(extractedPackagePath))
                throw new DirectoryNotFoundException(extractedPackagePath + " was not found.");

            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);

            Directory.Move(extractedPackagePath, outputDirectory);
            return outputDirectory;

        }

#if UNITY_2018_1_OR_NEWER

        public static bool IsTestAssembly(Assembly assembly)
        {
            return assembly.allReferences.Contains("TestAssemblies");
        }

        /// <summary>
        /// Returns the Assembly instances which contain one or more scripts in a package, given the list of files in the package.
        /// </summary>
        public static IEnumerable<Assembly> AssembliesForPackage(Assembly[] assemblies, IEnumerable<string> filesInPackage)
        {
            var assemblyNames = new HashSet<string>();
            foreach (var path in filesInPackage)
            {
                if (!string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                var assemblyName = CompilationPipeline.GetAssemblyNameFromScriptPath(path);
                if (assemblyName != null)
                    assemblyNames.Add(assemblyName.Replace(".dll", ""));

                if (string.Equals(".asmdef", Path.GetExtension(path), StringComparison.OrdinalIgnoreCase))
                {
                    var assemblyDefinition = GetDataFromJson<AssemblyDefinition>(path);
                    if (string.IsNullOrEmpty(assemblyDefinition.name))
                        throw new ArgumentException(path + " does not have a name field");

                    assemblyNames.Add(assemblyDefinition.name);
                }
            }
            return assemblies.Where(a => assemblyNames.Contains(a.name));
        }
#endif
    }
}
