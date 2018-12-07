using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    [CustomPropertyDrawer(typeof(JobTransform), true)]
    public class JobTransformDrawer : PropertyDrawer
    {
        private const int k_TransformPadding = 35;
        private const int k_TogglePadding = 8;
        private const int k_ToggleWidth = 15;
        private static readonly GUIContent k_syncTooltip = new GUIContent("", "Sync scene values");

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var weight = property.FindPropertyRelative("weight");
            var weightRect = new Rect();
            if (weight != null)
            {
                var w = rect.width * 0.65f;
                weightRect = new Rect(rect.x + w, rect.y, rect.width - w, rect.height);
                rect.width = w;
            }

            var transformRect = new Rect(rect.x, rect.y, rect.width - k_TransformPadding, EditorGUIUtility.singleLineHeight);
            var syncRect = new Rect(transformRect.x + transformRect.width + k_TogglePadding, rect.y, k_ToggleWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(transformRect, property.FindPropertyRelative("transform"), label);

            var indentLvl = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.PropertyField(syncRect, property.FindPropertyRelative("sync"), GUIContent.none);
            EditorGUI.LabelField(syncRect, k_syncTooltip);

            if (weight != null)
                EditorGUI.PropertyField(weightRect, weight, GUIContent.none);

            EditorGUI.indentLevel = indentLvl;

            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }
}