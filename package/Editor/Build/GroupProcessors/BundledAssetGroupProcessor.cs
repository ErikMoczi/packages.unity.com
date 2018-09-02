using System.Collections.Generic;
using System.IO;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;
using UnityEngine.ResourceManagement;
using UnityEngine.AddressableAssets;
using System.Linq;

namespace UnityEditor.AddressableAssets
{
    /// <summary>
    /// TODO - doc
    /// </summary>
    [System.ComponentModel.Description("Packed Content Group")]
    internal class BundledAssetGroupProcessor : AssetGroupProcessor
    {
        /// <summary>
        /// TODO - doc
        /// </summary>
        public enum BundleMode
        {
            PackTogether,
            PackSeparately
        }

        internal override void CreateDefaultData(AddressableAssetGroup assetGroup)
        {
            GetBuildPath(assetGroup);
            GetLoadPath(assetGroup, "");
            GetBundleMode(assetGroup);
            var unused = assetGroup.StaticContent;
        }

        protected string GetBuildPath(AddressableAssetGroup group)
        {
            return GetDataString(group, "BuildPath", "LocalBuildPath", Addressables.BuildPath);
        }

        protected string GetLoadPath(AddressableAssetGroup group, string name)
        {
            return GetDataString(group, "LoadPath", "LocalLoadPath", "{UnityEngine.AddressableAssets.Addressables.RuntimePath}") + "/" + name;
        }

        BundleMode GetBundleMode(AddressableAssetGroup group)
        {
            return group.Data.GetData("BundleMode", BundleMode.PackTogether, true);
        }

        internal override void ProcessGroup(AddressableAssetGroup assetGroup, List<AssetBundleBuild> bundleInputDefs, List<ContentCatalogDataEntry> locationData)
        {
            if (GetBundleMode(assetGroup) == BundleMode.PackTogether)
            {
                var allEntries = new List<AddressableAssetEntry>();
                foreach (var a in assetGroup.entries)
                    a.GatherAllAssets(allEntries, true, true);
                GenerateBuildInputDefinitions(allEntries, bundleInputDefs, assetGroup.Name, "all");
            }
            else
            {
                foreach (var a in assetGroup.entries)
                {
                    var allEntries = new List<AddressableAssetEntry>();
                    a.GatherAllAssets(allEntries, true, true);
                    GenerateBuildInputDefinitions(allEntries, bundleInputDefs, assetGroup.Name, a.address);
                }
            }
        }

        internal override void CreateResourceLocationData(
            AddressableAssetGroup assetGroup,
            string bundleName,
            List<GUID> assetsInBundle,
            Dictionary<GUID, List<string>> assetsToBundles,
            List<ContentCatalogDataEntry> locations)
        {
            var settings = assetGroup.Settings;
            locations.Add(new ContentCatalogDataEntry(bundleName, null, GetLoadPath(assetGroup, bundleName), typeof(AssetBundleProvider)));

            var assets = new List<AddressableAssetEntry>();
            assetGroup.GatherAllAssets(assets, true, true);
            var guidToEntry = new Dictionary<string, AddressableAssetEntry>();
            foreach (var a in assets)
                guidToEntry.Add(a.guid, a);

            foreach (var a in assetsInBundle)
            {
                AddressableAssetEntry entry;
                if (!guidToEntry.TryGetValue(a.ToString(), out entry))
                    continue;
                var assetPath = entry.GetAssetLoadPath(ProjectConfigData.editorPlayMode == ResourceManagerRuntimeData.EditorPlayMode.PackedMode);
                locations.Add(new ContentCatalogDataEntry(entry.address, entry.guid, assetPath, typeof(BundledAssetProvider), entry.labels, assetsToBundles[a].ToArray()));
            }
        }

        static bool IsInternalIdLocal(string path)
        {
            return path.StartsWith("{UnityEngine.AddressableAssets.Addressables.RuntimePath}");
        }

        internal override void PostProcessBundles(AddressableAssetGroup assetGroup, List<string> bundles, IBundleBuildResults buildResult, IWriteData writeData, ResourceManagerRuntimeData runtimeData, List<ContentCatalogDataEntry> locations)
        {

            var path = GetBuildPath(assetGroup);
            if (string.IsNullOrEmpty(path))
                return;

            foreach (var bundleName in bundles)
            {
                var info = buildResult.BundleInfos[bundleName];
                ContentCatalogDataEntry dataEntry = locations.First(s => bundleName == (string)s.Keys[0]);
                bool isLocalBundle = true;
                if (dataEntry != null)
                {
                    isLocalBundle = IsInternalIdLocal(dataEntry.InternalId);
                    if (!isLocalBundle)
                    {
                        var cacheData = new AssetBundleCacheInfo() { Crc = info.Crc, Hash = info.Hash.ToString() };
                        dataEntry.Data = cacheData;
                        dataEntry.InternalId = dataEntry.InternalId.Replace(".bundle", "_" + info.Hash + ".bundle");
                    }
                }

                var targetPath = Path.Combine(path, isLocalBundle ? bundleName : bundleName.Replace(".bundle", "_" + info.Hash + ".bundle"));
                if (!Directory.Exists(Path.GetDirectoryName(targetPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.Copy(Path.Combine(assetGroup.Settings.buildSettings.bundleBuildPath, bundleName), targetPath, true);
            }
        }

        private void GenerateBuildInputDefinitions(List<AddressableAssetEntry> allEntries, List<AssetBundleBuild> buildInputDefs, string groupName, string address)
        {
            var scenes = new List<AddressableAssetEntry>();
            var assets = new List<AddressableAssetEntry>();
            foreach (var e in allEntries)
            {
                if (e.AssetPath.EndsWith(".unity"))
                    scenes.Add(e);
                else
                    assets.Add(e);
            }
            if (assets.Count > 0)
                buildInputDefs.Add(GenerateBuildInputDefinition(assets, groupName + "_assets_" + address + ".bundle"));
            if (scenes.Count > 0)
                buildInputDefs.Add(GenerateBuildInputDefinition(scenes, groupName + "_scenes_" + address + ".bundle"));
        }

        private AssetBundleBuild GenerateBuildInputDefinition(List<AddressableAssetEntry> assets, string name)
        {
            var assetsInputDef = new AssetBundleBuild();
            assetsInputDef.assetBundleName = name.ToLower().Replace(" ", "").Replace('\\', '/').Replace("//", "/");
            var assetIds = new List<string>(assets.Count);
            var assetGuids = new List<string>(assets.Count);
            foreach (var a in assets)
            {
                assetIds.Add(a.AssetPath);
                assetGuids.Add(a.guid);
            }
            assetsInputDef.assetNames = assetIds.ToArray();
            assetsInputDef.addressableNames = new string[0];
            return assetsInputDef;
        }

        internal override void CreateCatalog(AddressableAssetGroup group, ContentCatalogData contentCatalog, List<ResourceLocationData> locations, string playerVersion)
        {
            var aaSettings = group.Settings;
            var buildPath = GetBuildPath(group) + aaSettings.profileSettings.EvaluateString(aaSettings.activeProfileId, "/catalog_" + playerVersion + ".json");
            var remoteHashLoadPath = GetLoadPath(group, "catalog_" + playerVersion + ".hash");
            var localCacheLoadPath = "{UnityEngine.Application.persistentDataPath}/com.unity.addressables/catalog_" + playerVersion + ".hash";

            var jsonText = JsonUtility.ToJson(contentCatalog);
            var contentHash = Build.Pipeline.Utilities.HashingMethods.Calculate(jsonText).ToString();

            var buildPathDir = Path.GetDirectoryName(buildPath);
            if (!Directory.Exists(buildPathDir))
                Directory.CreateDirectory(buildPathDir);
            File.WriteAllText(buildPath, jsonText);
            File.WriteAllText(buildPath.Replace(".json", ".hash"), contentHash);

            var depKeys = new string[] { "RemoteCatalogHash" + group.Guid, "LocalCatalogHash" + group.Guid };

            var remoteHash = new ResourceLocationData(new string[] { depKeys[0] }, remoteHashLoadPath, typeof(TextDataProvider));
            var localHash = new ResourceLocationData(new string[] { depKeys[1] }, localCacheLoadPath, typeof(TextDataProvider));

            var internalId = remoteHashLoadPath.Replace(".hash", ".json");
            var loadOrder = IsInternalIdLocal(internalId) ? "1" : "0";
            locations.Add(new ResourceLocationData(new string[] { loadOrder + "_RemoteCatalog_" + group.Guid, "catalogs" }, internalId, typeof(ContentCatalogProvider), depKeys));
            locations.Add(localHash);
            locations.Add(remoteHash);
        }

    }
}
