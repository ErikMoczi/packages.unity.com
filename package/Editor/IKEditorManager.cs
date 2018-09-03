using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Experimental.U2D.Common;
using UnityEngine.Experimental.U2D.IK;
using UnityEngine.Profiling;

namespace UnityEditor.Experimental.U2D.IK
{
    internal class IKEditorManager : ScriptableSingleton<IKEditorManager>
    {
        private readonly HashSet<IKManager2D> m_DirtyManagers = new HashSet<IKManager2D>();
        private readonly HashSet<Solver2D> m_IKSolvers = new HashSet<Solver2D>();
        private readonly List<IKManager2D> m_IKManagers = new List<IKManager2D>();
        private readonly Dictionary<IKChain2D, Vector3> m_ChainPositionOverrides = new Dictionary<IKChain2D, Vector3>();
        private readonly List<Vector3> m_EffectorPositions = new List<Vector3>();

        private GameObject m_Helper;
        private GameObject[] m_SelectedGameobjects;
        private bool m_IgnorePostProcessModifications = false;
        private HashSet<Transform> m_IgnoreTransformsOnUndo = new HashSet<Transform>();
        internal bool isDraggingDefaultTool { get; private set; }
        internal bool isDragging { get { return IKGizmos.instance.isDragging || isDraggingDefaultTool; } }


        [InitializeOnLoadMethod]
        private static void Setup()
        {
            instance.Create();
        }

        private void Create() {}

        private void OnEnable()
        {
            SetupLateUpdateHelper();
            RegisterCallbacks();
            Initialize();
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
            DestroyLateUpdateHelper();
        }

        private void RegisterCallbacks()
        {
            EditorApplication.hierarchyChanged += Initialize;
            Undo.postprocessModifications += OnPostProcessModifications;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void UnregisterCallbacks()
        {
            EditorApplication.hierarchyChanged -= Initialize;
            Undo.postprocessModifications -= OnPostProcessModifications;
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            m_SelectedGameobjects = null;
        }

        private void SetupLateUpdateHelper()
        {
            if (m_Helper != null)
                return;

            m_Helper = new GameObject("IKEditorManagerHelper");
            m_Helper.hideFlags = HideFlags.HideAndDontSave;
            var helper = m_Helper.AddComponent<IKEditorManagerHelper>();
            helper.onLateUpdate.AddListener(OnLateUpdate);
        }

        private void DestroyLateUpdateHelper()
        {
            if (m_Helper != null)
                GameObject.DestroyImmediate(m_Helper);
        }

        public void Initialize()
        {
            m_IKManagers.Clear();
            m_IKSolvers.Clear();
            m_DirtyManagers.Clear();
            m_ChainPositionOverrides.Clear();

            m_IKManagers.AddRange(GameObject.FindObjectsOfType<IKManager2D>());

            foreach (IKManager2D manager in m_IKManagers)
            {
                foreach (Solver2D solver in manager.solvers)
                {
                    if (solver)
                        m_IKSolvers.Add(solver);
                }
            }
        }

        public IKManager2D FindManager(Solver2D solver)
        {
            foreach (IKManager2D manager in m_IKManagers)
            {
                if (manager == null)
                    continue;

                foreach (Solver2D s in manager.solvers)
                {
                    if (s == null)
                        continue;

                    if (s == solver)
                        return manager;
                }
            }

            return null;
        }

        public void Record(Solver2D solver, string undoName)
        {
            var manager = FindManager(solver);

            DoUndo(manager, undoName, true);
        }

        public void RegisterUndo(Solver2D solver, string undoName)
        {
            var manager = FindManager(solver);

            DoUndo(manager, undoName, false);
        }

        public void Record(IKManager2D manager, string undoName)
        {
            DoUndo(manager, undoName, true);
        }

        public void RegisterUndo(IKManager2D manager, string undoName)
        {
            DoUndo(manager, undoName, false);
        }

        private void DoUndo(IKManager2D manager, string undoName, bool record)
        {
            foreach (var solver in manager.solvers)
            {
                if (solver == null || !solver.isActiveAndEnabled)
                    continue;

                if (!solver.isValid)
                    solver.Initialize();

                if (!solver.isValid)
                    continue;

                for (int i = 0; i < solver.chainCount; ++i)
                {
                    var chain = solver.GetChain(i);

                    if (record)
                    {
                        foreach(var t in chain.transforms)
                        {
                            if(m_IgnoreTransformsOnUndo.Contains(t))
                                continue;

                            Undo.RecordObject(t, undoName);
                        }
                        

                        if(chain.effector && !m_IgnoreTransformsOnUndo.Contains(chain.effector))
                            Undo.RecordObject(chain.effector, undoName);
                    }
                    else
                    {
                        foreach(var t in chain.transforms)
                        {
                            if(m_IgnoreTransformsOnUndo.Contains(t))
                                continue;

                            Undo.RegisterCompleteObjectUndo(t, undoName);
                        }

                        if(chain.effector && !m_IgnoreTransformsOnUndo.Contains(chain.effector))
                            Undo.RegisterCompleteObjectUndo(chain.effector, undoName);
                    }
                }

                m_IgnorePostProcessModifications = true;
            }

            m_IgnoreTransformsOnUndo.Clear();
        }

        public void UpdateManagerImmediate(IKManager2D manager, bool setLocalEulerHints)
        {
            SetManagerDirty(manager);
            UpdateDirtyManagers(setLocalEulerHints);
        }

        public void UpdateSolverImmediate(Solver2D solver, bool setLocalEulerHints)
        {
            SetSolverDirty(solver);
            UpdateDirtyManagers(setLocalEulerHints);
        }

        public void UpdateHierarchyImmediate(Transform hierarchyRoot, bool setLocalEulerHints)
        {
            SetDirtyUnderHierarchy(hierarchyRoot);
            UpdateDirtyManagers(setLocalEulerHints);
        }

        public void SetChainPositionOverride(IKChain2D chain, Vector3 position)
        {
            m_ChainPositionOverrides[chain] = position;
        }

        private bool IsViewToolActive()
        {
            int button = Event.current.button;
            return Tools.current == Tool.View || Event.current.alt || (button == 1) || (button == 2);
        }

        private bool DraggingDefaultTool()
        {
            return GUIUtility.hotControl != 0 && Event.current.type == EventType.MouseDrag && !IsViewToolActive();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (m_SelectedGameobjects == null)
                m_SelectedGameobjects = Selection.gameObjects;

            foreach (Solver2D solver in m_IKSolvers)
                IKGizmos.instance.DoSolverGUI(solver);

            if (!IKGizmos.instance.isDragging && DraggingDefaultTool())
            {
                foreach (var gameObject in m_SelectedGameobjects)
                {
                    if (gameObject != null && gameObject.transform != null)
                        SetDirtySolversAffectedByTransform(gameObject.transform);
                }

                if(!isDraggingDefaultTool)
                {
                    isDraggingDefaultTool = true;
                    Undo.SetCurrentGroupName("IK Update");

                    foreach(var transform in Selection.transforms)
                        m_IgnoreTransformsOnUndo.Add(transform);

                    RegisterUndoForDirtyManagers();
                }
            }

            if(GUIUtility.hotControl == 0)
                isDraggingDefaultTool = false;
        }

        internal void OnLateUpdate()
        {
            if (Application.isPlaying)
                return;

            Profiler.BeginSample("IKEditorManager.OnLateUpdate");

            SetAllManagersDirty();
            UpdateDirtyManagers(false);

            Profiler.EndSample();
        }

        private UndoPropertyModification[] OnPostProcessModifications(UndoPropertyModification[] modifications)
        {
            if(!m_IgnorePostProcessModifications && !isDragging)
            {
                //Prepare transforms that already have an undo modification
                foreach (var modification in modifications)
                {
                    var targetType = modification.currentValue.target.GetType();
                    if (targetType == typeof(Transform) || targetType.IsSubclassOf(typeof(Transform)))
                    {
                        var transform = (Transform)modification.currentValue.target;
                        m_IgnoreTransformsOnUndo.Add(transform);
                    }
                }

                foreach (var modification in modifications)
                {
                    var targetType = modification.currentValue.target.GetType();
                    if (targetType == typeof(Transform) || targetType.IsSubclassOf(typeof(Transform)))
                    {
                        var transform = (Transform)modification.currentValue.target;
                        SetDirtySolversAffectedByTransform(transform);
                        RegisterUndoForDirtyManagers();
                    }
                    if (targetType == typeof(Solver2D) || targetType.IsSubclassOf(typeof(Solver2D)))
                    {
                        var solver = (Solver2D)modification.currentValue.target;
                        SetSolverDirty(solver);
                        RegisterUndoForDirtyManagers();
                    }
                    if (targetType == typeof(IKManager2D))
                    {
                        var dirtyManager = (IKManager2D)modification.currentValue.target;
                        SetManagerDirty(dirtyManager);
                        RegisterUndoForDirtyManagers();
                    }
                }
            }

            m_IgnorePostProcessModifications = false;

            return modifications;
        }

        private void SetSolverDirty(Solver2D solver)
        {
            if (solver && solver.isValid && solver.isActiveAndEnabled)
                SetManagerDirty(FindManager(solver));
        }

        private void SetManagerDirty(IKManager2D manager)
        {
            if (manager && manager.isActiveAndEnabled)
                m_DirtyManagers.Add(manager);
        }

        private void SetAllManagersDirty()
        {
            m_DirtyManagers.Clear();

            foreach (IKManager2D manager in m_IKManagers)
                SetManagerDirty(manager);
        }

        private void SetDirtyUnderHierarchy(Transform hierarchyRoot)
        {
            if (hierarchyRoot == null)
                return;

            foreach (Solver2D solver in m_IKSolvers)
            {
                if (solver.isValid)
                {
                    for (int i = 0; i < solver.chainCount; ++i)
                    {
                        var chain = solver.GetChain(i);

                        if (chain.effector && (hierarchyRoot == chain.effector
                                               || (chain.effector && IKUtility.IsDescendentOf(chain.effector, hierarchyRoot))
                                               || IKUtility.IsDescendentOf(chain.target, hierarchyRoot)))
                        {
                            SetSolverDirty(solver);
                            break;
                        }
                    }
                }
            }
        }

        private void SetDirtySolversAffectedByTransform(Transform transform)
        {
            foreach (Solver2D solver in m_IKSolvers)
            {
                if (solver.isValid)
                {
                    for (int i = 0; i < solver.chainCount; ++i)
                    {
                        var chain = solver.GetChain(i);

                        if (!(chain.effector && IKUtility.IsDescendentOf(chain.effector, transform) && IKUtility.IsDescendentOf(chain.rootTransform, transform)) &&
                            (chain.effector == transform || (chain.effector && IKUtility.IsDescendentOf(chain.effector, transform)) || IKUtility.IsDescendentOf(chain.target, transform)))
                        {
                            SetSolverDirty(solver);
                            break;
                        }
                    }
                }
            }
        }

        private void RegisterUndoForDirtyManagers()
        {
            foreach (var manager in m_DirtyManagers)
                RegisterUndo(manager, Undo.GetCurrentGroupName());
        }

        private void UpdateDirtyManagers(bool setLocalEulerHints)
        {
            foreach (var manager in m_DirtyManagers)
            {
                if (manager == null || !manager.isActiveAndEnabled)
                    continue;

                foreach (var solver in manager.solvers)
                {
                    if (solver == null || !solver.isActiveAndEnabled)
                        continue;

                    if (!solver.isValid)
                        solver.Initialize();

                    if (!solver.isValid)
                        continue;

                    if(solver.allChainsHaveEffectors)
                        solver.UpdateIK(manager.weight);
                    else if(PrepareEffectorOverrides(solver))
                        solver.UpdateIK(m_EffectorPositions, manager.weight);

                    if (setLocalEulerHints)
                    {
                        for (int i = 0; i < solver.chainCount; ++i)
                        {
                            var chain = solver.GetChain(i);
                            InternalEngineBridge.SetLocalEulerHint(chain.rootTransform);
                        }
                    }
                }
            }

            m_DirtyManagers.Clear();
            m_ChainPositionOverrides.Clear();
        }

        private bool PrepareEffectorOverrides(Solver2D solver)
        {
            m_EffectorPositions.Clear();

            for (int i = 0; i < solver.chainCount; ++i)
            {
                var chain = solver.GetChain(i);

                Vector3 positionOverride;
                if (!m_ChainPositionOverrides.TryGetValue(chain, out positionOverride))
                {
                    m_EffectorPositions.Clear();
                    return false;
                }

                m_EffectorPositions.Add(positionOverride);
            }

            return true;
        }
    }
}
