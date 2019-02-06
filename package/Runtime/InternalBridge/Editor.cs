
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny.Bridge
{
    internal static class Editor
    {
        public static bool DisplayDialog(string title, string message, string ok) {
#if UNITY_EDITOR
            if(Application.isBatchMode) {
                return true;
            } else {
                return EditorUtility.DisplayDialog(title, message, ok);
            }
#else 
            return true;
#endif
        }

    }
}

