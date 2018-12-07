

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
            
            if (obj is UTModule)
            {
                TinyEditorApplication.SaveChanges();
                TinyEditorApplication.LoadModule(AssetDatabase.GetAssetPath(obj));
                return true;
            }
            return false;
        }
    }
}

