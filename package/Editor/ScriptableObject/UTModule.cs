

using UnityEditor;
using UnityEditor.Callbacks;

namespace Unity.Tiny
{
    internal class UTModule : TinyScriptableObject
    {
        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            
            if (obj is UTModule && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (TinyEditorApplication.SaveChanges())
                {
                    TinyEditorApplication.Close();
                    if (!obj)
                    {
                        // Fix for saving a renamed project when trying to re-opening it.
                        obj = EditorUtility.InstanceIDToObject(instanceId);
                    }
                    TinyEditorApplication.LoadModule(AssetDatabase.GetAssetPath(obj));
                }
                return true;
            }
            return false;
        }
    }
}

