
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
    }
}
