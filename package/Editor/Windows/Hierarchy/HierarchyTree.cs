using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Tiny.Runtime.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;
using DropOnItemAction = System.Action<Unity.Tiny.HierarchyTreeItemBase, System.Collections.Generic.List<Unity.Tiny.ISceneGraphNode>>;
using DropBetweenAction = System.Action<Unity.Tiny.HierarchyTreeItemBase, System.Collections.Generic.List<Unity.Tiny.ISceneGraphNode>, int>;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    internal class HierarchyTree : TreeView
    {
        [Serializable]
        internal class State : TreeViewState
        {
            public string CurrentSearchFilter;
            public List<string> ExpandedGuids = new List<string>();
        }
        
        private class DragDropObject : ScriptableObject
        {
            public List<ISceneGraphNode> Nodes { get; } = new List<ISceneGraphNode>();
        }
        
        #region Fields

        private readonly TinyContext m_Context;
        private readonly List<TinyEntityGroup.Reference> m_EntityGroups;
        private string m_FilterString = string.Empty;
        private readonly Dictionary<System.Type, DropOnItemAction> m_DroppedOnMethod;
        private readonly Dictionary<System.Type, DropBetweenAction> m_DroppedBetweenMethod;

        private static readonly Texture2D s_EntityIcon;
        private static readonly Texture2D s_PrefabIcon;
        private static readonly Texture2D s_PrefabOverlayAddedIcon;
        
        #endregion

        #region Properties

        internal TinyContext Context => m_Context;
        internal IRegistry Registry => m_Context.Registry;
        
        internal IEntityGroupManagerInternal EntityGroupManager { get; }
        private TinyEntityGroup.Reference ActiveScene => EntityGroupManager?.ActiveEntityGroup ?? TinyEntityGroup.Reference.None;

        private HierarchyFilter m_HierarchyFilter = FilterUtility.CreateFilter(string.Empty);
        
        public string FilterString
        {
            get
            {
                return m_FilterString;
            }
            set
            {
                if (m_FilterString != value)
                {
                    m_FilterString = value;
                    m_HierarchyFilter = FilterUtility.CreateFilter(m_FilterString);
                    Invalidate();
                }
            }
        }

        private bool ShouldReload { get; set; }
        private bool ContextClickedWithId { get; set; }

        private IList<int> IdsToExpand { get; set; }
        #endregion

        static HierarchyTree()
        {
            s_EntityIcon = TinyIcons.Entity;
            s_PrefabIcon = TinyIcons.Prefab;
            s_PrefabOverlayAddedIcon = EditorGUIUtility.IconContent("PrefabOverlayAdded Icon").image as Texture2D;
        }

        public HierarchyTree(TinyContext context, TreeViewState treeViewState)
        : base(treeViewState)
        {
            m_Context = context;
            EntityGroupManager = m_Context.GetManager<IEntityGroupManagerInternal>();
            m_EntityGroups = new List<TinyEntityGroup.Reference>();

            m_DroppedOnMethod = new Dictionary<System.Type, DropOnItemAction>
            {
                { typeof(EntityGroupTreeItem), DropUponSceneItem },
                { typeof(EntityTreeItem), DropUponEntityItem },
            };

            m_DroppedBetweenMethod = new Dictionary<System.Type, DropBetweenAction>
            {
                { typeof(HierarchyTreeItemBase), DropBetweenEntityGroupItems },
                { typeof(EntityGroupTreeItem), DropBetweenRootEntities },
                { typeof(EntityTreeItem), DropBetweenChildrenEntities },
            };
            Invalidate();
            Reload();
        }

        #region API

        public void TransferSelection(List<TinyEntity> entities)
        {
            Selection.instanceIDs = entities.Select(e => e.View.gameObject.GetInstanceID()).ToArray();
            IdsToExpand = entities
                .Select(e => e.View.transform.parent)
                .Where(p => p && null != p)
                .Select(p => p.gameObject.GetInstanceID()).ToArray();
        }

        internal IEnumerable<HierarchyTreeItemBase> GetExpandedItems()
        {
            return GetExpanded()
                .Select(FindItem)
                .OfType<HierarchyTreeItemBase>();
        }

        internal void TransferExpandedState(IEnumerable<TinyId> ids)
        {
            foreach (var id in ids)
            {
                var baseObject = Registry.FindById(id);
                if (null == baseObject)
                {
                    continue;
                }
                switch (baseObject)
                {
                    case TinyPrefabInstance prefab:
                    {
                        var parent = prefab.Parent.Dereference(Registry);
                        if (null == parent)
                        {
                            break;
                        }

                        var view = parent.View;
                        if (null == view)
                        {
                            break;
                        }

                        SetExpanded(view.gameObject.GetInstanceID(), true);
                        break;
                    }
                    case TinyEntity entity:
                    {
                        var view = entity.View;
                        if (null == view)
                        {
                            break;
                        }

                        SetExpanded(view.gameObject.GetInstanceID(), true);
                        break;
                    }
                    case TinyEntityGroup group:
                    {
                        var path = Persistence.GetAssetPath(group);
                        if (string.IsNullOrEmpty(path))
                        {
                            break;
                        }
                        var utGroup = AssetDatabase.LoadAssetAtPath<UTEntityGroup>(path);
                        if (null == utGroup)
                        {
                            break;
                        }
                        SetExpanded(utGroup.GetInstanceID(), true);
                        break;
                    }
                }
            }
        }

        public void AddEntityGroup(TinyEntityGroup.Reference entityGroupRef)
        {
            if (!m_EntityGroups.Contains(entityGroupRef))
            {
                m_EntityGroups.Add(entityGroupRef);
                Invalidate();
            }
        }

        public void RemoveEntityGroup(TinyEntityGroup.Reference entityGroupRef)
        {
            if (m_EntityGroups.Remove(entityGroupRef))
            {
                Invalidate();
            }
        }

        public void ClearScenes()
        {
            m_EntityGroups.Clear();
            Invalidate();
        }

        public List<EntityNode> GetEntitySelection()
        {
            return GetSelection()
                .Select(id => FindItem(id, rootItem))
                .OfType<EntityTreeItem>()
                .Select(item => item.Value)
                .ToList();
        }

        public IRegistryObject[] GetRegistryObjectSelection()
        {
            var items = GetSelection()
                .Select(id => FindItem(id, rootItem)).ToList();
            return (items.OfType<EntityTreeItem>().Select(i => i.Value.EntityRef).Deref(Registry).Cast<IRegistryObject>()
                .Concat(items.OfType<EntityGroupTreeItem>().Select(i => i.Value.EntityGroup))).ToArray();
        }

        private int GetInstanceId(EntityNode node)
        {
            return node.EntityRef.Dereference(Registry).View.gameObject.GetInstanceID();
        }

        public void DuplicateSelection()
        {
            using (new GameObjectTracker.DontTrackScope())
            {
                var selection = new List<int>();
                var expanded = new List<int>();
                foreach (var group in GetEntitySelection().GroupBy(n => n.Graph))
                {
                    var nodes = group.Key.Duplicate(group.Cast<ISceneGraphNode>().ToList()).Cast<EntityNode>();
                    foreach (var node in nodes)
                    {
                        selection.Add(GetInstanceId(node));

                        if (node.Parent is EntityNode parentNode)
                        {
                            expanded.Add(GetInstanceId(parentNode));
                        }
                    }
                }

                Selection.instanceIDs = selection.ToArray();
                IdsToExpand = expanded;
                TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.SceneGraph);
            }
        }

        public void DeleteSelection()
        {
            var entities = GetEntitySelection();
            
            // Validation
            foreach (var node in entities)
            {
                var entity = node.EntityRef.Dereference(m_Context.Registry);

                if (null == entity.Instance)
                {
                    // Standalone entity, we can safely delete
                    continue;
                }

                if (node is PrefabInstanceNode)
                {
                    // This is the root instance, we can safely delete
                    continue;
                }

                EditorGUIUtilityBridge.DisplayDialog("Cannot restructure Prefab instance", "Children of a Prefab instance cannot be deleted or moved.", "Close");
                return;
            }

            foreach (var node in entities)
            {
                node.Graph.Delete(node);
            }

            TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.SceneGraph);
        }

        public void Invalidate()
        {
            ShouldReload = true;
        }

        public void Rename(TinyEntity.Reference entity)
        {
            var item = FindItem(entity.Dereference(m_Context.Registry).View.gameObject.GetInstanceID(), rootItem);
            BeginRename(item);
        }

        public TreeViewItem FindItem(int id)
        {
            return TreeViewUtilityBridge.FindItem(id, rootItem);
        }
        #endregion
        
        #region TreeView
        protected override TreeViewItem BuildRoot()
        {
            var nextId = int.MaxValue;
            var root = new HierarchyTreeItemBase() { id = nextId--, depth = -1, displayName = "Root" };

            if (null == m_EntityGroups || m_EntityGroups.Count == 0)
            {
                var item = new TreeViewItem { id = nextId--, depth = 0, displayName = "No group Opened" };
                root.AddChild(item);
                return root;
            }

            foreach (var entityGroupRef in m_EntityGroups)
            {
                var graph = EntityGroupManager.GetSceneGraph(entityGroupRef);
                if (null == graph)
                {
                    RemoveEntityGroup(entityGroupRef);
                    continue;
                }

                var item = new EntityGroupTreeItem { depth = 0, Value = graph };
                root.AddChild(item);

                foreach (var node in graph.Roots)
                {
                    BuildFromNode(node, item);
                }
            }

            if (state is State s && s.expandedIDs.Count > 0)
            {
                TransferExpandedState(s.ExpandedGuids.Select(id => new TinyId(id)));
                s.ExpandedGuids.Clear();
            }
            ShouldReload = false;
            return root;
        }

        public override void OnGUI(Rect rect)
        {
            if (ShouldReload)
            {
                Reload();
            }

            if (null != IdsToExpand)
            {
                ForceExpanded(IdsToExpand);
                IdsToExpand = null;
            }

            base.OnGUI(rect);
        }

        protected override void KeyEvent()
        {
            base.KeyEvent();
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete)
            {
                DeleteSelection();
                Event.current.Use();
            }
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            var baseHeight = base.GetCustomRowHeight(row, item);
            if (item is EntityGroupTreeItem)
            {
                return baseHeight + 4.0f;
            }
            return baseHeight;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var itemRect = args.rowRect;
            switch (args.item)
            {
                case EntityGroupTreeItem graph:
                    DrawItem(itemRect, graph, args);
                    return;
                case EntityTreeItem treeItem:
                    DrawItem(itemRect, treeItem, args);
                    return;
                case PrefabInstanceTreeItem prefabItem:
                    DrawItem(itemRect, prefabItem, args);
                    return;
            }

            base.RowGUI(args);
        }

        protected override void ContextClickedItem(int id)
        {
            var item = FindItem(id, rootItem);

            switch (item)
            {
                case EntityGroupTreeItem graph:
                    this.ShowEntityGroupContextMenu(graph.Value.EntityGroupRef);
                    break;
                case EntityTreeItem treeItem:
                    this.ShowEntityContextMenu(treeItem.Node, treeItem.EntityRef);
                    break;
            }

            ContextClickedWithId = true;
        }

        protected override void ContextClicked()
        {
            if (!ContextClickedWithId)
            {
                this.ShowEntityGroupContextMenu(TinyEntityGroup.Reference.None);
            }
            ContextClickedWithId = false;
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem);
            if (item is ISceneGraphNodeTreeItem && null != SceneView.lastActiveSceneView)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            Selection.instanceIDs = selectedIds.ToArray();
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return item is ISceneGraphNodeTreeItem;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (!args.acceptedRename || string.IsNullOrEmpty(args.newName))
            {
                return;
            }
            
            var item = FindItem(args.itemID, rootItem);

            switch (item)
            {
                case EntityTreeItem entityTreeItem:
                {
                    var entityRef = entityTreeItem.EntityRef;
                    var entity = entityRef.Dereference(m_Context.Registry);
                    entity.Name = args.newName;
                    entity.View.gameObject.name = entity.Name;
                    entityTreeItem.EntityRef = (TinyEntity.Reference) entity;
                    item.displayName = args.newName;
                }
                    break;
                    
                case PrefabInstanceTreeItem hierarchyPrefabInstance:
                {
                    var node = hierarchyPrefabInstance.Value;
                    var prefabInstance = node.PrefabInstanceRef.Dereference(m_Context.Registry);
                    prefabInstance.Name = args.newName;
                    item.displayName = args.newName;
                }
                    break;
            }
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return args.draggedItem is ISceneGraphNodeTreeItem;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();

            var sortedDraggedIDs = SortItemIDsInRowOrder(args.draggedItemIDs);

            var objList = new List<GameObject>(sortedDraggedIDs.Count);
            foreach (var id in sortedDraggedIDs)
            {
                if (FindItem(id, rootItem) is EntityTreeItem item)
                {
                    objList.Add(item.EntityRef.Dereference(m_Context.Registry).View.gameObject);
                }
            }

            DragAndDrop.paths = new string[0];
            DragAndDrop.objectReferences = objList.Cast<Object>().ToArray();
            DragAndDrop.StartDrag("Multiple");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            var draggedObjects = DragAndDrop.objectReferences;

            var nodes = draggedObjects
                .Select(d => FindItem(d.GetInstanceID(), rootItem))
                .OfType<ISceneGraphNodeTreeItem>()
                .Select(item => item.Node)
                .ToList();

            if (!args.performDrop)
            {
                return DragAndDropVisualMode.Move;
            }
            
            if (args.parentItem is PrefabInstanceTreeItem)
            {
                // Disallow dropping anything on a prefab instance node
                return DragAndDropVisualMode.Rejected;
            }
                
            if (HandleSingleObjectDrop<Sprite>(args, HandleResourceDropped) == DragAndDropVisualMode.Link)
            {
                return DragAndDropVisualMode.Link;
            }
            if (HandleSingleObjectDrop<Texture2D>(args, HandleResourceDropped) == DragAndDropVisualMode.Link)
            {
                return DragAndDropVisualMode.Link;
            }                
            if (HandleSingleObjectDrop<TMPro.TMP_FontAsset>(args, HandleResourceDropped) == DragAndDropVisualMode.Link)
            {
                return DragAndDropVisualMode.Link;
            }
            if (HandleSingleObjectDrop<AudioClip>(args, HandleResourceDropped) == DragAndDropVisualMode.Link)
            {
                return DragAndDropVisualMode.Link;
            }
            if (HandleSingleObjectDrop<AnimationClip>(args, HandleResourceDropped) == DragAndDropVisualMode.Link)
            {
                return DragAndDropVisualMode.Link;
            }
            if (HandleSingleObjectDrop<UTPrefab>(args, HandlePrefabDropped) == DragAndDropVisualMode.Link)
            {
                return DragAndDropVisualMode.Link;
            }
                
            // @TODO This should be baked into the data model and NOT in the UI code...
            // Users should NOT be able to manipulate the hierarchy of a prefab instance
            if (IsPrefabRestructure(args, nodes))
            {
                EditorGUIUtilityBridge.DisplayDialog("Cannot restructure Prefab instance", "Children of a Prefab instance cannot be deleted or moved.", "Close");
                return DragAndDropVisualMode.Rejected;
            }

            // @TODO This should be baked into the data model and NOT in the UI code...
            // Disable nesting of prefabs until we are ready to support them
            if (IsNestingPrefab(args, nodes))
            {
                // EditorUtility.DisplayDialog("Cannot nest Prefab instances", "Prefab nesting is not supported yet!.", "Close");
                // return DragAndDropVisualMode.Rejected;
            }

            switch (args.dragAndDropPosition) {
                case DragAndDropPosition.UponItem:     return HandleDropUponItem(nodes, (HierarchyTreeItemBase)args.parentItem);
                case DragAndDropPosition.BetweenItems: return HandleDropBetweenItems(nodes, (HierarchyTreeItemBase)args.parentItem, args.insertAtIndex);
                case DragAndDropPosition.OutsideItems: return DropOutsideOfItems(nodes);
                default:                               return DragAndDropVisualMode.Rejected;
            }
        }

        private bool IsPrefabRestructure(DragAndDropArgs args, IEnumerable<ISceneGraphNode> nodes)
        {
            return nodes.OfType<EntityNode>().Any(n =>
            {
                var entity = n.EntityRef.Dereference(Registry);
                return entity.HasEntityInstanceComponent() && !PrefabTransformUtility.IsPrefabInstanceRootTransform(entity);
            });
        }

        private bool IsNestingPrefab(DragAndDropArgs args, IEnumerable<ISceneGraphNode> nodes)
        {
            if (!(args.parentItem is EntityTreeItem target))
            {
                return false;
            }

            return target.EntityRef.Dereference(Registry).HasEntityInstanceComponent() && nodes.OfType<PrefabInstanceNode>().Any();
        }
        #endregion

        #region Implementation
        private void DrawItem(Rect rect, EntityGroupTreeItem item, RowGUIArgs args)
        {
            if (null == item)
            {
                return;
            }

            var indent = GetContentIndent(item);
            if (!args.selected)
            {
                var headerRect = rect;
                headerRect.width += 1;

                var topLine = headerRect;
                topLine.height = 1;
                TinyGUI.BackgroundColor(topLine, TinyColors.Hierarchy.SceneSeparator);

                headerRect.y += 2;
                TinyGUI.BackgroundColor(headerRect, TinyColors.Hierarchy.SceneItem);
                
                var bottomLine = headerRect;
                bottomLine.y += bottomLine.height - 1;
                bottomLine.height = 1;
                TinyGUI.BackgroundColor(bottomLine, TinyColors.Hierarchy.SceneSeparator);
            }


            rect.y += 2;
            rect.x = indent;
            rect.width -= indent;

            var iconRect = rect;
            iconRect.width = 20;

            var image = ActiveScene.Equals(item.Value.EntityGroupRef) ? TinyIcons.EntityGroupActive : TinyIcons.EntityGroup;
            EditorGUI.LabelField(iconRect, new GUIContent { image = image });

            rect.x += 20;
            rect.width -= 40;

            var style = ActiveScene.Equals(item.Value.EntityGroupRef) ? EditorStyles.boldLabel : GUI.skin.label;
            EditorGUI.LabelField(rect, item.displayName, style);
            rect.x += rect.width;
            rect.width = 16;

            rect.y = rect.center.y - 5.5f;
            rect.height = 11;

            if (GUI.Button(rect, GUIContent.none, TinyStyles.PaneOptionStyle))
            {
                this.ShowEntityGroupContextMenu(item.Value.EntityGroupRef);
            }
        }

        private void DrawItem(Rect rect, EntityTreeItem item, RowGUIArgs args)
        {
            Color32 color;

            var entity = item.EntityRef.Dereference(Registry);

            if (entity.HasEntityInstanceComponent())
            {
                color = TinyColors.Hierarchy.Prefab;
            }
            else if (entity.HasEntityInstanceComponent(false))
            {
                color = Color.red;
            }
            else
            {
                color = Color.white;
            }

            GUI.contentColor = color;
            
            using (new TinyGUIColorScope(item.Node.EnabledInHierarchy ? Color.white : TinyColors.Hierarchy.Disabled))
            {
                base.RowGUI(args);
                
                if (null != item.OverlayIcon)
                {
                    var iconRect = rect;
                    iconRect.width = 16;
                    iconRect.x += GetContentIndent(item);
                    GUI.DrawTexture(iconRect, item.OverlayIcon, ScaleMode.ScaleToFit, true, 0, Color.white, 0, 0);
                }
            }
            
            GUI.contentColor = Color.white;
        }
        
        private void DrawItem(Rect rect, PrefabInstanceTreeItem item, RowGUIArgs args)
        {
            GUI.contentColor = TinyColors.Hierarchy.Prefab;
            GUI.backgroundColor = TinyColors.Hierarchy.Prefab;

            var boxRect = rect;
            boxRect.x += GetContentIndent(item) - 14f;
            GUI.Box(boxRect, GUIContent.none);
            
            base.RowGUI(args);
            
            if (null != item.OverlayIcon)
            {
                var iconRect = rect;
                iconRect.width = 16;
                iconRect.x += GetContentIndent(item);
                GUI.DrawTexture(iconRect, item.OverlayIcon, ScaleMode.ScaleToFit, true, 0, Color.white, 0, 0);
            }
            
            GUI.contentColor = Color.white;
            GUI.backgroundColor = Color.white;
        }

        private void BuildFromNode(ISceneGraphNode node, TreeViewItem parentItem)
        {
            TreeViewItem item = null;
            
            switch (node)
            {
                case EntityNode entityNode:
                {
                    var entity = entityNode.EntityRef.Dereference(m_Context.Registry);
                    
                    var hierarchyEntity = new EntityTreeItem
                    {
                        Value = entityNode,
                        Registry = entity.Registry,
                        depth = parentItem.depth + 1,
                        icon = entityNode is PrefabInstanceNode  ? s_PrefabIcon  : s_EntityIcon
                    };
                    
                    if (entity.IsAddedEntityOverride())
                    {
                        hierarchyEntity.OverlayIcon = s_PrefabOverlayAddedIcon;
                    }

                    if (m_HierarchyFilter.Keep(entity))
                    {
                        parentItem.AddChild(hierarchyEntity);
                    }

                    item = hierarchyEntity;
                }
                break;

                case FolderNode folderNode:
                {
                    var hierarchyFolder = new FolderTreeItem
                    {
                        Value = folderNode,
                        depth = parentItem.depth + 1,
                        icon = s_EntityIcon
                    };
                    parentItem.AddChild(hierarchyFolder);
                    item = hierarchyFolder;
                }
                break;
            }
            
            if (null == item)
            {
                return;
            }
            
            foreach (var child in node.Children)
            {
                BuildFromNode(child, string.IsNullOrEmpty(m_FilterString) ? item : parentItem);
            }
        }

        private DragAndDropVisualMode HandleDropUponItem(List<ISceneGraphNode> nodes, HierarchyTreeItemBase parentItem)
        {
            if (m_DroppedOnMethod.TryGetValue(parentItem.GetType(), out var method))
            {
                method(parentItem, nodes);
                var ids = AsInstanceIds(nodes).ToArray();
                Selection.instanceIDs = ids;
                IdsToExpand = ids;
                TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.SceneGraph);
                return DragAndDropVisualMode.Link;
            }
            return DragAndDropVisualMode.Rejected;
        }

        private DragAndDropVisualMode HandleDropBetweenItems(List<ISceneGraphNode> nodes, HierarchyTreeItemBase parentItem, int insertAtIndex)
        {
            if (m_DroppedBetweenMethod.TryGetValue(parentItem.GetType(), out var method))
            {
                method(parentItem, nodes, insertAtIndex);
                var ids = AsInstanceIds(nodes).ToArray();
                Selection.instanceIDs = ids;
                IdsToExpand = ids;
                TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.SceneGraph);
                return DragAndDropVisualMode.Link;
            }
            return DragAndDropVisualMode.Rejected;
        }

        private DragAndDropVisualMode HandleResourceDropped(Object obj, DragAndDropArgs args)
        {
            var parent = args.parentItem as HierarchyTreeItemBase;
            EntityNode entityNode = null;
            switch (args.dragAndDropPosition)
            {
                case DragAndDropPosition.UponItem:
                    {
                        switch (parent)
                        {
                            case EntityGroupTreeItem groupGraph:
                            {
                                var graph = groupGraph.Value;
                                entityNode = graph.CreateEntityNode();
                                break;
                            }
                            case EntityTreeItem entity:
                            {
                                var node = entity.Value;
                                entityNode = (node.Graph as EntityGroupGraph)?.CreateEntityNode(node);
                                break;
                            }
                        }
                    }
                    break;
                case DragAndDropPosition.BetweenItems:
                    {
                        if (rootItem == parent)
                        {
                            if (args.insertAtIndex <= 0)
                            {
                                return DragAndDropVisualMode.Rejected;
                            }

                            var graph = (parent.children[args.insertAtIndex - 1] as EntityGroupTreeItem).Value;
                            entityNode = graph.CreateEntityNode();
                        }
                        else if (parent is EntityGroupTreeItem entityGroupItem)
                        {
                            var graph = entityGroupItem.Value;
                            var index = (args.insertAtIndex >= entityGroupItem.children.Count || args.insertAtIndex >= graph.Roots.Count) ? -1 : args.insertAtIndex;
                            entityNode = graph.CreateEntityNode();
                            graph.Insert(index, entityNode);
                        }
                        else if (parent is EntityTreeItem entityTreeItem)
                        {
                            var parentNode = entityTreeItem.Value;
                            var index = args.insertAtIndex;
                            entityNode = (parentNode.Graph as EntityGroupGraph).CreateEntityNode();
                            entityNode.SetParent(index, parentNode);
                        }
                    }
                    break;
                case DragAndDropPosition.OutsideItems:
                    {
                        var graph = EntityGroupManager.GetSceneGraph(m_EntityGroups.Last());
                        entityNode = graph.CreateEntityNode();
                    }
                    break;
                default:
                    {
                        return DragAndDropVisualMode.Rejected;
                    }
            }


            if (!TinyEntity.Reference.None.Equals(entityNode.EntityRef))
            {
                AddToEntity(entityNode.EntityRef, obj);
                var ids = AsInstanceIds(entityNode);
                Selection.instanceIDs = ids;
                IdsToExpand = ids;
                return DragAndDropVisualMode.Link;
            }
            return DragAndDropVisualMode.Rejected;
        }

        private DragAndDropVisualMode HandlePrefabDropped(Object obj, DragAndDropArgs args)
        {
            var asset = obj as UTPrefab;
            var path = AssetDatabase.GetAssetPath(asset);
            var guid = AssetDatabase.AssetPathToGUID(path);
            var id = Persistence.GetRegistryObjectIdsForAssetGuid(guid).FirstOrDefault();

            if (string.IsNullOrEmpty(id))
            {
                return DragAndDropVisualMode.Rejected;
            }
            
            var group = Registry.FindById<TinyEntityGroup>(new TinyId(id));
            
            if (null == group)
            {
                return DragAndDropVisualMode.Rejected;
            }
            
            var parent = args.parentItem as HierarchyTreeItemBase;
            ISceneGraph graph = null;
            ISceneGraphNode node = null;
            int index = -1;

            // Lets figure out where we are creating this prefab
            switch (args.dragAndDropPosition)
            {
                case DragAndDropPosition.UponItem:
                {
                    switch (parent)
                    {
                        case EntityGroupTreeItem groupTreeItem:
                        {
                            graph = groupTreeItem.Value;
                            break;
                        }
                        case EntityTreeItem entityTreeItem:
                        {
                            node = entityTreeItem.Value;
                            graph = entityTreeItem.Value.Graph;
                            break;
                        }
                    }
                }
                break;
                
                case DragAndDropPosition.BetweenItems:
                {
                    if (rootItem == parent)
                    {
                        if (args.insertAtIndex <= 0)
                        {
                            return DragAndDropVisualMode.Rejected;
                        }

                        if (parent != null)
                        {
                            graph = (parent.children[args.insertAtIndex - 1] as EntityGroupTreeItem)?.Value;
                        }
                    }
                    else switch (parent)
                    {
                        case EntityGroupTreeItem groupTreeItem:
                            graph = groupTreeItem.Value;
                            index = (args.insertAtIndex >= groupTreeItem.children.Count || args.insertAtIndex >= graph.Roots.Count) ? -1 : args.insertAtIndex;
                            break;
                        case EntityTreeItem entityTreeItem:
                            node = entityTreeItem.Value;
                            graph = entityTreeItem.Value.Graph;
                            index = args.insertAtIndex;
                            break;
                    }
                } 
                break;
                
                case DragAndDropPosition.OutsideItems:
                {
                    graph = EntityGroupManager.GetSceneGraph(m_EntityGroups.Last());
                }
                break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var targetGroup = (graph as EntityGroupGraph)?.EntityGroup;

            if (targetGroup == null)
            {
                return DragAndDropVisualMode.Rejected;
            }
            
            // Detect prefab nesting 
            if (group.PrefabInstances.Count > 0)
            {
                EditorGUIUtilityBridge.DisplayDialog("Prefab nesting is not supported yet", "You are trying to instantiate a Prefab that already contains PrefabInstances", "Close");
                return DragAndDropVisualMode.Rejected;
            }

            if (Registry.FindAllByType<TinyPrefabInstance>().Any(i => i.PrefabEntityGroup.Equals(targetGroup.Ref)))
            {
                EditorGUIUtilityBridge.DisplayDialog("Prefab nesting is not supported yet", "You are trying to instantiate a Prefab in a group that is already being used as a Prefab", "Close");
                return DragAndDropVisualMode.Rejected;
            }

            var prefabManager = m_Context.GetManager<IPrefabManager>();
            var instance = prefabManager.Instantiate(group);
            var start = graph.Roots.GetDescendants().OfType<EntityNode>().ToList().IndexOf(node as EntityNode);

            for (var i = 0; i < instance.Entities.Count; i++)
            {
                var entity = instance.Entities[i];
                
                if (start != -1)
                {
                    targetGroup.Entities.Insert(start + i, entity);
                }
                else
                {
                    targetGroup.AddEntityReference(entity);
                }
            }

            foreach (var entity in instance.Entities.Deref(Registry))
            {
                entity.EntityGroup = targetGroup;
                
                if (PrefabTransformUtility.IsPrefabInstanceRootTransform(entity))
                {
                    if (node is EntityNode parentEntityNode)
                    {
                        entity.SetParent(parentEntityNode.EntityRef);
                        prefabManager.RecordEntityInstanceModifications(entity);
                    }
                }
            }

            targetGroup.PrefabInstances.Add(instance.Ref);
            instance.EntityGroup = targetGroup.Ref;
          
            TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
            
            return DragAndDropVisualMode.Link;
        }

        private DragAndDropVisualMode HandleSingleObjectDrop<T>(DragAndDropArgs args, System.Func<T, DragAndDropArgs, DragAndDropVisualMode> action) where T : Object
        {
            var draggedObjects = DragAndDrop.objectReferences;
            if (draggedObjects.Length > 1)
            {
                return DragAndDropVisualMode.Rejected;
            }
            var objects = draggedObjects
               .Select(d => d as T)
               .Where(s => null != s).ToList();

            if (objects.Count == 1)
            {
                return action(objects[0], args);
            }

            return DragAndDropVisualMode.Rejected;
        }

        private bool AddToEntity(TinyEntity.Reference entity, Object obj)
        {
            if (obj is Texture2D)
            {
                var texture = (Texture2D) obj;
                var path = AssetDatabase.GetAssetPath(texture);
                var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
                if (null == sprite)
                {
                    Debug.LogWarning($"{TinyConstants.ApplicationName}: Only Sprites are supported in {TinyConstants.ApplicationName}.");
                }
                return AddSpriteToEntity(entity, sprite);
            }
            else if (obj is Sprite)
            {
                return AddSpriteToEntity(entity, obj as Sprite);
            }
            else if (obj is AudioClip)
            {
                return AddAudioClipToEntity(entity, obj as AudioClip);
            }
            else if (obj is AnimationClip)
            {
                return AddAnimationClipToEntity(entity, obj as AnimationClip);
            }
            else if (obj is TMPro.TMP_FontAsset)
            {
                return AddFontToEntity(entity, obj as TMPro.TMP_FontAsset);
            }
            return false;
        }

        private bool AddSpriteToEntity(TinyEntity.Reference entityRef, Sprite sprite)
        {
            var entity = entityRef.Dereference(m_Context.Registry);
            entity.Name = sprite?.name ?? "NullSprite";

            entity.GetOrAddComponent(TypeRefs.Core2D.TransformNode);
            entity.GetOrAddComponent(TypeRefs.Core2D.TransformLocalPosition);
            entity.GetOrAddComponent(TypeRefs.Core2D.TransformLocalRotation);
            entity.GetOrAddComponent(TypeRefs.Core2D.TransformLocalScale);
            var renderer = entity.GetOrAddComponent(TypeRefs.Core2D.Sprite2DRenderer);
            renderer["sprite"] = sprite;

            TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.SceneGraph);
            return true;
        }

        private bool AddAudioClipToEntity(TinyEntity.Reference entityRef, AudioClip audioClip)
        {
            var entity = entityRef.Dereference(m_Context.Registry);
            entity.Name = audioClip.name;

            var audioSource = entity.GetOrAddComponent<Runtime.Audio.TinyAudioSource>();
            audioSource.clip = audioClip;

            TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.SceneGraph);
            return true;
        }

        private bool AddAnimationClipToEntity(TinyEntity.Reference entityRef, AnimationClip animationClip)
        {
            var entity = entityRef.Dereference(m_Context.Registry);
            entity.Name = animationClip.name;

            var animationClipPlayer = entity.GetOrAddComponent(TypeRefs.Animation.AnimationClipPlayer);
            animationClipPlayer["animationClip"] = animationClip;

            TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.SceneGraph);
            return true;
        }
        
        private bool AddFontToEntity(TinyEntity.Reference entityRef, TMPro.TMP_FontAsset font)
        {
            var entity = entityRef.Dereference(m_Context.Registry);
            entity.Name = font.name;

            var renderer = entity.AddComponent<TinyText2DRenderer>();
            renderer.text = "Sample Text";
            var tinyFont = entity.AddComponent<TinyText2DStyleBitmapFont>();
            tinyFont.font = font;
            var fontStyle = entity.AddComponent<TinyText2DStyle>();
            fontStyle.size = 15;

            TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.SceneGraph);
            return true;
        }

        private void DropUponSceneItem(HierarchyTreeItemBase parent, List<ISceneGraphNode> nodes)
        {
            var graph = (parent as EntityGroupTreeItem)?.Value;
            
            Assert.IsNotNull(graph);
            
            foreach (var node in nodes)
            {
                graph.Add(node);
            }
            var ids = AsInstanceIds(nodes).ToArray();
            Selection.instanceIDs = ids;
            IdsToExpand = ids;
        }

        private void DropUponEntityItem(HierarchyTreeItemBase parent, List<ISceneGraphNode> nodes)
        {
            var item = parent as EntityTreeItem;
            var parentNode = item.Node;
            var graph = item.Graph;
            graph.Insert(-1, nodes, parentNode);
        }

        private DragAndDropVisualMode DropOutsideOfItems(List<ISceneGraphNode> nodes)
        {
            var graph = EntityGroupManager.GetSceneGraph(m_EntityGroups.Last());
            graph.Add(nodes);
            var ids = AsInstanceIds(nodes).ToArray();
            Selection.instanceIDs = ids;
            IdsToExpand = ids;
            TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.SceneGraph);
            return DragAndDropVisualMode.Link;
        }

        private static void DropBetweenEntityGroupItems(HierarchyTreeItemBase parent, List<ISceneGraphNode> nodes, int insertAtIndex)
        {
            // Can't add entities before the first group.
            if (insertAtIndex <= 0)
            {
                return;
            }

            var graph = (parent.children[insertAtIndex - 1] as EntityGroupTreeItem).Value;
            graph.Add(nodes);
        }

        private static void DropBetweenRootEntities(HierarchyTreeItemBase parent, List<ISceneGraphNode> nodes, int insertAtIndex)
        {
            var item = parent as EntityGroupTreeItem;
            var graph = item.Value;

            var firstIndex = insertAtIndex;
            foreach (var node in nodes)
            {
                if (graph.IsRoot(node) && node.SiblingIndex() < firstIndex)
                {
                    firstIndex -= 1;
                }

                graph.Insert(firstIndex++, node);
            }
        }

        private static void DropBetweenChildrenEntities(HierarchyTreeItemBase parent, List<ISceneGraphNode> nodes, int insertAtIndex)
        {
            var entityNode = (parent as EntityTreeItem).Value;
            var firstIndex = insertAtIndex;
            foreach (var node in nodes)
            {
                if (node.IsChildOf(entityNode) && node.SiblingIndex() < firstIndex)
                {
                    firstIndex -= 1;
                }

                entityNode.Insert(firstIndex++, node);
            }
        }

        private int[] AsInstanceIds(EntityNode entity)
        {
            return new [] { entity.EntityRef.Dereference(Registry).View.gameObject.GetInstanceID() };
        }

        private IEnumerable<int> AsInstanceIds(IEnumerable<ISceneGraphNode> nodes)
        {
            foreach (var node in nodes)
            {
                switch (node)
                {
                    case EntityNode entityNode:
                    {
                        var entity = entityNode.EntityRef.Dereference(Registry);
                        if (null == entity)
                        {
                            continue;
                        }

                        yield return entity.View.gameObject.GetInstanceID();
                    }
                    break;
                }
            }
        }

        private void ForceExpanded(IEnumerable<int> ids)
        {
            foreach(var id in ids)
            {
                foreach (var ancestorId in GetAncestors(id))
                {
                    SetExpanded(ancestorId, true);
                }
                SetExpanded(id, true);
            }
        }
        #endregion
    }
}

