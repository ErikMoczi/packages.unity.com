
using UnityEditor;

namespace Unity.Tiny
{
    internal static class MenuItem_Panel
    {
        [MenuItem("GameObject/Tiny/UI/Panel",true)]
        public static bool ValidatePanel()
        {
            return Unity.Tiny.EntityTemplateMenuItems.ValidateMenuItems();
        }

        [MenuItem("GameObject/Tiny/UI/Panel",false,55)]
        public static void Panel()
        {
            Unity.Tiny.EntityTemplateMenuItems.Panel(TinySelectionUtility.GetRegistryObjectSelection());
        }
    }
}
