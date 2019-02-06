using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    [InitializeOnLoad]
    public static class UserStartupCode
    {
        internal static void RunStartupCode()
        {
            TutorialManager.WriteAssetsToTutorialDefaultsFolder();

            if (TutorialProjectSettings.instance.startupTutorial != null)
                TutorialManager.instance.StartTutorial(TutorialProjectSettings.instance.startupTutorial);

            // Ensure Editor is in predictable state
            EditorPrefs.SetString("ComponentSearchString", string.Empty);
            Tools.current = Tool.Move;
        }

        internal static string initFileMarkerPath = "Temp/InitCodeMarker";
        internal static string dontRunInitCodeMarker = "Assets/DontRunInitCodeMarker";

        static UserStartupCode()
        {
            if (ProjectMode.IsAuthoringMode())
                return;
            if (IsDontRunInitCodeMarkerSet())
                return;
            if (IsInitilized())
                return;

            // Temporarily enter play mode to work around issue with asset importing
            // ScriptableObject asset might be imported twice depending on the asset import order
            // When the same asset is imported again the previous instance is unloaded
            // This can cause existing references to become invalid (i.e. m_CachedPtr == 0x0)
            // Entering and and immediately existing play mode forces a domain reload which fixes this issue
            EditorApplication.isPlaying = true;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            switch (playModeStateChange)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    EditorApplication.isPlaying = false;
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                    InitRunStartupCode();
                    break;
            }
        }

        private static void InitRunStartupCode()
        {
            SetInitilized();
            EditorApplication.update -= InitRunStartupCode;
            RunStartupCode();
        }

        public static bool IsInitilized()
        {
            return File.Exists(initFileMarkerPath);
        }

        private static bool IsDontRunInitCodeMarkerSet()
        {
            return Directory.Exists(dontRunInitCodeMarker);
        }

        public static void SetInitilized()
        {
            File.CreateText(initFileMarkerPath).Close();
        }
    }
}
