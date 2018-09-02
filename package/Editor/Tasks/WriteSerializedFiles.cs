using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class WriteSerializedFiles : IBuildTask
    {
        const int k_Version = 2;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IDependencyData), typeof(IWriteData), typeof(IBuildResults) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCode Run(IBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            IProgressTracker tracker;
            context.TryGetContextObject(out tracker);
            IBuildCache cache;
            context.TryGetContextObject(out cache);
            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IDependencyData>(), context.GetContextObject<IWriteData>(), context.GetContextObject<IBuildResults>(), tracker, cache);
        }

        static void CalcualteCacheEntry(IBuildCache cache, IWriteOperation operation, BuildSettings settings, BuildUsageTagGlobal globalUsage, ref CacheEntry cacheEntry)
        {
            HashSet<CacheEntry> dependencies;
            bool validHashes = cache.GetCacheEntries(operation.Command.serializeObjects.Select(x => x.serializationObject.guid), out dependencies);

            // Using dependencies.ToArray() here because Old Mono Runtime doesn't implement HashSet serialization
            cacheEntry.Hash = !validHashes ? new Hash128() : HashingMethods.CalculateMD5Hash(k_Version, operation.GetHash128(), dependencies.ToArray(), settings.GetHash128(), globalUsage);
            cacheEntry.Guid = HashingMethods.CalculateMD5Guid("WriteSerializedFiles", operation.Command.internalName);
        }

        static ReturnCode Run(IBuildParameters parameters, IDependencyData dependencyData, IWriteData writeData, IBuildResults results, IProgressTracker tracker = null, IBuildCache cache = null)
        {
            BuildUsageTagGlobal globalUSage = new BuildUsageTagGlobal();
            foreach (var sceneInfo in dependencyData.SceneInfo)
                globalUSage |= sceneInfo.Value.globalUsage;

            foreach (var op in writeData.WriteOperations)
            {
                WriteResult result = new WriteResult();

                var cacheEntry = new CacheEntry();
                if (parameters.UseCache && cache != null)
                {
                    CalcualteCacheEntry(cache, op, parameters.GetContentBuildSettings(), globalUSage, ref cacheEntry);
                    if (cache.TryLoadFromCache(cacheEntry, ref result))
                    {
                        if (!tracker.UpdateInfoUnchecked(string.Format("{0} (Cached)", op.Command.internalName)))
                            return ReturnCode.Canceled;

                        SetOutputInformation(op.Command.internalName, result, results);
                        continue;
                    }
                }

                if (!tracker.UpdateInfoUnchecked(op.Command.internalName))
                    return ReturnCode.Canceled;

                var outputFolder = parameters.UseCache && cache != null ? cache.GetArtifactCacheDirectory(cacheEntry) : parameters.TempOutputFolder;
                result = op.Write(outputFolder, parameters.GetContentBuildSettings(), globalUSage);
                SetOutputInformation(op.Command.internalName, result, results);

                if (parameters.UseCache && cache != null && !cache.TrySaveToCache(cacheEntry, result))
                    BuildLogger.LogWarning("Unable to cache WriteSerializedFiles result for file {0}.", op.Command.internalName);
            }

            return ReturnCode.Success;
        }

        static void SetOutputInformation(string fileName, WriteResult result, IBuildResults results)
        {
            results.WriteResults.Add(fileName, result);
        }
    }
}
