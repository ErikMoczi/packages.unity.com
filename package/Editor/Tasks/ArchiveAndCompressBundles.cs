using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.Tasks
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
            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IBundleWriteData>(), context.GetContextObject<IBundleBuildResults>(), tracker);
        }

        static Hash128 CalculateInputHash(bool useCache, ResourceFile[] resources, BuildCompression compression)
        {
            if (!useCache)
                return new Hash128();

            var fileHashes = new List<string>();
            foreach (ResourceFile resource in resources)
                fileHashes.Add(HashingMethods.CalculateFileMD5Hash(resource.fileName).ToString());
            return HashingMethods.CalculateMD5Hash(k_Version, fileHashes, compression);
        }

        public static ReturnCodes Run(IBuildParameters parameters, IBundleWriteData writeData, IBundleBuildResults results, IProgressTracker tracker = null)
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
                Hash128 hash = CalculateInputHash(parameters.UseCache, resourceFiles, compression);

                var finalPath = string.Format("{0}/{1}", parameters.OutputFolder, bundle.Key);
                var writePath = string.Format("{0}/{1}", parameters.GetTempOrCacheBuildPath(hash), bundle.Key);

                var details = new BundleDetails();
                if (TryLoadFromCache(parameters.UseCache, hash, ref details))
                {
                    if (!tracker.UpdateInfoUnchecked(string.Format("{0} (Cached)", bundle.Key)))
                        return ReturnCodes.Canceled;

                    SetOutputInformation(writePath, finalPath, bundle.Key, details, results);
                    continue;
                }

                if (!tracker.UpdateInfoUnchecked(bundle.Key))
                    return ReturnCodes.Canceled;

                details.fileName = finalPath;
                details.crc = BundleBuildInterface.ArchiveAndCompress(resourceFiles, writePath, compression);
                details.hash = HashingMethods.CalculateFileMD5Hash(writePath);
                SetOutputInformation(writePath, finalPath, bundle.Key, details, results);

                if (!TrySaveToCache(parameters.UseCache, hash, details))
                    BuildLogger.LogWarning("Unable to cache ArchiveAndCompressBundles result for bundle {0}.", bundle.Key);
            }

            return ReturnCodes.Success;
        }

        static bool TryLoadFromCache(bool useCache, Hash128 hash, ref BundleDetails details)
        {
            BundleDetails cachedDetails;
            if (useCache && BuildCache.TryLoadCachedResults(hash, out cachedDetails))
            {
                details = cachedDetails;
                return true;
            }
            return false;
        }

        static bool TrySaveToCache(bool useCache, Hash128 hash, BundleDetails details)
        {
            if (useCache && !BuildCache.SaveCachedResults(hash, details))
                return false;
            return true;
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
