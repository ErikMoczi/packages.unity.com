using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Sprites;
using UnityEngine;

#if UNITY_2018_1_OR_NEWER
using UnityEditor.Compilation;
#endif

namespace UnityEditor.PackageManager.ValidationSuite
{
    internal class Utilities
    {
        internal const string PackageJsonFilename = "package.json";
        internal const string ChangeLogFilename = "CHANGELOG.md";
        internal const string EditorAssemblyDefintionSuffix = ".Editor.asmdef";
        internal const string EditorTestsAssemblyDefintionSuffix = ".EditorTests.asmdef";
        internal const string RuntimeAssemblyDefintionSuffix = ".Runtime.asmdef";
        internal const string RuntimeTestsAssemblyDefintionSuffix = ".RuntimeTests.asmdef";
        internal const string ProductionRepositoryUrl = "https://packages.unity.com/";

        internal static T GetDataFromJson<T>(string jsonFile)
        {
            return JsonUtility.FromJson<T>(File.ReadAllText(jsonFile));
        }

        internal static string CreatePackage(string path, string workingDirectory)
        {
            //No Need to delete the file, npm pack always overwrite: https://docs.npmjs.com/cli/pack
            var packagePath =  Path.Combine(Path.Combine(Application.dataPath, ".."), path);

            var launcher = new NpmLauncher();
            launcher.WorkingDirectory = workingDirectory;
            launcher.Pack(packagePath);

            var packageName = launcher.OutputLog.ToString().Trim();
            return packageName;
        }

        internal static string DownloadPackage(string packageId, string workingDirectory)
        {
            //No Need to delete the file, npm pack always overwrite: https://docs.npmjs.com/cli/pack
            var launcher = new NpmLauncher();
            launcher.WorkingDirectory = workingDirectory;
            launcher.Registry = NpmLauncher.ProductionRepositoryUrl;

            try
            {
                launcher.Pack(packageId);
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
            var launcher = new NpmLauncher();
            launcher.Registry = NpmLauncher.ProductionRepositoryUrl;

            try
            {
                launcher.View(packageId);
            }
            catch (ApplicationException exception)
            {
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
            
            var launcher = new NpmLauncher();
            launcher.LogLevel = "error";
            launcher.WorkingDirectory = workingPath;
            launcher.Install(packageFileName);
            
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

        public static string GetNormalizedRelativePath(string path)
        {
            var baseDirectory = new Uri(Path.GetDirectoryName(Application.dataPath) + "/");
            var relativeUri = baseDirectory.MakeRelativeUri(new Uri(path));
            return relativeUri.ToString();
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
