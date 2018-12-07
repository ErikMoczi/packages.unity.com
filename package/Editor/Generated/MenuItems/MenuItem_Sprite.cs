
using UnityEditor;

namespace Unity.Tiny
{
    internal static class MenuItem_Sprite
    {
        [MenuItem("GameObject/Tiny/2D Object/Sprite",true)]
        public static bool ValidateSprite()
        {
            return Unity.Tiny.EntityTemplateMenuItems.ValidateMenuItems();
        }

        [MenuItem("GameObject/Tiny/2D Object/Sprite",false,51)]
        public static void Sprite()
        {
            Unity.Tiny.EntityTemplateMenuItems.Sprite(TinySelectionUtility.GetRegistryObjectSelection());
        }
    }
}
