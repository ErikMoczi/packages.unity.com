using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(OrbitalTransposerHeadingPropertyAttribute))]
    public sealed class OrbitalTransposerHeadingPropertyDrawer : PropertyDrawer
    {
        const int vSpace = 2;
        bool mExpanded = true;
        CinemachineOrbitalTransposer.Heading def = new CinemachineOrbitalTransposer.Heading(); // to access name strings

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            rect.height = height;
            mExpanded = EditorGUI.Foldout(rect, mExpanded, label);
            if (mExpanded)
            {
                ++EditorGUI.indentLevel;

                rect.y += height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.m_HeadingDefinition));

                if (IsVelocityMode(property))
                {
                    rect.y += height + vSpace;
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.m_VelocityFilterStrength));
                }

                rect.y += height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.m_HeadingBias));

                --EditorGUI.indentLevel;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + vSpace;
            if (mExpanded)
                height *= IsVelocityMode(property) ? 4 : 3;
            return height;
        }

        bool IsVelocityMode(SerializedProperty property)
        {
            var mode = property.FindPropertyRelative(() => def.m_HeadingDefinition);
            var value = (CinemachineOrbitalTransposer.Heading.HeadingDefinition)
                (System.Enum.GetValues(typeof(CinemachineOrbitalTransposer.Heading.HeadingDefinition))).GetValue(mode.enumValueIndex);
            return value == CinemachineOrbitalTransposer.Heading.HeadingDefinition.Velocity
                || value == CinemachineOrbitalTransposer.Heading.HeadingDefinition.PositionDelta;
        }
    }
}
