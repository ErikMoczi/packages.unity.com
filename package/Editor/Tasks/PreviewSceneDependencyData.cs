using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class PreviewSceneDependencyData : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IBuildContent), typeof(IDependencyData) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCode Run(IBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            IProgressTracker tracker;
            context.TryGetContextObject(out tracker);
            IBuildCache cache;
            context.TryGetContextObject(out cache);
            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IBuildContent>(), context.GetContextObject<IDependencyData>(), tracker, cache);
        }

        static ReturnCode Run(IBuildParameters parameters, IBuildContent content, IDependencyData dependencyData, IProgressTracker tracker = null, IBuildCache cache = null)
        {
            foreach (GUID asset in content.Scenes)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(asset.ToString());

                var usageTags = new BuildUsageTagSet();
                var sceneInfo = new SceneDependencyInfo();

                var cacheEntry = new CacheEntry { Guid = asset };
                if (parameters.UseCache && cache != null)
                {
                    cacheEntry = cache.GetCacheEntry(asset);
                    var result = cache.IsCacheEntryValid(cacheEntry);
                    if (result && cache.TryLoadFromCache(cacheEntry, ref sceneInfo, ref usageTags))
                    {
                        if (!tracker.UpdateInfoUnchecked(string.Format("{0} (Cached)", scenePath)))
                            return ReturnCode.Canceled;

                        SetOutputInformation(asset, sceneInfo, usageTags, dependencyData);
                        continue;
                    }
                }

                if (!tracker.UpdateInfoUnchecked(scenePath))
                    return ReturnCode.Canceled;

                var references = new HashSet<ObjectIdentifier>();
                string[] dependencies = AssetDatabase.GetDependencies(scenePath);
                foreach (var assetPath in dependencies)
                {
                    var assetGuid = new GUID(AssetDatabase.AssetPathToGUID(assetPath));
                    if (!ValidationMethods.ValidAsset(assetGuid))
                        continue;
                    // TODO: Use Cache to speed this up?
                    var assetIncludes = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(assetGuid, parameters.Target);
                    var assetReferences = ContentBuildInterface.GetPlayerDependenciesForObjects(assetIncludes, parameters.Target, parameters.ScriptInfo);
                    references.UnionWith(assetIncludes);
                    references.UnionWith(assetReferences);
                }

                var boxedInfo = (object)sceneInfo;
                typeof(SceneDependencyInfo).GetField("m_Scene", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boxedInfo, scenePath);
                typeof(SceneDependencyInfo).GetField("m_ProcessedScene", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boxedInfo, scenePath);
                typeof(SceneDependencyInfo).GetField("m_ReferencedObjects", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boxedInfo, references.ToArray());
                sceneInfo = (SceneDependencyInfo)boxedInfo;

                SetOutputInformation(asset, sceneInfo, usageTags, dependencyData);

                if (parameters.UseCache && cache != null && !cache.TrySaveToCache(cacheEntry, sceneInfo, usageTags))
                    BuildLogger.LogWarning("Unable to cache PreviewSceneDependencyData results for asset '{0}'.", AssetDatabase.GUIDToAssetPath(asset.ToString()));
            }

            return ReturnCode.Success;
        }

        static void SetOutputInformation(GUID asset, SceneDependencyInfo sceneInfo, BuildUsageTagSet usageTags, IDependencyData dependencyData)
        {
            // Add generated scene information to BuildDependencyData
            dependencyData.SceneInfo.Add(asset, sceneInfo);
            dependencyData.SceneUsage.Add(asset, usageTags);
        }
    }
}
