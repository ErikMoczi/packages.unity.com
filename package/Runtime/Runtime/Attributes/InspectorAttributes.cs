using UnityEditor;

namespace Unity.Tiny
{
    internal static class InspectorAttributes
    {
        internal class HideInInspectorAttribute : IPropertyAttribute { }
        internal class ReadonlyAttribute : IPropertyAttribute { }
        internal class TooltipAttribute : IPropertyAttribute
        {
            public string Text { get; set; } = string.Empty;
        }
        internal class VisibilityAttribute : IPropertyAttribute
        {
            public InspectorMode Mode { get; set; } = InspectorMode.Normal;
        }

        internal class HeaderAttribute : IPropertyAttribute
        {
            public string Text { get; set; } = string.Empty;
        }


        internal class DontListAttribute : IPropertyAttribute { }

        public static readonly HideInInspectorAttribute HideInInspector = new HideInInspectorAttribute();
        public static readonly ReadonlyAttribute Readonly = new ReadonlyAttribute();
        public static TooltipAttribute Tooltip(string text) { return new TooltipAttribute { Text = text }; }
        public static VisibilityAttribute Visibility(InspectorMode mode) { return new VisibilityAttribute{ Mode = mode }; }
        public static HeaderAttribute Header(string text) { return new HeaderAttribute { Text = text }; }
        public static readonly DontListAttribute DontList = new DontListAttribute();
    }
}
