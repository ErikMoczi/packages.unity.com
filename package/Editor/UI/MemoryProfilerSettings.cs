using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Unity.MemoryProfiler.Editor
{
    internal static class MemoryProfilerSettings
    {
        const string k_SnapshotPathEditorPerf = "Unity.MemoryProfiler.Editor.MemorySnapshotStoragePath";
        const string k_DefaultPath = "./MemoryCaptures";
        public static string MemorySnapshotStoragePath
        {
            get
            {
                return EditorPrefs.GetString(k_SnapshotPathEditorPerf, k_DefaultPath);
            }
            set
            {
                EditorPrefs.SetString(k_SnapshotPathEditorPerf, value);
            }
        }

        public static string AbsoluteMemorySnapshotStoragePath
        {
            get
            {
                string folderPath = MemoryProfilerSettings.MemorySnapshotStoragePath;
                if (folderPath.StartsWith("./"))
                {
                    folderPath = Path.Combine(Application.dataPath.Replace("/Assets", ""), MemoryProfilerSettings.MemorySnapshotStoragePath.Replace("./", ""));
                }
                else if (folderPath.StartsWith("../"))
                {
                    var path = Application.dataPath.Split('/');
                    int folderUpCount = 1;
                    while (folderPath.IndexOf("../", folderUpCount * 3) == folderUpCount * 3)
                    {
                        folderUpCount++;
                    }
                    int pathLength = path.Length - 1 - folderUpCount;
                    if (pathLength > 0)
                    {
                        string combinedPath = path[0] + Path.DirectorySeparatorChar;
                        for (int i = 1; i < pathLength; i++)
                        {
                            combinedPath = Path.Combine(combinedPath, path[i]);
                        }
                        folderPath = Path.Combine(combinedPath, folderPath.Substring(folderUpCount * 3));
                    }
                    else
                    {
                        Debug.LogError(folderPath + " Is not a valid relative path, it has more instances of '../' than folders above the project folder. Please change the path for memory snapshots in the Preferences.");
                        return null;
                    }
                }
                else
                {
                    Debug.LogError(folderPath + " Is not a valid path. Only relative paths starting with './' or '../' are allowed. Please change the path for memory snapshots in the Preferences.");
                    return null;
                }
                return folderPath;
            }
        }

        public static void ResetMemorySnapshotStoragePathToDefault()
        {
            EditorPrefs.SetString(k_SnapshotPathEditorPerf, k_DefaultPath);
        }
    }

    internal class MemoryProfilerSettingsEditor
    {
        class Content
        {
            public static readonly GUIContent SnapshotPathLabel = EditorGUIUtility.TrTextContent("Memory Snapshot Storage Path");
            public static readonly string OnlyRelativePaths = L10n.Tr("Only relative paths are allowed");
            public static readonly string OKButton = L10n.Tr("OK");
            public static readonly string InvalidPathWindow = L10n.Tr("Invalid Path");
        }
        const string k_RootPathSignifier = "./";
        const string k_PathOneUpSignifier = "../";

        [SettingsProvider()]
        static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Preferences/Profiling/MemoryProfiler", SettingsScope.User)
            {
                guiHandler = searchConext =>
                {
                    PreferencesGUI();
                }
            };
            provider.PopulateSearchKeywordsFromGUIContentProperties<Content>();
            return provider;
        }

        static void PreferencesGUI()
        {
            float layoutMaxWidth = 500;
            float s_DefaultLabelWidth = 250;
            float m_LabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = s_DefaultLabelWidth;
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(layoutMaxWidth));
            GUILayout.Space(10);
            GUILayout.BeginVertical();
            GUILayout.Space(10);
            {
                EditorGUI.BeginChangeCheck();
                var prevControl = GUI.GetNameOfFocusedControl();
                var val = EditorGUILayout.TextField(Content.SnapshotPathLabel, MemoryProfilerSettings.MemorySnapshotStoragePath);
                if (EditorGUI.EndChangeCheck())
                {
                    if (!(val.StartsWith(k_RootPathSignifier) || val.StartsWith(k_PathOneUpSignifier)))
                    {
                        if (EditorUtility.DisplayDialog(Content.InvalidPathWindow, Content.OnlyRelativePaths, Content.OKButton))
                        {
                            GUI.FocusControl(prevControl);
                            var currentlySavedPath = MemoryProfilerSettings.MemorySnapshotStoragePath;
                            // in case this faulty path has actually been saved, fix it back to default
                            if (!(currentlySavedPath.StartsWith(k_RootPathSignifier) || currentlySavedPath.StartsWith(k_PathOneUpSignifier)))
                                MemoryProfilerSettings.ResetMemorySnapshotStoragePathToDefault();
                        }
                    }
                    else
                    {
                        MemoryProfilerSettings.MemorySnapshotStoragePath = val;
                        var collectionPath = MemoryProfilerSettings.AbsoluteMemorySnapshotStoragePath;
                        var info = new DirectoryInfo(collectionPath);
                        if (!info.Exists)
                        {
                            info = Directory.CreateDirectory(collectionPath);
                            if (!info.Exists)
                                throw new UnityException("Failed to create directory, with provided preferences path: " + collectionPath);
                        }
                    }
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = m_LabelWidth;
        }
    }
}
