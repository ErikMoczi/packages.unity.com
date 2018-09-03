using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System;

namespace Cinemachine.Editor
{
    public class ScriptableObjectUtility : ScriptableObject
    {
        public static string kPackageRoot = "Packages/com.unity.cinemachine";

        public static string CinemachineInstallPath
        {
            get 
            { 
                // First see if we're a UPM package - use some asset that we expect to find
                string path = Path.GetFullPath(kPackageRoot + "/Editor/Resources/cm_logo_sm.png");
                int index = path.LastIndexOf("/Editor");
                if (index < 0 || !File.Exists(path))
                {
                    // Try as an ordinary asset
                    ScriptableObject dummy = ScriptableObject.CreateInstance<ScriptableObjectUtility>();
                    path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(dummy));
                    if (path.Length > 0)
                        path = Path.GetFullPath(path);
                    DestroyImmediate(dummy);
                }
                path = path.Replace('\\', '/'); // because of GetFullPath()
                index = path.LastIndexOf("/Editor");
                if (index >= 0)
                    path = path.Substring(0, index);
                if (path.Length > 0)
                    path = Path.GetFullPath(path);  // stupid backslashes
                return path;
            }
        }

        public static T CreateAt<T>(string assetPath) where T : ScriptableObject
        {
            return CreateAt(typeof(T), assetPath) as T;
        }

        public static ScriptableObject CreateAt(Type assetType, string assetPath)
        {
            ScriptableObject asset = ScriptableObject.CreateInstance(assetType);
            if (asset == null)
            {
                Debug.LogError("failed to create instance of " + assetType.Name + " at " + assetPath);
                return null;
            }
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;        
        }


#if false // unused
        public static bool AddDefineForAllBuildTargets(string k_Define)
        {
            bool added = false;
            var targets = Enum.GetValues(typeof(BuildTargetGroup))
                .Cast<BuildTargetGroup>()
                .Where(x => x != BuildTargetGroup.Unknown)
                .Where(x => !BuildTargetIsObsolete(x));

            foreach (var target in targets)
            {
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target).Trim();

                var list = defines.Split(';', ' ')
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();

                if (!list.Contains(k_Define))
                {
                    list.Add(k_Define);
                    defines = list.Aggregate((a, b) => a + ";" + b);

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
                    added = true;
                }
            }
            return added;
        }

        static bool BuildTargetIsObsolete(BuildTargetGroup group)
        {
            var attrs = typeof(BuildTargetGroup)
                .GetField(group.ToString())
                .GetCustomAttributes(typeof(ObsoleteAttribute), false);

            return attrs != null && attrs.Length > 0;
        }
#endif

        public static void Create<T>(bool prependFolderName = false, bool trimName = true) where T : ScriptableObject
        {
            string className = typeof(T).Name;
            string assetName = className;
            string folder = GetSelectedAssetFolder();

            if (trimName)
            {
                string[] standardNames = new string[] { "Asset", "Attributes", "Container" };
                foreach (string standardName in standardNames)
                {
                    assetName = assetName.Replace(standardName, "");
                }
            }

            if (prependFolderName)
            {
                string folderName = Path.GetFileName(folder);
                assetName = (string.IsNullOrEmpty(assetName) ? folderName : string.Format("{0}_{1}", folderName, assetName));
            }

            Create(className, assetName, folder);
        }

        private static ScriptableObject Create(string className, string assetName, string folder)
        {
            ScriptableObject asset = ScriptableObject.CreateInstance(className);
            if (asset == null)
            {
                Debug.LogError("failed to create instance of " + className);
                return null;
            }

            asset.name = assetName ?? className;

            string assetPath = GetUnusedAssetPath(folder, asset.name);
            AssetDatabase.CreateAsset(asset, assetPath);

            return asset;
        }

        private static string GetSelectedAssetFolder()
        {
            if ((Selection.activeObject != null) && AssetDatabase.Contains(Selection.activeObject))
            {
                string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                string assetPathAbsolute = string.Format("{0}/{1}", Path.GetDirectoryName(Application.dataPath), assetPath);

                if (Directory.Exists(assetPathAbsolute))
                {
                    return assetPath;
                }
                else
                {
                    return Path.GetDirectoryName(assetPath);
                }
            }

            return "Assets";
        }

        private static string GetUnusedAssetPath(string folder, string assetName)
        {
            for (int n = 0; n < 9999; n++)
            {
                string assetPath = string.Format("{0}/{1}{2}.asset", folder, assetName, (n == 0 ? "" : n.ToString()));
                string existingGUID = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(existingGUID))
                {
                    return assetPath;
                }
            }

            return null;
        }
    }
}
