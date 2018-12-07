using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    [CustomEditor(typeof(BlendConstraint))]
    public class BlendConstraintEditor : Editor
    {
        static readonly GUIContent k_SourceObjectsLabel = new GUIContent("Source Objects");
        static readonly GUIContent k_SettingsLabel = new GUIContent("Settings");
        static readonly GUIContent k_SrcASrcBBlend = new GUIContent("A | B Blend");

        SerializedProperty m_Weight;
        SerializedProperty m_ConstrainedObject;
        SerializedProperty m_SourceA;
        SerializedProperty m_SourceB;
        SerializedProperty m_BlendPosition;
        SerializedProperty m_BlendRotation;
        SerializedProperty m_PositionWeight;
        SerializedProperty m_RotationWeight;

        SerializedProperty m_SourceObjectsToggle;
        SerializedProperty m_SettingsToggle;

        void OnEnable()
        {
            m_Weight = serializedObject.FindProperty("m_Weight");
            m_SourceObjectsToggle = serializedObject.FindProperty("m_SourceObjectsGUIToggle");
            m_SettingsToggle = serializedObject.FindProperty("m_SettingsGUIToggle");

            var data = serializedObject.FindProperty("m_Data");
            m_ConstrainedObject = data.FindPropertyRelative("m_ConstrainedObject");
            m_SourceA = data.FindPropertyRelative("m_SourceA");
            m_SourceB = data.FindPropertyRelative("m_SourceB");
            m_BlendPosition = data.FindPropertyRelative("m_BlendPosition");
            m_BlendRotation = data.FindPropertyRelative("m_BlendRotation");
            m_PositionWeight = data.FindPropertyRelative("m_PositionWeight");
            m_RotationWeight = data.FindPropertyRelative("m_RotationWeight");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Weight);
            EditorGUILayout.PropertyField(m_ConstrainedObject);

            m_SourceObjectsToggle.boolValue = EditorGUILayout.Foldout(m_SourceObjectsToggle.boolValue, k_SourceObjectsLabel);
            if (m_SourceObjectsToggle.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_SourceA);
                EditorGUILayout.PropertyField(m_SourceB);
                EditorGUI.indentLevel--;
            }

            m_SettingsToggle.boolValue = EditorGUILayout.Foldout(m_SettingsToggle.boolValue, k_SettingsLabel);
            if (m_SettingsToggle.boolValue)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_BlendPosition);
                EditorGUI.indentLevel++;
                using (new EditorGUI.DisabledScope(!m_BlendPosition.boolValue))
                    EditorGUILayout.PropertyField(m_PositionWeight, k_SrcASrcBBlend);
                EditorGUI.indentLevel--;

                EditorGUILayout.PropertyField(m_BlendRotation);
                EditorGUI.indentLevel++;
                using (new EditorGUI.DisabledScope(!m_BlendRotation.boolValue))
                    EditorGUILayout.PropertyField(m_RotationWeight, k_SrcASrcBBlend);
                EditorGUI.indentLevel--;

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
