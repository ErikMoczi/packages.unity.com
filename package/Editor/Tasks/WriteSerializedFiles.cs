using System;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class WriteSerializedFiles : IBuildTask
    {
        const int k_Version = 1;
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

        static void CalcualteCacheEntry(IWriteOperation operation, BuildSettings settings, BuildUsageTagGlobal globalUsage, ref CacheEntry cacheEntry)
        {
            string[] assetHashes = new string[operation.Command.serializeObjects.Count];
            for (var index = 0; index < operation.Command.serializeObjects.Count; index++)
                assetHashes[index] = AssetDatabase.GetAssetDependencyHash(operation.Command.serializeObjects[index].serializationObject.guid.ToString()).ToString();

            cacheEntry.Hash = HashingMethods.CalculateMD5Hash(k_Version, operation.GetHash128(), assetHashes, settings.GetHash128(), globalUsage);
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
                    CalcualteCacheEntry(op, parameters.GetContentBuildSettings(), globalUSage, ref cacheEntry);
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
