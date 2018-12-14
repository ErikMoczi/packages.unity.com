
using UnityEditor;

namespace Unity.Tiny
{
    internal static class MenuItem_AudioSource
    {
        [MenuItem("GameObject/Tiny/Audio/Audio Source",true)]
        public static bool ValidateAudio_Source()
        {
            return Unity.Tiny.EntityTemplateMenuItems.ValidateMenuItems();
        }

        [MenuItem("GameObject/Tiny/Audio/Audio Source",false,52)]
        public static void Audio_Source()
        {
            Unity.Tiny.EntityTemplateMenuItems.Audio_Source(TinySelectionUtility.GetRegistryObjectSelection());
        }
    }
}
