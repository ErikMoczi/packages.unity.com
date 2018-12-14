using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.U2D.IK;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace UnityEditor.Experimental.U2D.IK
{
    [CustomEditor(typeof(IKManager2D))]
    [CanEditMultipleObjects]
    public class IKManager2DEditor : Editor
    {
        private static class Contents
        {
            public static readonly GUIContent findAllSolversLabel = new GUIContent("Find Solvers", "Find all applicable solvers handled by this manager");
            public static readonly GUIContent weightLabel = new GUIContent("Weight", "Blend between Forward and Inverse Kinematics");
            public static readonly string listHeaderLabel = "IK Solvers";
            public static readonly string createSolverString = "Create Solver";
            public static readonly string restoreDefaultPoseString = "Restore Default Pose";
        }

        private ReorderableList m_ReorderableList;
        private Solver2D m_SelectedSolver;
        private Editor m_SelectedSolverEditor;

        SerializedProperty m_SolversProperty;
        SerializedProperty m_WeightProperty;
        List<Type> m_SolverTypes;
        IKManager2D m_Manager;

        private void OnEnable()
        {
            m_Manager = target as IKManager2D;
            m_SolverTypes = GetDerivedTypes<Solver2D>();
            m_SolversProperty = serializedObject.FindProperty("m_Solvers");
            m_WeightProperty = serializedObject.FindProperty("m_Weight");
            SetupReordeableList();
        }

        public void SetupReordeableList()
        {
            m_ReorderableList = new ReorderableList(serializedObject, m_SolversProperty, true, true, true, true);
            m_ReorderableList.drawHeaderCallback = (Rect rect) =>
                {
                    GUI.Label(rect, Contents.listHeaderLabel);
                };
            m_ReorderableList.elementHeightCallback = (int index) =>
                {
                    return EditorGUIUtility.singleLineHeight + 6;
                };
            m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isactive, bool isfocused) =>
                {
                    rect.y += 2f;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    SerializedProperty element = m_SolversProperty.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, element, GUIContent.none);
                };
            m_ReorderableList.onAddCallback = (ReorderableList list) =>
                {
                    var menu = new GenericMenu();

                    foreach (Type type in m_SolverTypes)
                    {
                        Solver2DMenuAttribute attribute = Attribute.GetCustomAttribute(type, typeof(Solver2DMenuAttribute)) as Solver2DMenuAttribute;

                        if (attribute != null)
                            menu.AddItem(new GUIContent(attribute.menuPath), false, OnSelectMenu, type);
                        else
                            menu.AddItem(new GUIContent(type.Name), false, OnSelectMenu, type);
                    }

                    menu.ShowAsContext();
                };
            m_ReorderableList.onRemoveCallback = (ReorderableList list) =>
                {
                    Solver2D solver = m_Manager.solvers[list.index];
                    if (solver)
                    {
                        Undo.RegisterCompleteObjectUndo(m_Manager, Undo.GetCurrentGroupName());

                        m_Manager.RemoveSolver(solver);

                        GameObject solverGO = solver.gameObject;

                        if (solverGO.transform.childCount == 0)
                            Undo.DestroyObjectImmediate(solverGO);
                        else
                            Undo.DestroyObjectImmediate(solver);

                        EditorUtility.SetDirty(m_Manager);
                    }
                    else
                    {
                        ReorderableList.defaultBehaviours.DoRemoveButton(list);
                    }
                };
        }

        private void OnSelectMenu(object param)
        {
            Type solverType = param as Type;

            GameObject solverGO = new GameObject(GameObjectUtility.GetUniqueNameForSibling(m_Manager.transform, "New " + solverType.Name));
            solverGO.transform.SetParent(m_Manager.transform);
            solverGO.transform.localPosition = Vector3.zero;
            solverGO.transform.rotation = Quaternion.identity;
            solverGO.transform.localScale = Vector3.one;

            Solver2D solver = solverGO.AddComponent(solverType) as Solver2D;

            Undo.RegisterCreatedObjectUndo(solverGO, Contents.createSolverString);
            Undo.RegisterCompleteObjectUndo(m_Manager, Contents.createSolverString);
            m_Manager.AddSolver(solver);
            EditorUtility.SetDirty(m_Manager);

            Selection.activeGameObject = solverGO;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            m_ReorderableList.DoLayoutList();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_WeightProperty, Contents.weightLabel);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            DoRestoreDefaultPoseButton();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void DoRestoreDefaultPoseButton()
        {
            if (GUILayout.Button(Contents.restoreDefaultPoseString, GUILayout.MaxWidth(150f)))
            {
                foreach (var l_target in targets)
                {
                    var manager = l_target as IKManager2D;

                    IKEditorManager.instance.Record(manager, Contents.restoreDefaultPoseString);

                    foreach(var solver in manager.solvers)
                    {
                        for(int i = 0; i < solver.chainCount; ++i)
                        {
                            var chain = solver.GetChain(i);
                            chain.RestoreDefaultPose(solver.constrainRotation);
                            
                            if(chain.target)
                            {
                                chain.target.position = chain.effector.position;
                                chain.target.rotation = chain.effector.rotation;
                            }
                        }
                    }

                    IKEditorManager.instance.UpdateManagerImmediate(manager, true);
                }
            }
        }

        public List<Type> GetDerivedTypes<T>() where T : class
        {
            List<Type> types = Assembly.GetAssembly(typeof(T)).GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T))).ToList();
            return types;
        }
    }
}
