using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditor.Experimental.U2D.Common
{
    internal static class InternalEditorBridge
    {
        public static EditorWindow GetCurrentInspectorWindow()
        {
            return InspectorWindow.s_CurrentInspectorWindow;
        }

        public static Vector3 GetSnapSettingMove()
        {
            return SnapSettings.move;
        }

        public static void RenderSortingLayerFields(SerializedProperty order, SerializedProperty layer)
        {
            SortingLayerEditorUtility.RenderSortingLayerFields(order, layer);
        }

        public static void RepaintImmediately(EditorWindow window)
        {
            window.RepaintImmediately();
        }

        public static ISpriteEditorDataProvider GetISpriteEditorDataProviderFromPath(string importedAsset)
        {
            return AssetImporter.GetAtPath(importedAsset) as ISpriteEditorDataProvider;
        }

        public static void GenerateOutline(Texture2D texture, Rect rect, float detail, byte alphaTolerance, bool holeDetection, out Vector2[][] paths)
        {
            SpriteUtility.GenerateOutline(texture, rect, detail, alphaTolerance, holeDetection, out paths);
        }

        public static bool DoesHardwareSupportsFullNPOT()
        {
            return ShaderUtil.hardwareSupportsFullNPOT;
        }

        public static Texture2D CreateTemporaryDuplicate(Texture2D tex, int width, int height)
        {
            return UnityEditor.SpriteUtility.CreateTemporaryDuplicate(tex, width, height);
        }

        public static void ShowSpriteEditorWindow()
        {
            var window = EditorWindow.GetWindow<SpriteEditorWindow>();
            window.Show();
        }

        public static void ApplyWireMaterial()
        {
            HandleUtility.ApplyWireMaterial();
        }

        public static void ResetSpriteEditorView(ISpriteEditor spriteEditor)
        {
            SpriteEditorWindow sew = spriteEditor as SpriteEditorWindow;
            if (sew != null)
            {
                Type t = sew.GetType();
                var zoom = t.GetField("m_Zoom", BindingFlags.Instance | BindingFlags.NonPublic);
                if (zoom != null)
                {
                    zoom.SetValue(sew, -1);
                }

                var scrollPosition = t.GetField("m_ScrollPosition", BindingFlags.Instance | BindingFlags.NonPublic);
                if (scrollPosition != null)
                {
                    scrollPosition.SetValue(sew, new Vector2());
                }
            }
        }

        public class ShortcutContext : IShortcutToolContext
        {
            public Func<bool> isActive;
            public bool active
            {
                get
                {
                    if (isActive != null)
                        return isActive();
                    return true;
                }
            }
            public object context { get; set; }
        }

        public class WrappedShortcutAttribute : ShortcutAttribute
        {
            static readonly WrappedShortcutArguments[] k_ReusableShortcutArgs = { new WrappedShortcutArguments() };
            static readonly object[] k_EmptyReusableShortcutArgs = {};

            public WrappedShortcutAttribute(string identifier, Type context = null, string defaultKeyCombination = null)
                : base(identifier, context, defaultKeyCombination)
            {}

            public override ShortcutEntry CreateShortcutEntry(MethodInfo methodInfo)
            {
                var identifier = new Identifier(methodInfo, this);

                IEnumerable<KeyCombination> defaultCombination;

                KeyCombination keyCombination;
                if (KeyCombination.TryParseMenuItemBindingString(defaultKeyCombination, out keyCombination))
                    defaultCombination = new[] { keyCombination };
                else
                    defaultCombination = Enumerable.Empty<KeyCombination>();

                var type = ShortcutType.Action;
                //var type = this is ClutchShortcutAttribute ? ShortcutType.Clutch : ShortcutType.Action;
                var methodParams = methodInfo.GetParameters();
                Action<ShortcutArguments> action;
                if (methodParams.Length == 0)
                {
                    action = shortcutArgs =>
                    {
                        methodInfo.Invoke(null, k_EmptyReusableShortcutArgs);
                    };
                }
                else
                {
                    action = shortcutArgs =>
                    {
                        k_ReusableShortcutArgs[0].context = shortcutArgs.context;
                        k_ReusableShortcutArgs[0].state = shortcutArgs.state;
                        methodInfo.Invoke(null, k_ReusableShortcutArgs);
                    };
                }

                return new ShortcutEntry(identifier, defaultCombination, action, context, type);
            }
        }

        public class WrappedShortcutArguments
        {
            public object context;
            public ShortcutState state;
        }

        public static ShortcutContext CreateShortcutContext(Func<bool> isActiveFunc)
        {
            return new ShortcutContext() { isActive = isActiveFunc, context = null };
        }

        public static void RegisterShortcutContext(ShortcutContext context)
        {
            ShortcutIntegration.instance.contextManager.RegisterToolContext(context);
        }

        public static void UnregisterShortcutContext(ShortcutContext context)
        {
            ShortcutIntegration.instance.contextManager.DeregisterToolContext(context);
        }

        public static void AddEditorApplicationProjectLoadedCallback(UnityAction callback)
        {
            EditorApplication.projectWasLoaded += callback;
        }

        public static void RemoveEditorApplicationProjectLoadedCallback(UnityAction callback)
        {
            EditorApplication.projectWasLoaded -= callback;
        }
    }
}
