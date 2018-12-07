
using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal struct IMGUIContainerListWrapperScope<TValue> : IDisposable
    {
        private UIVisitContext<TValue> m_Context;

        public IMGUIContainerListWrapperScope(UIVisitContext<TValue> context)
        {
            m_Context = context;

            if (m_Context.IsListItem)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(context.Label, GUILayout.MaxWidth(16.0f));
                EditorGUILayout.BeginVertical();
            }
            else
            {
                EditorGUILayout.LabelField(context.Label);
                ++EditorGUI.indentLevel;
            }
        }

        public void Dispose()
        {
            if (m_Context.IsListItem)
            {
                EditorGUILayout.EndVertical();
                if (GUILayout.Button(TinyIcons.X_Icon_16, GUILayout.Width(16.0f), GUILayout.Height(16.0f)))
                {
                    m_Context.Visitor.RemoveAtIndex = m_Context.Index;
                }
                EditorGUILayout.EndHorizontal();
                TinyGUILayout.Separator(TinyColors.Inspector.Separator, 2.0f);
            }
            else
            {
                --EditorGUI.indentLevel;
            }
        }
    }

}
