

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Tiny
{
    /// <summary>
    /// Helper class to draw a generic toolbar in a declarative way
    /// </summary>
    internal class TinyToolbar : IDrawable
    {
        public enum Alignment
        {
            Left,
            Center,
            Right
        }

        internal interface IToolbarItem : IDrawable
        {
            Alignment Alignment { get; }
        }

        internal class Popup : IToolbarItem
        {
            private Rect m_ButtonRect;

            public Alignment Alignment { get; set; }
            public string Name { get; set; }
            public PopupWindowContent Content { get; set; }

            public bool DrawLayout()
            {
                if (GUILayout.Button(Name, EditorStyles.toolbarPopup, GUILayout.Width(100)))
                {
                    m_ButtonRect.x -= Mathf.Max(0, Content.GetWindowSize().x - m_ButtonRect.width);
                    PopupWindow.Show(m_ButtonRect, Content);
                }

                if (Event.current.type == EventType.Repaint)
                {
                    m_ButtonRect = GUILayoutUtility.GetLastRect();
                }

                return false;
            }
        }

        internal class FilterPopup : PopupWindowContent
        {
            private const float KMargin = 5;

            internal class Item
            {
                public const float KHeight = 18;

                public string Name { get; set; }
                public bool State { get; set; }
                public Action<bool> Changed { get; set; }
            }

            public Item[] Items { get; set; }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(150, Items.Length * Item.KHeight + KMargin + KMargin);
            }

            public override void OnGUI(Rect rect)
            {
                GUILayout.Space(KMargin);
                
                var @event = Event.current;
                
                if (@event.type == EventType.KeyDown && @event.keyCode == KeyCode.Escape)
                {
                    editorWindow.Close();
                    GUIUtility.ExitGUI();
                }

                foreach (var item in Items)
                {
                    EditorGUI.BeginChangeCheck();

                    item.State = EditorGUILayout.ToggleLeft(item.Name, item.State);

                    if (EditorGUI.EndChangeCheck())
                    {
                        item.Changed(item.State);
                    }
                }

                GUILayout.Space(KMargin);
            }

            public override void OnOpen()
            {
            }

            public override void OnClose()
            {
            }
        }

        internal class Search : IToolbarItem
        {
            private readonly SearchField m_SearchField = new SearchField();

            public Alignment Alignment { get; set; }
            public string SearchString { get; set; }
            public Action<string> Changed { get; set; }

            public bool DrawLayout()
            {
                EditorGUI.BeginChangeCheck();

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(5);
                
                    SearchString = m_SearchField.OnToolbarGUI(SearchString);
                
                    GUILayout.Space(4);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    Changed.Invoke(SearchString);
                }

                return false;
            }
        }

        internal class DropDown : IToolbarItem
        {
            public Alignment Alignment { get; set; }
            public string[] Items;
            public int SelectedIndex;
            public Action<int> Action { get; set; }

            public bool DrawLayout()
            {
                var index = EditorGUILayout.Popup(SelectedIndex, Items, EditorStyles.toolbarDropDown, GUILayout.Width(100));

                if (index == SelectedIndex)
                {
                    return false;
                }

                SelectedIndex = index;
                Action.Invoke(SelectedIndex);

                return false;
            }
        }

        internal class Menu : IToolbarItem
        {
            internal class Item
            {
                public string Path { get; set; }
                public GenericMenu.MenuFunction Action { get; set; }
                public Func<bool> Validation { get; set; }
            }

            public Alignment Alignment { get; set; }
            public string Name { get; set; }
            public Item[] Items { get; set; }

            public bool DrawLayout()
            {
                using (var scope = new EditorGUILayout.HorizontalScope(GUILayout.Width(75)))
                {
                    if (!GUILayout.Button(Name, EditorStyles.toolbarDropDown))
                    {
                        return false;
                    }

                    var dropDown = new GenericMenu();

                    foreach (var item in Items)
                    {
                        var validation = null == item.Validation || item.Validation.Invoke();
                        
                        if (validation)
                        {
                            dropDown.AddItem(new GUIContent(item.Path), false, item.Action);
                        }
                        else
                        {
                            dropDown.AddDisabledItem(new GUIContent(item.Path));
                        }
                    }

                    dropDown.DropDown(scope.rect);
                }

                return false;
            }
        }

        internal class Button : IToolbarItem
        {
            public Alignment Alignment { get; set; }
            public Texture2D Icon { get; set; }
            public string Tooltip { get; set; }
            public GenericMenu.MenuFunction Action { get; set; }
            public Func<bool> Validation { get; set; }

            public bool DrawLayout()
            {
                using (new TinyGUIEnabledScope(null == Validation || Validation.Invoke()))
                {
                    if (GUILayout.Button(new GUIContent(Icon, Tooltip),
                        EditorStyles.toolbarButton, GUILayout.Width(45)))
                    {
                        Action.Invoke();
                    }
                }

                return false;
            }
        }

        internal class Toggle : IToolbarItem
        {
            public Alignment Alignment { get; set; }
            public Texture2D Icon { get; set; }
            public string Tooltip { get; set; }
            public bool State { get; set; }
            public Action<bool> Action { get; set; }

            public bool DrawLayout()
            {
                EditorGUI.BeginChangeCheck();

                State = GUILayout.Toggle(State,
                    new GUIContent(Icon, Tooltip),
                    EditorStyles.toolbarButton,
                    GUILayout.Width(45));

                if (EditorGUI.EndChangeCheck())
                {
                    Action.Invoke(State);
                }

                return false;
            }
        }

        private readonly IList<IToolbarItem> m_Items = new List<IToolbarItem>();
        public IList<IToolbarItem> Items => m_Items;

        public TinyToolbar Add(IToolbarItem item)
        {
            m_Items.Add(item);
            return this;
        }

        public bool DrawLayout()
        {
            GUILayout.Space(1);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                DrawLayout(Alignment.Left);
                DrawLayout(Alignment.Center, true);
                DrawLayout(Alignment.Right);
            }

            return false;
        }

        private void DrawLayout(Alignment alignment, bool fill = false)
        {
            var draw = false;

            foreach (var item in m_Items)
            {
                if (item.Alignment == alignment)
                {
                    draw = true;
                    item.DrawLayout();
                }
            }

            if (!draw && fill)
            {
                GUILayout.FlexibleSpace();
            }
        }
    }
}

