using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;

namespace UnityEditor.Build.Utilities
{
    internal static class ValidationMethods
    {
        public static bool ValidScene(GUID asset)
        {
            var path = AssetDatabase.GUIDToAssetPath(asset.ToString());
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".unity") || !File.Exists(path))
                return false;
            return true;
        }

        public static bool ValidSceneBundle(List<GUID> assets)
        {
            return assets.All(ValidScene);
        }

        public static bool ValidAsset(GUID asset)
        {
            var path = AssetDatabase.GUIDToAssetPath(asset.ToString());
            if (string.IsNullOrEmpty(path) || path.EndsWith(".unity") || !File.Exists(path))
                return false;
            return true;
        }

        public static bool ValidAssetBundle(List<GUID> assets)
        {
            return assets.All(ValidAsset);
        }

        public static bool HasDirtyScenes()
        {
            var unsavedChanges = false;
            var sceneCount = EditorSceneManager.sceneCount;
            for (var i = 0; i < sceneCount; ++i)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isDirty)
                    continue;
                unsavedChanges = true;
                break;
            }

            return unsavedChanges;
        }
    }
}