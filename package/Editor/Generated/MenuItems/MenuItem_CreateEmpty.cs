
using UnityEditor;

namespace Unity.Tiny
{
    internal static class MenuItem_CreateEmpty
    {
        [MenuItem("GameObject/Tiny/Create Empty",true)]
        public static bool ValidateCreate_Empty()
        {
            return Unity.Tiny.EntityTemplateMenuItems.ValidateMenuItems();
        }

        [MenuItem("GameObject/Tiny/Create Empty",false,49)]
        public static void Create_Empty()
        {
            Unity.Tiny.EntityTemplateMenuItems.Create_Empty(TinySelectionUtility.GetRegistryObjectSelection());
        }
    }
}
