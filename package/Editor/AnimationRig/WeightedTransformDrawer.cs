using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    [CustomPropertyDrawer(typeof(WeightedTransform))]
    public class WeightedTransformDrawer : PropertyDrawer
    {
        private const int k_TransformPadding = 6;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var w = rect.width * 0.65f;
            var weightRect = new Rect(rect.x + w, rect.y, rect.width - w, rect.height);
            rect.width = w;

            var transformRect = new Rect(rect.x, rect.y, rect.width - k_TransformPadding, EditorGUIUtility.singleLineHeight);
            
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(transformRect, property.FindPropertyRelative("transform"), label);

            var indentLvl = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(weightRect, property.FindPropertyRelative("weight"), GUIContent.none);
            EditorGUI.indentLevel = indentLvl;

            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }
}