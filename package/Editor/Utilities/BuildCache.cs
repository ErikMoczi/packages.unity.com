using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
#if !NET_4_6
using System.Threading;
#endif
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities
{
    public class BuildCache : IBuildCache
    {
        interface ICachedDependency
        {
            CacheEntry asset { get; set; }
            CacheEntry[] dependencies { get; set; }
        }

        [Serializable]
        class CachedDependency<T> : ICachedDependency
        {
            public CacheEntry asset { get; set; }
            public CacheEntry[] dependencies { get; set; }

            public T info;
            public BuildUsageTagSet usage;

            public CachedDependency(T assetInfo, BuildUsageTagSet assetUsage)
            {
                asset = new CacheEntry();
                dependencies = null;
                info = assetInfo;
                usage = assetUsage;
            }
        }

        internal const string kCachePath = "Library/BuildCache";
        internal const string kDependencyCachePath = "Library/BuildCache/Dependency";
        internal const string kArtifactCachePath = "Library/BuildCache/Artifact";

        Dictionary<GUID, Hash128> m_HashCache = new Dictionary<GUID, Hash128>();

        Dictionary<CacheEntry, ICachedDependency> m_DependencyCache = new Dictionary<CacheEntry, ICachedDependency>();

        public static string GetDependencyCacheDirectory(CacheEntry cacheEntry)
        {
            Directory.CreateDirectory(kDependencyCachePath);
            return kDependencyCachePath;
        }

        string IBuildCache.GetDependencyCacheDirectory(CacheEntry cacheEntry)
        {
            return GetDependencyCacheDirectory(cacheEntry);
        }

        public static string GetArtifactCacheDirectory(CacheEntry cacheEntry)
        {
            var folder = string.Format("{0}/{1}/{2}", kArtifactCachePath, cacheEntry.Guid.ToString(), cacheEntry.Hash.ToString());
            Directory.CreateDirectory(folder);
            return folder;
        }

        string IBuildCache.GetArtifactCacheDirectory(CacheEntry cacheEntry)
        {
            return GetArtifactCacheDirectory(cacheEntry);
        }

        public CacheEntry GetCacheEntry(GUID asset)
        {
            var entry = new CacheEntry { Guid = asset };
            Hash128 hash;
            if (m_HashCache.TryGetValue(asset, out hash))
            {
                entry.Hash = hash;
                return entry;
            }

            string path = AssetDatabase.GUIDToAssetPath(asset.ToString());
            string assetHash = AssetDatabase.GetAssetDependencyHash(path).ToString();

            entry.Hash = Hash128.Parse(assetHash);
            m_HashCache[asset] = entry.Hash;
            return entry;
        }

        static bool LoadFromCache(CacheEntry cacheEntry, out ICachedDependency cachedDependency)
        {
            // TODO: Cache server integration
            try
            {
                var file = string.Format("{0}/{1}_{2}.bytes", GetDependencyCacheDirectory(cacheEntry), cacheEntry.Guid.ToString(), cacheEntry.Hash.ToString());
                if (!File.Exists(file))
                {
                    cachedDependency = default(ICachedDependency);
                    return false;
                }

                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                    cachedDependency = (ICachedDependency)formatter.Deserialize(stream);
            }
            catch (Exception e)
            {
                BuildLogger.LogException(e);
                cachedDependency = default(ICachedDependency);
                return false;
            }
            return true;
        }

        static bool LoadFromCache<T>(CacheEntry cacheEntry, out T results)
        {
            // TODO: Cache server integration
            try
            {
                var file = string.Format("{0}/{1}.bytes", GetArtifactCacheDirectory(cacheEntry), typeof(T).Name);
                if (!File.Exists(file))
                {
                    results = default(T);
                    return false;
                }

                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                    results = (T)formatter.Deserialize(stream);
            }
            catch (Exception e)
            {
                BuildLogger.LogException(e);
                results = default(T);
                return false;
            }
            return true;
        }

        public bool IsCacheEntryValid(CacheEntry cacheEntry)
        {
            ICachedDependency cachedDependency;
            if (!m_DependencyCache.TryGetValue(cacheEntry, out cachedDependency))
            {
                if (!LoadFromCache(cacheEntry, out cachedDependency))
                    return false;
            }

            foreach (CacheEntry dependency in cachedDependency.dependencies)
            {
                if (dependency == GetCacheEntry(dependency.Guid))
                    continue;
                return false;
            }

            m_DependencyCache[cacheEntry] = cachedDependency;
            return true;
        }

        public bool TryLoadFromCache(CacheEntry cacheEntry, ref AssetLoadInfo info, ref BuildUsageTagSet usage)
        {
            ICachedDependency cachedDependency;
            if (!m_DependencyCache.TryGetValue(cacheEntry, out cachedDependency))
                return false;

            if (cachedDependency is CachedDependency<AssetLoadInfo>)
            {
                var dependency = (CachedDependency<AssetLoadInfo>)cachedDependency;
                info = dependency.info;
                usage = dependency.usage;
                return true;
            }

            return false;
        }

        public bool TryLoadFromCache(CacheEntry cacheEntry, ref SceneDependencyInfo info, ref BuildUsageTagSet usage)
        {
            ICachedDependency cachedDependency;
            if (!m_DependencyCache.TryGetValue(cacheEntry, out cachedDependency))
                return false;

            if (cachedDependency is CachedDependency<SceneDependencyInfo>)
            {
                var dependency = (CachedDependency<SceneDependencyInfo>)cachedDependency;
                info = dependency.info;
                usage = dependency.usage;
                return true;
            }

            return false;
        }

        public bool TryLoadFromCache<T>(CacheEntry cacheEntry, ref T results)
        {
            T cachedResults;
            var success = LoadFromCache(cacheEntry, out cachedResults);
            if (success)
                results = cachedResults;
            return success;
        }

        public bool TryLoadFromCache<T1, T2>(CacheEntry cacheEntry, ref T1 results1, ref T2 results2)
        {
            //TODO: try and batch the load into one file
            T1 cachedResults1;
            T2 cachedResults2;
            var success = LoadFromCache(cacheEntry, out cachedResults1);
            success = LoadFromCache(cacheEntry, out cachedResults2) & success;
            if (success)
            {
                results1 = cachedResults1;
                results2 = cachedResults2;
            }
            return success;
        }

#if NET_4_6
        static async void WriteToFile(string file, byte[] data)
        {
            using (var fileStream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write))
                await fileStream.WriteAsync(data, 0, data.Length);
        }
#else
        struct FileWrite
        {
            public string file;
            public byte[] data;
        }

        static void Write(object data)
        {
            var fileWrite = (FileWrite)data;
            using (var fileStream = new FileStream(fileWrite.file, FileMode.OpenOrCreate, FileAccess.Write))
                fileStream.Write(fileWrite.data, 0, fileWrite.data.Length);
        }

        public static void WriteToFile(string file, byte[] data)
        {
            var fileWrite = new FileWrite
            {
                file = file,
                data = data
            };

            ThreadPool.QueueUserWorkItem(Write, fileWrite);
        }
#endif

        static bool SaveToCache(CacheEntry cacheEntry, ICachedDependency cachedDependency)
        {
            // TODO: Cache server integration
            try
            {
                var file = string.Format("{0}/{1}_{2}.bytes", GetDependencyCacheDirectory(cacheEntry), cacheEntry.Guid.ToString(), cacheEntry.Hash.ToString());
                var formatter = new BinaryFormatter();
                using (var stream = new MemoryStream())
                {
                    formatter.Serialize(stream, cachedDependency);
                    WriteToFile(file, stream.ToArray());
                }
            }
            catch (Exception e)
            {
                BuildLogger.LogException(e);
                return false;
            }
            return true;
        }

        static bool SaveToCache<T>(CacheEntry cacheEntry, T results)
        {
            // TODO: Cache server integration
            try
            {
                var file = string.Format("{0}/{1}.bytes", GetArtifactCacheDirectory(cacheEntry), typeof(T).Name);
                var formatter = new BinaryFormatter();
                using (var stream = new MemoryStream())
                {
                    formatter.Serialize(stream, results);
                    WriteToFile(file, stream.ToArray());
                }
            }
            catch (Exception e)
            {
                BuildLogger.LogException(e);
                return false;
            }
            return true;
        }

        public bool TrySaveToCache(CacheEntry cacheEntry, AssetLoadInfo info, BuildUsageTagSet usage)
        {
            ICachedDependency dependency = new CachedDependency<AssetLoadInfo>(info, usage);
            dependency.asset = cacheEntry;

            var dependencies = new HashSet<CacheEntry>();
            foreach (var reference in info.referencedObjects)
            {
                CacheEntry entry = GetCacheEntry(reference.guid);
                dependencies.Add(entry);
            }
            dependency.dependencies = dependencies.ToArray();
            m_DependencyCache[cacheEntry] = dependency;
            
            var success = SaveToCache(cacheEntry, dependency);
            return success;
        }

        public bool TrySaveToCache(CacheEntry cacheEntry, SceneDependencyInfo info, BuildUsageTagSet usage)
        {
            ICachedDependency dependency = new CachedDependency<SceneDependencyInfo>(info, usage);
            dependency.asset = cacheEntry;

            var dependencies = new HashSet<CacheEntry>();
            foreach (var reference in info.referencedObjects)
            {
                CacheEntry entry = GetCacheEntry(reference.guid);
                dependencies.Add(entry);
            }
            dependency.dependencies = dependencies.ToArray();
            m_DependencyCache[cacheEntry] = dependency;
            
            var success = SaveToCache(cacheEntry, dependency);
            return success;
        }

        public bool TrySaveToCache<T>(CacheEntry cacheEntry, T results)
        {
            var success = SaveToCache(cacheEntry, results);
            return success;
        }

        public bool TrySaveToCache<T1, T2>(CacheEntry cacheEntry, T1 results1, T2 results2)
        {
            //TODO: try and batch the save into one file
            var success = SaveToCache(cacheEntry, results1);
            success &= SaveToCache(cacheEntry, results2);
            return success;
        }

        [MenuItem("Window/Build Pipeline/Purge Build Cache", priority = 10)]
        public static void PurgeCache()
        {
            if (!EditorUtility.DisplayDialog("Purge Build Cache", "Do you really want to purge your entire build cache?", "Yes", "No"))
                return;
            if (Directory.Exists(kCachePath))
                Directory.Delete(kCachePath, true);
        }
    }
}