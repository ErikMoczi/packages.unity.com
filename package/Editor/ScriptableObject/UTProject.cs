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
            
            if (obj is UTProject)
            {
                TinyEditorApplication.SaveChanges();
                TinyEditorApplication.LoadProject(AssetDatabase.GetAssetPath(obj));
                return true;
            }
            return false;
        }
    }
}

