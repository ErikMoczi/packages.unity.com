using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ResourceManagement;
using ResourceManagement.AsyncOperations;
using ResourceManagement.ResourceLocations;
using ResourceManagement.ResourceLocators;
using ResourceManagement.ResourceProviders;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ResourceManagerTests : MonoBehaviour
{
    [UnityTest]
    public IEnumerator CanLoadAssetsFrom_ResourcesFolder_WtihCallback()
    {
        ResourceManager.resourceProviders.Add(new LegacyResourcesProvider());
        ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());

        GameObject cube = null;
        var oper = ResourceManager.LoadAsync<GameObject, string>("test");
        oper.completed +=
            (op) =>
            {
                cube = op.result as GameObject;
            };

        yield return null;
        Assert.IsNotNull(cube);
    }

    [UnityTest]
    public IEnumerator CanLoadFrom_ResourceFolder_WithAsyncOperation()
    {
        ResourceManager.resourceProviders.Add(new LegacyResourcesProvider());
        ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());

        IAsyncOperation op = ResourceManager.LoadAsync<GameObject, string>("test");

        while (!op.isDone)
            yield return null;

        GameObject cube = op.result as GameObject;

        Assert.IsNotNull(cube);
    }

    [UnityTest]
    public IEnumerator CanLoadAllAssets_FromResourcesFolder()
    {
        ResourceManager.resourceProviders.Add(new LegacyResourcesProvider());
        ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());

        List<GameObject> gameObjects = new List<GameObject>();
        IAsyncOperation op = ResourceManager.LoadAllAsync<GameObject, string>(new List<string>() {"Cube", "Cube 1", "Cube 2"}, (operation) =>
        {
            gameObjects.Add(operation.result);
        });

        yield return op;

        Assert.AreEqual(3, gameObjects.Count);
    }

    [UnityTest]
    public IEnumerator GetResourceLocation()
    {
        ResourceManager.resourceProviders.Add(new LegacyResourcesProvider());
        ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());
        ResourceManager.resourceLocators.Add(new ResourceLocationLocator());

        IResourceLocation location = ResourceManager.GetResourceLocation("Cube");

        IAsyncOperation op = ResourceManager.LoadAsync<GameObject, IResourceLocation>(location);
        yield return op;

        GameObject cube = op.result as GameObject;
        Assert.IsNotNull(cube);
        Assert.AreEqual("Cube", cube.name);
    }

    [Test]
    public void GetResourceProvider()
    {
        IResourceProvider p = new LegacyResourcesProvider();
        ResourceManager.resourceProviders.Add(p);
        ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());

        IResourceLocation location = ResourceManager.GetResourceLocation("Cube");
        IResourceProvider provider = ResourceManager.GetResourceProvider<GameObject>(location);

        Assert.AreEqual(p.providerId, provider.providerId);
    }

    [UnityTest]
    public IEnumerator InstansiateObject_Async()
    {
        ResourceManager.resourceProviders.Add(new LegacyResourcesProvider());
        ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());
        ResourceManager.instanceProvider = new InstanceProvider();

        IAsyncOperation op = ResourceManager.InstantiateAsync<GameObject, string>("Cube 1");
        yield return op;

        GameObject obj = op.result as GameObject;
        Assert.IsNotNull(obj);
        Assert.IsNotNull(GameObject.Find("Cube 1(Clone)"));

        Destroy(GameObject.Find("Cube 1(Clone)"));
    }

    [UnityTest]
    public IEnumerator InstansiateAllObjects_Async()
    {
        ResourceManager.resourceProviders.Add(new LegacyResourcesProvider());
        ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());
        ResourceManager.instanceProvider = new InstanceProvider();

        List<GameObject> objects = new List<GameObject>();
        IAsyncOperation op =
            ResourceManager.InstantiateAllAsync<GameObject, string>(new List<string>() {"Cube", "Cube 1", "Cube 2"},
                (o) =>
                {
                    objects.Add(o.result);
                });
        yield return op;

        Assert.AreEqual(3, objects.Count);
        Assert.IsNotNull(GameObject.Find("Cube(Clone)"));
        Assert.IsNotNull(GameObject.Find("Cube 1(Clone)"));
        Assert.IsNotNull(GameObject.Find("Cube 2(Clone)"));

        Destroy(GameObject.Find("Cube(Clone)"));
        Destroy(GameObject.Find("Cube 1(Clone)"));
        Destroy(GameObject.Find("Cube 2(Clone)"));
    }

    [UnityTest]
    public IEnumerator LoadAllDependencies_FromResourceLocation()
    {
        IResourceProvider provider = new LegacyResourcesProvider();
        ResourceManager.resourceProviders.Add(provider);
        ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());

        IResourceLocation dep1 = ResourceManager.GetResourceLocation("Cube 1");
        IResourceLocation dep2 = ResourceManager.GetResourceLocation("Cube 2");
        IResourceLocation[] deps = new IResourceLocation[] { dep1, dep2 };
        IResourceLocation location = new ResourceLocationBase<string>("Cube", "Cube", provider.providerId, deps);

        List<GameObject> loadedDependencies = new List<GameObject>();
        IAsyncOperation asyncOperation = ResourceManager.PreloadDependenciesAsync(location, (op) =>
        {
            loadedDependencies.Add(op.result as GameObject);
        });

        while (!asyncOperation.isDone)
            yield return null;

        Assert.AreEqual(2, loadedDependencies.Count);
    }

    [UnityTest]
    public IEnumerator ReleaseInstance()
    {
        ResourceManager.resourceProviders.Add(new LegacyResourcesProvider());
        ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());
        ResourceManager.instanceProvider = new InstanceProvider();

        IAsyncOperation op = ResourceManager.InstantiateAsync<GameObject, string>("Cube 1");
        while (!op.isDone)
            yield return null;

        Assert.IsNotNull(GameObject.Find("Cube 1(Clone)"));

        ResourceManager.ReleaseInstance<GameObject, string>("Cube 1", op.result as GameObject);

        yield return null;
        Assert.IsNull(GameObject.Find("Cube 1(Clone)"));
    }

    [UnityTest]
    public IEnumerator LoadAllObjects_Async()
    {
        ResourceManager.resourceProviders.Add(new LegacyResourcesProvider());
        ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());

        IResourceLocation loc1 = ResourceManager.GetResourceLocation("Cube");
        IResourceLocation loc2 = ResourceManager.GetResourceLocation("Cube 1");
        List<IResourceLocation> locs = new List<IResourceLocation>() { loc1, loc2 };

        List<GameObject> loadedObjects = new List<GameObject>();
        IAsyncOperation loadOp = ResourceManager.LoadAllAsync<GameObject, IResourceLocation>(locs, (op) =>
        {
            GameObject go = op.result as GameObject;
            loadedObjects.Add(go);
        });

        while (!loadOp.isDone)
            yield return null;

        Assert.AreEqual(2, loadedObjects.Count);
    }
}
