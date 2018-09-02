using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildScript : MonoBehaviour
{
    [MenuItem("Build/AssetBundles")]
    public static void Build()
    {
        string dir = "AssetBundles";

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        UnityEditor.BuildPipeline.BuildAssetBundles(dir, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
    }
}
