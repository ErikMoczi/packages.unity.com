using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement;
#if UNITY_EDITOR

#endif

#if UNITY_EDITOR
public class ResourceManagerFastModeTests : ResourceManagerBaseTests
{
    protected override void CreateLocations(List<IResourceLocation> locations)
    {
        ResourceManager.InstanceProvider = new InstanceProvider();
        ResourceManager.ResourceProviders.Add(new AssetDatabaseProvider());
        for (int i = 0; i < 20; i++)
            locations.Add(CreateTestAsset("", "asset" + i, RootFolder + "/asset" + i + ".prefab", typeof(AssetDatabaseProvider)));
    }
}
#endif
