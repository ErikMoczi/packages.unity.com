using System;
using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    //Currently we are not using pop up windows. This class is for the pop up window
    //If we need this, it needs to be changed to reflect new UI design for a pop up window.
    class TutorialModalWindow : EditorWindow
    {
        [SerializeField]
        private TutorialStyles m_Styles = null;
        [SerializeField]
        private TutorialWelcomePage m_WelcomePage;
        private static bool m_IsShowing;
        private bool m_DrawAsCompleted;
        private Action onClose;

        public static void TryToShow(TutorialWelcomePage welcomePage, bool drawAsCompleted, Action onClose)
        {
            if (m_IsShowing)
                return;
            var window = GetWindow<TutorialModalWindow>();

            window.m_WelcomePage = welcomePage;
            window.onClose = onClose;
            window.m_DrawAsCompleted = drawAsCompleted;

            window.ShowAuxWindow();
            m_IsShowing = true;
            EditorGUIUtility.PingObject(window);
        }

        public static bool IsShowing()
        {
            return m_IsShowing;
        }

        protected virtual void OnLostFocus()
        {
            Focus();
        }

        void OnGUI()
        {
            if (m_Styles == null)
            {
                TutorialStyles.DisplayErrorMessage("TutorialModalWindow.cs");
                return;
            }

            if (m_WelcomePage == null)
            {
                return;
            }

            if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                return;
            }

            GUISkin oldSkin = GUI.skin;
            GUI.skin = m_Styles.skin;

            using (new EditorGUILayout.VerticalScope(AllTutorialStyles.background ?? GUIStyle.none, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)))
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Box(m_WelcomePage.icon, GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (m_DrawAsCompleted)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Box(GUIContent.none, AllTutorialStyles.line ?? GUIStyle.none);
                    GUILayout.Label("Completed", AllTutorialStyles.instructionLabel, GUILayout.ExpandWidth(false));
                    GUILayout.Box(GUIContent.none, AllTutorialStyles.line ?? GUIStyle.none);
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Box(GUIContent.none, AllTutorialStyles.line ?? GUIStyle.none);
                }


                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(m_WelcomePage.title, AllTutorialStyles.headerLabel ?? GUIStyle.none);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
                var btnStyle = GUI.skin.button;
                btnStyle.fixedWidth = 0;
                btnStyle.stretchWidth = true;
                if (GUILayout.Button(m_DrawAsCompleted ? m_WelcomePage.finishButtonLabel : " ", btnStyle))
                {
                    Close();
                }
            }

            GUI.skin = oldSkin;
        }

        void OnDestroy()
        {
            m_IsShowing = false;
            if (onClose != null)
            {
                onClose();
            }
        }
    }
}
