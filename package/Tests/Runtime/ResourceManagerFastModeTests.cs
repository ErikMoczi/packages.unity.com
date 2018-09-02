using System.Collections.Generic;
using UnityEngine.ResourceManagement;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ResourceManagerFastModeTests : ResourceManagerBaseTests
{
    protected override void CreateLocations(List<IResourceLocation> k_locations)
    {
        ResourceManager.InstanceProvider = new InstanceProvider();
        ResourceManager.ResourceProviders.Add(new AssetDatabaseProvider());
        for (int i = 0; i < 20; i++)
            k_locations.Add(CreateTestAsset("", "asset" + i, RootFolder + "/asset" + i + ".prefab", typeof(AssetDatabaseProvider)));
    }
}
