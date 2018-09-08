using UnityEngine;
namespace Unity.MemoryProfiler.Editor.UI
{
    public interface IViewPaneEventListener
    {
        void OnOpenTable(Database.View.LinkRequest link);
        void OnOpenMemoryMap();
        void OnOpenTreeMap();
        void OnRepaint();
    }
    public abstract class ViewPane : UI.IViewEventListener
    {
        public UIState m_UIState;
        public IViewPaneEventListener m_EventListener;
        public ViewPane(UIState s, IViewPaneEventListener l)
        {
            m_UIState = s;
            m_EventListener = l;
        }

        public abstract UI.HistoryEvent GetCurrentHistoryEvent();
        public virtual void OnPreGUI() {}
        public abstract void OnGUI(Rect r);
        void UI.IViewEventListener.OnRepaint()
        {
            m_EventListener.OnRepaint();
        }

        public abstract void OnClose();
    }
}
