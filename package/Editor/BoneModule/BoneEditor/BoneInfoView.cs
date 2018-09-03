using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation
{
    public interface IBoneInfoView
    {
        void SetRect(Rect rect);
        void SelectionChanged();
        bool HandleName(ref string name);
        bool HandleNextSelection();
    }

    internal class BoneInfoView : IBoneInfoView
    {
        private class Styles
        {
            public static readonly GUIContent windowsTitle = new GUIContent("Information");
            public static readonly GUIContent nameLabel = new GUIContent("Name");

            public static readonly string nameControl = "BoneName";
        }

        private Rect m_NextDrawArea;
        private bool m_RegainFocus;

        public void SetRect(Rect rect)
        {
            GUILayout.BeginArea(rect, Styles.windowsTitle, GUI.skin.window);
            GUILayout.EndArea();

            m_NextDrawArea = rect;
            m_NextDrawArea.xMin += 5.0f; // margin
            m_NextDrawArea.width -= 5.0f;
            m_NextDrawArea.yMin += 20.0f; // title height
        }

        public bool HandleName(ref string name)
        {
            GUI.SetNextControlName(Styles.nameControl);
            if (m_RegainFocus && Event.current.type == EventType.Repaint)
            {
                // Focus and select all text in the text field.
                EditorGUIUtility.editingTextField = true;
                GUI.FocusControl(Styles.nameControl);
                m_RegainFocus = false;
            }

            EditorGUIUtility.labelWidth = 40f;
            m_NextDrawArea.height = EditorStyles.textField.CalcSize(Styles.nameLabel).y;

            EditorGUI.BeginChangeCheck();
            var newName = EditorGUI.TextField(m_NextDrawArea, Styles.nameLabel, name);
            EditorGUIUtility.labelWidth = 0;
            if (EditorGUI.EndChangeCheck())
            {
                name = newName;
                return true;
            }

            return false;
        }

        public bool HandleNextSelection()
        {
            var currentEvent = Event.current;
            var wantFocus = (GUI.GetNameOfFocusedControl() == Styles.nameControl
                             && currentEvent.type == EventType.KeyDown
                             && currentEvent.keyCode == KeyCode.Tab);
            if (wantFocus)
            {
                m_RegainFocus = wantFocus;
                currentEvent.Use();
            }
            return wantFocus;
        }

        public void SelectionChanged()
        {
            // Relieve the keyboard control.
            // This prevent text box not changing selected text, and allow key press (delete) on bone hierarchy
            GUIUtility.keyboardControl = -1;
        }
    }
}
