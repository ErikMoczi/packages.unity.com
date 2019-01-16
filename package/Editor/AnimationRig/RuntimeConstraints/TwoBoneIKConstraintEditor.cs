using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    [CustomEditor(typeof(TwoBoneIKConstraint))]
    public class TwoBoneIKConstraintEditor : Editor
    {
        static readonly GUIContent k_SourceObjectsLabel = new GUIContent("Source Objects");
        static readonly GUIContent k_SettingsLabel = new GUIContent("Settings");
        static readonly GUIContent k_MaintainTargetOffsetLabel = new GUIContent("Maintain Target Offset");

        SerializedProperty m_Weight;
        SerializedProperty m_Root;
        SerializedProperty m_Mid;
        SerializedProperty m_Tip;
        SerializedProperty m_Target;
        SerializedProperty m_Hint;
        SerializedProperty m_TargetPositionWeight;
        SerializedProperty m_TargetRotationWeight;
        SerializedProperty m_HintWeight;
        SerializedProperty m_MaintainTargetPositionOffset;
        SerializedProperty m_MaintainTargetRotationOffset;

        SerializedProperty m_SourceObjectsToggle;
        SerializedProperty m_SettingsToggle;

        void OnEnable()
        {
            m_Weight = serializedObject.FindProperty("m_Weight");
            m_SourceObjectsToggle = serializedObject.FindProperty("m_SourceObjectsGUIToggle");
            m_SettingsToggle = serializedObject.FindProperty("m_SettingsGUIToggle");

            var data = serializedObject.FindProperty("m_Data");
            m_Root = data.FindPropertyRelative("m_Root");
            m_Mid = data.FindPropertyRelative("m_Mid");
            m_Tip = data.FindPropertyRelative("m_Tip");
            m_Target = data.FindPropertyRelative("m_Target");
            m_Hint = data.FindPropertyRelative("m_Hint");
            m_TargetPositionWeight = data.FindPropertyRelative("m_TargetPositionWeight");
            m_TargetRotationWeight = data.FindPropertyRelative("m_TargetRotationWeight");
            m_HintWeight = data.FindPropertyRelative("m_HintWeight");
            m_MaintainTargetPositionOffset = data.FindPropertyRelative("m_MaintainTargetPositionOffset");
            m_MaintainTargetRotationOffset = data.FindPropertyRelative("m_MaintainTargetRotationOffset");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Weight);
            EditorGUILayout.PropertyField(m_Root);
            EditorGUILayout.PropertyField(m_Mid);
            EditorGUILayout.PropertyField(m_Tip);

            m_SourceObjectsToggle.boolValue = EditorGUILayout.Foldout(m_SourceObjectsToggle.boolValue, k_SourceObjectsLabel);
            if (m_SourceObjectsToggle.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_Target);
                EditorGUILayout.PropertyField(m_Hint);
                EditorGUI.indentLevel--;
            }

            m_SettingsToggle.boolValue = EditorGUILayout.Foldout(m_SettingsToggle.boolValue, k_SettingsLabel);
            if (m_SettingsToggle.boolValue)
            {
                EditorGUI.indentLevel++;
                MaintainOffsetHelper.DoDropdown(k_MaintainTargetOffsetLabel, m_MaintainTargetPositionOffset, m_MaintainTargetRotationOffset);
                EditorGUILayout.PropertyField(m_TargetPositionWeight);
                EditorGUILayout.PropertyField(m_TargetRotationWeight);
                EditorGUILayout.PropertyField(m_HintWeight);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
