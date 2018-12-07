using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEditorInternal;

namespace UnityEditor.Animations.Rigging
{
    [CustomEditor(typeof(MultiPositionConstraint))]
    public class MultiPositionConstraintEditor : Editor
    {
        static readonly GUIContent k_SourceObjectsLabel = new GUIContent("Source Objects");
        static readonly GUIContent k_SettingsLabel = new GUIContent("Settings");

        SerializedProperty m_Weight;
        SerializedProperty m_ConstrainedObject;
        SerializedProperty m_ConstrainedAxes;
        SerializedProperty m_SourceObjects;
        SerializedProperty m_MaintainOffset;
        SerializedProperty m_Offset;

        SerializedProperty m_SourceObjectsToggle;
        SerializedProperty m_SettingsToggle;
        ReorderableList m_ReorderableList;

        void OnEnable()
        {
            m_Weight = serializedObject.FindProperty("m_Weight");
            m_SourceObjectsToggle = serializedObject.FindProperty("m_SourceObjectsGUIToggle");
            m_SettingsToggle = serializedObject.FindProperty("m_SettingsGUIToggle");

            var data = serializedObject.FindProperty("m_Data");
            m_ConstrainedObject = data.FindPropertyRelative("m_ConstrainedObject");
            m_ConstrainedAxes = data.FindPropertyRelative("m_ConstrainedAxes");
            m_SourceObjects = data.FindPropertyRelative("m_SourceObjects");
            m_MaintainOffset = data.FindPropertyRelative("m_MaintainOffset");
            m_Offset = data.FindPropertyRelative("m_Offset");

            m_ReorderableList = ReorderableListHelper.Create(serializedObject, m_SourceObjects);
            if (m_ReorderableList.count == 0)
                ((MultiPositionConstraint)serializedObject.targetObject).data.sourceObjects.Add(WeightedJobTransform.defaultNoSync(1f));

            m_ReorderableList.onAddCallback = (ReorderableList list) =>
            {
                ((MultiPositionConstraint)serializedObject.targetObject).data.sourceObjects.Add(WeightedJobTransform.defaultNoSync(1f));
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Weight);
            EditorGUILayout.PropertyField(m_ConstrainedObject);
            EditorGUILayout.PropertyField(m_ConstrainedAxes);

            m_SourceObjectsToggle.boolValue = EditorGUILayout.Foldout(m_SourceObjectsToggle.boolValue, k_SourceObjectsLabel);
            if (m_SourceObjectsToggle.boolValue)
            {
                EditorGUI.indentLevel++;
                m_ReorderableList.DoLayoutList();
                EditorGUI.indentLevel--;
            }

            m_SettingsToggle.boolValue = EditorGUILayout.Foldout(m_SettingsToggle.boolValue, k_SettingsLabel);
            if (m_SettingsToggle.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_MaintainOffset);
                EditorGUILayout.PropertyField(m_Offset);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
