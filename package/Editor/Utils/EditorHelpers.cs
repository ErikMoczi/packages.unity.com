using UnityEngine;
using UnityEngine.Animations.Rigging;
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

    public static class WeightedTransformHelper
    {
        const int k_NoHeaderHeight = 2;
        const int k_ElementHeightPadding = 2;
        const int k_TransformPadding = 6;

        public static ReorderableList CreateReorderableList(SerializedProperty property, ref WeightedTransformArray array, RangeAttribute range = null, bool draggable = true, bool displayHeader = false)
        {
            var reorderableList = new ReorderableList(array, typeof(WeightedTransform), draggable, displayHeader, true, true);

            reorderableList.drawElementBackgroundCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                // Didn't find a way to register a callback before we repaint the reorderable list to toggle draggable flag.
                // Ideally, we'd need a callback canReorderElementCallback that would enable/disable draggable handle.
                reorderableList.draggable = !AnimationMode.InAnimationMode() && !Application.isPlaying;
            };

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = property.FindPropertyRelative("m_Item" + index);

                var offset = k_ElementHeightPadding * 0.5f;
                rect.y += offset;
                rect.height = EditorGUIUtility.singleLineHeight;

                EditorGUI.BeginChangeCheck();

                WeightedTransformOnGUI(rect, element, GUIContent.none, range);

                if (EditorGUI.EndChangeCheck())
                {
                    var transformProperty = element.FindPropertyRelative("transform");
                    var weightProperty = element.FindPropertyRelative("weight");

                    reorderableList.list[index] = new WeightedTransform(transformProperty.objectReferenceValue as Transform, weightProperty.floatValue);;
                }
            };

            reorderableList.onCanAddCallback = (ReorderableList list) =>
            {
                return list.list.Count < WeightedTransformArray.k_MaxLength && !AnimationMode.InAnimationMode() && !Application.isPlaying;
            };

            reorderableList.onCanRemoveCallback = (ReorderableList list) =>
            {
                return !AnimationMode.InAnimationMode() && !Application.isPlaying;
            };

            reorderableList.onAddCallback = (ReorderableList list) =>
            {
                list.list.Add(WeightedTransform.Default(1f));
            };

            reorderableList.elementHeight = EditorGUIUtility.singleLineHeight + k_ElementHeightPadding;

            if (!displayHeader)
                reorderableList.headerHeight = k_NoHeaderHeight;

            return reorderableList;
        }

        public static void WeightedTransformOnGUI(Rect rect, SerializedProperty property, GUIContent label, RangeAttribute range = null)
        {
            EditorGUI.BeginProperty(rect, GUIContent.none, property);

            var w = rect.width * 0.65f;
            var weightRect = new Rect(rect.x + w, rect.y, rect.width - w, rect.height);
            rect.width = w;

            var transformRect = new Rect(rect.x, rect.y, rect.width - k_TransformPadding, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(transformRect, property.FindPropertyRelative("transform"), GUIContent.none);

            var indentLvl = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            if (range != null)
                EditorGUI.Slider(weightRect, property.FindPropertyRelative("weight"), range.min, range.max, GUIContent.none);
            else
                EditorGUI.PropertyField(weightRect, property.FindPropertyRelative("weight"), GUIContent.none);
            EditorGUI.indentLevel = indentLvl;

            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
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
