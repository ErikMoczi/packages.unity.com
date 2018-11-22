using System.Linq;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEditor;

public class SceneLoadingTestsSetup : IPrebuildSetup
{
    public void Setup()
    {
        var editorBuildSettingsScenes = EditorBuildSettings.scenes.ToList();
        var testSceneGuid = AssetDatabase.FindAssets("TestRunner-TestScene").Single();
        editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(AssetDatabase.GUIDToAssetPath(testSceneGuid), true));
        EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
    }
}
