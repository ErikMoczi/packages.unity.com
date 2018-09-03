using UnityEngine;
using UnityEngine.Experimental.U2D.IK;

namespace UnityEditor.Experimental.U2D.IK
{
    [CustomEditor(typeof(Solver2D))]
    [CanEditMultipleObjects]
    public class Solver2DEditor : Editor
    {
        private static class Contents
        {
            public static readonly GUIContent constrainRotationLabel = new GUIContent("Constrain Rotation", "Set Effector's rotation to Target");
            public static readonly GUIContent restoreDefaultPoseLabel = new GUIContent("Restore Default Pose", "Restore transform's rotation to default value before solving the IK");
            public static readonly GUIContent weightLabel = new GUIContent("Weight", "Blend between Forward and Inverse Kinematics");
        }

        private SerializedProperty m_ConstrainRotationProperty;
        private SerializedProperty m_RestoreDefaultPoseProperty;
        private SerializedProperty m_WeightProperty;

        private void SetupProperties()
        {
            if(m_ConstrainRotationProperty == null || m_RestoreDefaultPoseProperty == null || m_WeightProperty == null)
            {
                m_ConstrainRotationProperty = serializedObject.FindProperty("m_ConstrainRotation");
                m_RestoreDefaultPoseProperty = serializedObject.FindProperty("m_RestoreDefaultPose");
                m_WeightProperty = serializedObject.FindProperty("m_Weight");
            }
        }

        protected void DrawCommonSolverInspector()
        {
            SetupProperties();

            EditorGUILayout.PropertyField(m_ConstrainRotationProperty, Contents.constrainRotationLabel);
            EditorGUILayout.PropertyField(m_RestoreDefaultPoseProperty, Contents.restoreDefaultPoseLabel);
            EditorGUILayout.PropertyField(m_WeightProperty, Contents.weightLabel);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(!EnableCreateEffector());
            DoCreateEffectorButton();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!EnableRestoreDefaultPose());
            DoRestoreDefaultPoseButton();
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private bool EnableRestoreDefaultPose()
        {
            foreach (var l_target in targets)
            {
                var solver = l_target as Solver2D;

                if (!solver.isValid)
                    continue;

                return true;
            }

            return false;
        }

        private bool EnableCreateEffector()
        {
            foreach (var l_target in targets)
            {
                var solver = l_target as Solver2D;

                if (!solver.isValid)
                    continue;

                for(int i = 0; i < solver.chainCount; ++i)
                {
                    var chain = solver.GetChain(i);

                    if(chain.effector == null)
                        return true;
                }
            }

            return false;
        }

        private void DoRestoreDefaultPoseButton()
        {
            if (GUILayout.Button("Restore Default Pose", GUILayout.MaxWidth(150f)))
            {
                foreach (var l_target in targets)
                {
                    var solver = l_target as Solver2D;

                    if (!solver.isValid)
                        continue;

                    IKEditorManager.instance.Record(solver, "Restore Default Pose");

                    for(int i = 0; i < solver.chainCount; ++i)
                    {
                        var chain = solver.GetChain(i);
                        chain.RestoreDefaultPose(solver.constrainRotation);
                        
                        if(chain.effector)
                        {
                            chain.effector.position = chain.target.position;
                            chain.effector.rotation = chain.target.rotation;
                        }
                    }

                    IKEditorManager.instance.UpdateSolverImmediate(solver, true);
                }
            }
        }

        private void DoCreateEffectorButton()
        {
            if (GUILayout.Button("Create Effector", GUILayout.MaxWidth(125f)))
            {
                foreach (var l_target in targets)
                {
                    var solver = l_target as Solver2D;

                    if (!solver.isValid)
                        continue;

                    for(int i = 0; i < solver.chainCount; ++i)
                    {
                        var chain = solver.GetChain(i);
                        
                        if(chain.effector == null)
                        {
                            Undo.RegisterCompleteObjectUndo(solver, "Create Effector");
                            
                            chain.effector = new GameObject(solver.name + "_Effector").transform;
                            chain.effector.SetParent(solver.transform);
                            chain.effector.position = chain.target.position;
                            chain.effector.rotation = chain.target.rotation;

                            Undo.RegisterCreatedObjectUndo(chain.effector.gameObject, "Create Effector");
                        }
                    }
                }
            }
        }
    }
}
