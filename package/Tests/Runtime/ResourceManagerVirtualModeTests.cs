using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.ResourceManagement;
using UnityEngine;
using UnityEngine.TestTools;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ResourceManagerVirtualModeTests : ResourceManagerBaseTests
{
    protected override void CreateLocations(List<IResourceLocation> k_locations)
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
                PrefabUtility.CreatePrefab(path, go);
                UnityEngine.Object.Destroy(go);

                var asset = new VirtualAssetBundleEntry(path, UnityEngine.Random.Range(1024, 1024 * 1024));
                b.Assets.Add(asset);
                k_locations.Add(new ResourceLocationBase(name, path, typeof(BundledAssetProvider).FullName, bundleLocation, sharedBundleLocations[UnityEngine.Random.Range(0, sharedBundleLocations.Count)], sharedBundleLocations[UnityEngine.Random.Range(0, sharedBundleLocations.Count)]));
            }
            b.OnAfterDeserialize();
            virtualBundleData.AssetBundles.Add(b);
        }

        new GameObject("AssetBundleSimulator", typeof(VirtualAssetBundleManager)).GetComponent<VirtualAssetBundleManager>().Initialize(virtualBundleData, (s) => s, 0, 0, 0, 0);
    }
}
