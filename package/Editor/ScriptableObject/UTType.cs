

using UnityEditor;
using UnityEditor.Callbacks;

namespace Unity.Tiny
{
    internal class UTType : TinyScriptableObject
    {
        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);

            if (obj is UTType)
            {
                // @TODO
                return true;
            }
            
            return false;
        }
    }
}

