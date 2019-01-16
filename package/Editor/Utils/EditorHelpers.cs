using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor.Animations.Rigging
{
    public static class ReorderableListHelper
    {
        const int k_NoHeaderHeight = 2;
        const int k_ElementHeightPadding = 2;

        public static ReorderableList Create(SerializedObject obj, SerializedProperty property, bool draggable = true, bool displayHeader = false)
        {
            var list = new ReorderableList(obj, property, draggable, displayHeader, true, true);

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = list.serializedProperty.GetArrayElementAtIndex(index);

                var offset = k_ElementHeightPadding * 0.5f;
                rect.y += offset;
                rect.height = EditorGUIUtility.singleLineHeight;

                EditorGUI.PropertyField(rect, element, GUIContent.none);
            };

            list.elementHeight = EditorGUIUtility.singleLineHeight + k_ElementHeightPadding;

            if (!displayHeader)
                list.headerHeight = k_NoHeaderHeight;

            return list;
        }
    }

    public static class MaintainOffsetHelper
    {
        static readonly string[] k_MaintainOffsetTypeLables = { "None", "Position and Rotation", "Position", "Rotation"};
        static readonly int[] k_BitsToIndex = new int[] {0, 2, 3, 1};
        static readonly int[] k_IndexToBits = new int[] {0, 3, 1, 2};

        public static void DoDropdown(GUIContent label, SerializedProperty maintainPosition, SerializedProperty maintainRotation)
        {
            int currIndex = k_BitsToIndex[System.Convert.ToInt32(maintainPosition.boolValue) | (System.Convert.ToInt32(maintainRotation.boolValue) << 1)];
            int newIndex = EditorGUILayout.Popup(label, currIndex, k_MaintainOffsetTypeLables);
            if (newIndex == currIndex)
                return;

            var bits = k_IndexToBits[newIndex];
            maintainPosition.boolValue = (bits & 0x1) != 0;
            maintainRotation.boolValue = (bits & 0x2) != 0;
        }
    }
}
