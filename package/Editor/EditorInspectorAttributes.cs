

namespace Unity.Tiny
{
    internal static class EditorInspectorAttributes
    {
        internal class ComponentCallbackAttribute : IPropertyAttribute
        {
            public IComponentCallback Callback { get; set; }
            public TinyType.Reference TypeRef { get; set; }
        }

        internal static ComponentCallbackAttribute Callbacks(IComponentCallback callback, TinyType.Reference type) { return new ComponentCallbackAttribute { Callback = callback, TypeRef = type}; }

    }
}

