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

            //we delay the call because (i think) all tutorial assets are not loaded yet
            EditorApplication.update += InitRunStartupCode;
        }

        private static void InitRunStartupCode()
        {
            // Work around issue where loaded tutorial has invalid references to its pages
            // Something goes wrong when importing the tutorial assets
            // This causes the tutorial pages to be deserialized twice invaliding the previous instance
            // Entering play mode here triggers a domain reload which fixes the issue
            // When the first tutorial is started we exit play mode again
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
                return;
            }

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
