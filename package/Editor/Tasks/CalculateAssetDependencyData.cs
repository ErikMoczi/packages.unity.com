using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class CalculateAssetDependencyData : IBuildTask
    {
        public int Version { get { return 1; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IBuildParameters m_Parameters;

        [InjectContext(ContextUsage.In)]
        IBuildContent m_Content;

        [InjectContext]
        IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.InOut, true)]
        IBuildSpriteData m_SpriteData;

        [InjectContext(ContextUsage.In, true)]
        IProgressTracker m_Tracker;

        [InjectContext(ContextUsage.In, true)]
        IBuildCache m_Cache;
#pragma warning restore 649

        CachedInfo GetCachedInfo(GUID asset, AssetLoadInfo assetInfo, BuildUsageTagSet usageTags, SpriteImporterData importerData)
        {
            var info = new CachedInfo();
            info.Asset = m_Cache.GetCacheEntry(asset);

            var dependencies = new HashSet<CacheEntry>();
            foreach (var reference in assetInfo.referencedObjects)
                dependencies.Add(m_Cache.GetCacheEntry(reference));
            info.Dependencies = dependencies.ToArray();

            info.Data = new object[] { assetInfo, usageTags, importerData };

            return info;
        }

        public ReturnCode Run()
        {
            var globalUsage = m_DependencyData.GlobalUsage;
            foreach (SceneDependencyInfo sceneInfo in m_DependencyData.SceneInfo.Values)
                globalUsage |= sceneInfo.globalUsage;

            if (m_SpriteData == null)
                m_SpriteData = new BuildSpriteData();

            IList<CachedInfo> cachedInfo = null;
            List<CachedInfo> uncachedInfo = null;
            if (m_Parameters.UseCache && m_Cache != null)
            {
                IList<CacheEntry> entries = m_Content.Assets.Select(m_Cache.GetCacheEntry).ToList();
                m_Cache.LoadCachedData(entries, out cachedInfo);

                uncachedInfo = new List<CachedInfo>();
            }

            for (int i = 0; i < m_Content.Assets.Count; i++)
            {
                GUID asset = m_Content.Assets[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(asset.ToString());

                AssetLoadInfo assetInfo;
                BuildUsageTagSet usageTags;
                SpriteImporterData importerData;

                if (cachedInfo != null && cachedInfo[i] != null)
                {
                    if (!m_Tracker.UpdateInfoUnchecked(string.Format("{0} (Cached)", assetPath)))
                        return ReturnCode.Canceled;

                    assetInfo = cachedInfo[i].Data[0] as AssetLoadInfo;
                    usageTags = cachedInfo[i].Data[1] as BuildUsageTagSet;
                    importerData = cachedInfo[i].Data[2] as SpriteImporterData;
                }
                else
                {
                    if (!m_Tracker.UpdateInfoUnchecked(assetPath))
                        return ReturnCode.Canceled;

                    assetInfo = new AssetLoadInfo();
                    usageTags = new BuildUsageTagSet();
                    importerData = null;

                    assetInfo.asset = asset;
                    var includedObjects = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(asset, m_Parameters.Target);
                    assetInfo.includedObjects = new List<ObjectIdentifier>(includedObjects);
                    var referencedObjects = ContentBuildInterface.GetPlayerDependenciesForObjects(includedObjects, m_Parameters.Target, m_Parameters.ScriptInfo);
                    assetInfo.referencedObjects = new List<ObjectIdentifier>(referencedObjects);
                    ContentBuildInterface.CalculateBuildUsageTags(referencedObjects, includedObjects, globalUsage, usageTags, m_DependencyData.DependencyUsageCache);

                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer != null && importer.textureType == TextureImporterType.Sprite)
                    {
                        importerData = new SpriteImporterData();
                        importerData.PackedSprite = !string.IsNullOrEmpty(importer.spritePackingTag);
                        importerData.SourceTexture = includedObjects.First();
                    }

                    if (uncachedInfo != null)
                        uncachedInfo.Add(GetCachedInfo(asset, assetInfo, usageTags, importerData));
                }

                SetOutputInformation(asset, assetInfo, usageTags, importerData);
            }

            if (m_SpriteData.ImporterData.Count == 0)
                m_SpriteData = null;

            if (m_Parameters.UseCache && m_Cache != null)
                m_Cache.SaveCachedData(uncachedInfo);

            return ReturnCode.Success;
        }

        void SetOutputInformation(GUID asset, AssetLoadInfo assetInfo, BuildUsageTagSet usageTags, SpriteImporterData importerData)
        {
            // Add generated asset information to IDependencyData
            m_DependencyData.AssetInfo.Add(asset, assetInfo);
            m_DependencyData.AssetUsage.Add(asset, usageTags);

            // Add generated importer data to IBuildSpriteData
            if (importerData != null)
                m_SpriteData.ImporterData.Add(asset, importerData);
        }
    }
}
