using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;

using UnityEngine;
using UnityEditor;

namespace UnityEditor.XR.Management
{
    public interface XRPackageInitializationBase
    {
        string PackageName { get; }
        string LoaderFullTypeName { get; }
        string LoaderTypeName { get; }
        string SettingsFullTypeName { get; }
        string SettingsTypeName { get; }
        string PackageInitKey { get; }

        bool PopulateSettingsOnInitialization(ScriptableObject obj);
    }

    class PackageInitializationSettings : ScriptableObject
    {
        private static PackageInitializationSettings s_PackageSettings = null;
        private static object s_Lock = new object();

        [SerializeField]
        private List<string> m_Settings = new List<string>();

        private PackageInitializationSettings(){ }

        public static PackageInitializationSettings Instance
        {
            get
            {
                if (s_PackageSettings == null)
                {
                    lock(s_Lock)
                    {
                        if (s_PackageSettings == null)
                        {
                            s_PackageSettings = ScriptableObject.CreateInstance<PackageInitializationSettings>();
                        }
                    }
                }
                return s_PackageSettings;
            }
        }

        public void LoadSettings()
        {
            string packageInitPath = Path.Combine("ProjectSettings", "XRPackageSettings.asset");

            if (File.Exists(packageInitPath))
            {
                using (StreamReader sr = new StreamReader(packageInitPath))
                {
                    string settings = sr.ReadToEnd();
                    JsonUtility.FromJsonOverwrite(settings, this);
                }
            }
        }


        public void SaveSettings()
        {
            string packageInitPath = Path.Combine("ProjectSettings", "XRPackageSettings.asset");
            using (StreamWriter sw = new StreamWriter(packageInitPath))
            {
                string settings = JsonUtility.ToJson(this, true);
                sw.Write(settings);
            }
        }

        public bool HasSettings(string key)
        {
            return m_Settings.Contains(key);
        }

        public void AddSettings(string key)
        {
            if (!HasSettings(key))
                m_Settings.Add(key);
        }
    }


    [InitializeOnLoad]
    class PackageInitializationBootstrap
    {
        static PackageInitializationBootstrap()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.update += BeginPackageInitialization;
            }
        }

        static void BeginPackageInitialization()
        {

            foreach (var t in TypeLoaderExtensions.GetAllTypesWithInterface<XRPackageInitializationBase>())
            {
                XRPackageInitializationBase packageInit = Activator.CreateInstance(t) as XRPackageInitializationBase;
                InitPackage(packageInit);
            }
        }

        static void InitPackage(XRPackageInitializationBase packageInit)
        {
            PackageInitializationSettings.Instance.LoadSettings();

            if (PackageInitializationSettings.Instance.HasSettings(packageInit.PackageInitKey))
                return;

            EditorApplication.update -= BeginPackageInitialization;

            if (!InitializeLoaderInstance(packageInit))
            {
                Debug.LogWarning(
                    String.Format("{0} Loader Initialization not completed. You will need to create an instance of the loader using an instance of XRManager before you can use the Windows MR Package.", packageInit.PackageName));
            }

            if (!InitializeSettingsInstance(packageInit))
            {
                Debug.LogWarning(
                    String.Format("{0} Settings Initialization not completed. You will need to create an instance of settings to customize options specific to this pacakge. This can be done by selecting Edit->Settings and navigating to the XR/Windows Mixed Reality panel.", packageInit.PackageName));
            }

            PackageInitializationSettings.Instance.AddSettings(packageInit.PackageInitKey);
            PackageInitializationSettings.Instance.SaveSettings();
        }

        static ScriptableObject CreateScriptableObjectInstance(string packageName, string typeName, string instanceType, string path)
        {
            ScriptableObject obj = ScriptableObject.CreateInstance(typeName) as ScriptableObject;
            if (obj != null)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    string fileName = String.Format("{0}.asset", EditorUtilities.TypeNameToString(typeName));
                    string targetPath = Path.Combine(path, fileName);
                    AssetDatabase.CreateAsset(obj, targetPath);
                    Debug.LogFormat("{0} package initialization created default {1} instance at path {2}", packageName, instanceType.ToLower(), path);
                    return obj;
                }
            }
            return null;
        }

        static bool InitializeLoaderInstance(XRPackageInitializationBase packageInit)
        {
            bool ret = EditorUtilities.AssetDatabaseHasInstanceOfType(packageInit.LoaderTypeName);
            if (!ret)
            {
                ret = EditorUtility.DisplayDialog(
                    String.Format("{0} Package Initialization", packageInit.PackageName),
                    String.Format("Before using the {0} package you need to create an instance of the {0} Loader. Would you like to do that now?", packageInit.PackageName),
                    "Create Loader",
                    "Cancel");
                if (ret)
                {
                    var obj = CreateScriptableObjectInstance(packageInit.PackageName,
                        packageInit.LoaderFullTypeName,
                        "Loader",
                        EditorUtilities.GetAssetPathForComponents(EditorUtilities.s_DefaultLoaderPath));
                    ret = (obj != null);
                }
            }

            return ret;
        }
        static bool InitializeSettingsInstance(XRPackageInitializationBase packageInit)
        {
            bool ret = EditorUtilities.AssetDatabaseHasInstanceOfType(packageInit.SettingsTypeName);
            if (!ret)
            {
                ret = EditorUtility.DisplayDialog(
                    String.Format("{0} Package Initialization", packageInit.PackageName),
                    String.Format("Before using the {0} package you should create an instance of {0} Settings to provide for custom configuration. Would you like to do that now?", packageInit.PackageName),
                    "Create Settings",
                    "Cancel");
                if (ret)
                {
                    var obj = CreateScriptableObjectInstance(packageInit.PackageName,
                        packageInit.SettingsFullTypeName,
                        "Settings",
                        EditorUtilities.GetAssetPathForComponents(EditorUtilities.s_DefaultSettingsPath));
                    ret = packageInit.PopulateSettingsOnInitialization(obj);
                }
            }

            return ret;
        }

    }
}
