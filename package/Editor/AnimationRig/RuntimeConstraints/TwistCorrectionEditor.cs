using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEditorInternal;

namespace UnityEditor.Animations.Rigging
{
    [CustomEditor(typeof(TwistCorrection))]
    public class TwistCorrectionEditor : Editor
    {
        static readonly GUIContent k_TwistNodesLabel = new GUIContent("Twist Nodes");

        SerializedProperty m_Weight;
        SerializedProperty m_Source;
        SerializedProperty m_TwistAxis;
        SerializedProperty m_TwistNodes;

        SerializedProperty m_TwistNodesToggle;
        ReorderableList m_ReorderableList;

        void OnEnable()
        {
            m_Weight = serializedObject.FindProperty("m_Weight");
            m_TwistNodesToggle = serializedObject.FindProperty("m_TwistNodesGUIToggle");

            var data = serializedObject.FindProperty("m_Data");
            m_Source = data.FindPropertyRelative("m_Source");
            m_TwistAxis = data.FindPropertyRelative("m_TwistAxis");
            m_TwistNodes = data.FindPropertyRelative("m_TwistNodes");

            m_ReorderableList = ReorderableListHelper.Create(serializedObject, m_TwistNodes, false);
            if (m_ReorderableList.count == 0)
                ((TwistCorrection)serializedObject.targetObject).data.twistNodes.Add(new WeightedJobTransform(null, false, 0.5f));

            m_ReorderableList.onAddCallback = (ReorderableList list) =>
            {
                ((TwistCorrection)serializedObject.targetObject).data.twistNodes.Add(WeightedJobTransform.defaultNoSync(0.5f));
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Weight);
            EditorGUILayout.PropertyField(m_Source);
            EditorGUILayout.PropertyField(m_TwistAxis);

            m_TwistNodesToggle.boolValue = EditorGUILayout.Foldout(m_TwistNodesToggle.boolValue, k_TwistNodesLabel);
            if (m_TwistNodesToggle.boolValue)
            {
                EditorGUI.indentLevel++;
                m_ReorderableList.DoLayoutList();
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}