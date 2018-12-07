using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

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
                element.style.positionType = PositionType.Absolute;
            }
            else
            {
                element.SetEnabled(true);
                element.visible = true;
                element.style.positionType = PositionType.Relative;
            }
        }
    }
}
