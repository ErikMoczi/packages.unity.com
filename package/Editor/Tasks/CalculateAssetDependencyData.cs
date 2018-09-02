using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class CalculateAssetDependencyData : IBuildTask
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

            IBuildSpriteData spriteData;
            var result = Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IBuildContent>(), context.GetContextObject<IDependencyData>(), out spriteData, tracker, cache);
            if (spriteData != null && spriteData.ImporterData.Count > 0)
                context.SetContextObject(spriteData);
            return result;
        }

        static ReturnCode Run(IBuildParameters parameters, IBuildContent content, IDependencyData dependencyData, out IBuildSpriteData spriteData, IProgressTracker tracker = null, IBuildCache cache = null)
        {
            var globalUsage = new BuildUsageTagGlobal();
            foreach (SceneDependencyInfo sceneInfo in dependencyData.SceneInfo.Values)
                globalUsage |= sceneInfo.globalUsage;

            spriteData = new BuildSpriteData();

            foreach (GUID asset in content.Assets)
            {
                var assetInfo = new AssetLoadInfo();
                var usageTags = new BuildUsageTagSet();
                string assetPath = AssetDatabase.GUIDToAssetPath(asset.ToString());

                var cacheEntry = new CacheEntry { Guid = asset };
                if (parameters.UseCache && cache != null)
                {
                    cacheEntry = cache.GetCacheEntry(asset);
                    var result = cache.IsCacheEntryValid(cacheEntry);
                    if (result && cache.TryLoadFromCache(cacheEntry, ref assetInfo, ref usageTags))
                    {
                        if (!tracker.UpdateInfoUnchecked(string.Format("{0} (Cached)", assetPath)))
                            return ReturnCode.Canceled;

                        SetSpriteImporterData(asset, assetPath, assetInfo.includedObjects, spriteData);
                        SetOutputInformation(asset, assetInfo, usageTags, dependencyData);
                        continue;
                    }
                }

                if (!tracker.UpdateInfoUnchecked(assetPath))
                    return ReturnCode.Canceled;

                assetInfo.asset = asset;
                assetInfo.includedObjects = new List<ObjectIdentifier>(ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(asset, parameters.Target));
                var includedObjects = assetInfo.includedObjects.ToArray();
                assetInfo.referencedObjects = new List<ObjectIdentifier>(ContentBuildInterface.GetPlayerDependenciesForObjects(includedObjects, parameters.Target, parameters.ScriptInfo));
                ContentBuildInterface.CalculateBuildUsageTags(assetInfo.referencedObjects.ToArray(), includedObjects, globalUsage, usageTags);

                SetSpriteImporterData(asset, assetPath, includedObjects, spriteData);
                SetOutputInformation(asset, assetInfo, usageTags, dependencyData);
                if (parameters.UseCache && cache != null && !cache.TrySaveToCache(cacheEntry, assetInfo, usageTags))
                    BuildLogger.LogWarning("Unable to cache AssetCachedDependency results for asset '{0}'.", AssetDatabase.GUIDToAssetPath(asset.ToString()));
            }

            return ReturnCode.Success;
        }

        static void SetSpriteImporterData(GUID asset, string assetPath, IEnumerable<ObjectIdentifier> includedObjects, IBuildSpriteData spriteData)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && importer.textureType == TextureImporterType.Sprite)
            {
                var importerData = new SpriteImporterData();
                importerData.PackedSprite = !string.IsNullOrEmpty(importer.spritePackingTag);
                importerData.SourceTexture = includedObjects.First();
                spriteData.ImporterData.Add(asset, importerData);
            }
        }

        static void SetOutputInformation(GUID asset, AssetLoadInfo assetInfo, BuildUsageTagSet usageTags, IDependencyData dependencyData)
        {
            // Add generated asset information to BuildDependencyData
            dependencyData.AssetInfo.Add(asset, assetInfo);
            dependencyData.AssetUsage.Add(asset, usageTags);
        }
    }
}
