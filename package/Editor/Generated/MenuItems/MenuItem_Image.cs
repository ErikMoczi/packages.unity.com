
using UnityEditor;

namespace Unity.Tiny
{
    internal static class MenuItem_Image
    {
        [MenuItem("GameObject/Tiny/UI/Image",true)]
        public static bool ValidateImage()
        {
            return Unity.Tiny.EntityTemplateMenuItems.ValidateMenuItems();
        }

        [MenuItem("GameObject/Tiny/UI/Image",false,54)]
        public static void Image()
        {
            Unity.Tiny.EntityTemplateMenuItems.Image(TinySelectionUtility.GetRegistryObjectSelection());
        }
    }
}
