using UnityEngine.Experimental.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal static class UIUtils
    {
        internal static void SetElementDisplay(VisualElement element, bool value)
        {
            if (value)
                element.RemoveFromClassList("display-none");
            else
                element.AddToClassList("display-none");

            element.visible = value;
        }
    }
}
