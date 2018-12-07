
using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [UsedImplicitly]
    internal class TinySettingsProvider : SettingsProvider
    {
        public TinySettingsProvider()
            : base("Project/Tiny")
        {
        }

        [SettingsProvider]
        [UsedImplicitly]
        public static SettingsProvider Provider()
        {
            return new TinySettingsProvider(){ label = "Tiny" };
        }

        #region Unity
        private Vector2 m_ScrollPosition = Vector2.zero;

        public override void OnGUI(string searchContext)
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            try
            {
                if (TinyEditorApplication.ContextType == EditorContextType.Project)
                {
                    var project = TinyEditorApplication.Project;
                    GUILayout.Label(project.Name, TinyStyles.Header1);
                    DrawProjectSettings(project);
                }
                else if (TinyEditorApplication.ContextType == EditorContextType.Module)
                {
                    var module = TinyEditorApplication.Module;
                    GUILayout.Label(module.Name, TinyStyles.Header1);
                    DrawModuleSettings(module);
                }
                else
                {
                    ++EditorGUI.indentLevel;
                    GUILayout.Label("Tiny Settings", TinyStyles.Header1);
                    EditorGUILayout.LabelField("No Tiny context is currently opened.");
                    --EditorGUI.indentLevel;
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        #endregion

        #region Project Settings
        private static TinyContext s_Context;
        private static Vector2 m_PreviousCanvasSize = -Vector2.one;

        [TinyInitializeOnLoad]
        private static void ResetState()
        {
            TinyEditorApplication.OnLoadProject += SetUpTree;
        }

        private static void SetUpTree(TinyProject project, TinyContext context)
        {
            s_Context = context;
        }

        private static void DrawProjectSettings(TinyProject project)
        {
            EditorGUIUtility.labelWidth = 225;
            var module = project.Module.Dereference(project.Registry);

            EditorGUILayout.BeginVertical();

            try
            {
                GUILayout.Label("Project Settings", TinyStyles.Header2);
                EditorGUILayout.Space();

                ++EditorGUI.indentLevel;
                project.Settings.CanvasAutoResize = EditorGUILayout.Toggle("Auto-Resize Canvas", project.Settings.CanvasAutoResize);

                if (!project.Settings.CanvasAutoResize)
                {
                    ++EditorGUI.indentLevel;
                    project.Settings.CanvasWidth = EditorGUILayout.DelayedIntField("Width", project.Settings.CanvasWidth);
                    project.Settings.CanvasHeight = EditorGUILayout.DelayedIntField("Height", project.Settings.CanvasHeight);
                    --EditorGUI.indentLevel;
                }

                project.Settings.RenderMode = (RenderingMode)EditorGUILayout.EnumPopup("Render Mode", project.Settings.RenderMode);
                project.Settings.IncludeWebPDecompressor = EditorGUILayout.Toggle(new GUIContent("Include WebP Decompressor", "Include WebP decompressor code in build. Required for browsers that does not support WebP image format."), project.Settings.IncludeWebPDecompressor);
                project.Settings.EmbedAssets = EditorGUILayout.Toggle(new GUIContent("Embedded Assets", "Assets are embedded as base64 (this will increase asset size by approx 34%)."), project.Settings.EmbedAssets);
                project.Settings.MemorySize = EditorGUILayout.DelayedIntField(new GUIContent("Memory Size", "Total memory size pre-allocated for the entire project."), project.Settings.MemorySize);

                TextureSettingsField(project.Settings.DefaultTextureSettings);

                DrawCodeSettings(module);

                --EditorGUI.indentLevel;

                EditorGUILayout.Space();
                GUILayout.Label("Build Settings", TinyStyles.Header2);
                EditorGUILayout.Space();
                ++EditorGUI.indentLevel;

                var workspace = TinyEditorApplication.EditorContext.Workspace;
                workspace.BuildConfiguration = (TinyBuildConfiguration)EditorGUILayout.EnumPopup("Build Configuration", workspace.BuildConfiguration);
                workspace.Platform = (TinyPlatform) EditorGUILayout.EnumPopup("Build Target", workspace.Platform);

                workspace.ClearConsoleAfterCompilation = EditorGUILayout.Toggle(
                    new GUIContent("Clear Console After Compilation",
                        "Clears the Console window each time scripts are compiled"),
                    workspace.ClearConsoleAfterCompilation);

                GUI.enabled = (workspace.BuildConfiguration != TinyBuildConfiguration.Release);
                {
                    workspace.AutoConnectProfiler = EditorGUILayout.Toggle(
                        new GUIContent("Auto-Connect Profiler",
                            "Automatically connect to the Unity Profiler at launch."),
                        workspace.AutoConnectProfiler);
                }
                GUI.enabled = true;

                project.Settings.LocalWSServerPort = EditorGUILayout.DelayedIntField(new GUIContent("Local WS Server Port", "Port used by the local WebSocket server for live-link connection."), project.Settings.LocalWSServerPort);
                project.Settings.LocalHTTPServerPort = EditorGUILayout.DelayedIntField(new GUIContent("Local HTTP Server Port", "Port used by the local HTTP server for hosting project."), project.Settings.LocalHTTPServerPort);

                GUI.enabled = (workspace.BuildConfiguration == TinyBuildConfiguration.Release);
                {
                    project.Settings.MinifyJavaScript = EditorGUILayout.Toggle(new GUIContent("Minify JavaScript", "Minify JavaScript game code. Release builds only."), project.Settings.MinifyJavaScript);
                    project.Settings.SingleFileHtml = EditorGUILayout.Toggle(new GUIContent("Single File Output", "Embed JavaScript code in index.html. Release builds only."), project.Settings.SingleFileHtml);
                }
                GUI.enabled = (workspace.BuildConfiguration != TinyBuildConfiguration.Release);
                {
                    project.Settings.LinkToSource = EditorGUILayout.Toggle(new GUIContent("Link To Source", "Link code files directly from your Unity project - no export required. Development builds only."), project.Settings.LinkToSource);
                    project.Settings.IncludeWSClient = EditorGUILayout.Toggle(new GUIContent("Include WebSocket Client", "Include WebSocket client code in build. Required for live-link connection. Development builds only."), project.Settings.IncludeWSClient);
                }
                GUI.enabled = true;

                project.Settings.RunBabel = EditorGUILayout.Toggle(new GUIContent("Transpile to ECMAScript 5", "Transpile user code to ECMAScript 5 for greater compatibility across browsers."), project.Settings.RunBabel);

                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                var content = new GUIContent("Build and Run");

                var rect = GUILayoutUtility.GetRect(content, TinyStyles.AddComponentStyle);
                if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, TinyStyles.AddComponentStyle))
                {
                   TinyBuildPipeline.BuildAndLaunch();
                   GUIUtility.ExitGUI();
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();


                --EditorGUI.indentLevel;
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private static void TextureSettingsField(TinyTextureSettings textureSettings)
        {
            textureSettings.FormatType = (TextureFormatType) EditorGUILayout.EnumPopup("Default Texture Format", textureSettings.FormatType);

            switch (textureSettings.FormatType)
            {
                case TextureFormatType.JPG:
                    textureSettings.JpgCompressionQuality = EditorGUILayout.IntSlider("Compression Quality", textureSettings.JpgCompressionQuality, 1, 100);
                    break;
                case TextureFormatType.WebP:
                    textureSettings.WebPCompressionQuality = EditorGUILayout.IntSlider("Compression Quality", textureSettings.WebPCompressionQuality, 1, 100);
                    break;
                case TextureFormatType.Source:
                    break;
                case TextureFormatType.PNG:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion

        #region Module Settings
        private static void DrawModuleSettings(TinyModule module)
        {
            EditorGUIUtility.labelWidth = 225;
            EditorGUILayout.BeginVertical();
            try
            {
                GUILayout.Label("Module Settings", TinyStyles.Header2);
                EditorGUILayout.Space();
                ++EditorGUI.indentLevel;
                DrawCodeSettings(module);
                --EditorGUI.indentLevel;
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }
        #endregion

        #region Common
        private static void DrawCodeSettings(TinyModule module)
        {
            var scriptRoot = AssetDatabase.GUIDToAssetPath(module.ScriptRootDirectory);
            DefaultAsset folder = null;
            if (!string.IsNullOrEmpty(scriptRoot))
            {
                folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(scriptRoot);
            }

            var version = module.Version;
            EditorGUI.BeginChangeCheck();
            folder = TinyGUILayout.FolderField("Script Root Directory", folder);
            if (null != folder)
            {
                module.ScriptRootDirectory = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(folder));
            }

            module.Namespace = NamespaceField("Default Namespace", module.Namespace);
            if (EditorGUI.EndChangeCheck() && version != module.Version)
            {
                module.Registry.Context.GetManager<TinyScriptingManager>().Refresh();
            }
            EditorGUILayout.LabelField("Description");
            ++EditorGUI.indentLevel;
            module.Documentation.Summary = DescriptionField(module.Documentation.Summary);
            --EditorGUI.indentLevel;

        }

        private static string NamespaceField(string label, string @namespace)
        {
            EditorGUI.BeginChangeCheck();

            var newNamespace = EditorGUILayout.DelayedTextField(label, @namespace);

            if (!EditorGUI.EndChangeCheck())
            {
                return @namespace;
            }

            if (string.IsNullOrEmpty(newNamespace))
            {
                Debug.LogWarning("Namespace can not be empty.");
                return @namespace;
            }

            if (!TinyUtility.IsValidNamespaceName(newNamespace))
            {
                Debug.LogWarning($"{newNamespace} is not a valid namespace. Must be a valid code name");
                return @namespace;
            }

            return newNamespace;
        }

        private static string DescriptionField(string summary)
        {
            var wrapStyle = new GUIStyle(EditorStyles.textArea);
            wrapStyle.wordWrap = true;

            summary = EditorGUILayout.TextArea(summary, wrapStyle);
            return summary;
        }
        #endregion

    }
}
