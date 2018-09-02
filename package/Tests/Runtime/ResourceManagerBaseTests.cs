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


public abstract class ResourceManagerBaseTests : IPrebuildSetup
{
    protected string RootFolder { get { return string.Format("Assets/{0}_AssetsToDelete", GetType().Name); } }

    private List<IResourceLocation> k_locations = new List<IResourceLocation>();

    public void Setup()
    {
        if (!Directory.Exists(RootFolder))
            Directory.CreateDirectory(RootFolder);
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        AssetDatabase.DeleteAsset(RootFolder);
    }

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        ResourceManager.ResourceProviders.Clear();
        ResourceManager.InstanceProvider = null;
        ResourceManager.SceneProvider = null;
        AsyncOperationCache.Instance.Clear();
        DelayedActionManager.Clear();
        AssetDatabase.StartAssetEditing();
        CreateLocations(k_locations);
        AssetDatabase.StopAssetEditing();
    }

    internal IResourceLocation CreateTestAsset(string assetPrefix, string objectName, string loadPath, Type provider)
    {
        var assetPath = RootFolder + "/" + assetPrefix + objectName + ".prefab";
        if (!Directory.Exists(Path.GetDirectoryName(assetPath)))
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objectName;
        PrefabUtility.CreatePrefab(assetPath, go);
        UnityEngine.Object.Destroy(go);
        return new ResourceLocationBase(objectName, loadPath, provider.FullName);
    }

    protected abstract void CreateLocations(List<IResourceLocation> k_locations);


    [UnityTest]
    public IEnumerator CanProvideWithCallback()
    {
        ResourceManager.ProvideResource<GameObject>(k_locations[0]).Completed += (op) => Assert.IsNotNull(op.Result);
        yield return null;
    }


    [UnityTest]
    public IEnumerator VerifyKey()
    {
        ResourceManager.ProvideResource<GameObject>(k_locations[0]).Completed += (op) => Assert.IsNotNull(op.Key == k_locations[0]);
        yield return null;
    }

    [UnityTest]
    public IEnumerator CanProvideWithYield()
    {
        var op = ResourceManager.ProvideResource<GameObject>(k_locations[0]);
        yield return op;
        Assert.IsNotNull(op.Result);
        op.Release();
    }

    [UnityTest]
    public IEnumerator CanProvideMultipleResources()
    {
        ResourceManager.ProvideResources<GameObject>(k_locations, (perOp) => Assert.IsNotNull(perOp.Result)).Completed += (op) =>
        {
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(op.Result.Count, k_locations.Count);
        };
        yield return null;
    }

    [UnityTest]
    public IEnumerator CanProvideInstance()
    {
        var loadOp = ResourceManager.ProvideInstance<GameObject>(k_locations[0], new InstantiationParameters(null, true));
        loadOp.Completed += (op) =>
        {
            Assert.IsNotNull(op.Result);
            Assert.IsNotNull(GameObject.Find(k_locations[0] + "(Clone)"));
        };

        yield return loadOp;
        ResourceManager.ReleaseInstance(loadOp.Result, k_locations[0]);
        yield return null;
        Assert.IsNull(GameObject.Find(k_locations[0] + "(Clone)"));
    }

    [UnityTest]
    public IEnumerator CanProvideMultipleInstances()
    {
        var loadOp = ResourceManager.ProvideInstances<GameObject>(k_locations, (perOp) => Assert.IsNotNull(perOp.Result), new InstantiationParameters(null, true));
        loadOp.Completed += (op) =>
        {
            Assert.IsNotNull(op.Result);
            for (int i = 0; i < k_locations.Count; i++)
                Assert.IsNotNull(GameObject.Find(k_locations[i] + "(Clone)"));
        };
        yield return loadOp;
        for (int i = 0; i < loadOp.Result.Count; i++)
            ResourceManager.ReleaseInstance(loadOp.Result[i], k_locations[i]);
        yield return null;
        for (int i = 0; i < k_locations.Count; i++)
            Assert.IsNull(GameObject.Find(k_locations[i] + "(Clone)"));
    }

    [UnityTest]
    public IEnumerator StressInstantiation()
    {
        for (int i = 0; i < 100; i++)
        {
            var loc = k_locations[UnityEngine.Random.Range(0, k_locations.Count)];
            ResourceManager.ProvideInstance<GameObject>(loc, new InstantiationParameters(null, true)).Completed += (op) =>
            {
                Assert.IsNotNull(op.Result);
                DelayedActionManager.AddAction((Action<UnityEngine.Object, IResourceLocation>)ResourceManager.ReleaseInstance, UnityEngine.Random.Range(.25f, .5f), op.Result, loc);
            };

            if (UnityEngine.Random.Range(0, 100) > 20)
                yield return null;
        }

        while (DelayedActionManager.IsActive)
            yield return null;

        var objs = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var r in objs)
            Assert.False(r.name.EndsWith("(Clone)"), "All instances were not cleaned up");
    }

}
