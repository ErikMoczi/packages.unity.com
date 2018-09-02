using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Pipeline.WriteTypes;

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

            IBuildParameters parameters = context.GetContextObject<IBuildParameters>();

            IProgressTracker tracker;
            context.TryGetContextObject(out tracker);
            IBuildCache cache = null;
            if (parameters.UseCache)
                context.TryGetContextObject(out cache);

            return Run(parameters, context.GetContextObject<IDependencyData>(), context.GetContextObject<IWriteData>(), context.GetContextObject<IBuildResults>(), tracker, cache);
        }

        static CacheEntry GetCacheEntry(IWriteOperation operation, BuildSettings settings, BuildUsageTagGlobal globalUsage)
        {
            var entry = new CacheEntry();
            entry.Type = CacheEntry.EntryType.Data;
            entry.Guid = HashingMethods.Calculate("WriteSerializedFiles", operation.Command.internalName).ToGUID();
            entry.Hash = HashingMethods.Calculate(k_Version, operation.GetHash128(), settings.GetHash128(), globalUsage).ToHash128();
            return entry;
        }

        static CachedInfo GetCachedInfo(IBuildCache cache, CacheEntry entry, IWriteOperation operation, WriteResult result)
        {
            var info = new CachedInfo();
            info.Asset = entry;

            var dependencies = new HashSet<CacheEntry>();
            var sceneBundleOp = operation as SceneBundleWriteOperation;
            if (sceneBundleOp != null)
                dependencies.Add(cache.GetCacheEntry(sceneBundleOp.ProcessedScene));
            var sceneDataOp = operation as SceneDataWriteOperation;
            if (sceneDataOp != null)
                dependencies.Add(cache.GetCacheEntry(sceneDataOp.ProcessedScene));
            foreach (var serializeObject in operation.Command.serializeObjects)
                dependencies.Add(cache.GetCacheEntry(serializeObject.serializationObject));
            info.Dependencies = dependencies.ToArray();

            info.Data = new object[] { result };

            return info;
        }

        static ReturnCode Run(IBuildParameters parameters, IDependencyData dependencyData, IWriteData writeData, IBuildResults results, IProgressTracker tracker, IBuildCache cache)
        {
            BuildUsageTagGlobal globalUsage = new BuildUsageTagGlobal();
            foreach (var sceneInfo in dependencyData.SceneInfo)
                globalUsage |= sceneInfo.Value.globalUsage;

            IList<CacheEntry> entries = null;
            IList<CachedInfo> cachedInfo = null;
            List<CachedInfo> uncachedInfo = null;
            if (cache != null)
            {
                entries = writeData.WriteOperations.Select(x => GetCacheEntry(x, parameters.GetContentBuildSettings(), globalUsage)).ToList();
                cache.LoadCachedData(entries, out cachedInfo);

                uncachedInfo = new List<CachedInfo>();
            }

            for (int i = 0; i < writeData.WriteOperations.Count; i++)
            {
                IWriteOperation op = writeData.WriteOperations[i];

                WriteResult result;
                if (cachedInfo != null && cachedInfo[i] != null)
                {
                    if (!tracker.UpdateInfoUnchecked(string.Format("{0} (Cached)", op.Command.internalName)))
                        return ReturnCode.Canceled;

                    result = (WriteResult)cachedInfo[i].Data[0];
                }
                else
                {
                    if (!tracker.UpdateInfoUnchecked(op.Command.internalName))
                        return ReturnCode.Canceled;

                    var outputFolder = parameters.UseCache && cache != null
                        ? cache.GetCachedArtifactsDirectory(entries[i])
                        : parameters.TempOutputFolder;
                    Directory.CreateDirectory(outputFolder);

                    result = op.Write(outputFolder, parameters.GetContentBuildSettings(), globalUsage);

                    if (cache != null)
                        uncachedInfo.Add(GetCachedInfo(cache, entries[i], op, result));
                }

                SetOutputInformation(op.Command.internalName, result, results);
            }

            if (cache != null)
                cache.SaveCachedData(uncachedInfo);

            return ReturnCode.Success;
        }

        static void SetOutputInformation(string fileName, WriteResult result, IBuildResults results)
        {
            results.WriteResults.Add(fileName, result);
        }
    }
}
