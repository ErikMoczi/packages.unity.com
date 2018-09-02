using UnityEngine.Experimental.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal static class UIUtils
    {
        public static string DisplayNone = "display-none";
        
        internal static void SetElementDisplay(VisualElement element, bool value)
        {
            if (value)
                element.RemoveFromClassList(DisplayNone);
            else
                element.AddToClassList(DisplayNone);

            element.visible = value;
        }

        internal static bool IsElementVisible(VisualElement element)
        {
            return element.visible && !element.ClassListContains(DisplayNone);
        }
    }
}
