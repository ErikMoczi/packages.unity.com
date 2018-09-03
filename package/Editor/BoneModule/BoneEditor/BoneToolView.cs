using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation
{
    public interface IBoneToolView
    {
        void SetRect(Rect rect);
        bool HandleCreate(bool activated, bool enabled);
        bool HandleDelete(bool enabled);
        bool HandleSplit(bool enabled);
        bool HandleMove(bool activated, bool enabled);
        bool HandleParent(bool activated, bool enabled);
        bool HandleFreeCreate(bool activated, bool enabled);
        bool HandleGlobalCancel();
        bool HandleMultipleSelection();
    }

    internal class BoneToolView : IBoneToolView
    {
        private class Styles
        {
            public static readonly GUIStyle buttonStyle = "CommandLeft";

            public static readonly GUIContent createButton = new GUIContent(Resources.Load<Texture>("NormalCreate"), "Create Bone(B)");
            public static readonly GUIContent freeCreateButton = new GUIContent(Resources.Load<Texture>("FreeCreate"), "Create Free Bone(N)");
            public static readonly GUIContent freeMoveButton = new GUIContent(Resources.Load<Texture>("FreeMove"), "Free Move(M)");
            public static readonly GUIContent parentButton = new GUIContent(Resources.Load<Texture>("Parent"), "Parent(P)");
            public static readonly GUIContent splitButton = new GUIContent(Resources.Load<Texture>("Split"), "Split(S)");
            public static readonly GUIContent deleteButton = new GUIContent(Resources.Load<Texture>("Delete"), "Delete(Delete)");
            public static readonly GUIContent windowsTitle = new GUIContent("Tools");
        }

        private Rect m_NextDrawArea;

        public void SetRect(Rect rect)
        {
            // TODO : This way of forcing a window view is really wrong.
            GUILayout.BeginArea(rect, Styles.windowsTitle, GUI.skin.window);
            GUILayout.EndArea();

            m_NextDrawArea = rect;
            m_NextDrawArea.xMin += 5.0f; // margin
            m_NextDrawArea.yMin += 20.0f; // title height
        }

        public bool HandleCopy()
        {
            throw new NotImplementedException();
        }

        private bool HandleToggle(bool activated, GUIContent content, KeyCode shortcutKey)
        {
            m_NextDrawArea.width = Styles.buttonStyle.CalcSize(content).x;
            activated = GUI.Toggle(m_NextDrawArea, activated, content, Styles.buttonStyle);
            m_NextDrawArea.x += m_NextDrawArea.width;

            var currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown
                && currentEvent.keyCode == shortcutKey
                && string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()))
            {
                currentEvent.Use();
                activated = !activated;
            }

            return activated;
        }

        private bool HandleButton(GUIContent content, KeyCode shortcutKey)
        {
            m_NextDrawArea.width = Styles.buttonStyle.CalcSize(content).x;
            var activated = GUI.Button(m_NextDrawArea, content, Styles.buttonStyle);
            m_NextDrawArea.x += m_NextDrawArea.width;

            var currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown
                && currentEvent.keyCode == shortcutKey
                && string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()))
            {
                currentEvent.Use();
                activated = !activated;
            }

            return activated;
        }

        public bool HandleCreate(bool activated, bool enabled)
        {
            using (new EditorGUI.DisabledScope(!enabled))
            {
                return HandleToggle(activated, Styles.createButton, KeyCode.B);
            }
        }

        public bool HandleFreeCreate(bool activated, bool enabled)
        {
            using (new EditorGUI.DisabledScope(!enabled))
            {
                return HandleToggle(activated, Styles.freeCreateButton, KeyCode.N);
            }
        }

        public bool HandleDelete(bool enabled)
        {
            using (new EditorGUI.DisabledScope(!enabled))
            {
                return HandleButton(Styles.deleteButton, KeyCode.Delete);
            }
        }

        public bool HandleSplit(bool enabled)
        {
            using (new EditorGUI.DisabledScope(!enabled))
            {
                return HandleButton(Styles.splitButton, KeyCode.S);
            }
        }

        public bool HandleMove(bool activated, bool enabled)
        {
            using (new EditorGUI.DisabledScope(!enabled))
            {
                return HandleToggle(activated, Styles.freeMoveButton, KeyCode.M);
            }
        }

        public bool HandleParent(bool activated, bool enabled)
        {
            using (new EditorGUI.DisabledScope(!enabled))
            {
                return HandleToggle(activated, Styles.parentButton, KeyCode.P);
            }
        }

        public bool HandleGlobalCancel()
        {
            var currentEvent = Event.current;
            return ((currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape)
                    || (currentEvent.type == EventType.MouseDown && currentEvent.button == 1));
        }

        public bool HandleMultipleSelection()
        {
            var currentEvent = Event.current;
            return currentEvent.shift;
        }
    }
}
