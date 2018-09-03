using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Experimental.U2D.IK;

namespace UnityEditor.Experimental.U2D.IK
{
    [CustomEditor(typeof(LimbSolver2D))]
    [CanEditMultipleObjects]
    public class LimbSolver2DEditor : Solver2DEditor
    {
        private static class Contents
        {
            public static readonly GUIContent targetLabel = new GUIContent("Target", "The last Transform of a hierarchy constrained by the effector");
            public static readonly GUIContent effectorLabel = new GUIContent("Effector", "Transfrom which the target will follow");
            public static readonly GUIContent flipLabel = new GUIContent("Flip", "Select between the two possible solutions of the solver");
        }

        SerializedProperty m_ChainProperty;
        SerializedProperty m_FlipProperty;

        private void OnEnable()
        {
            m_ChainProperty = serializedObject.FindProperty("m_Chain");
            m_FlipProperty = serializedObject.FindProperty("m_Flip");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_ChainProperty.FindPropertyRelative("m_Target"), Contents.targetLabel);
            EditorGUILayout.PropertyField(m_ChainProperty.FindPropertyRelative("m_Effector"), Contents.effectorLabel);
            EditorGUILayout.PropertyField(m_FlipProperty, Contents.flipLabel);

            DrawCommonSolverInspector();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
