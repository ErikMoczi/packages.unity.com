using System;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.Tasks
{
    public struct CalculateSceneDependencyData : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IBuildContent), typeof(IDependencyData) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            IProgressTracker tracker;
            context.TryGetContextObject(out tracker);
            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IBuildContent>(), context.GetContextObject<IDependencyData>(), tracker);
        }

        static Hash128 CalculateInputHash(bool useCache, GUID asset, BuildSettings settings)
        {
            if (!useCache)
                return new Hash128();

            string path = AssetDatabase.GUIDToAssetPath(asset.ToString());
            string assetHash = AssetDatabase.GetAssetDependencyHash(path).ToString();
            string[] dependencies = AssetDatabase.GetDependencies(path);
            var dependencyHashes = new string[dependencies.Length];
            for (int i = 0; i < dependencies.Length; ++i)
                dependencyHashes[i] = AssetDatabase.GetAssetDependencyHash(dependencies[i]).ToString();
            return HashingMethods.CalculateMD5Hash(k_Version, assetHash, dependencyHashes, settings);
        }

        public static ReturnCodes Run(IBuildParameters parameters, IBuildContent content, IDependencyData dependencyData, IProgressTracker tracker = null)
        {
            foreach (GUID asset in content.Scenes)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(asset.ToString());

                var usageTags = new BuildUsageTagSet();
                var sceneInfo = new SceneDependencyInfo();

                Hash128 hash = CalculateInputHash(parameters.UseCache, asset, parameters.GetContentBuildSettings());
                if (TryLoadFromCache(parameters.UseCache, hash, ref sceneInfo, ref usageTags))
                {
                    if (!tracker.UpdateInfoUnchecked(string.Format("{0} (Cached)", scenePath)))
                        return ReturnCodes.Canceled;

                    SetOutputInformation(asset, sceneInfo, usageTags, dependencyData);
                    continue;
                }

                if (!tracker.UpdateInfoUnchecked(scenePath))
                    return ReturnCodes.Canceled;

                sceneInfo = BundleBuildInterface.PrepareScene(scenePath, parameters.GetContentBuildSettings(), usageTags, parameters.GetTempOrCacheBuildPath(hash));
                SetOutputInformation(asset, sceneInfo, usageTags, dependencyData);

                if (!TrySaveToCache(parameters.UseCache, hash, sceneInfo, usageTags))
                    BuildLogger.LogWarning("Unable to cache SceneDependency results for asset '{0}'.", AssetDatabase.GUIDToAssetPath(asset.ToString()));
            }

            return ReturnCodes.Success;
        }

        static void SetOutputInformation(GUID asset, SceneDependencyInfo sceneInfo, BuildUsageTagSet usageTags, IDependencyData dependencyData)
        {
            // Add generated scene information to BuildDependencyData
            dependencyData.SceneInfo.Add(asset, sceneInfo);
            dependencyData.SceneUsage.Add(asset, usageTags);
        }

        static bool TryLoadFromCache(bool useCache, Hash128 hash, ref SceneDependencyInfo sceneInfo, ref BuildUsageTagSet usageTags)
        {
            SceneDependencyInfo cachedSceneInfo;
            BuildUsageTagSet cachedUsageTags;
            if (useCache && BuildCache.TryLoadCachedResults(hash, out cachedSceneInfo) && BuildCache.TryLoadCachedResults(hash, out cachedUsageTags))
            {
                sceneInfo = cachedSceneInfo;
                usageTags = cachedUsageTags;
                return true;
            }

            return false;
        }

        static bool TrySaveToCache(bool useCache, Hash128 hash, SceneDependencyInfo sceneInfo, BuildUsageTagSet usageTags)
        {
            if (useCache && !(BuildCache.SaveCachedResults(hash, sceneInfo) && BuildCache.SaveCachedResults(hash, usageTags)))
                return false;
            return true;
        }
    }
}
