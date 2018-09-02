using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.ResourceManagement;
using UnityEngine;
using UnityEngine.TestTools;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ResourceManagerTests : MonoBehaviour, IPrebuildSetup
{
    private string resourcesFolder = "Assets/ResourceManager/Tests/Runtime/TestResources/Resources/";
    public void Setup()
    {
        if (!Directory.Exists(resourcesFolder))
            Directory.CreateDirectory(resourcesFolder);
    }

    [UnityTest]
    public IEnumerator CanLoadAssetsFrom_ResourcesFolder_WithCallback()
    {
        ResourceManager.ResourceProviders.Add(new LegacyResourcesProvider());
        ResourceManager.ResourceLocators.Add(new LegacyResourcesLocator());

        string cubePath = resourcesFolder + "Cube.prefab";
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        PrefabUtility.CreatePrefab(cubePath, go);
        Destroy(go);

        GameObject cube = null;
        var oper = ResourceManager.LoadAsync<GameObject, string>("Cube");
        oper.Completed +=
            (op) =>
            {
                cube = op.Result as GameObject;
            };

        yield return null;
        Assert.IsNotNull(cube);
        DestroyAsset(cubePath);
    }

    [UnityTest]
    public IEnumerator CanLoadFrom_ResourceFolder_WithAsyncOperation()
    {
        ResourceManager.ResourceProviders.Add(new LegacyResourcesProvider());
        ResourceManager.ResourceLocators.Add(new LegacyResourcesLocator());

        string cubePath = resourcesFolder + "Cube.prefab";
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        PrefabUtility.CreatePrefab(cubePath, go);
        Destroy(go);

        IAsyncOperation op = ResourceManager.LoadAsync<GameObject, string>("Cube");
        yield return op;

        GameObject cube = op.Result as GameObject;
        op.Release();
        Assert.IsNotNull(cube);

        DestroyAsset(cubePath);
        Destroy(go);
    }

    [UnityTest]
    public IEnumerator CanLoadAllAssets_FromResourcesFolder()
    {
        ResourceManager.ResourceProviders.Add(new LegacyResourcesProvider());
        ResourceManager.ResourceLocators.Add(new LegacyResourcesLocator());

        string cubePath = resourcesFolder + "Cube.prefab";
        string cube1Path = resourcesFolder + "Cube1.prefab";
        string cube2Path = resourcesFolder + "Cube2.prefab";

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        PrefabUtility.CreatePrefab(cubePath, go);
        PrefabUtility.CreatePrefab(cube1Path, go);
        PrefabUtility.CreatePrefab(cube2Path, go);
        Destroy(go);

        List<GameObject> gameObjects = new List<GameObject>();
        IAsyncOperation op = ResourceManager.LoadAllAsync<GameObject, string>(new List<string>() { "Cube", "Cube1", "Cube2" }, (operation) =>
            {
                gameObjects.Add(operation.Result);
            });

        yield return op;

        Assert.AreEqual(3, gameObjects.Count);
        DestroyAsset(cubePath);
        DestroyAsset(cube1Path);
        DestroyAsset(cube2Path);
    }

    [UnityTest]
    public IEnumerator GetResourceLocation()
    {
        ResourceManager.ResourceProviders.Add(new LegacyResourcesProvider());
        ResourceManager.ResourceLocators.Add(new LegacyResourcesLocator());
        ResourceManager.ResourceLocators.Add(new ResourceLocationLocator());

        string cubePath = resourcesFolder + "Cube.prefab";
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        PrefabUtility.CreatePrefab(cubePath, go);
        Destroy(go);

        IResourceLocation location = ResourceManager.GetResourceLocation("Cube");

        IAsyncOperation op = ResourceManager.LoadAsync<GameObject, IResourceLocation>(location);
        yield return op;

        GameObject cube = op.Result as GameObject;
        Assert.IsNotNull(cube);
        Assert.AreEqual("Cube", cube.name);
        DestroyAsset(cubePath);
    }

    [Test]
    public void GetResourceProvider()
    {
        IResourceProvider p = new LegacyResourcesProvider();
        ResourceManager.ResourceProviders.Add(p);
        ResourceManager.ResourceLocators.Add(new LegacyResourcesLocator());

        string cubePath = resourcesFolder + "Cube.prefab";
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        PrefabUtility.CreatePrefab(cubePath, go);
        Destroy(go);


        IResourceLocation location = ResourceManager.GetResourceLocation("Cube");
        IResourceProvider provider = ResourceManager.GetResourceProvider<GameObject>(location);

        Assert.AreEqual(p.ProviderId, provider.ProviderId);
        DestroyAsset(cubePath);
    }

    [UnityTest]
    public IEnumerator InstansiateObject_Async()
    {
        ResourceManager.ResourceProviders.Add(new LegacyResourcesProvider());
        ResourceManager.ResourceLocators.Add(new LegacyResourcesLocator());
        ResourceManager.InstanceProvider = new InstanceProvider();

        string cubePath = resourcesFolder + "Cube1.prefab";
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        PrefabUtility.CreatePrefab(cubePath, go);
        Destroy(go);

        IAsyncOperation op = ResourceManager.InstantiateAsync<GameObject, string>("Cube1");
        yield return op;

        GameObject obj = op.Result as GameObject;
        Assert.IsNotNull(obj);
        Assert.IsNotNull(GameObject.Find("Cube1(Clone)"));

        Destroy(GameObject.Find("Cube1(Clone)"));
        DestroyAsset(cubePath);
    }

    [UnityTest]
    public IEnumerator InstansiateAllObjects_Async()
    {
        ResourceManager.ResourceProviders.Add(new LegacyResourcesProvider());
        ResourceManager.ResourceLocators.Add(new LegacyResourcesLocator());
        ResourceManager.InstanceProvider = new InstanceProvider();

        string cubePath = resourcesFolder + "Cube.prefab";
        string cube1Path = resourcesFolder + "Cube1.prefab";
        string cube2Path = resourcesFolder + "Cube2.prefab";

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        PrefabUtility.CreatePrefab(cubePath, go);
        PrefabUtility.CreatePrefab(cube1Path, go);
        PrefabUtility.CreatePrefab(cube2Path, go);
        Destroy(go);

        List<GameObject> objects = new List<GameObject>();
        IAsyncOperation op =
            ResourceManager.InstantiateAllAsync<GameObject, string>(new List<string>() { "Cube", "Cube1", "Cube2" },
                (o) =>
            {
                objects.Add(o.Result);
            });
        yield return op;

        Assert.AreEqual(3, objects.Count);
        Assert.IsNotNull(GameObject.Find("Cube(Clone)"));
        Assert.IsNotNull(GameObject.Find("Cube1(Clone)"));
        Assert.IsNotNull(GameObject.Find("Cube2(Clone)"));

        Destroy(GameObject.Find("Cube(Clone)"));
        Destroy(GameObject.Find("Cube1(Clone)"));
        Destroy(GameObject.Find("Cube2(Clone)"));

        DestroyAsset(cubePath);
        DestroyAsset(cube1Path);
        DestroyAsset(cube2Path);
    }

    [UnityTest]
    public IEnumerator LoadAllDependencies_FromResourceLocation()
    {
        IResourceProvider provider = new LegacyResourcesProvider();
        ResourceManager.ResourceProviders.Add(provider);
        ResourceManager.ResourceLocators.Add(new LegacyResourcesLocator());

        string cubePath = resourcesFolder + "Cube.prefab";
        string cube1Path = resourcesFolder + "Cube1.prefab";
        string cube2Path = resourcesFolder + "Cube2.prefab";

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        PrefabUtility.CreatePrefab(cubePath, go);
        PrefabUtility.CreatePrefab(cube1Path, go);
        PrefabUtility.CreatePrefab(cube2Path, go);
        Destroy(go);

        IResourceLocation dep1 = ResourceManager.GetResourceLocation("Cube1");
        IResourceLocation dep2 = ResourceManager.GetResourceLocation("Cube2");
        IResourceLocation[] deps = new IResourceLocation[] { dep1, dep2 };
        IResourceLocation location = new ResourceLocationBase<string>("Cube", "Cube", provider.ProviderId, deps);

        List<GameObject> loadedDependencies = new List<GameObject>();
        IAsyncOperation asyncOperation = ResourceManager.PreloadDependenciesAsync(location, (op) =>
            {
                loadedDependencies.Add(op.Result as GameObject);
            });
        yield return asyncOperation;
        asyncOperation.Release();

        Assert.AreEqual(2, loadedDependencies.Count);
        DestroyAsset(cubePath);
        DestroyAsset(cube1Path);
        DestroyAsset(cube2Path);
    }

    [UnityTest]
    public IEnumerator ReleaseInstance()
    {
        ResourceManager.ResourceProviders.Add(new LegacyResourcesProvider());
        ResourceManager.ResourceLocators.Add(new LegacyResourcesLocator());
        ResourceManager.InstanceProvider = new InstanceProvider();

        string cube1Path = resourcesFolder + "Cube1.prefab";
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        PrefabUtility.CreatePrefab(cube1Path, go);
        Destroy(go);

        IAsyncOperation op = ResourceManager.InstantiateAsync<GameObject, string>("Cube1");
        yield return op;

        Assert.IsNotNull(GameObject.Find("Cube1(Clone)"));

        ResourceManager.ReleaseInstance<GameObject>(op.Result as GameObject);
        op.Release();
        yield return null;
        Assert.IsNull(GameObject.Find("Cube1(Clone)"));
        DestroyAsset(cube1Path);
    }

    [UnityTest]
    public IEnumerator LoadAllObjects_Async()
    {
        ResourceManager.ResourceProviders.Add(new LegacyResourcesProvider());
        ResourceManager.ResourceLocators.Add(new LegacyResourcesLocator());

        string cubePath = resourcesFolder + "Cube.prefab";
        string cube1Path = resourcesFolder + "Cube1.prefab";

        GameObject go1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        PrefabUtility.CreatePrefab(cubePath, go1);
        PrefabUtility.CreatePrefab(cube1Path, go1);
        Destroy(go1);

        IResourceLocation loc1 = ResourceManager.GetResourceLocation("Cube");
        IResourceLocation loc2 = ResourceManager.GetResourceLocation("Cube1");
        List<IResourceLocation> locs = new List<IResourceLocation>() { loc1, loc2 };

        List<GameObject> loadedObjects = new List<GameObject>();
        IAsyncOperation loadOp = ResourceManager.LoadAllAsync<GameObject, IResourceLocation>(locs, (op) =>
            {
                GameObject go = op.Result as GameObject;
                loadedObjects.Add(go);
            });
        yield return loadOp;
        loadOp.Release();

        Assert.AreEqual(2, loadedObjects.Count);
        DestroyAsset(cubePath);
        DestroyAsset(cube1Path);
    }

    void DestroyAsset(string path)
    {
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
