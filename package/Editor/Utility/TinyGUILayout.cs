


using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class TinyGUILayout
    {
        public static void Separator(Color color, float height)
        {
            var rect = GUILayoutUtility.GetRect(0, height);
            TinyGUI.BackgroundColor(rect, color);
        }

        public static DefaultAsset FolderField(string label, DefaultAsset folder)
        {
            var rect = GUILayoutUtility.GetRect(new GUIContent(label), EditorStyles.objectField);
            return TinyGUI.FolderField(rect, label, folder);
        }
        
        internal class Splitter
        {
            private float k_SplitterSize = 2.0f;
            
            private Action m_LeftGUI;
            private Action m_RightGUI;
            private float m_Split;
             bool resize = false;

            public float Split
            {
                get => m_Split;
                set => m_Split = value;
            }

            public Vector2 MinMax { get; set; }= new Vector2(0.25f, 0.75f);

            private Rect LastRect;
            
            public Splitter(float split, Action leftGui, Action rightGui)
            {
                m_Split = split;
                m_LeftGUI = leftGui;
                m_RightGUI = rightGui;
            }

            public void OnGUI(Rect rect)
            {
                LastRect = rect;
                var size = rect.width - k_SplitterSize;
                var left = size * Split;
                var right = Mathf.Min(size - left, rect.width - left - k_SplitterSize);

                EditorGUILayout.BeginHorizontal();
                try
                {
                    var leftRect = EditorGUILayout.BeginVertical(GUILayout.Width(left));
                    try
                    {
                        m_LeftGUI();
                    }
                    finally
                    {
                        EditorGUILayout.EndVertical();
                    }


                    var splitRect = leftRect;
                    splitRect.x += leftRect.width;
                    splitRect.width = k_SplitterSize;
                    ResizeScrollView(splitRect);

                    EditorGUILayout.BeginVertical(GUILayout.Width(right));
                    try
                    {
                        m_RightGUI();
                    }
                    finally
                    {
                        EditorGUILayout.EndVertical();
                    }
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            private void ResizeScrollView(Rect splitterRect)
            {
                GUILayout.Space(k_SplitterSize);
                EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
                GUI.color = Color.black;
                GUI.DrawTexture(splitterRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
                GUI.color = Color.white;
                if(Event.current.rawType == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
                {
                    resize = true;
                }
                if(resize)
                {
                    Split = (Event.current.mousePosition.x + k_SplitterSize) / LastRect.width;
                    Split = Mathf.Clamp(Split, MinMax.x, MinMax.y);
                    GUIViewBridge.RepaintCurrentView();
                }

                if (Event.current.rawType == EventType.MouseUp)
                {
                    resize = false;
                }
            }
        }
    }
}

