using System;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;
using UnityEngine.XR.Management;


namespace UnityEditor.XR.Management
{
 #if UNITY_EDITOR
    [InitializeOnLoad]
#endif
   public class XRGeneralSettingsPerBuildTarget : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<BuildTargetGroup> Keys = new List<BuildTargetGroup>();

        [SerializeField]
        List<XRGeneralSettings> Values = new List<XRGeneralSettings>();
        Dictionary<BuildTargetGroup, XRGeneralSettings> Settings = new Dictionary<BuildTargetGroup, XRGeneralSettings>();


#if UNITY_EDITOR
        // Simple class to give us updates when the asset database changes.
        class AssetCallbacks : AssetPostprocessor
        {
            static bool m_Upgrade = true;
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {             
                if (m_Upgrade)  
                {
                    m_Upgrade = false;
                    BeginUpgradeSettings();
                }
            }

            static void BeginUpgradeSettings()
            {
                string searchText = "t:XRGeneralSettings";
                string[] assets = AssetDatabase.FindAssets(searchText);
                if (assets.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(assets[0]);
                    XRGeneralSettingsUpgrade.UpgradeSettingsToPerBuildTarget(path);
                }
            }
        }
        
        
        static XRGeneralSettingsPerBuildTarget()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        static void PlayModeStateChanged(PlayModeStateChange state)
        {
            XRGeneralSettingsPerBuildTarget buildTargetSettings = null;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out buildTargetSettings);
            if (buildTargetSettings == null)
                return;

            XRGeneralSettings instance = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (instance == null)
                return;

            instance.InternalPlayModeStateChanged(state);
        }
#endif

        public void SetSettingsForBuildTarget(BuildTargetGroup targetGrroup, XRGeneralSettings settings)
        {
            Settings[targetGrroup] = settings;
        }

        public XRGeneralSettings SettingsForBuildTarget(BuildTargetGroup targetGroup)
        {
            XRGeneralSettings ret = null;
            Settings.TryGetValue(targetGroup, out ret);
            return ret;
        }

        public void OnBeforeSerialize()
        {
            Keys.Clear();
            Values.Clear();

            foreach (var kv in Settings)
            {
                Keys.Add(kv.Key);
                Values.Add(kv.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Settings = new Dictionary<BuildTargetGroup, XRGeneralSettings>();
            for (int i = 0; i < Math.Min(Keys.Count, Values.Count); i++)
            {
                Settings.Add(Keys[i], Values[i]);
            }
        }
    }
}