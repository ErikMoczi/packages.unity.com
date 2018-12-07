

using System;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.Experimental.U2D;
using UnityEditorInternal;
using Unity.Experimental.EditorMode;

namespace Unity.Tiny
{
    internal static class TinyEditorBridge
    {
        internal static Type ProjectWindowType { get; } = typeof(UnityEditor.ProjectBrowser);
        internal static Type ConsoleWindowType { get; } = typeof(UnityEditor.ConsoleWindow);
        internal static Type HierarchyWindowType { get; } = typeof(UnityEditor.SceneHierarchyWindow);
        internal static Type SceneWindowType { get; } = typeof(UnityEditor.SceneView);
        internal static Type InspectorWindowType { get; } = typeof(UnityEditor.InspectorWindow);
        internal static Type GameWindowType { get; } = typeof(UnityEditor.GameView);
        internal static Type ProfilerWindowType { get; } = typeof(UnityEditor.ProfilerWindow);

        #region Editor Utils

        internal static void ShowProfiler(bool startRecording = true)
        {
            var profilerWindow = EditorWindow.GetWindow<UnityEditor.ProfilerWindow>(GameWindowType, SceneWindowType);
            profilerWindow.Show();
            if (startRecording)
            {
                if (profilerWindow.GetClearOnPlay())
                {
                    ProfilerDriver.ClearAllFrames();
                }

                ProfilerDriver.enabled = true;
            }
        }

        internal static void ClearConsole()
        {
            UnityEditor.LogEntries.Clear();
        }

        internal static void RepaintGameViews()
        {
            foreach (var view in Resources.FindObjectsOfTypeAll<GameView>())
            {
                if (view && null != view)
                {
                    view.Repaint();
                }
            }
        }

        #endregion

        #region SpriteAtlas

        internal static void PackAllSpriteAtlases()
        {
            SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget, false);
        }

        internal static Sprite[] GetAtlasPackedSprites(SpriteAtlas spriteAtlas)
        {
            return spriteAtlas.GetPackedSprites();
        }

        internal static SpriteAtlas GetSpriteActiveAtlas(Sprite sprite)
        {
            return sprite.GetActiveAtlas();
        }

        internal static Texture2D GetSpriteActiveAtlasTexture(Sprite sprite)
        {
            var atlasTexture = sprite.GetActiveAtlasTexture();
            if (null == atlasTexture || !atlasTexture)
            {
                return null;
            }
            return atlasTexture.GetInstanceID() == sprite.texture.GetInstanceID() ? null : atlasTexture;
        }

        internal static Rect GetSpriteActiveAtlasTextureRect(Sprite sprite)
        {
            return sprite.GetActiveAtlasTextureRect();
        }

        internal static Vector2 GetSpriteActiveAtlasTextureRectOffset(Sprite sprite)
        {
            return sprite.GetActiveAtlasTextureRectOffset();
        }

        internal static Texture2D GetSpriteActiveAtlasAlphaTexture(Sprite sprite)
        {
            return sprite.GetActiveAtlasAlphaTexture();
        }

        #endregion

        internal static void AddInbetweenKeyToCurve(AnimationCurve curve, float time)
        {
            AnimationUtility.AddInbetweenKey(curve, time);
        }

        #region Editor Modes

        internal static class CoreWindowTypes
        {
            public static Type Inspector { get; } = typeof(InspectorWindow);
            public static Type Hierarchy { get; } = typeof(SceneHierarchyWindow);
            public static Type SceneView { get; } = typeof(UnityEditor.SceneView);
            public static Type GameView { get; } = typeof(GameView);
            public static Type ProjectBrowser { get; } = typeof(ProjectBrowser);
            public static Type ProfilerWindow { get; } = typeof(ProfilerWindow);
            public static Type ConsoleWindow { get; } = typeof(ConsoleWindow);
            public static Type Animation { get; } = typeof(AnimationWindow);
        }

        internal static Type[] UnsupportedWindows = new Type[]
        {
            typeof(AudioMixerWindow),
            typeof(LookDevView),
            typeof(LightingWindow),
            typeof(LightingExplorerWindow),
            typeof(OcclusionCullingWindow),
            typeof(NavMeshEditorWindow)
        };

        internal static string[] UnsupportedWindowsByName = new[]
        {
            "UnityEditor.Graphs.AnimatorControllerTool",
            "UnityEditor.Graphs.ParameterControllerEditor",
            "UnityEditor.Timeline.TimelineWindow",
            "UnityEditor.XR.WSA.HolographicEmulationWindow"
        };

        internal static void RequestEnterMode<TMode>()
            where TMode : EditorModeProxy, new()
        {
            EditorModes.RequestEnterMode<EditorModeBridge<TMode>>();
        }

        internal static void RequestExitMode<TMode>()
            where TMode : EditorModeProxy, new()
        {
            EditorModes.RequestExitMode<EditorModeBridge<TMode>>();
        }

        #endregion // Editor Modes
    }
}
