using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageWithSamplesJsonHelper
    {
        public List<SampleJsonHelper> samples = new List<SampleJsonHelper>();
    }

    [Serializable]
    internal class SampleJsonHelper
    {
        public string displayName = "";
        public string description = "";
        public string path = "";
        public string resolvedPath = "";
        public string importPath = "";
        public bool interactiveImport = true;

        internal static List<SampleJsonHelper> LoadSamplesFromPackageJson(string packagePath)
        {
            var packageJsonPath = System.IO.Path.Combine(packagePath, "package.json");
            if (!File.Exists(packageJsonPath))
                throw new FileNotFoundException(packageJsonPath);
            var packageJsonText = File.ReadAllText(packageJsonPath);
            var packageJson = JsonUtility.FromJson<PackageWithSamplesJsonHelper>(packageJsonText);
            return packageJson.samples;
        }
    }

    /// <summary>
    /// Struct for Package Sample
    /// </summary>
    [Serializable]
    public struct Sample
    {
        /// <summary>
        /// Sample import options
        /// </summary>
        [Flags]
        public enum ImportOptions
        {
            ///<summary>None</summary>
            None = 0x0,
            ///<summary>Override previous imports of the sample</summary>
            OverridePreviousImports = 0x1,
            ///<summary>Hide the import window when importing a sample that is an asset package (a .unitypackage file)</summary>
            HideImportWindow = 0x2
        }


        /// <value>
        /// The display name of the package sample
        /// </value>
        public string displayName { get; private set; }

        /// <value>
        /// The description of the package sample
        /// </value>
        public string description { get; private set; }

        /// <value>
        /// <para>The full path to where the sample is on disk, inside the package that contains the sample.</para>
        /// It is usually in the form of `Resolved Full Path to Package/Samples~/Sample Display Name/`
        /// </value>
        public string resolvedPath { get; private set; }

        /// <value>
        /// <para>The full path to where the sample will be imported, under the project assets folder.</para>
        /// <para>It is in the form of `Project Full Path/Assets/Samples/Package Display Name/Package Version/Sample Display Name/`.</para>
        /// If the sample is an asset package (a .unitypackage file), this value won't be taken into consideration during import
        /// </value>
        public string importPath { get; private set; }

        /// <value>
        /// Indicates whether to show the import window when importing a sample that is an asset package (a .unitypackage file)
        /// </value>
        public bool interactiveImport { get; private set; }

        /// <value>
        /// Indicates if the sample has already been imported
        /// </value>
        public bool isImported
        {
            get { return !string.IsNullOrEmpty(importPath) && Directory.Exists(importPath); }
        }

        internal Sample(SampleJsonHelper sample)
        {
            this.displayName = sample.displayName;
            this.description = sample.description;
            this.resolvedPath = sample.resolvedPath;
            this.importPath = sample.importPath;
            this.interactiveImport = sample.interactiveImport;
        }

        internal Sample(string displayName, string description, string resolvedPath, string importPath, bool interactiveImport)
        {
            this.displayName = displayName;
            this.description = description;
            this.resolvedPath = resolvedPath;
            this.importPath = importPath;
            this.interactiveImport = interactiveImport;
        }

        /// <summary>
        /// Given a package of a specific version, find a list of samples in that package.
        /// </summary>
        /// <param name="packageName">The name of the package</param>
        /// <param name="packageVersion">The version of the package</param>
        /// <returns>A list of samples in the given package</returns>
        public static IEnumerable<Sample> FindByPackage(string packageName, string packageVersion)
        {
            var package = PackageCollection.packages[packageName];
            if (package != null)
            {
                var packageInfo = package.Current;
                if (!string.IsNullOrEmpty(packageVersion))
                    packageInfo = package.Versions.FirstOrDefault(v => v.Version == packageVersion);
                if (packageInfo != null)
                    return packageInfo.Samples;
            }
            return new List<Sample>();
        }

        /// <summary>
        /// Imports the package sample into the `Assets` folder.
        /// </summary>
        /// <param name="options">
        /// <para>Custom import options. See <see cref="UnityEditor.PackageManager.UI.Sample.ImportOptions"/> for more information.</para>
        /// Note that <see cref="UnityEditor.PackageManager.UI.Sample.ImportOptions"/> are flag attributes,
        /// therefore you can set multiple import options using the `|` operator
        /// </param>
        /// <returns>Returns whether the import is successful</returns>
        public bool Import(ImportOptions options = ImportOptions.None)
        {
            string[] unityPackages;
            var interactive = (options & ImportOptions.HideImportWindow) != ImportOptions.None ? false : interactiveImport;
            if ((unityPackages = Directory.GetFiles(resolvedPath, "*.unitypackage")).Length == 1)
                AssetDatabase.ImportPackage(unityPackages[0], interactive);
            else
            {
                var prevImports = PreviousImports;
                if (prevImports.Count > 0 && (options & ImportOptions.OverridePreviousImports) == ImportOptions.None)
                    return false;
                foreach (var v in prevImports)
                    IOUtils.RemovePathAndMeta(v, true);

                IOUtils.DirectoryCopy(resolvedPath, importPath);
                AssetDatabase.Refresh();
            }
            return true;
        }

        internal List<string> PreviousImports
        {
            get
            {
                var result = new List<string>();
                if (!string.IsNullOrEmpty(importPath))
                {
                    var importDirectoryInfo = new DirectoryInfo(importPath);
                    if (importDirectoryInfo.Parent.Parent.Exists)
                    {
                        var versionDirs = importDirectoryInfo.Parent.Parent.GetDirectories();
                        foreach (var d in versionDirs)
                        {
                            var p = System.IO.Path.Combine(d.ToString(), importDirectoryInfo.Name);
                            if (Directory.Exists(p))
                                result.Add(p);
                        }
                    }
                }
                return result;
            }
        }

        internal string Size
        {
            get
            {
                if (string.IsNullOrEmpty(resolvedPath) || !Directory.Exists(resolvedPath))
                    return "0 KB";
                var sizeInBytes = IOUtils.DirectorySizeInBytes(resolvedPath);
                string[] sizes = { "KB", "MB", "GB", "TB" };
                double len = sizeInBytes / 1024.0;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                return string.Format("{0:0.##} {1}", len, sizes[order]);
            }
        }
    }
}
