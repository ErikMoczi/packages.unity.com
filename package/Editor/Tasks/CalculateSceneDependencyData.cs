using System;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
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
            IBuildCache cache;
            context.TryGetContextObject(out cache);
            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IBuildContent>(), context.GetContextObject<IDependencyData>(), tracker, cache);
        }

        public static ReturnCodes Run(IBuildParameters parameters, IBuildContent content, IDependencyData dependencyData, IProgressTracker tracker = null, IBuildCache cache = null)
        {
            using (new SceneStateCleanup())
            {

                foreach (GUID asset in content.Scenes)
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(asset.ToString());

                    var usageTags = new BuildUsageTagSet();
                    var sceneInfo = new SceneDependencyInfo();

                    var cacheEntry = new CacheEntry { guid = asset };
                    if (parameters.UseCache && cache != null)
                    {
                        cacheEntry = cache.GetCacheEntry(asset);
                        var result = cache.IsCacheEntryValid(cacheEntry);
                        if (result && cache.TryLoadFromCache(cacheEntry, ref sceneInfo, ref usageTags))
                        {
                            if (!tracker.UpdateInfoUnchecked(string.Format("{0} (Cached)", scenePath)))
                                return ReturnCodes.Canceled;

                            SetOutputInformation(asset, sceneInfo, usageTags, dependencyData);
                            continue;
                        }
                    }

                    if (!tracker.UpdateInfoUnchecked(scenePath))
                        return ReturnCodes.Canceled;

                    var outputFolder = parameters.UseCache && cache != null ? cache.GetArtifactCacheDirectory(cacheEntry) : parameters.TempOutputFolder;
                    sceneInfo = ContentBuildInterface.PrepareScene(scenePath, parameters.GetContentBuildSettings(), usageTags, outputFolder);
                    SetOutputInformation(asset, sceneInfo, usageTags, dependencyData);

                    if (parameters.UseCache && cache != null && !cache.TrySaveToCache(cacheEntry, sceneInfo, usageTags))
                        BuildLogger.LogWarning("Unable to cache CalculateSceneDependencyData results for asset '{0}'.", AssetDatabase.GUIDToAssetPath(asset.ToString()));
                }
            }

            return ReturnCodes.Success;
        }

        static void SetOutputInformation(GUID asset, SceneDependencyInfo sceneInfo, BuildUsageTagSet usageTags, IDependencyData dependencyData)
        {
            // Add generated scene information to BuildDependencyData
            dependencyData.SceneInfo.Add(asset, sceneInfo);
            dependencyData.SceneUsage.Add(asset, usageTags);
        }
    }
}
