

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditor.Experimental.UIElements;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Experimental.UIElements;

namespace Unity.Tiny
{
    [AutoRepaintOnTypeChange(typeof(TinyEntity))]
    [AutoRepaintOnTypeChange(typeof(TinyEntityGroup))]
    internal class TinyHierarchyWindow : TinyEditorWindowOverride<EditorWindow>
    {
        #region Static
        private static readonly List<TinyHierarchyWindow> s_ActiveWindows = new List<TinyHierarchyWindow>();

        [TinyInitializeOnLoad]
        private static void RegisterChangeHandlers()
        {
            TinyEventDispatcher<ChangeSource>.AddListener<object>(ChangeSource.DataModel, HandleDataModelChange);
            TinyEventDispatcher<ChangeSource>.AddListener<object>(ChangeSource.SceneGraph, HandleSceneGraphChange);
        }
        #endregion

        #region Properties
        private static HierarchyTree AnyTree
        {
            get
            {
                if (s_ActiveWindows.Count > 0)
                {
                    return s_ActiveWindows[0].m_TreeView;
                }
                return null;
            }
        }

        private static string WindowName { get; set; }
        private static TinyContext Context { get; set; }
        private static TinyProject Project { get; set; }
        private static IRegistry Registry { get; set; }
        private static IEntityGroupManagerInternal EntityGroupManager { get; set; }
        private static ReadOnlyCollection<TinyEntityGroup.Reference> LoadedEntityGroups => EntityGroupManager?.LoadedEntityGroups ?? new List<TinyEntityGroup.Reference>().AsReadOnly();
        private static TinyEntityGroup.Reference ActiveScene => EntityGroupManager?.ActiveEntityGroup ?? TinyEntityGroup.Reference.None;
        private static int LoadedEntityGroupCount => EntityGroupManager?.LoadedEntityGroupCount ?? 0;
        private static TinyUndoManager Undo { get; set; }
        private Rect position => Window.position;
        #endregion

        #region Fields
        [SerializeField]
        private TreeViewState m_TreeState = new TreeViewState();

        private HierarchyTree m_TreeView;
        private Vector2 m_ScrollPosition;
        private SearchField m_Filter;
        private List<TinyEntity> TransferSelection { get; } = new List<TinyEntity>();
        #endregion

        #region Menu Items
        [MenuItem(TinyConstants.MenuItemNames.DuplicateSelection, priority = 250)]
        public static void DuplicateSelection()
        {
            AnyTree.DuplicateSelection();
        }

        [MenuItem(TinyConstants.MenuItemNames.DuplicateSelection, validate = true)]
        public static bool ValidateDuplicateSelection()
        {
            return s_ActiveWindows.Count > 0 && AnyTree?.GetEntitySelection().Count > 0;
        }

        [MenuItem(TinyConstants.MenuItemNames.DeleteSelection, priority = 251)]
        public static void DeleteSelection()
        {
            AnyTree.DeleteSelection();
        }

        [MenuItem(TinyConstants.MenuItemNames.DeleteSelection, validate = true)]
        public static bool ValidateDeleteSelection()
        {
            return s_ActiveWindows.Count > 0 && AnyTree?.GetEntitySelection().Count > 0;
        }
        #endregion

        #region Static API
        public static void RepaintAll()
        {
            foreach (var window in s_ActiveWindows)
            {
                window.m_TreeView.Invalidate();
                window.Repaint();
            }
        }

        public static void SelectOnNextPaint(List<TinyEntity> entities)
        {
            foreach (var window in s_ActiveWindows)
            {
                window.TransferSelection.AddRange(entities);
                window.Repaint();
            }
        }
        #endregion

        #region Unity

        public override void OnEnable()
        {
            WindowName = Window.titleContent.text;
            s_ActiveWindows.Add(this);
            TinyEditorApplication.OnLoadProject += HandleProjectLoaded;
            TinyEditorApplication.OnSaveProject += HandleProjectSaved;
            TinyEditorApplication.OnCloseProject += HandleProjectClosed;
            HandleProjectLoaded(TinyEditorApplication.Project, TinyEditorApplication.EditorContext.Context);

            if (null == m_TreeView)
            {
                m_TreeView = new HierarchyTree(Context, m_TreeState);
            }

            m_Filter = new SearchField();

            var imgui = new IMGUIContainer(OnGUI);
            Root.Add(imgui);
            imgui.StretchToParentSize();
            InvokeOnGUIEnabled = false;
            Window.GetRootVisualContainer().visible = false;
        }

        public override  void OnDisable()
        {
            if (null != m_TreeView)
            {
                foreach (var entityGroup in LoadedEntityGroups)
                {
                    m_TreeView.RemoveEntityGroup(entityGroup);
                }
            }
            s_ActiveWindows.Remove(this);

            TinyEditorApplication.OnLoadProject -= HandleProjectLoaded;
            TinyEditorApplication.OnCloseProject -= HandleProjectClosed;
        }

        private void OnGUI()
        {
            if (null == Context)
            {
                EditorGUILayout.LabelField("No project loaded.");
                return;
            }

            try
            {
                VerifyEntityGroups();

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.ExpandWidth(true)))
                {
                    if (GUILayout.Button("Create", EditorStyles.toolbarDropDown, GUILayout.Width(75)))
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Create EntityGroup"), false, () =>
                        {
                            EntityGroupManager.CreateNewEntityGroup();
                        });

                        menu.AddSeparator("");

                        if (LoadedEntityGroups.Count > 0)
                        {
                            HierarchyContextMenus.PopulateEntityTemplate(menu, m_TreeView.GetRegistryObjectSelection());
                        }

                        menu.ShowAsContext();
                    }

                    if (GUILayout.Button("Load", EditorStyles.toolbarDropDown, GUILayout.Width(75)))
                    {
                        EntityGroupManager.ShowOpenEntityGroupMenu();
                    }

                    // HACK For some reason flexible space does not work here...
                    GUILayout.Space(position.width - 50);
                }

                var searchRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                m_TreeView.FilterString = m_Filter.OnGUI(searchRect, m_TreeView.FilterString);

                if (LoadedEntityGroupCount == 0)
                {
                    EditorGUILayout.LabelField("No EntityGroups are loaded.");
                    return;
                }

                if (TransferSelection.Count > 0)
                {
                    m_TreeView.TransferSelection(TransferSelection);
                    TransferSelection.Clear();
                }

                m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
                {
                    var rect = EditorGUILayout.GetControlRect(false, position.height - 50.0f);
                    rect.width = Screen.width + 1;
                    rect.x = 0;
                    m_TreeView.OnGUI(rect);

                    // Check for a click on empty space.
                    rect.height = rect.height - m_TreeView.totalHeight;
                    rect.y += m_TreeView.totalHeight;
                    if (rect.height > 0 && Event.current.type == EventType.MouseDown &&
                        rect.Contains(Event.current.mousePosition))
                    {
                        Selection.instanceIDs = new int[0];
                    }
                }
                EditorGUILayout.EndScrollView();

                ExecuteCommands();
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception e)
            {
                TinyEditorAnalytics.SendExceptionOnce("Hierarchy.OnGUI", e);
                throw;
            }
        }

        public override void OnSelectionChanged()
        {
            if (null == m_TreeView)
            {
                return;
            }

            var shouldRepaint = m_TreeView.GetSelection()
                .Concat(Selection.instanceIDs)
                .Select(EditorUtility.InstanceIDToObject)
                .OfType<GameObject>()
                .Any();
            
            m_TreeView.SetSelection(Selection.instanceIDs);

            for (var i = Selection.instanceIDs.Length - 1; i >=0; i--)
            {
                if (null == m_TreeView.FindItem(Selection.instanceIDs[i]))
                {
                    continue;
                }
                
                m_TreeView.FrameItem(Selection.instanceIDs[i]);
            }

            if (shouldRepaint)
            {
                Repaint();
            }
        }
        #endregion

        #region Implementation
        private static void HandleDataModelChange(ChangeSource source, object originator)
        {
            var @ref = originator as TinyEntityGroup;
            if (@ref != null)
            {
                EntityGroupManager?.RecreateEntityGroupGraph((TinyEntityGroup.Reference) @ref);
            }
            else
            {
                EntityGroupManager?.RecreateEntityGroupGraphs();
            }

            foreach (var window in s_ActiveWindows)
            {
                window.m_TreeView.Invalidate();
            }
        }

        private static void HandleSceneGraphChange(ChangeSource source, object originator)
        {
            foreach (var entityGroup in LoadedEntityGroups)
            {
                var graph = EntityGroupManager.GetSceneGraph(entityGroup);
                graph.CommitChanges();
            }

            Context.GetManager<BindingsManager>().TransferAll();

            foreach (var window in s_ActiveWindows)
            {
                window.m_TreeView.Invalidate();
            }
        }

        private static void AddToTrees(TinyEntityGroup.Reference entityGroupRef)
        {
            foreach (var window in s_ActiveWindows)
            {
                window.m_TreeView.AddEntityGroup(entityGroupRef);
                window.Repaint();
            }
        }

        private static void RemoveFromTrees(TinyEntityGroup.Reference entityGroupRef)
        {
            foreach (var window in s_ActiveWindows)
            {
                window.m_TreeView.RemoveEntityGroup(entityGroupRef);
                window.Repaint();
            }
        }

        private static void ReorderTrees(ReadOnlyCollection<TinyEntityGroup.Reference> entityGroupRefs)
        {
            foreach (var window in s_ActiveWindows)
            {
                foreach (var entityGroupRef in entityGroupRefs)
                {
                    window.m_TreeView.RemoveEntityGroup(entityGroupRef);
                    window.m_TreeView.AddEntityGroup(entityGroupRef);
                    window.Repaint();
                }
            }
        }

        private static void VerifyEntityGroups()
        {
            var entityGroupRefs = ListPool<TinyEntityGroup.Reference>.Get();
            foreach (var entityGroup in LoadedEntityGroups)
            {
                if (null == entityGroup.Dereference(Registry))
                {
                    entityGroupRefs.Add(entityGroup);
                }
            }

            foreach (var entityGroup in entityGroupRefs)
            {
                EntityGroupManager?.UnloadEntityGroup(entityGroup);
            }

            ListPool<TinyEntityGroup.Reference>.Release(entityGroupRefs);
        }

        private static void ExecuteCommand(Action action, Event evt)
        {
            Assert.IsNotNull(action);
            var execute = evt.type == EventType.ExecuteCommand;
            if (execute)
            {
                action();
            }
            evt.Use();
            GUIUtility.ExitGUI();
        }

        private void ExecuteCommands()
        {
            Event evt = Event.current;

            if (evt.type != EventType.ExecuteCommand && evt.type != EventType.ValidateCommand)
                return;

            switch (evt.commandName)
            {
                case EventCommandNames.SoftDelete:
                case EventCommandNames.Delete:
                    ExecuteCommand(DeleteSelectionImpl, evt);
                    break;
                case EventCommandNames.Duplicate:
                    ExecuteCommand(DuplicateSelectionImpl, evt);
                    break;
                case EventCommandNames.Copy:
                    ExecuteCommand(CopySelection, evt);
                    break;
                case EventCommandNames.Paste:
                    ExecuteCommand(PasteSelection, evt);
                    break;
                default:
                    break;
            }
        }


        private void HandleProjectLoaded(TinyProject project, TinyContext context)
        {
            if (null == project)
            {
                Repaint();
                return;
            }

            Window.titleContent.text = $"Hierarchy - {project.Name}";
            Context = context;
            Project = project;
            Registry = Context.Registry;
            EntityGroupManager = Context.GetManager<IEntityGroupManagerInternal>();
            Undo = Context.GetManager<TinyUndoManager>();

            EntityGroupManager.OnEntityGroupLoaded += AddToTrees;
            EntityGroupManager.OnEntityGroupUnloaded += RemoveFromTrees;
            EntityGroupManager.OnEntityGroupsReordered += ReorderTrees;
            m_TreeView = new HierarchyTree(Context, m_TreeState);

            foreach (var entityGroup in LoadedEntityGroups)
            {
                AddToTrees(entityGroup);
            }
            Undo.OnUndoPerformed += changes => HandleDataModelChange(ChangeSource.DataModel, null);
            Undo.OnRedoPerformed += changes => HandleDataModelChange(ChangeSource.DataModel, null);
        }

        private void HandleProjectSaved(TinyProject project, TinyContext context)
        {
            m_TreeView.Reload();
        }

        private void HandleProjectClosed(TinyProject project, TinyContext context)
        {
            Window.titleContent.text = WindowName;
            m_TreeView.ClearScenes();
        }

        private void DeleteSelectionImpl()
        {
            m_TreeView.DeleteSelection();
        }

        private void DuplicateSelectionImpl()
        {
            m_TreeView.DuplicateSelection();
        }

        private void CopySelection()
        {
            // [MP] @TODO: Implement this
            //AnyTree.CopySelection();
        }

        private void PasteSelection()
        {
            // [MP] @TODO: Implement this
            //AnyTree.PasteSelection();
        }
        #endregion

        /// <summary>
        /// Subset of the Unity's own command names.
        /// </summary>
        internal static class EventCommandNames
        {
            //Some of these strings are also hard-coded on the native side. Change them at your own risk!
            public const string Cut = "Cut";
            public const string Copy = "Copy";
            public const string Paste = "Paste";
            public const string SelectAll = "SelectAll";
            public const string Duplicate = "Duplicate";
            public const string Delete = "Delete";
            public const string SoftDelete = "SoftDelete";
            public const string Find = "Find";
        }

        private void Repaint()
        {
            Window.Repaint();
        }
    }
}

