
using UnityEditor;

namespace Unity.Tiny
{
    internal static class MenuItem_Camera
    {
        [MenuItem("GameObject/Tiny/Camera",true)]
        public static bool ValidateCamera()
        {
            return Unity.Tiny.EntityTemplateMenuItems.ValidateMenuItems();
        }

        [MenuItem("GameObject/Tiny/Camera",false,65)]
        public static void Camera()
        {
            Unity.Tiny.EntityTemplateMenuItems.Camera(TinySelectionUtility.GetRegistryObjectSelection());
        }
    }
}
