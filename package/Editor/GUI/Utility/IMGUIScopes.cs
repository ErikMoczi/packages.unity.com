using System;
using Unity.Properties;

namespace Unity.Tiny
{
    internal static class IMGUIScopes
    {
        public static IDisposable MakePrefabScopes<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context, int minHeight = 0)
            where TContainer : IPropertyContainer
            => new IMGUIPrefabValueScope<TContainer, TValue>(ref container, ref context, minHeight, PrefabScopeOptions.Default);
        
        public static IMGUIOverridenValueScope<TContainer, TValue> MakeOverridenScope<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : IPropertyContainer
            => new IMGUIOverridenValueScope<TContainer, TValue>(ref container, ref context);

        public static IMGUIMixedValueScope<TContainer, TValue> MakeMixedValueScope<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : IPropertyContainer
            => new IMGUIMixedValueScope<TContainer, TValue>(ref container, ref context);

        public static IMGUIValueListWrapperScope<TValue> MakeListScope<TValue>(UIVisitContext<TValue> context)
            => new IMGUIValueListWrapperScope<TValue>(context);

        public static IMGUIContainerListWrapperScope<TValue> MakeContainerListScope<TValue>(UIVisitContext<TValue> context)
            => new IMGUIContainerListWrapperScope<TValue>(context);
    }
}
