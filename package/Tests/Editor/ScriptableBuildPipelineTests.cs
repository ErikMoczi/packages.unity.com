using System.Collections;
using System.IO;
using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;

[TestFixture]
class ScriptableBuildPipelineTests
{
    const string k_FolderPath = "Test";
    const string k_TmpPath = "tmp";
    const string k_ScenePath = "Assets/testScene.unity";
    string k_TestAssetsPath = "Assets/TestAssetsOnlyWillBeDeleted";
    string k_CubePath = "";

    [OneTimeSetUp]
    public void Setup()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        Directory.CreateDirectory(k_TestAssetsPath);
        k_CubePath = k_TestAssetsPath + "/Cube.prefab";
        PrefabUtility.CreatePrefab(k_CubePath, GameObject.CreatePrimitive(PrimitiveType.Cube));
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        AssetDatabase.DeleteAsset(k_ScenePath);
        CleanupFolders();
    }

    public static ReturnCode RunTask<T>(params IContextObject[] args) where T : IBuildTask
    {
        IBuildContext context = new BuildContext(args);
        IBuildTask instance = Activator.CreateInstance<T>();
        return instance.Run(context);
    }

    [UnityTest]
    public IEnumerator BuildPipeline_AssetBundleBuild_DoesNotResetUnsavedScene()
    {
        Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        yield return null;
        EditorSceneManager.SaveScene(s, k_ScenePath);
        GameObject.CreatePrimitive(PrimitiveType.Cube);
        EditorSceneManager.MarkSceneDirty(s);

        GameObject objectWeAdded = GameObject.Find("Cube");
        Assert.IsNotNull(objectWeAdded, "No object before entering playmode");
        Assert.AreEqual("testScene", EditorSceneManager.GetActiveScene().name);

        IBuildParameters buildParameters = GetBuildParameters(k_FolderPath, k_TmpPath);
        IBundleBuildContent buildContent = GetBundleContent();
        IBundleBuildResults results;

        ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParameters, buildContent, out results);
        Assert.AreEqual(ReturnCode.UnsavedChanges, exitCode);

        Assert.AreEqual("testScene", EditorSceneManager.GetActiveScene().name);
        objectWeAdded = GameObject.Find("Cube");
        Assert.IsNotNull(objectWeAdded, "No object after entering playmode");
    }

    [UnityTest]
    public IEnumerator DefaultBuildTasks_ProjectInCleanState()
    {
        Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        yield return null;

        ReturnCode exitCode = RunTask<ProjectInCleanState>();
        Assert.AreEqual(ReturnCode.Success, exitCode);

        EditorSceneManager.MarkSceneDirty(s);

        exitCode = RunTask<ProjectInCleanState>();
        Assert.AreEqual(ReturnCode.UnsavedChanges, exitCode);
    }


    [Test]
    public void DefaultBuildTasks_WriteSerialziedFiles()
    {
        IBuildParameters buildParams = GetBuildParameters(k_FolderPath, k_TmpPath);
        IDependencyData dependencyData = new BuildDependencyData();
        IWriteData writeData = new BuildWriteData();
        IBuildResults results = new BuildResults();
        
        ReturnCode exitCode = RunTask<WriteSerializedFiles>(buildParams, dependencyData, writeData, results);
        Assert.AreEqual(ReturnCode.Success, exitCode);
    }

    [Test]
    public void DefaultBuildTasks_GenerateBundlePacking()
    {
        IBundleBuildContent buildContent = GetBundleContent();
        IDependencyData dep = GetDependancyData();
        IBundleWriteData writeData = new BundleWriteData();
        IDeterministicIdentifiers determinsiticId = new PrefabPackedIdentifiers();
        
        ReturnCode exitCode = RunTask<GenerateBundlePacking>(buildContent, dep, writeData, determinsiticId);
        Assert.AreEqual(ReturnCode.Success, exitCode);
    }

    [Test]
    public void DefaultBuildTasks_GenerateBundleCommands()
    {
        IBundleBuildContent buildContent = GetBundleContent();
        IDependencyData dep = GetDependancyData();
        IBundleWriteData writeData = new BundleWriteData();
        IDeterministicIdentifiers determinsiticId = new PrefabPackedIdentifiers();
        
        RunTask<GenerateBundlePacking>(buildContent, dep, writeData, determinsiticId);
        
        ReturnCode exitCode = RunTask<GenerateBundleCommands>(buildContent, dep, writeData, determinsiticId);
        Assert.AreEqual(ReturnCode.Success, exitCode);
    }

    [Test]
    public void DefaultBuildTasks_GenerateBundleMaps()
    {
        IDependencyData dep = GetDependancyData();
        IBundleWriteData writeData = new BundleWriteData();
        
        ReturnCode exitCode = RunTask<GenerateBundleMaps>(dep, writeData);
        Assert.AreEqual(ReturnCode.Success, exitCode);
    }

    [Test]
    public void DefaultBuildTasks_PostPackingCallback()
    {
        bool packingCallbackCalled = false;

        IBuildParameters buildParams = GetBuildParameters(k_FolderPath, k_TmpPath);
        IDependencyData dep = GetDependancyData();
        IBundleWriteData writeData = new BundleWriteData();
        BuildCallbacks callback = new BuildCallbacks();
        callback.PostPackingCallback = (parameters, data, arg3) =>
        {
            packingCallbackCalled = true;
            return ReturnCode.Success;
        };
        
        ReturnCode exitCode = RunTask<PostPackingCallback>(buildParams, dep, writeData, callback);
        Assert.AreEqual(ReturnCode.Success, exitCode);
        Assert.IsTrue(packingCallbackCalled);
    }

    [Test]
    public void DefaultBuildTasks_PostWritingCallback()
    {
        bool writingCallbackCalled = false;

        IBuildParameters buildParams = GetBuildParameters(k_FolderPath, k_TmpPath);
        IDependencyData dep = GetDependancyData();
        IWriteData writeData = new BuildWriteData();
        IBuildResults results = new BuildResults();
        BuildCallbacks callback = new BuildCallbacks();
        callback.PostWritingCallback = (parameters, data, arg3, arg4) =>
        {
            writingCallbackCalled = true;
            return ReturnCode.Success;
        };

        ReturnCode exitCode = RunTask<PostWritingCallback>(buildParams, dep, writeData, results, callback);
        Assert.AreEqual(ReturnCode.Success, exitCode);
        Assert.IsTrue(writingCallbackCalled);
    }

    [Test]
    public void DefaultBuildTasks_PostDependencyCallback()
    {
        bool dependencyCallbackCalled = false;

        IBuildParameters buildParameters = GetBuildParameters(k_FolderPath, k_TmpPath);
        IDependencyData dep = GetDependancyData();
        BuildCallbacks callback = new BuildCallbacks();
        callback.PostDependencyCallback = (parameters, data) =>
        {
            dependencyCallbackCalled = true;
            return ReturnCode.Success;
        };

        ReturnCode exitCode = RunTask<PostDependencyCallback>(buildParameters, dep, callback);
        Assert.AreEqual(ReturnCode.Success, exitCode);
        Assert.IsTrue(dependencyCallbackCalled);
    }

    [Test]
    public void DefaultBuildTasks_PostScriptsCallbacks()
    {
        bool scriptsCallbackCalled = false;

        IBuildParameters buildParameters = GetBuildParameters(k_FolderPath, k_TmpPath);
        IBuildResults results = new BuildResults();
        BuildCallbacks callback = new BuildCallbacks();
        callback.PostScriptsCallbacks = (parameters, buildResults) =>
        {
            scriptsCallbackCalled = true;
            return ReturnCode.Success;
        };

        ReturnCode exitCode = RunTask<PostScriptsCallback>(buildParameters, results, callback);
        Assert.AreEqual(ReturnCode.Success, exitCode);
        Assert.IsTrue(scriptsCallbackCalled);
    }

    IBundleBuildContent GetBundleContent()
    {
        List<AssetBundleBuild> buildData = new List<AssetBundleBuild>();
        AssetBundleBuild dataPoint1 = new AssetBundleBuild()
        {
            addressableNames = new string[] { },
            assetBundleName = "bundle",
            assetBundleVariant = "",
            assetNames = new string[] { k_CubePath }
        };
        buildData.Add(dataPoint1);
        IBundleBuildContent buildContent = new BundleBuildContent(buildData);
        return buildContent;
    }

    IDependencyData GetDependancyData()
    {
        GUID guid;
        GUID.TryParse(AssetDatabase.AssetPathToGUID(k_CubePath), out guid);
        ObjectIdentifier[] oId = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(guid, EditorUserBuildSettings.activeBuildTarget);
        AssetLoadInfo loadInfo = new AssetLoadInfo()
        {
            asset = guid,
            address = k_CubePath,
            includedObjects = oId.ToList(),
            referencedObjects = oId.ToList()
        };

        IDependencyData dep = new BuildDependencyData();
        dep.AssetInfo.Add(guid, loadInfo);

        return dep;
    }

    IBuildParameters GetBuildParameters(string folderPath, string tmpPath)
    {
        if (Directory.Exists(folderPath))
            Directory.Delete(folderPath, true);
        if (Directory.Exists(tmpPath))
            Directory.Delete(tmpPath, true);

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        if (!Directory.Exists(tmpPath))
            Directory.CreateDirectory(tmpPath);

        IBuildParameters buildParams = new BuildParameters(EditorUserBuildSettings.activeBuildTarget, BuildTargetGroup.Unknown, folderPath);
        buildParams.TempOutputFolder = tmpPath;
        return buildParams;
    }

    void CleanupFolders()
    {
        if (Directory.Exists(k_FolderPath))
            Directory.Delete(k_FolderPath, true);
        if (Directory.Exists(k_TmpPath))
            Directory.Delete(k_TmpPath, true);
        if (Directory.Exists(k_TestAssetsPath))
            Directory.Delete(k_TestAssetsPath, true);
        if (File.Exists(k_TestAssetsPath + ".meta"))
            File.Delete(k_TestAssetsPath + ".meta");
    }
}
