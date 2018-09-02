//#define BUILD_CACHE_DEBUG

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace UnityEditor.Build.Utilities
{
    public static class BuildCache
    {
        private const string kCachePath = "Library/BuildCache";

        public static string GetPathForCachedResults(Hash128 hash)
        {
            var file = hash.ToString();
            return string.Format("{0}/{1}/{2}/Results", kCachePath, file.Substring(0, 2), file);
        }

        public static string GetPathForCachedArtifacts(Hash128 hash)
        {
            var file = hash.ToString();
            return string.Format("{0}/{1}/{2}/Artifacts", kCachePath, file.Substring(0, 2), file);
        }

        [MenuItem("Window/Build Pipeline/Purge Build Cache", priority = 10)]
        public static void PurgeCache()
        {
            if (!EditorUtility.DisplayDialog("Purge Build Cache", "Do you really want to purge your entire build cache?", "Yes", "No"))
                return;

            if (Directory.Exists(kCachePath))
                Directory.Delete(kCachePath, true);
        }

        public static bool TryLoadCachedResults<T>(Hash128 hash, out T results)
        {
            var path = GetPathForCachedResults(hash);
            var filePath = string.Format("{0}/{1}", path, typeof(T).Name);
            if (!File.Exists(filePath))
            {
                BuildLogger.LogCache("false TryLoadCachedResults<{0}>({1}, out ...)", typeof(T).Name, hash.ToString());
                results = default(T);
                return false;
            }

            try
            {
                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    results = (T)formatter.Deserialize(stream);
                BuildLogger.LogCache("true TryLoadCachedResults<{0}>({1}, out ...)", typeof(T).Name, hash.ToString());
                return true;
            }
            catch (Exception e)
            {
                BuildLogger.LogCache("Exception TryLoadCachedResults<{0}>({1}, out ...)", typeof(T).Name, hash.ToString());
                BuildLogger.LogException(e);
                results = default(T);
                return false;
            }
        }

        public static bool TryLoadCachedArtifacts(Hash128 hash, out string[] artifactPaths, out string rootCachePath)
        {
            rootCachePath = GetPathForCachedArtifacts(hash);
            if (!Directory.Exists(rootCachePath))
            {
                BuildLogger.LogCache("false TryLoadCachedArtifacts({0}, out ..., out ...)", hash.ToString());
                artifactPaths = null;
                return false;
            }

            artifactPaths = Directory.GetFiles(rootCachePath, "*", SearchOption.AllDirectories);
            BuildLogger.Log("true TryLoadCachedArtifacts({0}, out ..., out ...)", hash.ToString());
            return true;
        }

        public static bool TryLoadCachedResultsAndArtifacts<T>(Hash128 hash, out T results, out string[] artifactPaths, out string rootCachePath)
        {
            BuildLogger.LogCache("TryLoadCachedResultsAndArtifacts<{0}({1}, out ..., out ..., out ...)", typeof(T).Name, hash.ToString());
            artifactPaths = null;
            rootCachePath = GetPathForCachedArtifacts(hash);
            if (!TryLoadCachedResults(hash, out results))
                return false;

            return TryLoadCachedArtifacts(hash, out artifactPaths, out rootCachePath);
        }

        public static bool SaveCachedResults<T>(Hash128 hash, T results)
        {
            var path = GetPathForCachedResults(hash);
            var filePath = string.Format("{0}/{1}", path, typeof(T).Name);

            try
            {
                Directory.CreateDirectory(path);
                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                    formatter.Serialize(stream, results);
            }
            catch (Exception e)
            {
                BuildLogger.LogCache("Exception SaveCachedResults<{0}>({1}, T ...)", typeof(T).Name, hash.ToString());
                BuildLogger.LogException(e);
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
                return false;
            }
            BuildLogger.LogCache("true SaveCachedResults<{0}>({1}, T ...)", typeof(T).Name, hash.ToString());
            return true;
        }

        public static bool SaveCachedArtifacts(Hash128 hash, string[] artifactPaths, string rootPath)
        {
            var path = GetPathForCachedArtifacts(hash);

            var result = true;
            try
            {
                Directory.CreateDirectory(path);
                foreach (var artifact in artifactPaths)
                {
                    var source = string.Format("{0}/{1}", rootPath, artifact);
                    if (!File.Exists(source))
                    {
                        BuildLogger.LogWarning("Unable to find source file '{0}' to add to the build cache.", artifact);
                        result = false;
                        continue;
                    }
                    else if (result)
                    {
                        var copyToPath = string.Format("{0}/{1}", path, artifact);
                        var directory = Path.GetDirectoryName(copyToPath);
                        Directory.CreateDirectory(directory);
                        File.Copy(source, copyToPath, true);
                    }
                }
            }
            catch (Exception e)
            {
                BuildLogger.LogException(e);
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
                return false;
            }

            if (!result && Directory.Exists(path))
                Directory.Delete(path, true);

            BuildLogger.LogCache("{0} SaveCachedArtifacts({1}, string[] ..., string ...)", result, hash.ToString());
            return result;
        }

        public static bool SaveCachedResultsAndArtifacts<T>(Hash128 hash, T results, string[] artifactPaths, string rootPath)
        {
            BuildLogger.LogCache("SaveCachedResultsAndArtifacts<{0}({1}, T ..., string[] ..., string ...)", typeof(T).Name, hash.ToString());
            if (SaveCachedResults(hash, results) && SaveCachedArtifacts(hash, artifactPaths, rootPath))
                return true;

            var path = GetPathForCachedResults(hash);
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            path = GetPathForCachedArtifacts(hash);
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            return false;
        }
    }
}
