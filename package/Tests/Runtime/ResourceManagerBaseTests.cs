﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ResourceManagement;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif


public abstract class ResourceManagerBaseTests : IPrebuildSetup//, IPostBuildCleanup
{
    protected string RootFolder { get { return string.Format("Assets/{0}_AssetsToDelete", GetType().Name); } }

    List<IResourceLocation> m_Locations = new List<IResourceLocation>();

    public void Setup()
    {
#if UNITY_EDITOR
        if (!Directory.Exists(RootFolder))
            Directory.CreateDirectory(RootFolder);

        AssetDatabase.StartAssetEditing();
        CreateLocations(m_Locations);
        AssetDatabase.StopAssetEditing();
#endif
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
#if UNITY_EDITOR
        AssetDatabase.DeleteAsset(RootFolder);
#endif
    }

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        ResourceManager.ResourceProviders.Clear();
        ResourceManager.InstanceProvider = null;
        ResourceManager.SceneProvider = null;
        AsyncOperationCache.Instance.Clear();
        DelayedActionManager.Clear();

        CreateLocations(m_Locations);
    }

#if UNITY_EDITOR
    internal IResourceLocation CreateTestAsset(string assetPrefix, string objectName, string loadPath, Type provider)
    {
        var assetPath = RootFolder + "/" + assetPrefix + objectName + ".prefab";
        if (!Directory.Exists(Path.GetDirectoryName(assetPath)))
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objectName;
        
#if UNITY_2018_3_OR_NEWER
        PrefabUtility.SaveAsPrefabAsset(go, assetPath);
#else
        PrefabUtility.CreatePrefab(assetPath, go);
#endif
        Object.DestroyImmediate(go, false);
        return new ResourceLocationBase(objectName, loadPath, provider.FullName);
    }
#endif


    protected abstract void CreateLocations(List<IResourceLocation> locations);


    [UnityTest]
    public IEnumerator CanProvideWithCallback()
    {
        ResourceManager.ProvideResource<GameObject>(m_Locations[0]).Completed += op => Assert.IsNotNull(op.Result);
        yield return null;
    }


    [UnityTest]
    public IEnumerator VerifyKey()
    {
        ResourceManager.ProvideResource<GameObject>(m_Locations[0]).Completed += op => Assert.IsNotNull(op.Key == m_Locations[0]);
        yield return null;
    }

    [UnityTest]
    public IEnumerator CanProvideWithYield()
    {
        var op = ResourceManager.ProvideResource<GameObject>(m_Locations[0]);
        yield return op;
        Assert.IsNotNull(op.Result);
        op.Release();
    }

    [UnityTest]
    public IEnumerator CanProvideMultipleResources()
    {
        ResourceManager.ProvideResources<GameObject>(m_Locations, perOp => Assert.IsNotNull(perOp.Result)).Completed += op =>
        {
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(op.Result.Count, m_Locations.Count);
        };
        yield return null;
    }

    [UnityTest]
    public IEnumerator CanProvideInstance()
    {
        var loadOp = ResourceManager.ProvideInstance<GameObject>(m_Locations[0], new InstantiationParameters(null, true));
        loadOp.Completed += op =>
        {
            Assert.IsNotNull(op.Result);
            Assert.IsNotNull(GameObject.Find(m_Locations[0] + "(Clone)"));
        };

        yield return loadOp;
        ResourceManager.ReleaseInstance(loadOp.Result, m_Locations[0]);
        yield return null;
        Assert.IsNull(GameObject.Find(m_Locations[0] + "(Clone)"));
    }

    [UnityTest]
    public IEnumerator CanProvideMultipleInstances()
    {
        var loadOp = ResourceManager.ProvideInstances<GameObject>(m_Locations, perOp => Assert.IsNotNull(perOp.Result), new InstantiationParameters(null, true));
        loadOp.Completed += op =>
        {
            Assert.IsNotNull(op.Result);
            for (int i = 0; i < m_Locations.Count; i++)
                Assert.IsNotNull(GameObject.Find(m_Locations[i] + "(Clone)"));
        };
        yield return loadOp;
        for (int i = 0; i < loadOp.Result.Count; i++)
            ResourceManager.ReleaseInstance(loadOp.Result[i], m_Locations[i]);
        yield return null;
        for (int i = 0; i < m_Locations.Count; i++)
            Assert.IsNull(GameObject.Find(m_Locations[i] + "(Clone)"));
    }

    [UnityTest]
    public IEnumerator StressInstantiation()
    {
        for (int i = 0; i < 100; i++)
        {
            var loc = m_Locations[Random.Range(0, m_Locations.Count)];
            ResourceManager.ProvideInstance<GameObject>(loc, new InstantiationParameters(null, true)).Completed += op =>
            {
                Assert.IsNotNull(op.Result);
                DelayedActionManager.AddAction((Action<Object, IResourceLocation>)ResourceManager.ReleaseInstance, Random.Range(.25f, .5f), op.Result, loc);
            };

            if (Random.Range(0, 100) > 20)
                yield return null;
        }

        while (DelayedActionManager.IsActive)
            yield return null;

        var objs = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var r in objs)
            Assert.False(r.name.EndsWith("(Clone)"), "All instances were not cleaned up");
    }

}
