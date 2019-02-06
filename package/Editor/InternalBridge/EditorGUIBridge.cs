
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class EditorGUIBridge
    {
        public static float DoFloatField(Rect positionField, Rect positionLabel, int id, float value)
            => EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, positionField, positionLabel, id, value, EditorGUI.kFloatFieldFormatString, EditorStyles.textField, true);

        public static float MiniLabelWidth
            => EditorGUI.kMiniLabelW;

        public static float SingleLineHeight
            => EditorGUI.kSingleLineHeight;
    }
}
