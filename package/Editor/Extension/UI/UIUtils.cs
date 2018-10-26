using UnityEngine.Experimental.UIElements;

namespace UnityEditor.PackageManager.ValidationSuite.UI
{
    internal static class UIUtils
    {
        private const string DisplayNone = "display-none";

        public static void SetElementDisplay(VisualElement element, bool value)
        {
            if (element == null)
                return;
            
            if (value)
                element.RemoveFromClassList(DisplayNone);
            else
                element.AddToClassList(DisplayNone);

            element.visible = value;
        }

        public static bool IsElementVisible(VisualElement element)
        {
            return element.visible && !element.ClassListContains(DisplayNone);
        }
    }
}
