using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using UnityEditor.Build.Content;

namespace UnityEditor.Build.Pipeline.Utilities
{
    class CacheStatics
    {
        public const string kCachePath = "Library/BuildCache";
        public const string kDependencyCachePath = "Library/BuildCache/Dependency";
        public const string kArtifactCachePath = "Library/BuildCache/Artifact";

        public interface ICachedDependency
        {
            CacheEntry asset { get; set; }
            CacheEntry[] dependencies { get; set; }
        }

        [Serializable]
        public class CachedDependency<T> : ICachedDependency
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

        public static bool LoadFromCache(CacheEntry cacheEntry, string cacheDirectory, out ICachedDependency cachedDependency)
        {
            try
            {
                var file = string.Format("{0}/{1}_{2}.bytes", cacheDirectory, cacheEntry.Guid.ToString(), cacheEntry.Hash.ToString());
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

        public static bool LoadFromCache<T>(CacheEntry cacheEntry, string cacheDirectory, out T results)
        {
            try
            {
                var file = string.Format("{0}/{1}.bytes", cacheDirectory, typeof(T).Name);
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

        struct FileWrite
        {
            public string file;
            public MemoryStream data;
        }

        static void Write(object data)
        {
            var fileWrite = (FileWrite)data;
            using (var fileStream = new FileStream(fileWrite.file, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fileWrite.data.Position = 0;
                fileWrite.data.WriteTo(fileStream);
                fileStream.Flush();
            }
            fileWrite.data.Dispose();
        }

        static void WriteToFile(string file, MemoryStream data)
        {
            var fileWrite = new FileWrite
            {
                file = file,
                data = data
            };

            ThreadPool.QueueUserWorkItem(Write, fileWrite);
        }

        public static bool SaveToCache(CacheEntry cacheEntry, string cacheDirectory, ICachedDependency cachedDependency)
        {
            try
            {
                var file = string.Format("{0}/{1}_{2}.bytes", cacheDirectory, cacheEntry.Guid.ToString(), cacheEntry.Hash.ToString());
                var formatter = new BinaryFormatter();
                var stream = new MemoryStream();
                formatter.Serialize(stream, cachedDependency);
                WriteToFile(file, stream);
            }
            catch (Exception e)
            {
                BuildLogger.LogException(e);
                return false;
            }
            return true;
        }

        public static bool SaveToCache<T>(CacheEntry cacheEntry, string cacheDirectory, T results)
        {
            try
            {
                var file = string.Format("{0}/{1}.bytes", cacheDirectory, typeof(T).Name);
                var formatter = new BinaryFormatter();
                var stream = new MemoryStream();
                formatter.Serialize(stream, results);
                WriteToFile(file, stream);
            }
            catch (Exception e)
            {
                BuildLogger.LogException(e);
                return false;
            }
            return true;
        }
    }
}
