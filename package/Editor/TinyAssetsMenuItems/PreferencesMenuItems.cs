using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class PreferencesMenuItems
    {
        private const int k_BasePriority = 500;
        private const string k_TinyMenuPrefix = TinyConstants.ApplicationName + "/";
        private const string k_EnablePlayMode = "Enable Play Mode";

        private const string k_EnablePlayModePrefKey = "Unity.Tiny.EnablePlayMode";

        public static bool PlayModeEnabled
        {
            get => EditorPrefs.GetBool(k_EnablePlayModePrefKey, true);
            set => EditorPrefs.SetBool(k_EnablePlayModePrefKey, value);
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += () =>
            {
                Menu.SetChecked(k_TinyMenuPrefix + k_EnablePlayMode, PlayModeEnabled);
            };
        }

        [MenuItem(k_TinyMenuPrefix + k_EnablePlayMode, true)]
        public static bool ValidateIsEditMode()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(k_TinyMenuPrefix + k_EnablePlayMode, priority = k_BasePriority)]
        public static void ToggleEnablePlayMode()
        {
            var enabled = !PlayModeEnabled;
            PlayModeEnabled = enabled;
            Menu.SetChecked(k_TinyMenuPrefix + k_EnablePlayMode, enabled);
        }
    }
}
