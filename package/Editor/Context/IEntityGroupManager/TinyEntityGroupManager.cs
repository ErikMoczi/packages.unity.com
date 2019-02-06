using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    [ContextManager(ContextUsage.Edit | ContextUsage.LiveLink), UsedImplicitly]
    internal class TinyEntityGroupManager : ContextManager, IEntityGroupManagerInternal
    {
        #region Fields
        private TinyEntityGroup.Reference m_ActiveEntityGroup = TinyEntityGroup.Reference.None;
        private readonly List<TinyEntityGroup.Reference> m_LoadedEntityGroups = new List<TinyEntityGroup.Reference>();
        private readonly Dictionary<TinyEntityGroup.Reference, EntityGroupGraph> m_EntityGroupToGraph = new Dictionary<TinyEntityGroup.Reference, EntityGroupGraph>();

        #endregion

        #region Events
        public delegate void EntityGroupEventHandler(TinyEntityGroup.Reference entityGroupRef);
        public delegate void OnEntityGroupsReorderedHandler(ReadOnlyCollection<TinyEntityGroup.Reference> entityGroupRefs);

        public EntityGroupEventHandler OnWillLoadEntityGroup { get; set; }
        public EntityGroupEventHandler OnEntityGroupLoaded { get; set; }
        public EntityGroupEventHandler OnWillUnloadEntityGroup { get; set; }
        public EntityGroupEventHandler OnEntityGroupUnloaded { get; set; }
        public OnEntityGroupsReorderedHandler OnEntityGroupsReordered { get; set; }
        #endregion

        #region Properties
        public TinyEntityGroup.Reference ActiveEntityGroup => m_ActiveEntityGroup;
        public ReadOnlyCollection<TinyEntityGroup.Reference> LoadedEntityGroups => m_LoadedEntityGroups.AsReadOnly();
        public int LoadedEntityGroupCount => m_LoadedEntityGroups.Count;

        private Scene m_Scene;
        public Scene UnityScratchPad
        {
            get
            {
                if (!m_Scene.isLoaded || !m_Scene.IsValid())
                {
                    m_Scene = TinyCache.GetOrGenerateScratchPad();
                }

                return m_Scene;
            }
        }

        private IUndoManager Undo { get; set; }
        #endregion

        #region API
        public TinyEntityGroupManager(TinyContext context)
            :base(context)
        {
            Assert.IsNotNull(context);
        }

        public void LoadEntityGroup(TinyEntityGroup.Reference entityGroupRef)
        {
            LoadEntityGroup(entityGroupRef, -1, true);
        }

        public void LoadEntityGroupAtIndex(TinyEntityGroup.Reference entityGroupRef, int index)
        {
            LoadEntityGroup(entityGroupRef, index, true);
        }

        public void UnloadEntityGroup(TinyEntityGroup.Reference entityGroupRef)
        {
            UnloadEntityGroup(entityGroupRef, true);
        }

        public void UnloadAllEntityGroups()
        {
            var entityGroups = LoadedEntityGroups.ToArray();
            foreach (var entityGroup in entityGroups)
            {
                UnloadEntityGroup(entityGroup, false);
            }
            RebuildWorkspace();
        }

        public void LoadSingleEntityGroup(TinyEntityGroup.Reference entityGroupRef)
        {
            LoadEntityGroup(entityGroupRef);
            UnloadAllEntityGroupsExcept(entityGroupRef);
        }

        public void UnloadAllEntityGroupsExcept(TinyEntityGroup.Reference entityGroupRef)
        {
            var entityGroupList = ListPool<TinyEntityGroup.Reference>.Get();
            try
            {
                foreach (var entityGroup in LoadedEntityGroups)
                {
                    if (!entityGroup.Equals(entityGroupRef))
                    {
                        entityGroupList.Add(entityGroup);
                    }
                }
                foreach(var entityGroup in entityGroupList)
                {
                    UnloadEntityGroup(entityGroup);
                }
            }
            finally
            {
                ListPool<TinyEntityGroup.Reference>.Release(entityGroupList);
            }
        }

        public void SetActiveEntityGroup(TinyEntityGroup.Reference entityGroupRef)
        {
            SetActiveEntityGroup(entityGroupRef, true);
        }

        public void MoveUp(TinyEntityGroup.Reference entityGroupRef)
        {
            var index = LoadedEntityGroups.IndexOf(entityGroupRef);
            if (index < 0 || index == 0)
            {
                return;
            }

            m_LoadedEntityGroups.Swap(index, index - 1);
            RebuildWorkspace();
            SafeCallbacks.Invoke(OnEntityGroupsReordered, LoadedEntityGroups);
        }

        public void MoveDown(TinyEntityGroup.Reference entityGroupRef)
        {
            var index = LoadedEntityGroups.IndexOf(entityGroupRef);
            if (index < 0 || index == LoadedEntityGroupCount - 1)
            {
                return;
            }

            m_LoadedEntityGroups.Swap(index, index + 1);
            RebuildWorkspace();
            SafeCallbacks.Invoke(OnEntityGroupsReordered, LoadedEntityGroups);
        }

        public void CreateNewEntityGroup()
        {
            TinyAction.CreateEntityGroup(Registry, TinyEditorApplication.Project.Module, g =>
            {
                LoadEntityGroup((TinyEntityGroup.Reference) g);
            });
        }

        public void RecreateEntityGroupGraphs()
        {
            foreach (var entityGroupRef in LoadedEntityGroups)
            {
                RecreateEntityGroupGraph(entityGroupRef);
            }
        }

        public void RecreateEntityGroupGraph(TinyEntityGroup.Reference entityGroupRef)
        {
            var entityGroup = entityGroupRef.Dereference(Registry);
            if (null == entityGroup)
            {
                return;
            }
            m_EntityGroupToGraph[entityGroupRef] = EntityGroupGraph.CreateFromEntityGroup(entityGroup, Context);
        }

        public EntityGroupGraph GetActiveSceneGraph()
        {
            return GetSceneGraph(m_ActiveEntityGroup);
        }

        public EntityGroupGraph GetSceneGraph(TinyEntityGroup.Reference entityGroupRef)
        {
            EntityGroupGraph graph = null;
            return m_EntityGroupToGraph.TryGetValue(entityGroupRef, out graph) ? graph : null;
        }

        public void ShowOpenEntityGroupMenu()
        {
            var menu = new GenericMenu();
            var mainModule = TinyEditorApplication.Module;
            var any = false;
            foreach (var module in mainModule.EnumerateDependencies())
            {
                foreach (var entityGroupRef in module.EntityGroups)
                {
                    var entityGroup = entityGroupRef.Dereference(Registry);
                    if (null != entityGroup && !LoadedEntityGroups.Contains(entityGroupRef))
                    {
                        var entityGroupName = module.Name == TinyProject.MainProjectName ? entityGroup.Name : module.Name + "/" + entityGroup.Name;
                        menu.AddItem(new GUIContent(entityGroupName), false, () => LoadEntityGroup(entityGroupRef));
                        any = true;
                    }
                }
            }
            if (!any)
            {
                menu.AddDisabledItem(new GUIContent("All groups are loaded"));
            }
            menu.ShowAsContext();
        }

        [TinyInitializeOnLoad(10)]
        private static void RegisterInitialLoading()
        {
            TinyEditorApplication.OnLoadProject += (project, context) =>
            {
                if (!context.Usage.HasFlag(ContextUsage.LiveLink))
                {
                    context.GetManager<IEntityGroupManagerInternal>().InitialGroupLoading();
                }
            };
        }

        public void InitialGroupLoading()
        {
            var workspace = TinyEditorApplication.EditorContext.Workspace;

            foreach(var entityGroupRef in workspace.OpenedEntityGroups)
            {
                if (null != entityGroupRef.Dereference(Registry))
                {
                    LoadEntityGroup(entityGroupRef, -1, false);
                }
            }

            // The workspace was either empty or the entity groups have been removed.
            // Try to load the last active group and if it fails, load any scene.
            if (LoadedEntityGroupCount == 0)
            {
                if (null != workspace.ActiveEntityGroup.Dereference(Registry))
                {
                    LoadEntityGroup(workspace.ActiveEntityGroup, -1, false);
                }
                else
                {
                    var group = Registry.FindAllByType<TinyEntityGroup>().FirstOrDefault();
                    if (null != group)
                    {
                        LoadEntityGroup((TinyEntityGroup.Reference)group, -1, false);
                    }
                }
            }

            SetActiveEntityGroup(workspace.ActiveEntityGroup, false);

            if (null == workspace.ActiveEntityGroup.Dereference(Registry))
            {
                SetActiveEntityGroup(LoadedEntityGroups.FirstOrDefault(), false);
            }
            
            foreach (var kvp in m_EntityGroupToGraph)
            {
                kvp.Value.ClearChanged();
            }

            RebuildWorkspace();

            EditorSceneManager.sceneOpening += HandleSceneOpening;
            EditorSceneManager.newSceneCreated += HandleSceneCreated;

            FrameSceneBasedOnCamera();
        }

        public override  void Load()
        {
            m_Scene = UnityScratchPad;
            Undo = Context.GetManager<IUndoManager>();
        }

        private static void HandleSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            TinyEditorApplication.SaveChanges();
            TinyEditorApplication.Close();
        }

        private static void HandleSceneOpening(string path, OpenSceneMode mode)
        {
            TinyEditorApplication.SaveChanges();
            TinyEditorApplication.Close();
        }

        public override void Unload()
        {
            EditorSceneManager.sceneOpening -= HandleSceneOpening;
            EditorSceneManager.newSceneCreated -= HandleSceneCreated;

            var scene = UnityScratchPad;
            if (!scene.isLoaded || !scene.IsValid())
            {
                return;
            }

            foreach (var go in scene.GetRootGameObjects())
            {
                UnityEngine.Object.DestroyImmediate(go, false);
            }

            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.SaveScene(scene);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
        #endregion

        #region Implementation
        /// <summary>
        /// Rebuilds the TinyEditorWorkspace based on the current Hierarchy
        /// </summary>
        private void RebuildWorkspace()
        {
            var workspace = TinyEditorApplication.EditorContext.Workspace;

            workspace.ClearOpenedEntityGroups();
            foreach (var entityGroup in LoadedEntityGroups)
            {
                workspace.AddOpenedEntityGroup(entityGroup);
            }
            workspace.ActiveEntityGroup = ActiveEntityGroup;
        }

        private void LoadEntityGroup(TinyEntityGroup.Reference entityGroupRef, int index, bool rebuildWorkspace)
        {
            using (new GameObjectTracker.DontTrackScope())
            {
                var entityGroup = entityGroupRef.Dereference(Registry);
                if (null == entityGroup)
                {
                    Debug.Log($"{TinyConstants.ApplicationName}: Could not load group named '{entityGroupRef.Name}' as the reference could not be resolved.");
                    return;
                }

                if (m_LoadedEntityGroups.Contains(entityGroupRef))
                {
                    Debug.Log($"{TinyConstants.ApplicationName}: Cannot load the group named '{entityGroupRef.Name}'. It is already loaded");
                    return;
                }

                m_Scene = TinyCache.GetOrGenerateScratchPad();
                if (m_Scene.isLoaded && m_Scene.IsValid())
                {
                    SafeCallbacks.Invoke(OnWillLoadEntityGroup, entityGroupRef);
                    if (index >= 0 && index < m_LoadedEntityGroups.Count)
                    {
                        m_LoadedEntityGroups.Insert(index, entityGroupRef);
                    }
                    else
                    {
                        m_LoadedEntityGroups.Add(entityGroupRef);
                    }

                    m_EntityGroupToGraph[entityGroupRef] = EntityGroupGraph.CreateFromEntityGroup(entityGroup, Context);
                }

                if (rebuildWorkspace)
                {
                    RebuildWorkspace();
                }

                SetActiveEntityGroup(entityGroupRef, rebuildWorkspace);

                SafeCallbacks.Invoke(OnEntityGroupLoaded, entityGroupRef);
            }
        }

        private void UnloadEntityGroup(TinyEntityGroup.Reference entityGroupRef, bool rebuildWorkspace)
        {
            using (new GameObjectTracker.DontTrackScope())
            {
                if (!m_LoadedEntityGroups.Contains(entityGroupRef))
                {
                    Debug.Log($"{TinyConstants.ApplicationName}: Cannot unload the group named '{entityGroupRef.Name}'. It is not loaded");
                    return;
                }

                if (!TinyEditorApplication.ShowSaveEntityGroupPrompt(entityGroupRef))
                {
                    // The user has canceled the operation; bail out.
                    return;
                }
                
                // QUICK and dirty hack to support discarding changes
                ForceRelinkViews();

                if (m_EntityGroupToGraph.ContainsKey(entityGroupRef))
                {
                    SafeCallbacks.Invoke(OnWillUnloadEntityGroup, entityGroupRef);
                    m_EntityGroupToGraph[entityGroupRef].Unlink();
                    m_EntityGroupToGraph.Remove(entityGroupRef);
                }

                m_LoadedEntityGroups.Remove(entityGroupRef);

                var entityGroup = entityGroupRef.Dereference(Registry);
                if (null != entityGroup)
                {
                    Undo.SetAsBaseline(entityGroup);
                    Undo.SetAsBaseline(entityGroup.Entities.Deref(entityGroup.Registry));
                }

                if (m_LoadedEntityGroups.Count == 0)
                {
                    SetActiveEntityGroup(TinyEntityGroup.Reference.None, rebuildWorkspace);
                }
                else
                {
                    if (entityGroupRef.Id == ActiveEntityGroup.Id)
                    {
                        SetActiveEntityGroup(m_LoadedEntityGroups[0]);
                    }
                }

                if (rebuildWorkspace)
                {
                    RebuildWorkspace();
                }

                SafeCallbacks.Invoke(OnEntityGroupUnloaded, entityGroupRef);
            }
        }

        private void ForceRelinkViews()
        {
            if (!m_Scene.IsValid())
            {
                return;
            }
            
            foreach (var obj in m_Scene.GetRootGameObjects())
            {
                var views = obj.GetComponentsInChildren<TinyEntityView>();

                foreach (var view in views)
                {
                    var entity = view.EntityRef.Dereference(Registry);
                    if (null != entity)
                    {
                        entity.View = view;
                    }
                }
            }
        }

        public void SetActiveEntityGroup(TinyEntityGroup.Reference entityGroupRef, bool rebuildWorkspace)
        {
            if (m_ActiveEntityGroup.Equals(entityGroupRef))
            {
                return;
            }
            m_ActiveEntityGroup = entityGroupRef;
            if (rebuildWorkspace)
            {
                RebuildWorkspace();
            }
        }

        /// <summary>
        /// This will try to frame the scene view based on the first camera we find.
        /// We will look in the active scene first.
        /// </summary>
        private void FrameSceneBasedOnCamera()
        {
            var transformLocalPositionType = TypeRefs.Core2D.TransformLocalPosition;
            var cameraType = TypeRefs.Core2D.Camera2D;

            if (m_LoadedEntityGroups.Count == 0)
            {
                return;
            }

            var cameraEntity = m_LoadedEntityGroups
                .OrderByDescending(g => g.Equals(m_ActiveEntityGroup)) 
                .Deref(Registry)
                .Entities()
                .WithComponent(cameraType)
                .WithComponent(transformLocalPositionType)
                .FirstOrDefault();

            if (null == cameraEntity)
            {
                FrameSceneBasedOnObjects();
                return;
            }

            var transform = cameraEntity.GetComponent(transformLocalPositionType);
            var camera = cameraEntity.GetComponent(cameraType);
            foreach (SceneView view in SceneView.sceneViews)
            {
                view.pivot = transform.GetProperty<Vector3>("position");
                view.size = camera.GetProperty<float>("halfVerticalSize") * 4.0f;
            }
        }

        /// <summary>
        /// This will try to frame the scene "naively" by computing the average of the position.
        /// </summary>
        private void FrameSceneBasedOnObjects()
        {
            var transformLocalPositionType = TypeRefs.Core2D.TransformLocalPosition;

            var positions = m_LoadedEntityGroups
                .OrderByDescending(g => g.Equals(m_ActiveEntityGroup))
                .Deref(Registry)
                .Entities()
                .GetComponents(transformLocalPositionType)
                .Select(t => t.GetProperty<Vector3>("position")).ToList();

            if (positions.Count == 0)
            {
                return;
            }

            var position = positions.Aggregate(Vector3.zero, (lhs, rhs) => lhs + rhs) / positions.Count;
            var size = Mathf.Max(positions.Max(v => new Vector2(v.x, v.z).magnitude), 5);

            foreach (SceneView view in SceneView.sceneViews)
            {
                view.pivot = position;
                view.size = size * 3.0f;
            }
        }
        #endregion
    }
}
