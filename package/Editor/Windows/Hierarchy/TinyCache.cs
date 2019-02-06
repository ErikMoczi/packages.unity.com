

using System;
using System.IO;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

using UnityEditor;
using UnityEditor.SceneManagement;

namespace Unity.Tiny
{
    internal static class TinyCache
    {
        #region Constants
        private const string k_TinyCacheFolderName = "TinyCache";
        private const string k_SceneScratchPadName = "TinyProject-DoNotEdit.unity";
        private const string k_ScratchPadDirectory = "Assets/" + k_TinyCacheFolderName;
        private const string k_ScratchPadPath = k_ScratchPadDirectory + "/" + k_SceneScratchPadName;
        #endregion

        public static Scene GetOrGenerateScratchPad()
        {
            if (!AssetDatabase.IsValidFolder(k_ScratchPadDirectory))
            {
                AssetDatabase.CreateFolder("Assets", k_TinyCacheFolderName);
            }

            Scene scene;
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(k_ScratchPadPath);
            if (null != asset)
            {
                scene = SceneManager.GetSceneByPath(k_ScratchPadPath);
                if (!scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene(k_ScratchPadPath, OpenSceneMode.Single);
                }
                return scene;
            }
            else
            {
                scene = SceneManager.GetSceneByPath(k_ScratchPadPath);
                if (scene.isLoaded)
                {
                    EditorSceneManager.SaveScene(scene, k_ScratchPadPath);
                    return scene;
                }
            }

            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, k_ScratchPadPath);
            return scene;
        }
    }
}

