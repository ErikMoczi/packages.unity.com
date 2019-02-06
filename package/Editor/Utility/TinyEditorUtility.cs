

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    internal static class TinyEditorUtility
    {
        private static readonly MethodInfo s_SetBoldDefaultFontMethod;
        private static readonly Color s_OverrideMarginColor = new Color(1f / 255f, 153f / 255f, 235f / 255f, 1f);

        static TinyEditorUtility()
        {
            s_SetBoldDefaultFontMethod = typeof(EditorGUIUtility).GetMethod("SetBoldDefaultFont", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (null == s_SetBoldDefaultFontMethod)
            {
                Debug.Log($"{TinyConstants.ApplicationName}: Could not find the EditorGUIUtility.SetBoldDefaultFont method.");
            }
        }

        public delegate T[] ObjectSelector<out T>(Object[] objects);

        public static T[] DoDropField<T>(Rect position, int id, ObjectSelector<T> selector, GUIStyle style)
        {
            var evt = Event.current;
            var eventType = evt.type;

            if (!GUI.enabled && (Event.current.rawType == EventType.MouseDown))
            {
                eventType = Event.current.rawType;
            }

            switch (eventType)
            {
                case EventType.DragExited:
                    if (GUI.enabled)
                    {
                        HandleUtility.Repaint();
                    }
                    break;

                case EventType.DragUpdated:
                case EventType.DragPerform:
                {
                    if (position.Contains(Event.current.mousePosition) && GUI.enabled)
                    {
                        var validatedObject = selector(DragAndDrop.objectReferences);

                        if (validatedObject.Length > 0)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                            if (eventType == EventType.DragPerform)
                            {
                                GUI.changed = true;
                                DragAndDrop.AcceptDrag();
                                DragAndDrop.activeControlID = 0;
                                Event.current.Use();
                                return validatedObject;
                            }

                            DragAndDrop.activeControlID = id;
                        }
                    }
                }
                    break;

                case EventType.Repaint:
                {
                    style.Draw(position, GUIContent.none, id, DragAndDrop.activeControlID == id);
                }
                    break;
            }

            return null;
        }

        public static void RepaintAllWindows()
        {
            TinyInspector.RepaintAll();
            TinyHierarchyWindow.RepaintAll();
        }

        public static void SetEditorBoldDefault(bool bold)
        {
            if (null != s_SetBoldDefaultFontMethod)
            {
                s_SetBoldDefaultFontMethod.Invoke(null, new object[] { bold });
            }
        }

        internal static void DrawOverrideBackground(Rect position)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            var oldColor = GUI.backgroundColor;
            var oldEnabled = GUI.enabled;
            GUI.enabled = true;

            GUI.backgroundColor = s_OverrideMarginColor;
            position.x = 0;
            position.width = 3;
            
            EditorStylesBridge.overrideMargin.Draw(position, false, false, false, false);

            GUI.enabled = oldEnabled;
            GUI.backgroundColor = oldColor;
        }

        internal class ProgressBarScope : IDisposable
        {
            private string m_Title;
            
            private static int s_Depth;

            private static string s_LastTitle, s_LastInfo;
            private static float s_LastProgress;

            public ProgressBarScope()
            {
                ++s_Depth;
            }
            
            public ProgressBarScope(string title, string info = null, float progress = float.MinValue)
            {
                ++s_Depth;
                m_Title = title;
                Display(title, info, progress);
            }
            
            private static void Display(string title, string info, float progress)
            {
                title = title != null ? title : s_LastTitle != null ? s_LastTitle : string.Empty;
                info = info != null ? info : s_LastInfo != null ? s_LastInfo : string.Empty;
                progress = progress != float.MinValue ? progress : s_LastProgress != float.MinValue ? s_LastProgress : 0f;

                EditorUtility.DisplayProgressBar(title, info, progress);
                
                s_LastTitle = title;
                s_LastInfo = info;
                s_LastProgress = progress;
            }

            public void Update(string info, float progress = float.MinValue)
            {
                Display(m_Title, info, progress);
            }
            
            public void Update(string title, string info, float progress = float.MinValue)
            {
                m_Title = title;
                Display(m_Title, info, progress);
            }

            public static void Restore()
            {
                if (s_Depth > 0 && s_LastTitle != null)
                {
                    Display(s_LastTitle, s_LastInfo, s_LastProgress);
                }
            }
            
            public void Dispose()
            {
                if (--s_Depth == 0)
                {
                    EditorUtility.ClearProgressBar();
                    s_LastTitle = s_LastInfo = null;
                    s_LastProgress = 0.0f;
                }
            }
        }
    }
}

