using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class ModuleWindow
    {
        private static int s_WindowID = 0;

        public delegate void WindowGUICallbackDelegate();

        public WindowGUICallbackDelegate windowGUICallback;

        public string title
        {
            get { return m_Title; }
            set { m_Title = value; }
        }

        public Rect rect
        {
            get { return m_Rect; }
            set { m_Rect = value; }
        }

        public ModuleWindow(string title, Rect rect)
        {
            m_Title = title;
            m_Rect = rect;
            m_Id = s_WindowID++;
        }

        public virtual void OnWindowGUI(Rect viewRect)
        {
            m_Rect = GUI.Window(m_Id, m_Rect, DoWindowGUI, new GUIContent(m_Title));
        }

        public Rect DockBottomLeft(Rect viewRect, Vector2 offset)
        {
            m_Rect.position = new Vector2(viewRect.xMin, viewRect.yMax - rect.height) + offset;
            return rect;
        }

        public Rect DockBottomRight(Rect viewRect, Vector2 offset)
        {
            m_Rect.position = viewRect.max - rect.size + offset;
            return rect;
        }

        private void DoWindowGUI(int windowId)
        {
            if (Event.current.type == EventType.Layout && new Rect(Vector2.zero, m_Rect.size).Contains(Event.current.mousePosition))
                HandleUtility.nearestControl = 0;

            if (windowGUICallback != null)
                windowGUICallback();
        }

        string m_Title;
        Rect m_Rect;
        int m_Id;
    }
}
