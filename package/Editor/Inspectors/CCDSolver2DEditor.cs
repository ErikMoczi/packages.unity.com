using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Experimental.U2D.IK;

namespace UnityEditor.Experimental.U2D.IK
{
    [CustomEditor(typeof(CCDSolver2D))]
    [CanEditMultipleObjects]
    public class CCDSolver2DEditor : Solver2DEditor
    {
        private static class Contents
        {
            public static readonly GUIContent targetLabel = new GUIContent("Target", "The last Transform of a hierarchy constrained by the effector");
            public static readonly GUIContent effectorLabel = new GUIContent("Effector", "Transform which the target will follow");
            public static readonly GUIContent chainLengthLabel = new GUIContent("Chain Length", "Number of Transforms handled by the IK");
            public static readonly GUIContent iterationsLabel = new GUIContent("Iterations", "Number of iterations the IK solver is run per frame");
            public static readonly GUIContent toleranceLabel = new GUIContent("Tolerance", "How close the target is to the goal to be considered as successful");
            public static readonly GUIContent velocityLabel = new GUIContent("Velocity", "How fast the chain elements rotate to the effector per iteration");
        }

        SerializedProperty m_TargetProperty;
        SerializedProperty m_EffectorProperty;
        SerializedProperty m_TransformCountProperty;

        SerializedProperty m_IterationsProperty;
        SerializedProperty m_ToleranceProperty;
        SerializedProperty m_VelocityProperty;
        CCDSolver2D m_Solver;

        private void OnEnable()
        {
            m_Solver = target as CCDSolver2D;
            var chainProperty = serializedObject.FindProperty("m_Chain");
            m_TargetProperty = chainProperty.FindPropertyRelative("m_Target");
            m_EffectorProperty = chainProperty.FindPropertyRelative("m_Effector");
            m_TransformCountProperty = chainProperty.FindPropertyRelative("m_TransformCount");
            m_IterationsProperty = serializedObject.FindProperty("m_Iterations");
            m_ToleranceProperty = serializedObject.FindProperty("m_Tolerance");
            m_VelocityProperty = serializedObject.FindProperty("m_Velocity");
        }

        public override void OnInspectorGUI()
        {
            IKChain2D chain = m_Solver.GetChain(0);

            serializedObject.Update();
            EditorGUILayout.PropertyField(m_TargetProperty, Contents.targetLabel);
            EditorGUILayout.PropertyField(m_EffectorProperty, Contents.effectorLabel);
            EditorGUILayout.IntSlider(m_TransformCountProperty, 0, IKUtility.GetMaxChainCount(chain), Contents.chainLengthLabel);
            EditorGUILayout.PropertyField(m_IterationsProperty, Contents.iterationsLabel);
            EditorGUILayout.PropertyField(m_ToleranceProperty, Contents.toleranceLabel);
            EditorGUILayout.PropertyField(m_VelocityProperty, Contents.velocityLabel);

            DrawCommonSolverInspector();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
