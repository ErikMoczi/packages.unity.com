
using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal struct IMGUIValueListWrapperScope<TValue> : IDisposable
    {
        private UIVisitContext<TValue> m_Context;

        public IMGUIValueListWrapperScope(UIVisitContext<TValue> context)
        {
            m_Context = context;

            if (m_Context.IsListItem)
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();
            }
        }

        public void Dispose()
        {
            if (m_Context.IsListItem)
            {
                if (GUILayout.Button(TinyIcons.Remove, GUILayout.Width(16.0f), GUILayout.Height(16.0f)))
                {
                    m_Context.Visitor.RemoveAtIndex = m_Context.Index;
                }
                EditorGUILayout.EndHorizontal();
                
                TinyGUILayout.Separator(TinyColors.Inspector.Separator, 2.0f);
                EditorGUILayout.EndVertical();
            }
        }
    }
}
