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
            : base("Project/Tiny"
#if UNITY_2019_1_OR_NEWER
                , SettingsScope.Project
#endif
            )
        {
        }

        [SettingsProvider]
        [UsedImplicitly]
        public static SettingsProvider Provider()
        {
            return new TinySettingsProvider() {label = "Tiny"};
        }

        #region Unity

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.BeginHorizontal();
            try
            {
                GUILayout.Space(5.0f);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (TinyEditorApplication.ContextType == EditorContextType.Project)
                    {
                        var project = TinyEditorApplication.Project;
                        label = project.Name;
                        DrawProjectSettings(project);
                    }
                    else if (TinyEditorApplication.ContextType == EditorContextType.Module)
                    {
                        var module = TinyEditorApplication.Module;
                        label = module.Name;
                        DrawModuleSettings(module);
                    }
                    else
                    {
                        label = "Tiny Settings";
                        EditorGUILayout.LabelField("No Tiny context is currently opened.");
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        #endregion

        #region Project Settings

        private static TinyContext s_Context;

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
            var workspace = TinyEditorApplication.EditorContext.Workspace;
            var isRelease = workspace.BuildConfiguration == TinyBuildConfiguration.Release;

            EditorGUILayout.BeginVertical();

            try
            {
                EditorGUILayout.Space();
                GUILayout.Label("Project Settings", TinyStyles.SettingsSection);
                EditorGUILayout.Space();

                project.Settings.CanvasAutoResize =
                    EditorGUILayout.Toggle("Auto-Resize Canvas", project.Settings.CanvasAutoResize);
                if (!project.Settings.CanvasAutoResize)
                {
                    ++EditorGUI.indentLevel;
                    project.Settings.CanvasWidth =
                        EditorGUILayout.DelayedIntField("Width", project.Settings.CanvasWidth);
                    project.Settings.CanvasHeight =
                        EditorGUILayout.DelayedIntField("Height", project.Settings.CanvasHeight);
                    --EditorGUI.indentLevel;
                }

                project.Settings.RenderMode =
                    (RenderingMode) EditorGUILayout.EnumPopup("Render Mode", project.Settings.RenderMode);
                project.Settings.EmbedAssets = EditorGUILayout.Toggle(
                    new GUIContent("Embedded Assets",
                        "Assets are embedded as base64 (this will increase asset size by approx 34%)."),
                    project.Settings.EmbedAssets);

                TextureSettingsField(project.Settings.DefaultTextureSettings);

                DrawCodeSettings(module);

                DrawLiveLinkSettings(project, workspace, isRelease);
                DrawBuildSettings(project, workspace, isRelease);
                DrawPlayableAdSettings(project, workspace, isRelease);

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
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private static void TextureSettingsField(TinyTextureSettings textureSettings)
        {
            textureSettings.FormatType =
                (TextureFormatType) EditorGUILayout.EnumPopup("Default Texture Format", textureSettings.FormatType);

            switch (textureSettings.FormatType)
            {
                case TextureFormatType.JPG:
                    textureSettings.JpgCompressionQuality = EditorGUILayout.IntSlider("Compression Quality",
                        textureSettings.JpgCompressionQuality, 1, 100);
                    break;
                case TextureFormatType.WebP:
                    textureSettings.WebPCompressionQuality = EditorGUILayout.IntSlider("Compression Quality",
                        textureSettings.WebPCompressionQuality, 1, 100);
                    break;
                case TextureFormatType.Source:
                    break;
                case TextureFormatType.PNG:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void DrawBuildSettings(TinyProject project, TinyEditorWorkspace workspace, bool isRelease)
        {
            EditorGUILayout.Space();
            GUILayout.Label("Build Settings", TinyStyles.SettingsSection);
            EditorGUILayout.Space();

            workspace.BuildConfiguration =
                (TinyBuildConfiguration) EditorGUILayout.EnumPopup("Build Configuration", workspace.BuildConfiguration);
            workspace.Platform = (TinyPlatform) EditorGUILayout.EnumPopup("Build Target", workspace.Platform);

            project.Settings.IncludeWebPDecompressor = EditorGUILayout.Toggle(
                new GUIContent("Include WebP Decompressor",
                    "Include WebP decompressor code in build. Required for browsers that does not support WebP image format."),
                project.Settings.IncludeWebPDecompressor);
            project.Settings.RunBabel = EditorGUILayout.Toggle(
                new GUIContent("Transpile to ECMAScript 5",
                    "Transpile user code to ECMAScript 5 for greater compatibility across browsers."),
                project.Settings.RunBabel);
            project.Settings.MemorySize = EditorGUILayout.DelayedIntField(
                new GUIContent("Memory Size", "Total memory size pre-allocated for the entire project."),
                project.Settings.MemorySize);
            project.Settings.SymbolsInReleaseBuild = EditorGUILayout.Toggle(
                new GUIContent("Symbols In Release Build",
                    "Include full symbols in release build (this will increase runtime size, only enable for profiling and testing.)"),
                project.Settings.SymbolsInReleaseBuild);

            using (new EditorGUI.DisabledScope(!isRelease || workspace.Platform == TinyPlatform.PlayableAd))
            {
                var single = new GUIContent("Single File Output",
                    "Embed JavaScript code in index.html. Release builds only.");
                
                if (workspace.Platform != TinyPlatform.PlayableAd)
                {
                    if (isRelease)
                    {
                        project.Settings.SingleFileHtml = EditorGUILayout.Toggle(single, project.Settings.SingleFileHtml);
                    }
                    else
                    {
                        EditorGUILayout.Toggle(single, false);
                    }
                }
                else
                {
                    EditorGUILayout.Toggle(single, false);
                }
            }
            
            using (new EditorGUI.DisabledScope(!isRelease))
            {
                var minify = new GUIContent("Minify JavaScript", "Minify JavaScript game code. Release builds only.");

                if (isRelease)
                {
                    project.Settings.MinifyJavaScript = EditorGUILayout.Toggle(minify, project.Settings.MinifyJavaScript);
                }
                else
                {
                    EditorGUILayout.Toggle(minify, false);
                }
            }

            using (new EditorGUI.DisabledScope(isRelease || workspace.Platform == TinyPlatform.PlayableAd))
            {
                var link = new GUIContent("Link To Source",
                    "Link code files directly from your Unity project - no export required. Debug or Development builds only.");
                
                if (workspace.Platform != TinyPlatform.PlayableAd)
                {
                    if (isRelease)
                    {
                        project.Settings.LinkToSource = EditorGUILayout.Toggle(link, project.Settings.LinkToSource);
                    }
                    else
                    {
                        EditorGUILayout.Toggle(link, false);
                    }
                }
                else
                {
                    EditorGUILayout.Toggle(link, false);
                }
            }
        }

        private static void DrawLiveLinkSettings(TinyProject project, TinyEditorWorkspace workspace, bool isRelease)
        {
            EditorGUILayout.Space();
            GUILayout.Label("LiveLink Settings", TinyStyles.SettingsSection);
            EditorGUILayout.Space();

            project.Settings.LocalWSServerPort = EditorGUILayout.DelayedIntField(
                new GUIContent("Local WS Server Port",
                    "Port used by the local WebSocket server for live-link connection."),
                project.Settings.LocalWSServerPort);
            project.Settings.LocalHTTPServerPort = EditorGUILayout.DelayedIntField(
                new GUIContent("Local HTTP Server Port", "Port used by the local HTTP server for hosting project."),
                project.Settings.LocalHTTPServerPort);

            using (new EditorGUI.DisabledScope(isRelease))
            {
                var profiler = new GUIContent("Auto-Connect Profiler",
                    "Automatically connect to the Unity Profiler at launch. Debug or Development builds only.");
                if (!isRelease)
                {
                    workspace.AutoConnectProfiler = EditorGUILayout.Toggle(profiler, workspace.AutoConnectProfiler);
                }
                else
                {
                    EditorGUILayout.Toggle(profiler, false);
                }
            }
        }

        private static void DrawPlayableAdSettings(TinyProject project, TinyEditorWorkspace workspace, bool isRelease)
        {
            if (workspace.Platform == TinyPlatform.PlayableAd)
            {
                --EditorGUI.indentLevel;
                EditorGUILayout.Space();
                GUILayout.Label("Store URLs", TinyStyles.SettingsSection);
                EditorGUILayout.Space();
                ++EditorGUI.indentLevel;

                project.Settings.GooglePlayStoreUrl = EditorGUILayout.DelayedTextField("Google Play Store Url", project.Settings.GooglePlayStoreUrl);
                project.Settings.AppStoreUrl = EditorGUILayout.DelayedTextField("App Store Url", project.Settings.AppStoreUrl);
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
                GUILayout.Label("Module Settings", TinyStyles.SettingsSection);
                EditorGUILayout.Space();
                DrawCodeSettings(module);
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
            var version = module.Version;
            EditorGUI.BeginChangeCheck();

            module.Namespace = NamespaceField("Default Namespace", module.Namespace);
            if (EditorGUI.EndChangeCheck() && version != module.Version)
            {
                module.Registry.Context.GetManager<IScriptingManager>().Refresh();
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