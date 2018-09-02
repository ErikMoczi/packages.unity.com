using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public struct ArchiveAndCompressBundles : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IBundleWriteData), typeof(IBundleBuildResults) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            IProgressTracker tracker;
            context.TryGetContextObject(out tracker);
            IBuildCache cache;
            context.TryGetContextObject(out cache);
            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IBundleWriteData>(), context.GetContextObject<IBundleBuildResults>(), tracker, cache);
        }

        static void CalcualteCacheEntry(ResourceFile[] resources, BuildCompression compression, string bundleName, ref CacheEntry cacheEntry)
        {
            var fileHashes = new List<string>();
            foreach (ResourceFile resource in resources)
                fileHashes.Add(HashingMethods.CalculateFileMD5(resource.fileName).ToString());

            cacheEntry.guid = HashingMethods.CalculateMD5Guid("ArchiveAndCompressBundles", bundleName);
            cacheEntry.hash = HashingMethods.CalculateMD5Hash(k_Version, fileHashes, compression);
        }

        public static ReturnCodes Run(IBuildParameters parameters, IBundleWriteData writeData, IBundleBuildResults results, IProgressTracker tracker = null, IBuildCache cache = null)
        {
            Dictionary<string, List<WriteResult>> bundleToResults = new Dictionary<string, List<WriteResult>>();
            foreach (var result in results.WriteResults)
            {
                string bundle = writeData.FileToBundle[result.Key];
                List<WriteResult> writeResults;
                bundleToResults.GetOrAdd(bundle, out writeResults);
                writeResults.Add(result.Value);
            }

            foreach (KeyValuePair<string, List<WriteResult>> bundle in bundleToResults)
            {
                ResourceFile[] resourceFiles = bundle.Value.SelectMany(x => x.resourceFiles).ToArray();
                BuildCompression compression = parameters.GetCompressionForIdentifier(bundle.Key);

                var details = new BundleDetails();
                // TODO: Not fond of how caching is handled for archiving, rewrite it
                var writePath = string.Format("{0}/{1}", parameters.TempOutputFolder, bundle.Key);

                var cacheEntry = new CacheEntry();
                if (parameters.UseCache && cache != null)
                {
                    CalcualteCacheEntry(resourceFiles, compression, bundle.Key, ref cacheEntry);
                    writePath = string.Format("{0}/{1}", cache.GetArtifactCacheDirectory(cacheEntry), bundle.Key);
                    if (cache.TryLoadFromCache(cacheEntry, ref details))
                    {
                        if (!tracker.UpdateInfoUnchecked(string.Format("{0} (Cached)", bundle.Key)))
                            return ReturnCodes.Canceled;

                        details.fileName = string.Format("{0}/{1}", parameters.OutputFolder, bundle.Key);
                        SetOutputInformation(writePath, details.fileName, bundle.Key, details, results);
                        continue;
                    }
                }

                if (!tracker.UpdateInfoUnchecked(bundle.Key))
                    return ReturnCodes.Canceled;

                details.fileName = string.Format("{0}/{1}", parameters.OutputFolder, bundle.Key);
                details.crc = ContentBuildInterface.ArchiveAndCompress(resourceFiles, writePath, compression);
                details.hash = HashingMethods.CalculateFileMD5Hash(writePath);
                SetOutputInformation(writePath, details.fileName, bundle.Key, details, results);

                if (parameters.UseCache && cache != null && !cache.TrySaveToCache(cacheEntry, details))
                    BuildLogger.LogWarning("Unable to cache ArchiveAndCompressBundles result for bundle {0}.", bundle.Key);
            }

            return ReturnCodes.Success;
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
