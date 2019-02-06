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

            EditorApplication.update += InitRunStartupCode;
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
