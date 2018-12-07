using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEditorInternal;

namespace UnityEditor.Animations.Rigging
{
    [CustomEditor(typeof(MultiParentConstraint))]
    public class MultiParentConstraintEditor : Editor
    {
        static readonly GUIContent k_SourceObjectsLabel = new GUIContent("Source Objects");
        static readonly GUIContent k_SettingsLabel = new GUIContent("Settings");

        SerializedProperty m_Weight;
        SerializedProperty m_ConstrainedObject;
        SerializedProperty m_ConstrainedPositionAxes;
        SerializedProperty m_ConstrainedRotationAxes;
        SerializedProperty m_SourceObjects;
        SerializedProperty m_MaintainOffset;

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
            m_ConstrainedPositionAxes = data.FindPropertyRelative("m_ConstrainedPositionAxes");
            m_ConstrainedRotationAxes = data.FindPropertyRelative("m_ConstrainedRotationAxes");
            m_SourceObjects = data.FindPropertyRelative("m_SourceObjects");
            m_MaintainOffset = data.FindPropertyRelative("m_MaintainOffset");

            m_ReorderableList = ReorderableListHelper.Create(serializedObject, m_SourceObjects);
            if (m_ReorderableList.count == 0)
                ((MultiParentConstraint)serializedObject.targetObject).data.sourceObjects.Add(WeightedJobTransform.defaultNoSync(1f));

            m_ReorderableList.onAddCallback = (ReorderableList list) =>
            {
                ((MultiParentConstraint)serializedObject.targetObject).data.sourceObjects.Add(WeightedJobTransform.defaultNoSync(1f));
            };
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
                m_ReorderableList.DoLayoutList();
                EditorGUI.indentLevel--;
            }

            m_SettingsToggle.boolValue = EditorGUILayout.Foldout(m_SettingsToggle.boolValue, k_SettingsLabel);
            if (m_SettingsToggle.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_MaintainOffset);
                EditorGUILayout.PropertyField(m_ConstrainedPositionAxes);
                EditorGUILayout.PropertyField(m_ConstrainedRotationAxes);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
