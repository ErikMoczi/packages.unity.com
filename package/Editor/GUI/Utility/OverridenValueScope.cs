
using System;
using Unity.Properties;

namespace Unity.Tiny
{
    internal struct IMGUIOverridenValueScope<TContainer, TValue> : IDisposable
        where TContainer : IPropertyContainer
    {
        public IMGUIOverridenValueScope(ref TContainer container, ref UIVisitContext<TValue> context)
        {
            var isOverridden = (context.Property as ITinyValueProperty)?.IsOverridden(container) ?? true;
            TinyEditorUtility.SetEditorBoldDefault(isOverridden);
        }

        public void Dispose()
        {
            TinyEditorUtility.SetEditorBoldDefault(false);
        }
    }
}
