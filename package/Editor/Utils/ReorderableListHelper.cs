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
}
