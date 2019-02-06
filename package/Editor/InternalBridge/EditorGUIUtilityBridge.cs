
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class EditorGUIUtilityBridge
    {
        public static int LastControlID => EditorGUIUtility.s_LastControlID;

        public static GUIContent TrTextContent(string text) => EditorGUIUtility.TrTextContent(text);
        public static GUIContent TrTextContent(string text, string tooltip) => EditorGUIUtility.TrTextContent(text, tooltip);
        public static Texture2D LoadIcon(string name) => EditorGUIUtility.LoadIcon(name);

        public static bool DisplayDialog(string title, string message, string ok) {
            if (Application.isBatchMode) {
                return true;
            } else {
                return EditorUtility.DisplayDialog(title, message, ok);
            }
        }
    }
}
