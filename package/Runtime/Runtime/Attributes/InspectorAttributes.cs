using UnityEditor;

namespace Unity.Tiny
{
    internal static class InspectorAttributes
    {
        internal class HideInInspectorAttribute : IPropertyAttribute { }
        internal class ReadonlyAttribute : IPropertyAttribute { }

        internal class HeaderAttribute : IPropertyAttribute
        {
            public string Text { get; set; } = string.Empty;
        }

        internal class DontListAttribute : IPropertyAttribute { }

        public static readonly HideInInspectorAttribute HideInInspector = new HideInInspectorAttribute();
        public static readonly ReadonlyAttribute Readonly = new ReadonlyAttribute();
        public static HeaderAttribute Header(string text) { return new HeaderAttribute { Text = text }; }
        public static readonly DontListAttribute DontList = new DontListAttribute();
    }
}
