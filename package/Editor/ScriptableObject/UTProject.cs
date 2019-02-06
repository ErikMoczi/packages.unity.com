using UnityEditor;
using UnityEditor.Callbacks;

namespace Unity.Tiny
{
    internal class UTProject : TinyScriptableObject
    {
        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is UTProject && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (TinyEditorApplication.SaveChanges())
                {
                    TinyEditorApplication.Close();
                    if (!obj)
                    {
                        // Fix for saving a renamed project when trying to re-opening it.
                        obj = EditorUtility.InstanceIDToObject(instanceId);
                    }
                    
                    TinyEditorApplication.LoadProject(AssetDatabase.GetAssetPath(obj));
                }
                return true;
            }
            return false;
        }
    }
}

