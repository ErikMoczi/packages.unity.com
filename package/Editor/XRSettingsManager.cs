using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using UnityEditor;

using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.XR.Management;

namespace UnityEditor.XR.Management
{
    class XRSettingsManager : SettingsProvider
    {
        static XRSettingsManager s_SettingsManager = null;

        static GUIContent s_LoaderXRManagerLabel = new GUIContent("XR Manager Instance");

        SerializedObject m_SettingsWrapper;

        static XRGeneralSettings currentSettings
        {
            get
            {
                XRGeneralSettings generalSettings = null;
                EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out generalSettings);
                if (generalSettings == null)
                {
                    lock(s_SettingsManager)
                    {
                        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out generalSettings);
                        if (generalSettings == null)
                        {
                            string searchText = "t:XRGeneralSettings";
                            string[] assets = AssetDatabase.FindAssets(searchText);
                            if (assets.Length > 0)
                            {
                                string path = AssetDatabase.GUIDToAssetPath(assets[0]);
                                generalSettings = AssetDatabase.LoadAssetAtPath(path, typeof(XRGeneralSettings)) as XRGeneralSettings;
                            }
                        }

                        if (generalSettings == null)
                        {
                            generalSettings = ScriptableObject.CreateInstance(typeof(XRGeneralSettings)) as XRGeneralSettings;
                            string assetPath = EditorUtilities.GetAssetPathForComponents(EditorUtilities.s_DefaultGeneralSettingsPath);
                            if (!string.IsNullOrEmpty(assetPath))
                            {
                                assetPath = Path.Combine(assetPath, "XRGeneralSettings.asset");
                                AssetDatabase.CreateAsset(generalSettings, assetPath);
                            }
                        }

                        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, generalSettings, true);

                    }
                }
                return generalSettings;
            }
        }

        [UnityEngine.Internal.ExcludeFromDocs]
        XRSettingsManager(string path, SettingsScopes scopes = SettingsScopes.Project) : base(path, scopes)
        {
        }

        [SettingsProvider]
        [UnityEngine.Internal.ExcludeFromDocs]
        static SettingsProvider Create()
        {
            if (s_SettingsManager == null)
            {
                s_SettingsManager = new XRSettingsManager("XR");
            }

            return s_SettingsManager;
        }

        [SettingsProviderGroup]
        [UnityEngine.Internal.ExcludeFromDocs]
        static SettingsProvider[] CreateAllChildSettingsProviders()
        {
            List<SettingsProvider> ret = new List<SettingsProvider>();
            if (s_SettingsManager != null)
            {
                var ats = TypeLoaderExtensions.GetAllTypesWithAttribute<XRConfigurationDataAttribute>();
                foreach (var at in ats)
                {
                    XRConfigurationDataAttribute xrbda = at.GetCustomAttributes(typeof(XRConfigurationDataAttribute), true)[0] as XRConfigurationDataAttribute;
                    string settingsPath = String.Format("XR/{0}", xrbda.displayName);
                    var resProv = new XRConfigurationProvider(settingsPath, xrbda.buildSettingsKey, at);
                    ret.Add(resProv);
                }
            }

            return ret.ToArray();
        }

        void InitEditorData(ScriptableObject settings)
        {
            if (settings != null)
            {
                m_SettingsWrapper = new SerializedObject(settings);
            }
        }


        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            InitEditorData(currentSettings);
        }

        public override void OnDeactivate()
        {
            m_SettingsWrapper = null;
        }

        public override void OnGUI(string searchContext)
        {
            if (m_SettingsWrapper != null  && m_SettingsWrapper.targetObject != null)
            {
                m_SettingsWrapper.Update();

                SerializedProperty prop = m_SettingsWrapper.FindProperty("m_LoaderManagerInstance");
                // TODO: Need to get instance based on current build target.
                var obj = EditorGUILayout.ObjectField(s_LoaderXRManagerLabel, prop.objectReferenceValue, typeof(GameObject), false) as GameObject;
                EditorGUILayout.HelpBox("Drag or select a prefab that has an XRManager component assigned to it." +
                    " This will be used to initialize XR SDK at application start.\n\n" +
                    "The assigne manager instance will have automatic loading and running disabled.", MessageType.Info);
                if (obj != null && obj.GetComponent<XRManager>() != null)
                {
                    XRManager inst = obj.GetComponent<XRManager>();
                    inst.automaticLoading = false;
                    inst.automaticRunning = false;
                    prop.objectReferenceValue = obj;
                }
                else if (obj != null)
                {
                    Debug.LogError("The chosen prefab is missing an instance of the XRManager component.");
                }
                else if (obj == null)
                {
                    XRSettingsManager.currentSettings.LoaderManagerInstance = null;
                    prop.objectReferenceValue = null;
                }

                m_SettingsWrapper.ApplyModifiedProperties();

            }
        }

    }
}
