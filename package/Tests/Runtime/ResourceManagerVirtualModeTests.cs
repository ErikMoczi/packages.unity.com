using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
public class ResourceManagerVirtualModeTests : ResourceManagerBaseTests
{
    protected override void CreateLocations(List<IResourceLocation> locations)
    {
        ResourceManager.InstanceProvider = new InstanceProvider();
        ResourceManager.SceneProvider = new SceneProvider();
        var virtualBundleData = new VirtualAssetBundleRuntimeData();

        var sharedBundles = new List<VirtualAssetBundle>();
        var sharedBundleLocations = new List<IResourceLocation>();
        for (int i = 0; i < 10; i++)
        {
            var bundleName = "shared" + i;
            sharedBundles.Add(new VirtualAssetBundle("shared" + i, i % 2 == 0));
            sharedBundleLocations.Add(new ResourceLocationBase(bundleName, bundleName, typeof(AssetBundleProvider).FullName));
        }
        virtualBundleData.AssetBundles.AddRange(sharedBundles);

        for (int i = 0; i < 5; i++)
        {
            var isLocal = i % 2 == 0;
            var b = new VirtualAssetBundle("bundle" + i, isLocal);
            var bundleLocation = new ResourceLocationBase(b.Name, b.Name, typeof(AssetBundleProvider).FullName);
            for (int a = 0; a < 10; a++)
            {
                var name = b.Name + "_asset" + a;
                var path = RootFolder + "/" + name + ".prefab";
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                
#if UNITY_2018_3_OR_NEWER
                PrefabUtility.SaveAsPrefabAsset(go, path);
#else
                PrefabUtility.CreatePrefab(path, go);
#endif
                Object.DestroyImmediate(go, false);

                var asset = new VirtualAssetBundleEntry(path, Random.Range(1024, 1024 * 1024));
                b.Assets.Add(asset);
                locations.Add(new ResourceLocationBase(name, path, typeof(BundledAssetProvider).FullName, bundleLocation, sharedBundleLocations[Random.Range(0, sharedBundleLocations.Count)], sharedBundleLocations[Random.Range(0, sharedBundleLocations.Count)]));
            }
            b.OnAfterDeserialize();
            virtualBundleData.AssetBundles.Add(b);
        }

        var abManager = new GameObject("AssetBundleSimulator", typeof(VirtualAssetBundleManager)).GetComponent<VirtualAssetBundleManager>();
        abManager.Initialize(virtualBundleData, s => s);
        ResourceManager.ResourceProviders.Insert(0, new CachedProvider(new VirtualAssetBundleProvider(abManager, typeof(AssetBundleProvider).FullName)));
        ResourceManager.ResourceProviders.Insert(0, new CachedProvider(new VirtualBundledAssetProvider()));
    }
}
#endif
