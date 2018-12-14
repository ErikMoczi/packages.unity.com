
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal class SelectorSection
    {
        public string Header;
        public readonly List<SelectorOption> Options = new List<SelectorOption>();
    }

    internal class SelectorOption
    {
        public string Name;
        public Func<bool> GetState;
        public Action<bool> SetState;
        public bool Enabled;

        public SelectorOption(string name, Func<bool> getState, Action<bool> setState, bool enabled = false)
        {
            Name = name;
            GetState = getState ?? (() => false);
            SetState = setState ?? (b => {});
            Enabled = enabled;
        }
    }

    internal class TinyMultiSelectorMenu : PopupWindowContent
    {
        private static class Styles
        {
            private static GUIStyle s_MenuItem;
            private static GUIStyle s_Separator;

            public static GUIStyle MenuItem => s_MenuItem ?? (s_MenuItem = "MenuItem");
            public static GUIStyle Separator => s_Separator ?? (s_Separator = "sv_iconselector_sep");
        }

        private const float k_WindowWidth = 205;
        private const float k_SeparatorHeight = 3;
        private const float k_HeaderHorizontalPadding = 5f;
        private const float k_HeaderVerticalPadding = 1f;

        private List<SelectorSection> m_Sections;

        private float WindowHeight
        {
            get
            {
                var headers = m_Sections.Count;
                var modes = m_Sections.SelectMany(b => b.Options).Count();

                var separators = headers - 2;
                return ((headers + modes) * EditorGUIBridge.SingleLineHeight) + (k_SeparatorHeight * separators) + EditorGUIBridge.SingleLineHeight;
            }
        }

        public TinyMultiSelectorMenu(List<SelectorSection> sections)
        {
            m_Sections = sections;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(k_WindowWidth, WindowHeight);
        }

        public override void OnGUI(Rect rect)
        {
            // We do not use the layout event
            if (Event.current.type == EventType.Layout)
                return;

            Draw(editorWindow, rect.width);

            // Use mouse move so we get hover state correctly in the menu item rows
            if (Event.current.type == EventType.MouseMove)
                Event.current.Use();

            // Escape closes the window
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        private static void DrawSeparator(ref Rect rect)
        {
            var labelRect = rect;
            labelRect.x += k_HeaderHorizontalPadding;
            labelRect.y += k_SeparatorHeight;
            labelRect.width -= k_HeaderHorizontalPadding * 2;
            labelRect.height = k_SeparatorHeight;

            GUI.Label(labelRect, GUIContent.none, Styles.Separator);
            rect.y += k_SeparatorHeight;
        }

        private static void DrawHeader(ref Rect rect, GUIContent label)
        {
            var labelRect = rect;
            labelRect.y += k_HeaderVerticalPadding;
            labelRect.x += k_HeaderHorizontalPadding;
            labelRect.width = EditorStyles.miniLabel.CalcSize(label).x;
            labelRect.height = EditorStyles.miniLabel.CalcSize(label).y;
            GUI.Label(labelRect, label, EditorStyles.miniLabel);
            rect.y += EditorGUIBridge.SingleLineHeight;
        }

        private void Draw(EditorWindow caller, float listElementWidth)
        {
            var drawPos = new Rect(0, 0, listElementWidth, EditorGUIBridge.SingleLineHeight);

            foreach (var section in m_Sections)
            {
                DrawHeader(ref drawPos, new GUIContent(section.Header));
                DrawSeparator(ref drawPos);
                foreach (var option in section.Options)
                {
                    DrawOption(caller, ref drawPos, option);
                }
            }
        }

        private static void DrawOption(EditorWindow caller, ref Rect rect, SelectorOption option)
        {
            using (new EditorGUI.DisabledScope(!option.Enabled))
            {
                EditorGUI.BeginChangeCheck();

                var newBool = GUI.Toggle(rect, option.GetState(), new GUIContent(option.Name), Styles.MenuItem);
                if (EditorGUI.EndChangeCheck())
                {
                    option.SetState(newBool);
                    caller.Repaint();
                    GUIUtility.ExitGUI();
                }
            }

            rect.y += EditorGUIBridge.SingleLineHeight;
        }
    }
}
