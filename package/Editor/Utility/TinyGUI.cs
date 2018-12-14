

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class TinyGUI
    {
        public static void BackgroundColor(Rect rect, Color color)
        {
            var oldColor = GUI.color;
            GUI.color = color;

            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUI.color = oldColor;
        }

        private static readonly int s_FolderPickerHash = "TinyFolderPicker".GetHashCode();
        public static DefaultAsset FolderField(Rect rect, string label, DefaultAsset folder)
        {
            var folderAsset = folder;
            if (!AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folder)))
            {
                folderAsset = null;
            }

            folderAsset = (DefaultAsset)EditorGUI.ObjectField(rect, label, folderAsset, typeof(DefaultAsset), false);
            // By default, it will output None (Default Asset), we want to display None (Folder)
            if (null == folderAsset)
            {
                var id = GUIUtility.GetControlID(s_FolderPickerHash, FocusType.Keyboard, rect);
                rect.x += EditorGUIUtility.labelWidth;
                rect.width -= EditorGUIUtility.labelWidth;
                if (Event.current.type == EventType.Repaint)
                {
                    var highlighted = false;
                    if (rect.Contains(Event.current.mousePosition) && GUI.enabled)
                    {
                        if (null != DragAndDrop.objectReferences.FirstOrDefault(obj => obj is DefaultAsset))
                        {
                            highlighted = true;
                        }
                    }
                    EditorStyles.objectField.Draw(rect, new GUIContent("None (Folder)"), id, highlighted);
                }
            }

            if (null != folderAsset)
            {
                if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folderAsset)))
                {
                    return folderAsset;
                }
            }

            return null;
        }
    }
}

