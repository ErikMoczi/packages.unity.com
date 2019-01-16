using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    [CustomPropertyDrawer(typeof(TwistNode))]
    public class TwistNodeDrawer : PropertyDrawer
    {
        private const int k_TransformPadding = 5;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var weight = property.FindPropertyRelative("weight");
            var transform = property.FindPropertyRelative("transform");

            var w = rect.width * 0.65f;
            var weightRect = new Rect(rect.x + w, rect.y, rect.width - w, rect.height);
            rect.width = w;
            var transformRect = new Rect(rect.x, rect.y, rect.width - k_TransformPadding, EditorGUIUtility.singleLineHeight);
            
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(transformRect, transform, label);

            var indentLvl = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(weightRect, weight, GUIContent.none);
            EditorGUI.indentLevel = indentLvl;

            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }
}