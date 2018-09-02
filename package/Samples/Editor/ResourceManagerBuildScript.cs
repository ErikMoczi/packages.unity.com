#if RM_SAMPLES

using System.Collections.Generic;
using UnityEditor;
using System.IO;
using UnityEngine;
using ResourceManagement.ResourceProviders;
using ResourceManagement.ResourceProviders.Simulation;
using System;

namespace ResourceManagement.Samples
{
    /*
     * This sample build script will gather all prefabs from a folder in the project and create addresses for them and create the runtime data needed
     * to load and use the ResourceManager.  It is intended to be used as a starting point for those that want to implement a custom solution in a project.  
     * A more comprehensive system is implemented in the Addressable package and is inteneded to fit most use cases.
     */
    public class ResourceManagerBuildScript
    {
        //call from an [InitializeOnLoadMethod] to use
        static public void Initialize()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayer);
            EditorApplication.playModeStateChanged += OnEditorPlayModeChanged;
        }

        //handle entering play mode in the editor
        private static void OnEditorPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                SceneManagerState.Record(); //record scene lists state
                var mode = (ResourceManagerRuntimeData.ProviderMode)EditorPrefs.GetInt("RMProviderMode", 0);
                PrepareRuntimeData(mode, true, BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), EditorUserBuildSettings.activeBuildTarget);
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                //restore scene list state
                SceneManagerState.Restore();
            }
        }

        //handle building the player
        static void BuildPlayer(BuildPlayerOptions ops)
        {
            PrepareRuntimeData(ResourceManagerRuntimeData.ProviderMode.AssetBundles, (ops.options & BuildOptions.Development) != BuildOptions.None, ops.targetGroup, ops.target);
            BuildPipeline.BuildPlayer(ops);
        }


        //create ResourceManagerRuntimeData for the player to use
        //in the player, call ResourceManagerRuntimeData.Initialize() to load and init RM with this data
        static void PrepareRuntimeData(ResourceManagerRuntimeData.ProviderMode mode, bool isDevBuild, BuildTargetGroup targetGroup, BuildTarget target)
        {
            ResourceManagerRuntimeData runtimeData = new ResourceManagerRuntimeData();
            runtimeData.resourceProviderMode = mode;
            runtimeData.profileEvents = EditorPrefs.GetBool("RMProfileEvents", false);

            //replace this with code specific to your project that knows how to gather all assets that need to be loadable at runtime
            List<string> paths = new List<string>(Directory.GetFiles(EditorPrefs.GetString("RMTargetFolder", "Assets/Prefabs"), EditorPrefs.GetString("RMTargetExtension", "*.prefab"), SearchOption.AllDirectories));

            //create locations for all assets
            if (runtimeData.resourceProviderMode == ResourceManagerRuntimeData.ProviderMode.FastMode)
            {
                foreach (var p in paths)
                {
                    //for this mode just create locations for the AssetDatabase
                    var path = p.Replace('\\', '/');
                    runtimeData.locationData.Add(new ResourceManagerRuntimeData.ResourceLocationData(
                            Path.GetFileNameWithoutExtension(path),
                            AssetDatabase.AssetPathToGUID(path),
                            path, typeof(AssetDatabaseProvider).FullName, null));
                }
            }
            else
            {
                var assetBundleBuilds = new List<AssetBundleBuild>();
                string bundleBuildPath = EditorPrefs.GetString("RMBundlePath", "Assets/StreamingAssets");
                string bundleLoadPrefix = EditorPrefs.GetString("RMBundleLoadPrefix", "{Application.streamingAssetsPath}/");
                foreach (var p in paths)
                {
                    //create locations for bundled assets
                    var path = p.Replace('\\', '/');
                    string name = Path.GetFileNameWithoutExtension(path);
                    string bundleName = (name + ".bundle").ToLower();
                    assetBundleBuilds.Add(new AssetBundleBuild() { assetBundleName = bundleName, assetNames = new string[] { path } });
                    runtimeData.locationData.Add(new ResourceManagerRuntimeData.ResourceLocationData(
                            name, AssetDatabase.AssetPathToGUID(path), path, typeof(BundledAssetProvider).FullName, new string[] { bundleName }));
                    if (runtimeData.resourceProviderMode == ResourceManagerRuntimeData.ProviderMode.VirtualBundles)
                    {   //if simulating bundles, go ahead and create locations for bundles since they won't actually be created
                        runtimeData.locationData.Add(new ResourceManagerRuntimeData.ResourceLocationData(
                                bundleName, "", bundleLoadPrefix + bundleName, typeof(LocalAssetBundleProvider).FullName, new string[] {}));
                    }
                }
                if (runtimeData.resourceProviderMode == ResourceManagerRuntimeData.ProviderMode.AssetBundles)
                {   //build real bundles and then extract the locations
                    var manifest = BuildPipeline.BuildAssetBundles(bundleBuildPath, assetBundleBuilds.ToArray(), BuildAssetBundleOptions.UncompressedAssetBundle, target);
                    foreach (var bundleName in manifest.GetAllAssetBundles())
                        runtimeData.locationData.Add(new ResourceManagerRuntimeData.ResourceLocationData(
                                bundleName, "", bundleLoadPrefix + bundleName, typeof(LocalAssetBundleProvider).FullName, manifest.GetAllDependencies(bundleName)));
                }
            }

            // need to add all scenes to EditorBuildSettings.scenes so they can be loaded in the editor when in play mode
            if (runtimeData.resourceProviderMode != ResourceManagerRuntimeData.ProviderMode.AssetBundles)
            {
                var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                scenes.AddRange(EditorBuildSettings.scenes);
                foreach (var p in paths)
                    if (p.EndsWith(".unity"))
                        scenes.Add(new EditorBuildSettingsScene(new GUID(AssetDatabase.AssetPathToGUID(p)), true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            //extract data for virtual bundles if needed
            if (runtimeData.resourceProviderMode == ResourceManagerRuntimeData.ProviderMode.VirtualBundles)
                CreateVirtualBundleData(runtimeData);

            runtimeData.Save();
        }

        private static void CreateVirtualBundleData(ResourceManagerRuntimeData runtimeData)
        {
            var virtualBundleData = new VirtualAssetBundleRuntimeData(1000 * 1024, 200 * 1024); //1Mbs local speed, 200kbs remote speed
            var bundledAssets = new Dictionary<string, List<string>>();
            foreach (var loc in runtimeData.locationData)
            {
                if (loc.provider == typeof(BundledAssetProvider).FullName)
                {
                    if (loc.dependencies == null || loc.dependencies.Length == 0)
                        continue;
                    //create virtual bundle data
                    foreach (var dep in loc.dependencies)
                    {
                        List<string> assetsInBundle = null;
                        if (!bundledAssets.TryGetValue(dep, out assetsInBundle))
                            bundledAssets.Add(dep, assetsInBundle = new List<string>());
                        assetsInBundle.Add(loc.id);
                    }
                }
            }

            foreach (var bd in bundledAssets)
            {
                var bundleLocData = runtimeData.locationData.Find(a => a.key == bd.Key);
                var size = bd.Value.Count * 1024 * 1024; //for now estimate 1MB per entry
                virtualBundleData.simulatedAssetBundles.Add(new VirtualAssetBundle(bundleLocData.id, bundleLocData.provider == typeof(LocalAssetBundleProvider).FullName, size, bd.Value));
            }

            virtualBundleData.Save();
        }

        //this class is used to save and restore the EditorBuildSettings scene list when entering and existing play mode,
        //since the build script may modify it when running in Fast or Virtual mode
        [Serializable]
        internal class SceneManagerState
        {
            [Serializable]
            internal class EBSSceneState
            {
                [SerializeField]
                public string guid;
                [SerializeField]
                public bool enabled;
                public EBSSceneState() {}
                public EBSSceneState(EditorBuildSettingsScene s) { guid = s.guid.ToString(); enabled = s.enabled; }
                public EditorBuildSettingsScene GetBuildSettingsScene() { return new EditorBuildSettingsScene(new GUID(guid), enabled); }
            }

            [SerializeField]
            public EBSSceneState[] editorBuildSettingsSceneState;
            [SerializeField]
            public EditorBuildSettingsScene[] scenes;

            const string path = "Library/SceneManagerState.json";
            public static void Record()
            {
                try
                {
                    var state = new SceneManagerState();
                    var edbss = new List<EBSSceneState>();
                    state.scenes = EditorBuildSettings.scenes;
                    foreach (var s in EditorBuildSettings.scenes)
                        edbss.Add(new EBSSceneState(s));
                    state.editorBuildSettingsSceneState = edbss.ToArray();
                    File.WriteAllText(path, JsonUtility.ToJson(state));
                }
                catch (Exception)
                {
                }
            }

            public static void Restore()
            {
                try
                {
                    var state = JsonUtility.FromJson<SceneManagerState>(File.ReadAllText(path));
                    var scenes = new List<EditorBuildSettingsScene>();
                    foreach (var s in state.editorBuildSettingsSceneState)
                        scenes.Add(s.GetBuildSettingsScene());
                    EditorBuildSettings.scenes = scenes.ToArray();
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
#endif