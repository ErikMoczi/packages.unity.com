using UnityEngine.UIElements;

namespace UnityEditor.Experimental.U2D.Animation
{
    public static class VisualElementExtensions
    {
        public static void SetHiddenFromLayout(this VisualElement element, bool isHidden)
        {
            if (isHidden)
            {
                element.SetEnabled(false);
                element.visible = false;
                element.style.position = Position.Absolute;
            }
            else
            {
                element.SetEnabled(true);
                element.visible = true;
                element.style.position = Position.Relative;
            }
        }
    }
}
