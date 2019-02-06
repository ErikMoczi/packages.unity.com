

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#pragma warning disable 649
#pragma warning disable 414

namespace Unity.Tiny
{
    [InitializeOnLoad]
    internal static class TinyPackageUtility
    {
        private static readonly PackageInfo s_Pkg;
        private static readonly bool s_Registered;

        static TinyPackageUtility()
        {
            if (s_Registered)
            {
                return;
            }
            try
            {
                var realPath = Path.Combine(Path.GetFullPath(TinyConstants.PackagePath), "package.json");
                var packageJson = File.ReadAllText(realPath);

                var package = new PackageJson();
                EditorJsonUtility.FromJsonOverwrite(packageJson, package);

                // TODO: would be nice to get this information from Unity.PackageManager
                s_Pkg = new PackageInfo()
                {
                    version = package.version,

                    preview =
                        package.version.StartsWith("0.") ||
                        package.version.Contains("preview") ||
                        package.version.Contains("experimental"),

                    embedded = !realPath.Contains(TinyConstants.PackageName + "@")
                };
            }
            catch (Exception e)
            {
                TraceError(e.ToString());

                s_Pkg = new PackageInfo()
                {
                    version = "error",
                    preview = false,
                    embedded = false
                };
            }

            s_Registered = true;
        }

        public static bool IsTinyPackageEmbedded => s_Pkg.embedded;
        public static PackageInfo Package => s_Pkg;

        /// <summary>
        /// Returns whether or not the given path string is located under the Unity project folder.
        /// If a Package path is given, it'll return false, unless it's an embedded package (physically
        /// under the project folder).
        /// </summary>
        public static bool IsProjectPath(string path)
        {
            return path == null || Path.GetFullPath(path).StartsWith(Path.GetFullPath("."));
        }

        /// <summary>
        /// If the given path string is under the current Unity project folder, return it's Unity representation:
        /// relative to the project folder, with forward slashes, that can be used with AssetDatabase.
        /// Otherwise, return its absolute, OS-formatted path.
        /// </summary>
        public static string GetUnityOrOSPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            if (IsProjectPath(path))
            {
                //Going through GetFullPath will handle local folder renaming (package.name -> package.name-#.#-preview)
                return Path.GetFullPath(path).Substring(Path.GetFullPath(".").Length + 1).ToForwardSlash();
            }
            
            return Path.GetFullPath(path).ToForwardSlash();
        }

        [Serializable]
        private class PackageJson
        {
            public string name;
            public string displayName;
            public string version;
            public string unity;
            public string description;
            public string[] keywords;
            public Dictionary<string, string> dependencies;
        }

        private static void TraceError(string message)
        {
            message = "Tiny: " + message;
#if UNITY_TINY_INTERNAL
            Debug.LogError(message);
#else
            Console.WriteLine(message);
            #endif
        }

        [Serializable]
        internal struct PackageInfo
        {
            public string version;
            public bool preview;
            public bool embedded;

            public static PackageInfo Default => s_Pkg;
        }
    }
}

