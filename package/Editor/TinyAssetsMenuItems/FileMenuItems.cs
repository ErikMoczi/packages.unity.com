
using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class FileMenuItems
    {
        private const int k_BasePriority = 201;
        private const string k_FileMenuPrefix = "File/" + TinyConstants.ApplicationName + "/";
        private const string k_TinyMenuPrefix = TinyConstants.ApplicationName + "/File/";

        private const string k_NewProject = "New Project";
        private const string k_OpenProject = "Open Project";
        private const string k_NewModule = "New Module";
        private const string k_OpenModule = "Open Module";
        private const string k_Save = "Save";
        private const string k_Close = "Close";
        private const string k_Build = "Build";
        private const string k_ImportSampleProjects = "Import Sample Projects";

        [MenuItem(k_TinyMenuPrefix + k_NewProject, true)]
        [MenuItem(k_FileMenuPrefix + k_NewProject, true)]
        [MenuItem(k_TinyMenuPrefix + k_OpenProject, true)]
        [MenuItem(k_FileMenuPrefix + k_OpenProject, true)]
        [MenuItem(k_TinyMenuPrefix + k_NewModule, true)]
        [MenuItem(k_FileMenuPrefix + k_NewModule, true)]
        [MenuItem(k_TinyMenuPrefix + k_OpenModule, true)]
        [MenuItem(k_FileMenuPrefix + k_OpenModule, true)]
        [MenuItem(k_TinyMenuPrefix + k_ImportSampleProjects, true)]
        [MenuItem(k_FileMenuPrefix + k_ImportSampleProjects, true)]
        public static bool ValidateIsEditMode()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(k_TinyMenuPrefix + k_Build, true)]
        [MenuItem(k_FileMenuPrefix + k_Build, true)]
        private static bool ValidateProjectIsOpened()
        {
            return ValidateIsEditMode() && TinyEditorApplication.ContextType == EditorContextType.Project;
        }

        [MenuItem(k_TinyMenuPrefix + k_Save, true)]
        [MenuItem(k_FileMenuPrefix + k_Save, true)]
        [MenuItem(k_TinyMenuPrefix + k_Close, true)]
        [MenuItem(k_FileMenuPrefix + k_Close, true)]
        private static bool ValidateContextIsOpened()
        {
            return ValidateIsEditMode() && TinyEditorApplication.ContextType != EditorContextType.None;
        }

        [MenuItem(k_TinyMenuPrefix + k_NewProject, priority = k_BasePriority)]
        [MenuItem(k_FileMenuPrefix + k_NewProject, priority = k_BasePriority)]
        public static void CreateNewProject()
        {
            CreateNew(TinyEditorApplication.NewProject);
        }

        [MenuItem(k_TinyMenuPrefix + k_OpenProject, priority = k_BasePriority + 1)]
        [MenuItem(k_FileMenuPrefix + k_OpenProject, priority = k_BasePriority + 1)]
        public static void OpenProject()
        {
            OpenForEditing("Load Project", Persistence.ProjectFileImporterExtension, TinyEditorApplication.LoadProject);
        }

        [MenuItem(k_TinyMenuPrefix + k_NewModule, priority = k_BasePriority + 50)]
        [MenuItem(k_FileMenuPrefix + k_NewModule, priority = k_BasePriority + 50)]
        public static void CreateNewModule()
        {
            CreateNew(TinyEditorApplication.NewModule);
        }

        [MenuItem(k_TinyMenuPrefix + k_OpenModule, priority = k_BasePriority + 51)]
        [MenuItem(k_FileMenuPrefix + k_OpenModule, priority = k_BasePriority + 51)]
        public static void OpenModule()
        {
            OpenForEditing("Load Module", Persistence.ModuleFileImporterExtension, TinyEditorApplication.LoadModule);
        }

        [MenuItem(k_TinyMenuPrefix + k_Save, priority = k_BasePriority + 100)]
        [MenuItem(k_FileMenuPrefix + k_Save, priority = k_BasePriority + 100)]
        public static void Save()
        {
            TinyEditorApplication.Save();
        }

        [MenuItem(k_TinyMenuPrefix + k_Close, priority = k_BasePriority + 101)]
        [MenuItem(k_FileMenuPrefix + k_Close, priority = k_BasePriority + 101)]
        public static void Close()
        {
            if (TinyEditorApplication.SaveChanges())
            {
                TinyEditorApplication.Close();
            }
        }

        [MenuItem(k_TinyMenuPrefix + k_Build, priority = k_BasePriority + 200)]
        [MenuItem(k_FileMenuPrefix + k_Build, priority = k_BasePriority + 200)]
        public static void Build()
        {
            TinyBuildPipeline.BuildAndLaunch();
        }

        [MenuItem(k_TinyMenuPrefix + k_ImportSampleProjects, priority = k_BasePriority + 300)]
        [MenuItem(k_FileMenuPrefix + k_ImportSampleProjects, priority = k_BasePriority + 300)]
        public static void ImportSampleProjects()
        {
            TinyRuntimeInstaller.InstallSamples(true);
        }

        private static void SelectTinyObject(IPersistentObject obj)
        {
            if (null != obj)
            {
                var path = Persistence.GetAssetPath(obj);
                Selection.activeInstanceID = AssetDatabase.LoadAssetAtPath<TinyScriptableObject>(path).GetInstanceID();
            }
        }

        private static void CreateNew<TPersistentObject>(Func<TPersistentObject> creator)
            where TPersistentObject : IPersistentObject
        {
            if (!TinyEditorApplication.SaveChanges())
            {
                return;
            }

            SelectTinyObject(creator.Invoke());
        }

        private static void OpenForEditing<TPersistentObject>(string panelName, string extension, Func<string, TPersistentObject> loader)
            where TPersistentObject : IPersistentObject
        {
            var path = OpenFilePanel(panelName, extension);

            if (!string.IsNullOrEmpty(path))
            {
                if (!TinyEditorApplication.SaveChanges())
                {
                    return;
                }

                // Convert to relative path
                if (path.StartsWith(Application.dataPath)) {
                    path =  "Assets" + path.Substring(Application.dataPath.Length);
                }

                TinyEditorApplication.Close();
                loader.Invoke(path);
            }
        }

        private static string OpenFilePanel(string panelName, string extension)
        {
            return EditorUtility.OpenFilePanel(panelName, Application.dataPath, extension);
        }
    }
}
