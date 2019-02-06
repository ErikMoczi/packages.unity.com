

using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal class TinyQRCodeWindow : EditorWindow
    {
        public static string ContentUrl;
        public static Texture2D QrCode;

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(ContentUrl, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(new GUIContent("", QrCode), EditorStyles.label);
        }

        public static bool IsWindowOpen()
        {
            var windows = Resources.FindObjectsOfTypeAll<TinyQRCodeWindow>();
            return windows.Length > 0;
        }
    }
}
