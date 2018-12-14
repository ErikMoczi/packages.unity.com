

using System;
using Unity.Experimental.EditorMode;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal class EditorModeContextProxy
    {
        internal EditorModeContextProxy(EditorModeContext context)
        {
            m_Context = context;
        }

        internal readonly EditorModeContext m_Context;

        public void RegisterOverride<TOverride, TWindow>()
            where TOverride : TinyEditorWindowOverride<TWindow>, new()
            where TWindow : EditorWindow
        {
            m_Context.RegisterOverride<EditorWindowOverrideProxy<TOverride, TWindow>, TWindow>();
        }

        public void RegisterOverride<TOverride>(Type editorWindowType)
            where TOverride : TinyEditorWindowOverride<EditorWindow>, new()
        {
            m_Context.RegisterOverride<EditorWindowOverrideProxy<TOverride, EditorWindow>>(editorWindowType);
        }

        public void RegisterAsUnsupported(Type editorWindowType)
        {
            m_Context.RegisterAsUnsupported(editorWindowType);
        }

        public void RegisterAsPassthrough(Type editorWindowType)
        {
            m_Context.RegisterAsPassthrough(editorWindowType);
        }
    }
}
