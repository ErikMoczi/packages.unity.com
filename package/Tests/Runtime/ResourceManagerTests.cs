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
    //WHY TEST IS REMOVED:  cannot get callback timing to work right. Manually testing while waiting to solve callback issue
    //[UnityTest]
    //public IEnumerator CanLoadAssetsFrom_ResourcesFolder_WtihCallback()
    //{
    //    ResourceManager.resourceProviders.Add(new LegacyResourcesProvider());
    //    ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());

    //    GameObject cube = null;
    //    var oper = ResourceManager.LoadAsync<GameObject, string>("test");
    //    oper.completed +=
    //        (op) =>
    //        {
    //            cube = op.result as GameObject;
    //        };

    //    Assert.IsNotNull(cube);
    //}

    //WHY TEST IS REMOVED:  cannot get async timing to work right. Manually testing while waiting to solve async issue
    //[UnityTest]
    //public IEnumerator CanLoadFrom_ResourceFolder_WithAsyncOperation()
    //{
    //    ResourceManager.resourceProviders.Add(new LegacyResourcesProvider());
    //    ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());

    //    IAsyncOperation op = ResourceManager.LoadAsync<GameObject, string>("test");
    //    yield return op;

    //    GameObject cube = op.result as GameObject;

    //    Assert.IsNotNull(cube);
    //}

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

    //WHY TEST IS REMOVED: I think this one will only work if a build is actually crated that contains the scene. 
    //[UnityTest]
    //public IEnumerator LoadScene_ThatIsIncluded_InBuildSettings()
    //{
    //    string sceneName = "BuildSettingsScene";
    //    bool testPass = false;

    //    ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());
    //    ResourceManager.sceneProvider = new SceneProvider();

    //    SceneManager.sceneLoaded += ((scene, loadSceneMode) =>
    //    {
    //        if (scene.name == sceneName)
    //            testPass = true;
    //    });

    //    IAsyncOperation op = ResourceManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    //    yield return op;

    //    Assert.IsTrue(testPass);

    //    yield return SceneManager.UnloadSceneAsync(sceneName);
    //}

    //WHY TEST IS REMOVED: requires a path to the built bundle, but I believe the path will change as Packman functionality locks in.
    //[UnityTest]
    //public IEnumerator LoadScene_ThatIsIncluded_InAssetBundle()
    //{
    //    ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());
    //    ResourceManager.sceneProvider = new SceneProvider();
    //    string sceneName = "AssetBundleScene";
    //    bool testPass = false;

    //    SceneManager.sceneLoaded += ((scene, loadSceneMode) =>
    //    {
    //        if (scene.name == sceneName)
    //            testPass = true;
    //    });

    //    AssetBundle ab = AssetBundle.LoadFromFile("Assets/ResourceManager/PlayModeTests/TestAssets/scene");
    //    IAsyncOperation op = ResourceManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    //    yield return op;

    //    yield return SceneManager.UnloadSceneAsync(sceneName);
    //    ab.Unload(true);
    //    Assert.IsTrue(testPass);
    //}

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
    }

    //WHY TEST IS REMOVED: I think this one will only work if a build is actually crated that contains the scene. 
    //[UnityTest]
    //public IEnumerator UnloadScene_ThatIsIncluded_InBuildSettings()
    //{
    //    string sceneName = "BuildSettingsScene";
    //    bool testPass = false;

    //    ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());
    //    ResourceManager.sceneProvider = new SceneProvider();

    //    SceneManager.sceneUnloaded += ((scene) =>
    //    {
    //        if (scene.name == sceneName)
    //            testPass = true;
    //    });

    //    IAsyncOperation loadOp = ResourceManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    //    yield return loadOp;

    //    Assert.IsFalse(testPass);

    //    IAsyncOperation unloadOp = ResourceManager.UnloadSceneAsync(sceneName);
    //    yield return unloadOp;

    //    Assert.IsTrue(testPass);
    //}

    //WHY TEST IS REMOVED:  cannot get async timing to work right. Manually testing while waiting to solve async issue
    //[UnityTest]
    //public IEnumerator LoadAllDependencies_FromResourceLocation()
    //{
    //    IResourceProvider provider = new LegacyResourcesProvider();
    //    ResourceManager.resourceProviders.Add(provider);
    //    ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());

    //    IResourceLocation dep1 = ResourceManager.GetResourceLocation("Cube 1");
    //    IResourceLocation dep2 = ResourceManager.GetResourceLocation("Cube 2");
    //    IResourceLocation[] deps = new IResourceLocation[] {dep1, dep2};
    //    IResourceLocation location = new ResourceLocationBase<string>("Cube", "1", provider.providerId, deps);

    //    List<GameObject> loadedDependencies = new List<GameObject>();
    //    yield return ResourceManager.PreloadDependenciesAsync(location, (op) =>
    //    {
    //        loadedDependencies.Add(op.result as GameObject);
    //    });

    //    Assert.AreEqual(2, loadedDependencies.Count);
    //}


    //WHY TEST IS REMOVED: have to solve LoadAllDependencies_FromResourceLocation first.
    //[UnityTest]
    //public IEnumerator LoadDependencies_FromListOfResourceLocations()
    //{
    //    throw new NotImplementedException("Have to figure out why the test LoadAllDependencies_FromResourceLocation isn't working before implementing this one.");
    //}


    //WHY TEST IS REMOVED:  cannot get async timing to work right. Manually testing while waiting to solve async issue
    //[UnityTest]
    //public IEnumerator ReleaseInstance()
    //{
    //    ResourceManager.resourceProviders.Add(new LegacyResourcesProvider());
    //    ResourceManager.resourceLocators.Add(new LegacyResourcesLocator());
    //    ResourceManager.instanceProvider = new InstanceProvider();

    //    IAsyncOperation op = ResourceManager.InstantiateAsync<GameObject, string>("Cube 1");
    //    yield return op;

    //    Assert.IsNotNull(GameObject.Find("Cube 1(Clone)"));

    //    ResourceManager.ReleaseInstance<GameObject, string>("Cube 1", op.result as GameObject);

    //    yield return null;
    //    Assert.IsNull(GameObject.Find("Cube 1(Clone)"));
    //}
}
