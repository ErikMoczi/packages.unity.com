using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageJsonWithSamples
    {
        public List<PackageSample> samples = new List<PackageSample>();
    }

    [Serializable]
    internal class PackageSample
    {
        public string displayName = "";
        public string description = "";
        public string path = "";
        public string resolvedPath = "";
        public string importPath = "";
        public bool interactiveImport = true;

        public static List<PackageSample> LoadSamplesFromPackageJson(string packagePath)
        {
            var packageJsonPath = Path.Combine(packagePath, "package.json");
            if (!File.Exists(packageJsonPath))
                throw new FileNotFoundException(packageJsonPath);
            var packageJsonText = File.ReadAllText(packageJsonPath);
            var packageJson = JsonUtility.FromJson<PackageJsonWithSamples>(packageJsonText);
            return packageJson.samples;
        }

        public void ImportToAssets()
        {
            string[] unityPackages;
            if ((unityPackages = Directory.GetFiles(resolvedPath, "*.unitypackage")).Length == 1)
                AssetDatabase.ImportPackage(unityPackages[0], interactiveImport);
            else
            {
                IOUtils.DirectoryCopy(resolvedPath, importPath);
                AssetDatabase.Refresh();
                
                // Highlight import path
                var importRelativePath = importPath.Replace(Application.dataPath, "Assets");
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(importRelativePath, typeof(UnityEngine.Object));
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
        }

        public bool IsImportedToAssets
        {
            get { return !string.IsNullOrEmpty(importPath) && Directory.Exists(importPath); }
        }

        public string[] MismatchedVersions
        {
            get
            {
                var result = new List<string>();
                var importDirectoryInfo = new DirectoryInfo(importPath);
                var versionDirs = importDirectoryInfo.Parent.Parent.GetDirectories();
                foreach (var d in versionDirs)
                {
                    if (d.Name == importDirectoryInfo.Parent.Name)
                        continue;
                    var p = Path.Combine(d.ToString(), importDirectoryInfo.Name);
                    if (Directory.Exists(p))
                        result.Add(p);
                }
                return result.ToArray();
            }
        }

        public string Size
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
                    len = len/1024;
                }
                return string.Format("{0:0.##} {1}", len, sizes[order]);
            }
        }
    }
}