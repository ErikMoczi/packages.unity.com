
using UnityEditor;

namespace Unity.Tiny
{
    internal static class MenuItem_CreateEmptyChild
    {
        [MenuItem("GameObject/Tiny/Create Empty Child",true)]
        public static bool ValidateCreate_Empty_Child()
        {
            return Unity.Tiny.EntityTemplateMenuItems.ValidateMenuItems();
        }

        [MenuItem("GameObject/Tiny/Create Empty Child",false,50)]
        public static void Create_Empty_Child()
        {
            Unity.Tiny.EntityTemplateMenuItems.Create_Empty_Child(TinySelectionUtility.GetRegistryObjectSelection());
        }
    }
}
