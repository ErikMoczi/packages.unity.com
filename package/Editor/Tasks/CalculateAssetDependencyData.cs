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

            IBuildParameters parameters = context.GetContextObject<IBuildParameters>();

            IProgressTracker tracker;
            context.TryGetContextObject(out tracker);
            IBuildCache cache = null;
            if (parameters.UseCache)
                context.TryGetContextObject(out cache);

            IBuildSpriteData spriteData;
            var result = Run(parameters, context.GetContextObject<IBuildContent>(), context.GetContextObject<IDependencyData>(), out spriteData, tracker, cache);
            if (spriteData != null && spriteData.ImporterData.Count > 0)
                context.SetContextObject(spriteData);
            return result;
        }

        static CachedInfo GetCachedInfo(IBuildCache cache, GUID asset, AssetLoadInfo assetInfo, BuildUsageTagSet usageTags, SpriteImporterData importerData)
        {
            var info = new CachedInfo();
            info.Asset = cache.GetCacheEntry(asset);

            var dependencies = new HashSet<CacheEntry>();
            foreach (var reference in assetInfo.referencedObjects)
                dependencies.Add(cache.GetCacheEntry(reference));
            info.Dependencies = dependencies.ToArray();

            info.Data = new object[] { assetInfo, usageTags, importerData };

            return info;
        }

        static ReturnCode Run(IBuildParameters parameters, IBuildContent content, IDependencyData dependencyData, out IBuildSpriteData spriteData, IProgressTracker tracker, IBuildCache cache)
        {
            var globalUsage = new BuildUsageTagGlobal();
            foreach (SceneDependencyInfo sceneInfo in dependencyData.SceneInfo.Values)
                globalUsage |= sceneInfo.globalUsage;

            spriteData = new BuildSpriteData();

            IList<CachedInfo> cachedInfo = null;
            List<CachedInfo> uncachedInfo = null;
            if (cache != null)
            {
                IList<CacheEntry> entries = content.Assets.Select(cache.GetCacheEntry).ToList();
                cache.LoadCachedData(entries, out cachedInfo);

                uncachedInfo = new List<CachedInfo>();
            }

            for (int i = 0; i < content.Assets.Count; i++)
            {
                GUID asset = content.Assets[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(asset.ToString());

                AssetLoadInfo assetInfo;
                BuildUsageTagSet usageTags;
                SpriteImporterData importerData;

                if (cachedInfo != null && cachedInfo[i] != null)
                {
                    if (!tracker.UpdateInfoUnchecked(string.Format("{0} (Cached)", assetPath)))
                        return ReturnCode.Canceled;

                    assetInfo = cachedInfo[i].Data[0] as AssetLoadInfo;
                    usageTags = cachedInfo[i].Data[1] as BuildUsageTagSet;
                    importerData = cachedInfo[i].Data[2] as SpriteImporterData;
                }
                else
                {
                    if (!tracker.UpdateInfoUnchecked(assetPath))
                        return ReturnCode.Canceled;

                    assetInfo = new AssetLoadInfo();
                    usageTags = new BuildUsageTagSet();
                    importerData = null;

                    assetInfo.asset = asset;
                    var includedObjects = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(asset, parameters.Target);
                    assetInfo.includedObjects = new List<ObjectIdentifier>(includedObjects);
                    var referencedObjects = ContentBuildInterface.GetPlayerDependenciesForObjects(includedObjects, parameters.Target, parameters.ScriptInfo);
                    assetInfo.referencedObjects = new List<ObjectIdentifier>(referencedObjects);
                    ContentBuildInterface.CalculateBuildUsageTags(referencedObjects, includedObjects, globalUsage, usageTags);

                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer != null && importer.textureType == TextureImporterType.Sprite)
                    {
                        importerData = new SpriteImporterData();
                        importerData.PackedSprite = !string.IsNullOrEmpty(importer.spritePackingTag);
                        importerData.SourceTexture = includedObjects.First();
                    }

                    if (cache != null)
                        uncachedInfo.Add(GetCachedInfo(cache, asset, assetInfo, usageTags, importerData));
                }

                SetOutputInformation(asset, assetInfo, usageTags, importerData, dependencyData, spriteData);
            }

            if (cache != null)
                cache.SaveCachedData(uncachedInfo);

            return ReturnCode.Success;
        }

        static void SetOutputInformation(GUID asset, AssetLoadInfo assetInfo, BuildUsageTagSet usageTags, SpriteImporterData importerData, IDependencyData dependencyData, IBuildSpriteData spriteData)
        {
            // Add generated asset information to IDependencyData
            dependencyData.AssetInfo.Add(asset, assetInfo);
            dependencyData.AssetUsage.Add(asset, usageTags);

            // Add generated importer data to IBuildSpriteData
            if (importerData != null)
                spriteData.ImporterData.Add(asset, importerData);
        }
    }
}
