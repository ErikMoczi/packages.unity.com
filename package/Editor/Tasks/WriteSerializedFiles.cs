using System;
using UnityEditor.Build.WriteTypes;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.Tasks
{
    public class WriteSerializedFiles : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IDependencyData), typeof(IWriteData), typeof(IBuildResults) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            IProgressTracker tracker;
            context.TryGetContextObject(out tracker);
            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IDependencyData>(), context.GetContextObject<IWriteData>(), context.GetContextObject<IBuildResults>(), tracker);
        }

        protected static Hash128 CalculateInputHash(bool useCache, IWriteOperation operation, BuildSettings settings, BuildUsageTagGlobal globalUsage)
        {
            if (!useCache)
                return new Hash128();

            string[] assetHashes = new string[operation.command.serializeObjects.Count];
            for (var index = 0; index < operation.command.serializeObjects.Count; index++)
                assetHashes[index] = AssetDatabase.GetAssetDependencyHash(operation.command.serializeObjects[index].serializationObject.guid.ToString()).ToString();
            return HashingMethods.CalculateMD5Hash(k_Version, operation, assetHashes, settings, globalUsage);
        }

        public static ReturnCodes Run(IBuildParameters parameters, IDependencyData dependencyData, IWriteData writeData, IBuildResults results, IProgressTracker tracker = null)
        {
            BuildUsageTagGlobal globalUSage = new BuildUsageTagGlobal();
            foreach (var sceneInfo in dependencyData.SceneInfo)
                globalUSage |= sceneInfo.Value.globalUsage;

            foreach (var op in writeData.WriteOperations)
            {
                Hash128 hash = CalculateInputHash(parameters.UseCache, op, parameters.GetContentBuildSettings(), globalUSage);
                WriteResult result = new WriteResult();
                if (TryLoadFromCache(parameters.UseCache, hash, ref result))
                {
                    if (!tracker.UpdateInfoUnchecked(string.Format("{0} (Cached)", op.command.internalName)))
                        return ReturnCodes.Canceled;

                    SetOutputInformation(op.command.internalName, result, results);
                    continue;
                }

                if (!tracker.UpdateInfoUnchecked(op.command.internalName))
                    return ReturnCodes.Canceled;

                result = op.Write(parameters.GetTempOrCacheBuildPath(hash), parameters.GetContentBuildSettings(), globalUSage);
                SetOutputInformation(op.command.internalName, result, results);

                if (!TrySaveToCache(parameters.UseCache, hash, result))
                    BuildLogger.LogWarning("Unable to cache WriteSerializedFiles result for file {0}.", op.command.internalName);
            }

            return ReturnCodes.Success;
        }

        static void SetOutputInformation(string fileName, WriteResult result, IBuildResults results)
        {
            results.WriteResults.Add(fileName, result);
        }

        static bool TryLoadFromCache(bool useCache, Hash128 hash, ref WriteResult result)
        {
            WriteResult cachedResult;
            if (useCache && BuildCache.TryLoadCachedResults(hash, out cachedResult))
            {
                result = cachedResult;
                return true;
            }
            return false;
        }

        static bool TrySaveToCache(bool useCache, Hash128 hash, WriteResult result)
        {
            if (useCache && !BuildCache.SaveCachedResults(hash, result))
                return false;
            return true;
        }
    }
}
