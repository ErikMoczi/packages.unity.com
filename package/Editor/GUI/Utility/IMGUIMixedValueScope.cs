
using System;
using Unity.Properties;
using UnityEditor;

namespace Unity.Tiny
{
    internal struct IMGUIMixedValueScope<TContainer, TValue> : IDisposable
        where TContainer : IPropertyContainer
    {
        private bool m_MixedValue;

        public IMGUIMixedValueScope(ref TContainer container, ref UIVisitContext<TValue> context)
        {
            m_MixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = context.Visitor.ChangeTracker.HasMixedValues<TValue>(container, context.Property);
        }

        public void Dispose()
        {
            EditorGUI.showMixedValue = m_MixedValue;
        }
    }
}
