using System.Collections.Generic;
using System.IO;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnityEditor.XR.ARKit
{
    /// <summary>
    /// Holds settings that are used to configure the ARKit XR Plugin.
    /// </summary>
    public class ARKitSettings : ScriptableObject
    {
        /// <summary>
        /// Enum which defines whether ARKit is optional or required.
        /// </summary>
        public enum Requirement
        {
            /// <summary>
            /// ARKit is required, which means the app cannot be installed on devices that do not support ARKit.
            /// </summary>
            Required,

            /// <summary>
            /// ARKit is optional, which means the the app can be installed on devices that do not support ARKit.
            /// </summary>
            Optional
        }

        [SerializeField, Tooltip("Toggles whether ARKit is required for this app. Will make app only downloadable by devices with ARKit support if set to 'Required'.")]
        Requirement m_ARKitRequirement;

        /// <summary>
        /// Determines whether ARKit is required for this app: will make app only downloadable by devices with ARKit support if set to <see cref="Requirement.Required"/>.
        /// </summary>
        public Requirement ARKitRequirement
        {
            get { return m_ARKitRequirement; }
            set { m_ARKitRequirement = value; }
        }

        [SerializeField, Tooltip("Toggles whether ARKit FaceTracking is enabled for this app. Will make app use TrueDepth camera APIs when enabled (which requires privacy policy during submission).")]
        bool m_ARKitFaceTrackingEnabled;

        /// <summary>
        /// Determines whether ARKit FaceTracking is enabled for this app. Will make app use TrueDepth camera APIs when enabled (which requires privacy policy during submission)./>.
        /// </summary>
        public bool ARKitFaceTrackingEnabled
        {
            get { return m_ARKitFaceTrackingEnabled; }
            set { m_ARKitFaceTrackingEnabled = value; }
        }

        /// <summary>
        /// Gets the currently selected settings, or create a default one if no <see cref="ARKitSettings"/> has been set in Player Settings.
        /// </summary>
        /// <returns>The ARKit settings to use for the current Player build.</returns>
        public static ARKitSettings GetOrCreateSettings()
        {
            var settings = currentSettings;
            if (settings != null)
                return settings;

            return CreateInstance<ARKitSettings>();
        }

        /// <summary>
        /// Get or set the <see cref="ARKitSettings"/> that will be used for the player build.
        /// </summary>
        public static ARKitSettings currentSettings
        {
            get
            {
                ARKitSettings settings = null;
                if (EditorBuildSettings.TryGetConfigObject(k_ConfigObjectName, out settings) == false)
                {
                    settings = null;
                }
                return settings;
            }

            set
            {
                if (value == null)
                {
                    EditorBuildSettings.RemoveConfigObject(k_ConfigObjectName);
                }
                else
                {
                    EditorBuildSettings.AddConfigObject(k_ConfigObjectName, value, true);
                }
            }
        }

        internal static bool TrySelect()
        {
            var settings = currentSettings;
            if (settings == null)
                return false;

            Selection.activeObject = settings;
            return true;
        }

        static readonly string k_ConfigObjectName = "com.unity.xr.arkit.PlayerSettings";
    }

    internal class SettingsSelectionWindow : EditorWindow
    {
        [MenuItem("Edit/Project Settings/ARKit XR Plugin")]
        static void ShowSelectionWindow()
        {
            ARKitSettings.TrySelect();
            Rect rect = new Rect(500, 300, 400, 150);
            var window = GetWindowWithRect<SettingsSelectionWindow>(rect);
            window.titleContent = new GUIContent("ARKit XR Plugin");
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Space(5);
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.wordWrap = true;
            EditorGUILayout.LabelField("Select an existing ARKitSettings object or create a new one.", titleStyle);

            EditorGUI.BeginChangeCheck();
            ARKitSettings.currentSettings =
                EditorGUILayout.ObjectField("ARKitSettings", ARKitSettings.currentSettings, typeof(ARKitSettings), false) as ARKitSettings;
            if (EditorGUI.EndChangeCheck())
                ARKitSettings.TrySelect();

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Create New"))
                Create();

            if (GUILayout.Button("Close"))
                Close();

            EditorGUILayout.EndHorizontal();
        }

        void Create()
        {
            var path = EditorUtility.SaveFilePanelInProject("Save ARKit XR Plugin Settings", "ARKitSettings", "asset", "Please enter a filename to save the ARKit XR Plugin settings.");
            if (string.IsNullOrEmpty(path))
                return;

            var settings = CreateInstance<ARKitSettings>();
            AssetDatabase.CreateAsset(settings, path);
            ARKitSettings.currentSettings = settings;
        }
    }
}
