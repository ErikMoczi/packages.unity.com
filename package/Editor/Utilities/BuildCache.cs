using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities
{
    public class BuildCache : IBuildCache
    {
        Dictionary<GUID, Hash128> m_HashCache = new Dictionary<GUID, Hash128>();

        Dictionary<CacheEntry, CacheStatics.ICachedDependency> m_DependencyCache = new Dictionary<CacheEntry, CacheStatics.ICachedDependency>();

        public string GetDependencyCacheDirectory(CacheEntry cacheEntry)
        {
            Directory.CreateDirectory(CacheStatics.kDependencyCachePath);
            return CacheStatics.kDependencyCachePath;
        }

        public string GetArtifactCacheDirectory(CacheEntry cacheEntry)
        {
            var folder = string.Format("{0}/{1}/{2}", CacheStatics.kArtifactCachePath, cacheEntry.Guid.ToString(), cacheEntry.Hash.ToString());
            Directory.CreateDirectory(folder);
            return folder;
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
            entry.Hash = AssetDatabase.GetAssetDependencyHash(path);

            path = path.ToLower();
            if (path == CommonStrings.UnityBuiltInExtraPath || path == CommonStrings.UnityDefaultResourcePath)
                entry.Hash = HashingMethods.CalculateMD5Hash(Application.unityVersion, path);

            if (entry.IsValid())
                m_HashCache[asset] = entry.Hash;

            return entry;
        }

        public bool GetCacheEntries(IEnumerable<GUID> assets, out HashSet<CacheEntry> cacheEntries)
        {
            bool allValid = true;
            cacheEntries = new HashSet<CacheEntry>();
            foreach (var asset in assets)
            {
                if (asset.Empty())
                    continue;

                CacheEntry entry = GetCacheEntry(asset);
                allValid &= entry.IsValid();
                cacheEntries.Add(entry);
            }
            return allValid;
        }

        public bool IsCacheEntryValid(CacheEntry cacheEntry)
        {
            if (!cacheEntry.IsValid())
                return false;

            CacheStatics.ICachedDependency cachedDependency;
            if (!m_DependencyCache.TryGetValue(cacheEntry, out cachedDependency))
            {
                if (!CacheStatics.LoadFromCache(cacheEntry, GetDependencyCacheDirectory(cacheEntry), out cachedDependency))
                    return false;
            }

            foreach (CacheEntry dependency in cachedDependency.dependencies)
            {
                if (dependency.IsValid() && dependency == GetCacheEntry(dependency.Guid))
                    continue;
                return false;
            }

            m_DependencyCache[cacheEntry] = cachedDependency;
            return true;
        }

        public bool TryLoadFromCache(CacheEntry cacheEntry, ref AssetLoadInfo info, ref BuildUsageTagSet usage)
        {
            if (!cacheEntry.IsValid())
                return false;

            CacheStatics.ICachedDependency cachedDependency;
            if (!m_DependencyCache.TryGetValue(cacheEntry, out cachedDependency))
                return false;

            if (cachedDependency is CacheStatics.CachedDependency<AssetLoadInfo>)
            {
                var dependency = (CacheStatics.CachedDependency<AssetLoadInfo>)cachedDependency;
                info = dependency.info;
                usage = dependency.usage;
                return true;
            }

            return false;
        }

        public bool TryLoadFromCache(CacheEntry cacheEntry, ref SceneDependencyInfo info, ref BuildUsageTagSet usage)
        {
            if (!cacheEntry.IsValid())
                return false;

            CacheStatics.ICachedDependency cachedDependency;
            if (!m_DependencyCache.TryGetValue(cacheEntry, out cachedDependency))
                return false;

            if (cachedDependency is CacheStatics.CachedDependency<SceneDependencyInfo>)
            {
                var dependency = (CacheStatics.CachedDependency<SceneDependencyInfo>)cachedDependency;
                info = dependency.info;
                usage = dependency.usage;
                return true;
            }

            return false;
        }

        public bool TryLoadFromCache<T>(CacheEntry cacheEntry, ref T results)
        {
            if (!cacheEntry.IsValid())
                return false;

            T cachedResults;
            var success = CacheStatics.LoadFromCache(cacheEntry, GetArtifactCacheDirectory(cacheEntry), out cachedResults);
            if (success)
                results = cachedResults;
            return success;
        }

        public bool TryLoadFromCache<T1, T2>(CacheEntry cacheEntry, ref T1 results1, ref T2 results2)
        {
            if (!cacheEntry.IsValid())
                return false;

            //TODO: try and batch the load into one file
            T1 cachedResults1;
            T2 cachedResults2;
            var success = CacheStatics.LoadFromCache(cacheEntry, GetArtifactCacheDirectory(cacheEntry), out cachedResults1);
            success = CacheStatics.LoadFromCache(cacheEntry, GetArtifactCacheDirectory(cacheEntry), out cachedResults2) & success;
            if (success)
            {
                results1 = cachedResults1;
                results2 = cachedResults2;
            }
            return success;
        }

        public bool TrySaveToCache(CacheEntry cacheEntry, AssetLoadInfo info, BuildUsageTagSet usage)
        {
            if (!cacheEntry.IsValid())
                return false;

            CacheStatics.ICachedDependency cachedDependency = new CacheStatics.CachedDependency<AssetLoadInfo>(info, usage);
            cachedDependency.asset = cacheEntry;

            HashSet<CacheEntry> dependencies;
            if (!GetCacheEntries(info.referencedObjects.Select(x => x.guid), out dependencies))
                return false;

            cachedDependency.dependencies = dependencies.ToArray();
            m_DependencyCache[cacheEntry] = cachedDependency;
            
            var success = CacheStatics.SaveToCache(cacheEntry, GetDependencyCacheDirectory(cacheEntry), cachedDependency);
            return success;
        }

        public bool TrySaveToCache(CacheEntry cacheEntry, SceneDependencyInfo info, BuildUsageTagSet usage)
        {
            if (!cacheEntry.IsValid())
                return false;

            CacheStatics.ICachedDependency cachedDependency = new CacheStatics.CachedDependency<SceneDependencyInfo>(info, usage);
            cachedDependency.asset = cacheEntry;

            HashSet<CacheEntry> dependencies;
            if (!GetCacheEntries(info.referencedObjects.Select(x => x.guid), out dependencies))
                return false;

            cachedDependency.dependencies = dependencies.ToArray();
            m_DependencyCache[cacheEntry] = cachedDependency;
            
            var success = CacheStatics.SaveToCache(cacheEntry, GetDependencyCacheDirectory(cacheEntry), cachedDependency);
            return success;
        }

        public bool TrySaveToCache<T>(CacheEntry cacheEntry, T results)
        {
            if (!cacheEntry.IsValid())
                return false;

            var success = CacheStatics.SaveToCache(cacheEntry, GetArtifactCacheDirectory(cacheEntry), results);
            return success;
        }

        public bool TrySaveToCache<T1, T2>(CacheEntry cacheEntry, T1 results1, T2 results2)
        {
            if (!cacheEntry.IsValid())
                return false;

            //TODO: try and batch the save into one file
            var success = CacheStatics.SaveToCache(cacheEntry, GetArtifactCacheDirectory(cacheEntry), results1);
            success &= CacheStatics.SaveToCache(cacheEntry, GetArtifactCacheDirectory(cacheEntry), results2);
            return success;
        }

        [MenuItem("Window/Asset Management/Purge Build Cache", priority = 10)]
        public static void PurgeCache()
        {
            if (!EditorUtility.DisplayDialog("Purge Build Cache", "Do you really want to purge your entire build cache?", "Yes", "No"))
                return;

            if (Directory.Exists(CacheStatics.kCachePath))
                Directory.Delete(CacheStatics.kCachePath, true);
        }
    }
}
