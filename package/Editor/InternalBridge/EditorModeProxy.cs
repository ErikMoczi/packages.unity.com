using Unity.Experimental.EditorMode;
using UnityEngine;

namespace Unity.Tiny
{
    internal class EditorModeBridge<TEditorModeProxy> : EditorMode
        where TEditorModeProxy : EditorModeProxy, new()
    {
        TEditorModeProxy m_Proxy = new TEditorModeProxy();

        public override void OnEnterMode(EditorModeContext context)
        {
            m_Proxy.OnEnterMode(new EditorModeContextProxy(context));
            Name = m_Proxy.Name;
        }

        public override void OnExitMode()
        {
            m_Proxy.OnExitMode();
        }
    }

    internal class EditorModeProxy
    {
        public string Name { get; protected set; }

        public virtual void OnEnterMode(EditorModeContextProxy context) { }

        public virtual void OnExitMode() { }
    }
}
