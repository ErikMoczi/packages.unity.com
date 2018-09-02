using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.AssetBundle;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.Utilities;
using UnityEditor.Build.Tasks;
using System.IO;
using UnityEngine.ResourceManagement;
using UnityEngine.AddressableAssets;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.AddressableAssets
{
    public class BuildScript2
    {
        [InitializeOnLoadMethod]
        static void Init()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayer);
            EditorApplication.playModeStateChanged += OnEditorPlayModeChanged;
        }

        static void BuildPlayer(BuildPlayerOptions ops)
        {
            if(PrepareRuntimeData(true, (ops.options & BuildOptions.Development) != BuildOptions.None, (ops.options & BuildOptions.ConnectWithProfiler) != BuildOptions.None, false, false, ops.targetGroup, ops.target))
                BuildPipeline.BuildPlayer(ops);
        }
        

        
        private static void OnEditorPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                SceneManagerState.Record();
                if (!PrepareRuntimeData(false, true, true, false, true, BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), EditorUserBuildSettings.activeBuildTarget))
                {
                    EditorApplication.isPlaying = false;
                    ResourceManagerRuntimeData.Cleanup();
                    VirtualAssetBundleRuntimeData.Cleanup();
                    SceneManagerState.Restore();
                }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                ResourceManagerRuntimeData.Cleanup();
                VirtualAssetBundleRuntimeData.Cleanup();
                SceneManagerState.Restore();
            }
        }

        static bool LoadFromCache(AddressableAssetSettings aaSettings, string settingsHash, ref ResourceManagerRuntimeData runtimeData, ref ResourceLocationList contentCatalog)
        {
            if (!ResourceManagerRuntimeData.LoadFromLibrary(aaSettings.buildSettings.editorPlayMode.ToString(), ref runtimeData, ref contentCatalog))
                return false;

            if (runtimeData.settingsHash != settingsHash)
            {
                ResourceManagerRuntimeData.DeleteFromLibrary(aaSettings.buildSettings.editorPlayMode.ToString());
                if (runtimeData.resourceProviderMode == ResourceManagerRuntimeData.EditorPlayMode.VirtualMode)
                    VirtualAssetBundleRuntimeData.DeleteFromLibrary();
                return false;
            }

            if (runtimeData.resourceProviderMode == ResourceManagerRuntimeData.EditorPlayMode.VirtualMode)
            {
                if (!VirtualAssetBundleRuntimeData.CopyFromLibraryToPlayer())
                    WriteVirtualBundleDataTask.Run(aaSettings, runtimeData, contentCatalog);
            }

            return runtimeData.CopyFromLibraryToPlayer(aaSettings.buildSettings.editorPlayMode.ToString());
        }


        public static bool PrepareRuntimeData(bool isPlayerBuild, bool isDevBuild, bool allowProfilerEvents, bool forceRebuild, bool enteringPlayMode, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            var aaSettings = AddressableAssetSettings.GetDefault(false, false);
            if (aaSettings == null)
                return true;

            var settingsHash = aaSettings.currentHash.ToString();
            ResourceManagerRuntimeData runtimeData = null;
            ResourceLocationList contentCatalog = null;

            forceRebuild = true;
            if (!forceRebuild && LoadFromCache(aaSettings, settingsHash, ref runtimeData, ref contentCatalog))
            {
                if (enteringPlayMode && runtimeData.resourceProviderMode != ResourceManagerRuntimeData.EditorPlayMode.PackedMode)
                    AddAddressableScenesToEditorBuildSettingsSceneList(aaSettings, runtimeData);
                return true;
            }


            runtimeData = new ResourceManagerRuntimeData(isPlayerBuild ? ResourceManagerRuntimeData.EditorPlayMode.PackedMode : aaSettings.buildSettings.editorPlayMode);
            contentCatalog = new ResourceLocationList();
            contentCatalog.labels = aaSettings.labelTable.labelNames;
            runtimeData.profileEvents = allowProfilerEvents && aaSettings.buildSettings.postProfilerEvents;

            var allAssets = new Dictionary<GUID, AddressableAssetSettings.AssetGroup.AssetEntry>();
            foreach (var assetGroup in aaSettings.groups)
            {
                var allEntries = new List<AddressableAssetSettings.AssetGroup.AssetEntry>();
                foreach (var a in assetGroup.entries)
                    a.GatherAllAssets(allEntries, aaSettings);
                foreach (var a in allEntries)
                    allAssets.Add(new GUID(a.guid), a);
            }

            var buildContent = new BuildContent(allAssets.Keys);
            using (var progressTracker = new TimeThrottledProgressTracker(100))
            {
                var buildParams = new BuildParameters(buildTarget, buildTargetGroup, aaSettings.buildSettings.bundleBuildPath, ContentPipeline.kTempBuildPath);
                buildParams.UseCache = false;
               
                var dependencyData = new BuildDependencyData();
                var writeData = new BuildWriteData();
                var bundleBuildResults = new BuildBuildResults();
                using (var buildCleanup = new BuildStateCleanup(true, buildParams.TempOutputFolder))
                {

                    var buildContext = new BuildContext(buildContent, buildParams, bundleBuildResults, dependencyData, writeData, new LinearPackedIdentifiers(100));
                    buildContext.SetContextObject(progressTracker);

                    var buildTasks = new List<IBuildTask>();

                    // Setup
                    buildTasks.Add(new ProjectInCleanState());
                    buildTasks.Add(new SwitchToBuildPlatform());
                    buildTasks.Add(new RebuildAtlasCache());

                    // Dependency
                    buildTasks.Add(new CalculateSceneDependencyData());
                    buildTasks.Add(new CalculateAssetDependencyData());
                    buildTasks.Add(new StripUnusedSpriteSources());

                    // Packing
                    buildTasks.Add(new GenerateReleaseAutoPacking());
                    buildTasks.Add(new GenerateCommands());

                    // Writing
                    buildTasks.Add(new WriteSerializedFiles());

                    var result = BuildTasksRunner.Run(buildTasks, buildContext);
                    if (result < ReturnCodes.Success)
                    {
                        Debug.Log("Build Failed, result = " + result);
                        return false;
                    }

                    var serializedFileGroupReferences = new Dictionary<string, AddressableAssetSettings.AssetGroup>();
                    foreach (var kvp in writeData.AssetToFiles)
                    {
                        AddressableAssetSettings.AssetGroup.AssetEntry assetEntry = null;
                        if (allAssets.TryGetValue(kvp.Key, out assetEntry))
                        {
                            if (assetEntry.isScene)
                            {
                                //handle scenes here...
                            }
                            else
                            {
                                var objs = BundleBuildInterface.GetPlayerObjectIdentifiersInAsset(kvp.Key, buildParams.Target);
                                Debug.LogFormat("Added SF location for {0}.", assetEntry.address);
                                contentCatalog.locations.Add(new ResourceLocationData(
                                        assetEntry.address,
                                        assetEntry.guid,
                                        objs[0].localIdentifierInFile.ToString(),
                                        "UnityEngine.ResourceManagement.SerializedFileAssetProvider",
                                        ResourceLocationData.LocationType.String,
                                        aaSettings.GetLabelMask(assetEntry.labels),
                                        AssetDatabase.GetMainAssetTypeAtPath(assetEntry.assetPath).FullName,
                                        kvp.Value.ToArray()));

                                foreach (var f in kvp.Value)
                                {
                                    AddressableAssetSettings.AssetGroup group = null;
                                    if (!serializedFileGroupReferences.TryGetValue(f, out group) || assetEntry.parentGroup.priority > group.priority)
                                        serializedFileGroupReferences[f] = group = assetEntry.parentGroup;
                                }
                            }
                        }
                    }
                    foreach (var f in serializedFileGroupReferences)
                    {
                        var id = "{Application.streamingAssetsPath}/" + f.Key;//for now, this will use the group/processor later
                        Debug.LogFormat("Added SF location for {0}.", f.Key + f.Value.name);
                        contentCatalog.locations.Add(new ResourceLocationData(f.Key, "", id, "UnityEngine.ResourceManagement.SerializedFileProvider"));
                    }

                    /*
                    foreach (var w in writeData.WriteOperations)
                    {
                        foreach (var o in w.command.serializeObjects)
                        {
                            AddressableAssetSettings.AssetGroup.AssetEntry assetEntry = null;
                            if (allAssets.TryGetValue(o.serializationObject.guid, out assetEntry))
                            {
                                //get highest priority group that references this file
                                AddressableAssetSettings.AssetGroup groupThatReferenceFile = null;
                                if (!serializedFileGroupReferences.TryGetValue(w.command.fileName, out groupThatReferenceFile))
                                    serializedFileGroupReferences.Add(w.command.fileName, groupThatReferenceFile = assetEntry.parentGroup);
                                else if (groupThatReferenceFile.priority < assetEntry.parentGroup.priority)
                                    serializedFileGroupReferences[w.command.fileName] = groupThatReferenceFile = assetEntry.parentGroup;

                                Debug.LogFormat("Added location for {0}.", assetEntry.address);
                                contentCatalog.locations.Add(new ResourceLocationData(
                                    assetEntry.address, 
                                    assetEntry.guid,
                                    o.serializationObject.localIdentifierInFile.ToString(),
                                    "UnityEngine.ResourceManagement.SerializedFileAssetProvider",
                                    ResourceLocationData.LocationType.String, 
                                    aaSettings.GetLabelMask(assetEntry.labels),
                                    AssetDatabase.GetMainAssetTypeAtPath(assetEntry.assetPath).FullName,
                                    writeData.AssetToFiles[o.serializationObject.guid].ToArray())); //double check with ryan
                            }
                        }
                    }
                    foreach (var f in serializedFileGroupReferences)
                    {
                        var id = "{Application.streamingAssetsPath}/" + f.Key;//for now, this will use the group/processor later
                        Debug.LogFormat("Added SF location for {0}.", f.Key);
                        contentCatalog.locations.Add(new ResourceLocationData(f.Key, "", id, "UnityEngine.ResourceManagement.SerializedFileProvider"));
                    }

                    */
                }
            }

            if (enteringPlayMode && runtimeData.resourceProviderMode != ResourceManagerRuntimeData.EditorPlayMode.PackedMode)
                AddAddressableScenesToEditorBuildSettingsSceneList(aaSettings, runtimeData);

            runtimeData.settingsHash = settingsHash;

            if (aaSettings.buildSettings.downloadRemoteCatalog)
            {
                ResourceLocationData remoteLocation = new ResourceLocationData("RemoteCatalog", "",
                    aaSettings.buildSettings.remoteCatalogLocation.Evaluate(aaSettings.profileSettings, aaSettings.activeProfile),
                    typeof(JsonAssetProvider).FullName, ResourceLocationData.LocationType.String, 0, "", null);
                runtimeData.catalogLocations.Add(remoteLocation);
            }

            ResourceLocationData localCatalogLocation = new ResourceLocationData("LocalCatalog", "",
                ResourceManagerRuntimeData.PlayerCatalogLoadLocation, 
                typeof(JsonAssetProvider).FullName, ResourceLocationData.LocationType.String, 0, "", null);
            runtimeData.catalogLocations.Add(localCatalogLocation);


            runtimeData.Save(contentCatalog, aaSettings.buildSettings.editorPlayMode.ToString());
            var catalogBuildLocation = aaSettings.buildSettings.remoteCatalogBuildLocation.Evaluate(aaSettings.profileSettings, aaSettings.activeProfile);
            if (!string.IsNullOrEmpty(catalogBuildLocation))
            {
                var dirName = Path.GetDirectoryName(catalogBuildLocation);
                if (!string.IsNullOrEmpty(dirName)  && !Directory.Exists(dirName))
                    Directory.CreateDirectory(dirName);
                if (File.Exists(catalogBuildLocation))
                    File.Delete(catalogBuildLocation);
                File.Copy(ResourceManagerRuntimeData.PlayerCatalogLocation, catalogBuildLocation);
            }

            Debug.Log("Processed  " + aaSettings.assetEntries.Count() + " addressable assets in " + timer.Elapsed.TotalSeconds + " secs.");
            Resources.UnloadUnusedAssets();
            return true;
        }

        private static void AddAddressableScenesToEditorBuildSettingsSceneList(AddressableAssetSettings settings, ResourceManagerRuntimeData runtimeData)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var sceneEntries = new List<AddressableAssetSettings.AssetGroup.AssetEntry>();
            settings.GetAllSceneEntries(sceneEntries);
            foreach (var entry in sceneEntries)
                scenes.Add(new EditorBuildSettingsScene(new GUID(entry.guid), true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

    }
}
