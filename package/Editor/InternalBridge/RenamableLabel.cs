
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal class RenamableLabel
    {
        public delegate void RenameStartedHandler(RenamableLabel label);
        public delegate void RenameHandler(string newName, string originalName);

        private static GUIStyle EditStyle { get; } = new GUIStyle("PR TextField");
        private static GUIStyle TempStyle { get; } = new GUIStyle("PR TextField");

        public string CurrentName { get; private set; }
        public bool IsRenaming => m_Overlay.IsRenaming();
        public float Delay { get; set; } = 0.5f;
        public bool RenameOnFirstClick { get; set; } = false;
        public bool RenameOnNextUpdate { get; set; } = false;
        public event RenameStartedHandler OnRenamedStarted;
        public event RenameHandler OnRenamedEnded;
        
        private RenameOverlay m_Overlay = new RenameOverlay();

        public void EndRename(bool acceptChanges)
        {
            m_Overlay.EndRename(acceptChanges);
            OnRenamedEnded?.Invoke(m_Overlay.name, m_Overlay.originalName);
        }

        public void OnGUI(Rect rect, string label, GUIStyle style)
        {
            m_Overlay.OnEvent();
            var controlId = GUIUtility.GetControlID(FocusType.Keyboard);

            if (Event.current.type == EventType.Repaint && (!m_Overlay.IsRenaming() || m_Overlay.isWaitingForDelay))
            {
                style.Draw(rect, new GUIContent(label), controlId, controlId == GUIUtility.keyboardControl);
            }

            if (m_Overlay.IsRenaming())
            {
                m_Overlay.editFieldRect = rect;

                if (!m_Overlay.OnGUI(GetSimilarStyle(style)))
                {
                    if (m_Overlay.HasKeyboardFocus())
                    {
                        GUIUtility.keyboardControl = controlId;
                    }

                    OnRenamedEnded?.Invoke(m_Overlay.name, m_Overlay.originalName);
                    m_Overlay.Clear();
                }
            }

            if (RenameOnNextUpdate)
            {
                RenameOnNextUpdate = false;
                BeginRename(label, controlId, Delay);
            }

            if (Event.current.type == EventType.MouseDown)
            {
                if (rect.Contains(Event.current.mousePosition) && Event.current.button == 0)
                {
                    if (RenameOnFirstClick || GUIUtility.keyboardControl == controlId)
                    {
                        BeginRename(label, controlId, Delay);
                    }
                    GUIUtility.keyboardControl = controlId;
                    Event.current.Use();
                }
                else
                {
                    if (GUIUtility.keyboardControl == controlId)
                    {
                        GUIUtility.keyboardControl = 0;
                    }
                }
            }
            CurrentName = m_Overlay.IsRenaming() ? m_Overlay.name : label;
        }

        private void BeginRename(string label, int controlId, float delay)
        {
            m_Overlay.BeginRename(label, controlId, delay);
            OnRenamedStarted?.Invoke(this);
        }

        private static GUIStyle GetSimilarStyle(GUIStyle other)
        {
            if (null == other)
            {
                return EditStyle;
            }

            var temp = TempStyle;
            temp.alignment = other.alignment;
            temp.contentOffset = other.contentOffset;
            temp.margin = other.margin;
            temp.fixedHeight = other.fixedHeight;
            temp.padding = other.padding;
            temp.border = other.border;
            return EditStyle;
        }
    }
}
