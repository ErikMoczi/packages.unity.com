using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
#if UNITY_2018_3_OR_NEWER
using BuildCompression = UnityEngine.BuildCompression;
#else
using BuildCompression = UnityEditor.Build.Content.BuildCompression;
#endif


namespace UnityEditor.Build.Pipeline.Tasks
{
    public class ArchiveAndCompressBundles : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IBundleWriteData), typeof(IBundleBuildResults) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCode Run(IBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            
            IBuildParameters parameters = context.GetContextObject<IBuildParameters>();

            IProgressTracker tracker;
            context.TryGetContextObject(out tracker);
            IBuildCache cache = null;
            if (parameters.UseCache)
                context.TryGetContextObject(out cache);

            return Run(parameters, context.GetContextObject<IBundleWriteData>(), context.GetContextObject<IBundleBuildResults>(), tracker, cache);
        }

        static CacheEntry GetCacheEntry(string bundleName, IEnumerable<ResourceFile> resources, BuildCompression compression)
        {
            var entry = new CacheEntry();
            entry.Type = CacheEntry.EntryType.Data;
            entry.Guid = HashingMethods.Calculate("ArchiveAndCompressBundles", bundleName).ToGUID();
            entry.Hash = HashingMethods.Calculate(k_Version, resources, compression).ToHash128();
            return entry;
        }

        static CachedInfo GetCachedInfo(IBuildCache cache, CacheEntry entry, IEnumerable<ResourceFile> resources, BundleDetails details)
        {
            var info = new CachedInfo();
            info.Asset = entry;

            var dependencies = new HashSet<CacheEntry>();
            foreach (var resource in resources)
                dependencies.Add(cache.GetCacheEntry(resource.fileName));
            info.Dependencies = dependencies.ToArray();

            info.Data = new object[] { details };

            return info;
        }

        static ReturnCode Run(IBuildParameters parameters, IBundleWriteData writeData, IBundleBuildResults results, IProgressTracker tracker, IBuildCache cache)
        {
            List<KeyValuePair<string, List<ResourceFile>>> bundleResources;
            { 
                Dictionary<string, List<ResourceFile>> bundleToResources = new Dictionary<string, List<ResourceFile>>();
                foreach (var result in results.WriteResults)
                {
                    string bundle = writeData.FileToBundle[result.Key];
                    List<ResourceFile> resourceFiles;
                    bundleToResources.GetOrAdd(bundle, out resourceFiles);
                    resourceFiles.AddRange(result.Value.resourceFiles);
                }
                bundleResources = bundleToResources.ToList();
            }

            IList<CacheEntry> entries = null;
            IList<CachedInfo> cachedInfo = null;
            List<CachedInfo> uncachedInfo = null;
            if (cache != null)
            {
                entries = bundleResources.Select(x => GetCacheEntry(x.Key, x.Value, parameters.GetCompressionForIdentifier(x.Key))).ToList();
                cache.LoadCachedData(entries, out cachedInfo);

                uncachedInfo = new List<CachedInfo>();
            }

            for (int i = 0; i < bundleResources.Count; i++)
            {
                string bundleName = bundleResources[i].Key;
                ResourceFile[] resourceFiles = bundleResources[i].Value.ToArray();
                BuildCompression compression = parameters.GetCompressionForIdentifier(bundleName);

                string writePath;
                BundleDetails details;
                if (cachedInfo != null && cachedInfo[i] != null)
                {
                    if (!tracker.UpdateInfoUnchecked(string.Format("{0} (Cached)", bundleName)))
                        return ReturnCode.Canceled;

                    details = (BundleDetails)cachedInfo[i].Data[0];
                    writePath = string.Format("{0}/{1}", cache.GetCachedArtifactsDirectory(entries[i]), bundleName);
                }
                else
                {
                    if (!tracker.UpdateInfoUnchecked(bundleName))
                        return ReturnCode.Canceled;

                    details = new BundleDetails();
                    writePath = cache != null && parameters.UseCache ? string.Format("{0}/{1}", cache.GetCachedArtifactsDirectory(entries[i]), bundleName)
                        : string.Format("{0}/{1}", parameters.TempOutputFolder, bundleName);
                    Directory.CreateDirectory(Path.GetDirectoryName(writePath));

                    details.FileName = string.Format("{0}/{1}", parameters.OutputFolder, bundleName);
                    details.Crc = ContentBuildInterface.ArchiveAndCompress(resourceFiles, writePath, compression);
                    details.Hash = HashingMethods.CalculateFile(writePath).ToHash128();

                    if (cache != null)
                        uncachedInfo.Add(GetCachedInfo(cache, entries[i], resourceFiles, details));
                }
                
                SetOutputInformation(writePath, details.FileName, bundleName, details, results);
            }
            
            if (cache != null)
                cache.SaveCachedData(uncachedInfo);

            return ReturnCode.Success;
        }

        static void SetOutputInformation(string writePath, string finalPath, string bundleName, BundleDetails details, IBundleBuildResults results)
        {
            if (finalPath != writePath)
            {
                var directory = Path.GetDirectoryName(finalPath);
                Directory.CreateDirectory(directory);
                File.Copy(writePath, finalPath, true);
            }
            results.BundleInfos.Add(bundleName, details);
        }
    }
}
