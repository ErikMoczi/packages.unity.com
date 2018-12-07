using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement;
#if UNITY_EDITOR

#endif

#if UNITY_EDITOR
public class ResourceManagerTestsLegacy : ResourceManagerBaseTests
{
    protected override void CreateLocations(List<IResourceLocation> locations)
    {
        ResourceManager.InstanceProvider = new InstanceProvider();
        ResourceManager.ResourceProviders.Add(new LegacyResourcesProvider());
        for (int i = 0; i < 20; i++)
            locations.Add(CreateTestAsset("Resources/", "asset" + i, "asset" + i, typeof(LegacyResourcesProvider)));
    }
}
#endif
