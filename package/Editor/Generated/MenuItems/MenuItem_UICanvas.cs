
using UnityEditor;

namespace Unity.Tiny
{
    internal static class MenuItem_UICanvas
    {
        [MenuItem("GameObject/Tiny/UI/UI Canvas",true)]
        public static bool ValidateUI_Canvas()
        {
            return Unity.Tiny.EntityTemplateMenuItems.ValidateMenuItems();
        }

        [MenuItem("GameObject/Tiny/UI/UI Canvas",false,53)]
        public static void UI_Canvas()
        {
            Unity.Tiny.EntityTemplateMenuItems.UI_Canvas(TinySelectionUtility.GetRegistryObjectSelection());
        }
    }
}
