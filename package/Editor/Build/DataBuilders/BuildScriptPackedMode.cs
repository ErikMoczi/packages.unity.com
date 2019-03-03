﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using Debug = UnityEngine.Debug;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    /// <summary>
    /// Build scripts used for player builds and running with bundles in the editor.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildScriptPacked.asset", menuName = "Addressable Assets/Data Builders/Packed Mode")]
    public class BuildScriptPackedMode : BuildScriptBase
    {
        public override string Name
        {
            get
            {
                return "Packed Mode";
            }
        }

        public override bool CanBuildData<T>()
        {
            return typeof(T) == typeof(AddressablesPlayerBuildResult);
        }

        public override TResult BuildData<TResult>(IDataBuilderContext context)
        {
            var timer = new Stopwatch();
            timer.Start();
            var aaSettings = context.GetValue<AddressableAssetSettings>(AddressablesBuildDataBuilderContext.BuildScriptContextConstants.kAddressableAssetSettings);

            //gather entries
            var playerBuildVersion = context.GetValue<string>(AddressablesBuildDataBuilderContext.BuildScriptContextConstants.kPlayerBuildVersion);
            var locations = new List<ContentCatalogDataEntry>();
            var allBundleInputDefs = new List<AssetBundleBuild>();
            var bundleToAssetGroup = new Dictionary<string, string>();
            var runtimeData = new ResourceManagerRuntimeData();
            runtimeData.BuildTarget = context.GetValue<BuildTarget>(AddressablesBuildDataBuilderContext.BuildScriptContextConstants.kBuildTarget).ToString();
            runtimeData.ProfileEvents = ProjectConfigData.postProfilerEvents;
            runtimeData.LogResourceManagerExceptions = aaSettings.buildSettings.LogResourceManagerExceptions;
            runtimeData.ProfileEvents = ProjectConfigData.postProfilerEvents;

            var createdProviderIds = new HashSet<string>();
            var linker = new LinkXmlGenerator();
            var resourceProviderData = new List<ObjectInitializationData>();
            foreach (var assetGroup in aaSettings.groups)
            {
                if (assetGroup.HasSchema<PlayerDataGroupSchema>())
                {
                    if (CreateLocationsForPlayerData(assetGroup, locations))
                    {
                        if (!createdProviderIds.Contains(typeof(LegacyResourcesProvider).Name))
                        {
                            createdProviderIds.Add(typeof(LegacyResourcesProvider).Name);
                            resourceProviderData.Add(ObjectInitializationData.CreateSerializedInitializationData(typeof(LegacyResourcesProvider)));
                            linker.AddTypes(typeof(LegacyResourcesProvider));
                        }
                    }

                    continue;
                }

                var schema = assetGroup.GetSchema<BundledAssetGroupSchema>();
                if (schema == null || !schema.IncludeInBuild)
                    continue;

                var bundledProviderId = schema.GetBundleCachedProviderId();
                var assetProviderId = schema.GetAssetCachedProviderId();
                if (!createdProviderIds.Contains(bundledProviderId))
                {
                    createdProviderIds.Add(bundledProviderId);

                    var bundleProviderType = schema.AssetBundleProviderType.Value;
                    linker.AddTypes(bundleProviderType);

                    var bundleProviderData = ObjectInitializationData.CreateSerializedInitializationData(bundleProviderType);
                    var bundleCachedProviderData = ObjectInitializationData.CreateSerializedInitializationData<CachedProvider>(bundledProviderId,
                        new CachedProvider.Settings
                        {
                            maxLruAge = schema.BundleCachedProviderMaxLRUAge < 0 ? 0 : schema.BundleCachedProviderMaxLRUAge,
                            maxLruCount = schema.BundleCachedProviderMaxLRUCount < 0 ? 0 : schema.BundleCachedProviderMaxLRUCount,
                            InternalProviderData = bundleProviderData
                        });
                    linker.AddTypes(bundleCachedProviderData.GetRuntimeTypes());
                    resourceProviderData.Add(bundleCachedProviderData);
                }

                if (!createdProviderIds.Contains(assetProviderId))
                {
                    createdProviderIds.Add(assetProviderId);
                    var assetProviderType = schema.BundledAssetProviderType.Value;
                    linker.AddTypes(assetProviderType);

                    var assetProviderData = ObjectInitializationData.CreateSerializedInitializationData(assetProviderType);
                    var assetCachedProviderData = ObjectInitializationData.CreateSerializedInitializationData<CachedProvider>(assetProviderId,
                        new CachedProvider.Settings
                        {
                            maxLruAge = schema.AssetCachedProviderMaxLRUAge < 0 ? 0 : schema.AssetCachedProviderMaxLRUAge,
                            maxLruCount = schema.AssetCachedProviderMaxLRUCount < 0 ? 0 : schema.AssetCachedProviderMaxLRUCount,
                            InternalProviderData = assetProviderData
                        });
                    resourceProviderData.Add(assetCachedProviderData);
                    linker.AddTypes(assetProviderData.GetRuntimeTypes());
                    linker.AddTypes(assetCachedProviderData.GetRuntimeTypes());
                }

                var packTogether = schema.BundleMode == BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                var bundleInputDefs = new List<AssetBundleBuild>();
                ProcessGroup(assetGroup, bundleInputDefs, locations, packTogether);
                for (int i = 0; i < bundleInputDefs.Count; i++)
                {
                    if (bundleToAssetGroup.ContainsKey(bundleInputDefs[i].assetBundleName))
                    {
                        var bid = bundleInputDefs[i];
                        int count = 1;
                        var newName = bid.assetBundleName;
                        while (bundleToAssetGroup.ContainsKey(newName) && count < 1000)
                            newName = bid.assetBundleName.Replace(".bundle", string.Format("{0}.bundle", count++));
                        bundleInputDefs[i] = new AssetBundleBuild { assetBundleName = newName, addressableNames = bid.addressableNames, assetBundleVariant = bid.assetBundleVariant, assetNames = bid.assetNames };
                    }

                    bundleToAssetGroup.Add(bundleInputDefs[i].assetBundleName, assetGroup.Guid);
                }
                allBundleInputDefs.AddRange(bundleInputDefs);
            }
            ExtractDataTask extractData = new ExtractDataTask();
            

            if (allBundleInputDefs.Count > 0)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return AddressableAssetBuildResult.CreateResult<TResult>(null, 0, timer.Elapsed.TotalSeconds, "Unsaved scenes");

                var buildTarget = context.GetValue<BuildTarget>(AddressablesBuildDataBuilderContext.BuildScriptContextConstants.kBuildTarget);
                var buildTargetGroup = context.GetValue<BuildTargetGroup>(AddressablesBuildDataBuilderContext.BuildScriptContextConstants.kBuildTargetGroup);
                var buildParams = new BundleBuildParameters(buildTarget, buildTargetGroup, aaSettings.buildSettings.bundleBuildPath);
                buildParams.UseCache = true;
                buildParams.BundleCompression = aaSettings.buildSettings.compression;

                var buildTasks = RuntimeDataBuildTasks(aaSettings.DefaultGroup.Name + "_UnityBuiltInShaders.bundle");
                buildTasks.Add(extractData);

                var aaContext = new AddressableAssetsBuildContext
                {
                    settings = aaSettings,
                    runtimeData = runtimeData,
                    bundleToAssetGroup = bundleToAssetGroup,
                    locations = locations
                };
                string aaPath = aaSettings.AssetPath;
                IBundleBuildResults results;
                var exitCode = ContentPipeline.BuildAssetBundles(buildParams, new BundleBuildContent(allBundleInputDefs), out results, buildTasks, aaContext);

                if (exitCode < ReturnCode.Success)
                    return AddressableAssetBuildResult.CreateResult<TResult>(null, 0, timer.Elapsed.TotalSeconds, "SBP Error" + exitCode);
                if (aaSettings == null && !string.IsNullOrEmpty(aaPath))
                    aaSettings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(aaPath);

                foreach (var assetGroup in aaSettings.groups)
                {
                    List<string> bundles;
                    if (aaContext.assetGroupToBundles.TryGetValue(assetGroup, out bundles))
                        PostProcessBundles(assetGroup, bundles, results, extractData.WriteData, runtimeData, locations);
                }
                foreach (var r in results.WriteResults)
                    linker.AddTypes(r.Value.includedTypes);
            }

            linker.AddTypes(typeof(InstanceProvider), typeof(SceneProvider));

            //save catalog
            var contentCatalog = new ContentCatalogData(locations);
            contentCatalog.ResourceProviderData.AddRange(resourceProviderData);
            contentCatalog.InstanceProviderData = ObjectInitializationData.CreateSerializedInitializationData<InstanceProvider>();
            contentCatalog.SceneProviderData = ObjectInitializationData.CreateSerializedInitializationData<SceneProvider>();

            var createdCatalogs = new HashSet<string>();
            foreach (var assetGroup in aaSettings.groups)
                CreateCatalog(assetGroup, contentCatalog, runtimeData.CatalogLocations, playerBuildVersion, createdCatalogs);

			runtimeData.CatalogLocations.Sort((a, b) => a.Keys[0].CompareTo(b.Keys[0]));

            var catalogFilename = context.GetValue(AddressablesBuildDataBuilderContext.BuildScriptContextConstants.kRuntimeCatalogFilename, "catalog.json");
            var catalogPath = Addressables.BuildPath + "/" + catalogFilename;
            WriteFile(catalogPath, JsonUtility.ToJson(contentCatalog));
            runtimeData.CatalogLocations.Add(new ResourceLocationData(new[] { "catalogs" }, "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/" + catalogFilename, typeof(JsonAssetProvider)));



            foreach (var io in aaSettings.InitializationObjects)
            {
                if (io is IObjectInitializationDataProvider)
                {
                    var id = (io as IObjectInitializationDataProvider).CreateObjectInitializationData();
                    runtimeData.InitializationObjects.Add(id);
                    linker.AddTypes(id.ObjectType.Value);
                    linker.AddTypes(id.GetRuntimeTypes());
                }
            }
            linker.AddTypes(typeof(Addressables));
            linker.Save(Addressables.BuildPath + "/link.xml");
            var settingsPath = Addressables.BuildPath + "/" + context.GetValue(AddressablesBuildDataBuilderContext.BuildScriptContextConstants.kRuntimeSettingsFilename, "settings.json");
            WriteFile(settingsPath, JsonUtility.ToJson(runtimeData));

            var opResult = AddressableAssetBuildResult.CreateResult<TResult>(settingsPath, locations.Count, timer.Elapsed.TotalSeconds);
            //save content update data if building for the player
            var allEntries = new List<AddressableAssetEntry>();
            aaSettings.GetAllAssets(allEntries, g => g.HasSchema<ContentUpdateGroupSchema>() && g.GetSchema<ContentUpdateGroupSchema>().StaticContent);
            var tempPath = Path.GetDirectoryName(Application.dataPath) + "/Library/com.unity.addressables/addressables_content_state.bin";

            if (extractData.BuildCache != null && ContentUpdateScript.SaveContentState(tempPath, allEntries, extractData.BuildCache, playerBuildVersion))
            {
                try
                {
                    File.Copy(tempPath, ContentUpdateScript.GetContentStateDataPath(false), true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            return opResult;
        }

        internal static void ProcessGroup(AddressableAssetGroup assetGroup, List<AssetBundleBuild> bundleInputDefs, List<ContentCatalogDataEntry> locationData, bool packTogether)
        {
            if (packTogether)
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

        static void GenerateBuildInputDefinitions(List<AddressableAssetEntry> allEntries, List<AssetBundleBuild> buildInputDefs, string groupName, string address)
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

        static AssetBundleBuild GenerateBuildInputDefinition(List<AddressableAssetEntry> assets, string name)
        {
            var assetsInputDef = new AssetBundleBuild();
            assetsInputDef.assetBundleName = name.ToLower().Replace(" ", "").Replace('\\', '/').Replace("//", "/");
            var assetIds = new List<string>(assets.Count);
            foreach (var a in assets)
            {
                assetIds.Add(a.AssetPath);
            }
            assetsInputDef.assetNames = assetIds.ToArray();
            assetsInputDef.addressableNames = new string[0];
            return assetsInputDef;
        }

        internal static void CreateCatalog(AddressableAssetGroup group, ContentCatalogData contentCatalog, List<ResourceLocationData> locations, string playerVersion, HashSet<string> createdCatalogs)
        {
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema == null || !schema.IncludeInBuild)
                return;

            var aaSettings = group.Settings;
            var lPath = schema.LoadPath.GetValue(group.Settings);
            if (createdCatalogs.Contains(lPath))
                return;
            createdCatalogs.Add(lPath);

            var bPath = schema.BuildPath.GetValue(group.Settings);

            var buildPath = bPath + aaSettings.profileSettings.EvaluateString(aaSettings.activeProfileId, "/catalog_" + playerVersion + ".json");
            var remoteHashLoadPath = lPath + "/" + "catalog_" + playerVersion + ".hash";

            var localCacheLoadPath = "{UnityEngine.Application.persistentDataPath}/com.unity.addressables/catalog_" + playerVersion + ".hash";

            var jsonText = JsonUtility.ToJson(contentCatalog);
            var contentHash = HashingMethods.Calculate(jsonText).ToString();

            WriteFile(buildPath, jsonText);
            WriteFile(buildPath.Replace(".json", ".hash"), contentHash);

            var depKeys = new[] { "RemoteCatalogHash" + group.Guid, "LocalCatalogHash" + group.Guid };

            var remoteHash = new ResourceLocationData(new[] { depKeys[0] }, remoteHashLoadPath, typeof(TextDataProvider));
            var localHash = new ResourceLocationData(new[] { depKeys[1] }, localCacheLoadPath, typeof(TextDataProvider));

            var internalId = remoteHashLoadPath.Replace(".hash", ".json");
            locations.Add(new ResourceLocationData(new[] { schema.ContentCataLogLoadOrder + "_Catalog_" + group.Guid, "catalogs" }, internalId, typeof(ContentCatalogProvider), depKeys));
            locations.Add(localHash);
            locations.Add(remoteHash);
        }

        static IList<IBuildTask> RuntimeDataBuildTasks(string builtinShaderBundleName)
        {
            var buildTasks = new List<IBuildTask>();

            // Setup
            buildTasks.Add(new SwitchToBuildPlatform());
            buildTasks.Add(new RebuildSpriteAtlasCache());

            // Player Scripts
            buildTasks.Add(new BuildPlayerScripts());

            // Dependency
            buildTasks.Add(new CalculateSceneDependencyData());
            buildTasks.Add(new CalculateAssetDependencyData());
            buildTasks.Add(new StripUnusedSpriteSources());
            buildTasks.Add(new CreateBuiltInShadersBundle(builtinShaderBundleName));

            // Packing
            buildTasks.Add(new GenerateBundlePacking());
            buildTasks.Add(new UpdateBundleObjectLayout());
            buildTasks.Add(new GenerateLocationListsTask());

            buildTasks.Add(new GenerateBundleCommands());
            buildTasks.Add(new GenerateSubAssetPathMaps());
            buildTasks.Add(new GenerateBundleMaps());

            // Writing
            buildTasks.Add(new WriteSerializedFiles());
            buildTasks.Add(new ArchiveAndCompressBundles());
            //   buildTasks.Add(new PostProcessBundlesTask());

            return buildTasks;
        }

        static bool IsInternalIdLocal(string path)
        {
            return path.StartsWith("{UnityEngine.AddressableAssets.Addressables.RuntimePath}");
        }

        internal static void PostProcessBundles(AddressableAssetGroup assetGroup, List<string> bundles, IBundleBuildResults buildResult, IWriteData writeData, ResourceManagerRuntimeData runtimeData, List<ContentCatalogDataEntry> locations)
        {
            var schema = assetGroup.GetSchema<BundledAssetGroupSchema>();
            if (schema == null)
                return;

            var path = schema.BuildPath.GetValue(assetGroup.Settings);
            if (string.IsNullOrEmpty(path))
                return;

            foreach (var originalBundleName in bundles)
            {
                var newBundleName = originalBundleName;
                var info = buildResult.BundleInfos[newBundleName];
                ContentCatalogDataEntry dataEntry = locations.First(s => newBundleName == (string)s.Keys[0]);
                if (dataEntry != null)
                {
                    var requestOptions = new AssetBundleRequestOptions
                    {
                        Crc =  schema.UseAssetBundleCrc ? info.Crc : 0,
                        Hash = schema.UseAssetBundleCache ? info.Hash.ToString() : "",
                        ChunkedTransfer = schema.ChunkedTransfer,
                        RedirectLimit = schema.RedirectLimit,
                        RetryCount = schema.RetryCount,
                        Timeout = schema.Timeout,
                        BundleName = Path.GetFileName(info.FileName),
                        BundleSize = GetFileSize(info.FileName)
                    };
                    dataEntry.Data = requestOptions;
                    dataEntry.InternalId = dataEntry.InternalId.Replace(".bundle", "_" + info.Hash + ".bundle");
                    newBundleName = newBundleName.Replace(".bundle", "_" + info.Hash + ".bundle");
                }
                else
                {
                    Debug.LogWarningFormat("Unable to find ContentCatalogDataEntry for bundle {0}.", newBundleName);
                }

                var targetPath = Path.Combine(path, newBundleName);
                if (!Directory.Exists(Path.GetDirectoryName(targetPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.Copy(Path.Combine(assetGroup.Settings.buildSettings.bundleBuildPath, originalBundleName), targetPath, true);
            }
        }

        private static long GetFileSize(string fileName)
        {
            try
            {
                return new FileInfo(fileName).Length;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return 0;
            }

        }

        public override void ClearCachedData()
        {
            if (Directory.Exists(Addressables.BuildPath))
            {
                try
                {
                    var catalogPath = Addressables.BuildPath + "/catalog.json";
                    var settingsPath = Addressables.BuildPath + "/settings.json";
                    DeleteFile(catalogPath);
                    DeleteFile(settingsPath);
                    Directory.Delete(Addressables.BuildPath, true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

        }
    }
}