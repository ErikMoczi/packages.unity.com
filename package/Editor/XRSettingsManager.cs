using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using UnityEditor;

using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
using UnityEngine.XR.Management;

namespace UnityEditor.XR.Management
{
    class XRSettingsManager : SettingsProvider
    {
        static XRSettingsManager s_SettingsManager = null;

        static GUIContent s_LoaderXRManagerLabel = new GUIContent("XR Manager Instance");
        static GUIContent s_LoaderInitOnStartLabel = new GUIContent("Initialize on Startup");

        SerializedObject m_SettingsWrapper;

        static XRGeneralSettingsPerBuildTarget currentSettings
        {
            get
            {
                XRGeneralSettingsPerBuildTarget generalSettings = null;
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
                                generalSettings = AssetDatabase.LoadAssetAtPath(path, typeof(XRGeneralSettingsPerBuildTarget)) as XRGeneralSettingsPerBuildTarget;
                            }
                        }

                        if (generalSettings == null)
                        {
                            generalSettings = ScriptableObject.CreateInstance(typeof(XRGeneralSettingsPerBuildTarget)) as XRGeneralSettingsPerBuildTarget;
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
        XRSettingsManager(string path, SettingsScope scopes = SettingsScope.Project) : base(path, scopes)
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

                BuildTargetGroup buildTargetGroup = EditorGUILayout.BeginBuildTargetSelectionGrouping();

                XRGeneralSettings settings = currentSettings.SettingsForBuildTarget(buildTargetGroup);
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<XRGeneralSettings>() as XRGeneralSettings;
                    currentSettings.SetSettingsForBuildTarget(buildTargetGroup, settings);
                    AssetDatabase.AddObjectToAsset(settings, AssetDatabase.GetAssetOrScenePath(currentSettings));
                }

                var serializedSettingsObject = new SerializedObject(settings);
                serializedSettingsObject.Update();

                SerializedProperty loaderProp = serializedSettingsObject.FindProperty("m_LoaderManagerInstance");
                var obj = EditorGUILayout.ObjectField(s_LoaderXRManagerLabel, loaderProp.objectReferenceValue, typeof(GameObject), false) as GameObject;
                if (obj != null && obj.GetComponent<XRManager>() != null)
                {
                    XRManager inst = obj.GetComponent<XRManager>();
                    inst.automaticLoading = false;
                    inst.automaticRunning = false;
                    loaderProp.objectReferenceValue = obj;
                }
                else if (obj != null)
                {
                    Debug.LogError("The chosen prefab is missing an instance of the XRManager component.");
                }
                else if (obj == null)
                {
                    settings.LoaderManagerInstance = null;
                    loaderProp.objectReferenceValue = null;
                }
                
                SerializedProperty initOnStartProp = serializedSettingsObject.FindProperty("m_InitManagerOnStart");
                EditorGUILayout.PropertyField(initOnStartProp, s_LoaderInitOnStartLabel);

                EditorGUILayout.HelpBox("Select a prefab that has an XRManager component assigned to it." +
                    " If initialize is enabled, this will be used to initialize the XR SDK at application start, " +
                    "and the assigned manager instance will have automatic loading and running disabled.\n\n" +
                    "If initialize is disabled, the manager will only be used to do minimal pre-initialization at engine startup.", MessageType.Info);

                serializedSettingsObject.ApplyModifiedProperties();

                EditorGUILayout.EndBuildTargetSelectionGrouping();

                m_SettingsWrapper.ApplyModifiedProperties();
            }
        }

    }
}
