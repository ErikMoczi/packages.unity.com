using System;
using System.Linq;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using Unity.Properties;
using static Unity.Tiny.EditorInspectorAttributes;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    internal class EntityGroupGraph : SceneGraphBase
    {
        [TinyInitializeOnLoad]
        private static void HandleProjectClosed()
        {
            TinyEditorApplication.OnCloseProject += (p, c) => s_LastKnownView.Clear();
        }

        public static EntityGroupGraph CreateFromEntityGroup([NotNull] TinyEntityGroup entityGroup, [NotNull] TinyContext context)
        {
            var graph = new EntityGroupGraph(entityGroup, context);
            graph.ImportGraph();
            graph.CacheUnityScene();
            graph.CreateLinks();
            graph.ClearUnityScene();
            graph.ReparentLinks();
            graph.Transfer();
            return graph;
        }

        public TinyEntityGroup.Reference EntityGroupRef { get; }

        public TinyEntityGroup EntityGroup
        {
            get
            {
                var group = EntityGroupRef.Dereference(Registry);
                Assert.IsNotNull(group);
                return group;
            }
        }

        private TinyContext Context { get; }
        private IRegistry Registry { get; }
        private IEntityGroupManagerInternal EntityGroupManager { get; }
        private IBindingsManager Bindings { get; }

        private Scene m_Scene;
        private Scene ScratchPad
        {
            get
            {
                if (!m_Scene.isLoaded || !m_Scene.IsValid())
                {
                    m_Scene = EntityGroupManager.UnityScratchPad;
                }

                return m_Scene;
            }
        }

        private readonly Dictionary<TinyEntity.Reference, TinyEntityView> m_CachedViewsLookup = new Dictionary<TinyEntity.Reference, TinyEntityView>();
        private static readonly Dictionary<TinyEntity.Reference, TinyEntityView> s_LastKnownView = new Dictionary<TinyEntity.Reference, TinyEntityView>();

        private EntityGroupGraph([NotNull] TinyEntityGroup entityGroup, TinyContext context)
        {
            Context = context;
            EntityGroupRef = (TinyEntityGroup.Reference) entityGroup;
            Registry = entityGroup.Registry;
            EntityGroupManager = context.GetManager<IEntityGroupManagerInternal>();
            Bindings = context.GetManager<IBindingsManager>();
        }
        
        /// <inheritdoc />
        /// <summary>
        /// Invoked when adding a node to the graph
        /// </summary>
        protected override void OnInsertNode(ISceneGraphNode node, ISceneGraphNode parent)
        {
            if (node is EntityNode entity)
            {
                UpdateTransform(entity, node.GetFirstAncestorOfType<EntityNode>());
                
                // @NOTE This could be optimized since we don't really need to
                //       perform this work when re-parenting within our own graph
                //
                // We will avoid premature optimizations for now...
                foreach (var descendant in node.GetDescendants().OfType<EntityNode>())
                {
                    descendant.EntityRef.Dereference(Registry).EntityGroup = EntityGroup;
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Invoked when removing a node from the graph
        /// </summary>
        protected override void OnRemoveNode(ISceneGraphNode node)
        {
            
        }

        /// <inheritdoc />
        /// <summary>
        /// Invoked when deleting a node from the graph
        /// </summary>
        protected override void OnDeleteNode(ISceneGraphNode node)
        {
            switch (node)
            {
                case PrefabInstanceNode prefab:
                {
                    DeleteLink(prefab.EntityRef.Dereference(Registry));
                    Registry.Unregister(prefab.PrefabInstanceRef.Id);
                    Registry.Unregister(prefab.EntityRef.Id);
                }
                break;
                
                case EntityNode entity:
                {
                    DeleteLink(entity.EntityRef.Dereference(Registry));
                    Registry.Unregister(entity.EntityRef.Id);
                }
                break;
            }
        }
        
        protected override ISceneGraphNode CreateNode(ISceneGraphNode source, ISceneGraphNode parent)
        {
            switch (source)
            {
                case PrefabInstanceNode prefabInstanceNode:
                {
                    var sourceEntity = prefabInstanceNode.EntityRef.Dereference(Registry);
                    var targetEntity = CreateEntity(parent);
                    
                    // Copy top level entity data
                    targetEntity.Name = sourceEntity.Name;
                    targetEntity.Layer = sourceEntity.Layer;
                    targetEntity.Enabled = sourceEntity.Enabled;
                    
                    // Copy component data
                    foreach (var sourceComponent in sourceEntity.Components)
                    {
                        sourceComponent.Refresh();
                        var typeRef = sourceComponent.Type;

                        // There might be some automatic bindings that will add the component, so check if it is already present.
                        var targetComponent = targetEntity.GetOrAddComponent(typeRef);
                        targetComponent.Refresh();
                        targetComponent.CopyFrom(sourceComponent);
                    }
                    
                    var sourcePrefabInstance = prefabInstanceNode.PrefabInstanceRef.Dereference(Registry);
                    var targetPrefabInstance = Registry.CreatePrefabInstance(TinyId.New(), targetEntity.Name);
                    
                    targetPrefabInstance.EntityGroup = sourcePrefabInstance.EntityGroup;
                    targetPrefabInstance.PrefabEntityGroup = sourcePrefabInstance.PrefabEntityGroup;
                    
                    var node = new PrefabInstanceNode(this, Registry,targetPrefabInstance, targetEntity);
                    
                    Add(node, parent);

                    return node;
                }
                
                case EntityNode sourceEntityNode:
                {
                    var node = CreateEntityNode(parent);
                    var sourceEntity = sourceEntityNode.EntityRef.Dereference(Registry);
                    var targetEntity = node.EntityRef.Dereference(Registry);
                    
                    // Copy top level entity data
                    targetEntity.Name = sourceEntity.Name;
                    targetEntity.Layer = sourceEntity.Layer;
                    targetEntity.Enabled = sourceEntity.Enabled;
                    
                    // Copy component data
                    foreach (var sourceComponent in sourceEntity.Components)
                    {
                        sourceComponent.Refresh();
                        var typeRef = sourceComponent.Type;

                        // There might be some automatic bindings that will add the component, so check if it is already present.
                        var targetComponent = targetEntity.GetOrAddComponent(typeRef);
                        targetComponent.Refresh();
                        targetComponent.CopyFrom(sourceComponent);
                    }
                    
                    return node;
                }
            }
            
            return null;
        }

        /// <inheritdoc />
        /// <summary>
        /// Invoked after a duplication
        /// </summary>
        /// <param name="source">The root of the source tree being duplicated</param>
        /// <param name="target">The root of the newly duplicated tree</param>
        protected override void Remap(ISceneGraphNode source, ISceneGraphNode target)
        {
            // Remap any entity references that are WITHIN the tree
            // e.g. If any components point to children they should point to the newly duplicated children and not the original
            RemapReferences(source, target);
            
            // Remap any prefab instance objects
            RemapPrefabInstances(source, target);
        }
        
        private void RemapReferences(ISceneGraphNode source, ISceneGraphNode target)
        {
            // Extract all entities from the source and target trees
            var sourceEntities = source.GetDescendants().OfType<EntityNode>().Select(n => n.EntityRef.Dereference(Registry)).ToList();
            var targetEntities = target.GetDescendants().OfType<EntityNode>().Select(n => n.EntityRef.Dereference(Registry)).ToList();
            
            Assert.IsTrue(sourceEntities.Count == targetEntities.Count);
            
            // Build the remap information 
            var entityReferenceRemap = new Dictionary<TinyEntity.Reference, TinyEntity.Reference>();

            for (var i = 0; i < sourceEntities.Count; i++)
            {
                entityReferenceRemap.Add(sourceEntities[i].Ref, targetEntities[i].Ref);
            }
            
            var visitor = new EntityRemapVisitor(entityReferenceRemap);

            // Remap each component of the target tree
            foreach (var entity in targetEntities)
            {
                foreach (var component in entity.Components)
                {
                    component.Visit(visitor);
                }
            }
        }

        private void RemapPrefabInstances(ISceneGraphNode source, ISceneGraphNode target)
        {
            var sourcePrefabInstanceNodes = source.GetDescendants().OfType<PrefabInstanceNode>().ToList();
            var targetPrefabInstanceNodes = target.GetDescendants().OfType<PrefabInstanceNode>().ToList();
            
            Assert.IsTrue(sourcePrefabInstanceNodes.Count == targetPrefabInstanceNodes.Count);

            var prefabManager = Context.GetManager<IPrefabManager>();

            for (var i = 0; i < sourcePrefabInstanceNodes.Count; i++)
            {
                var sourcePrefabInstanceNode = sourcePrefabInstanceNodes[i];
                var targetPrefabInstanceNode = targetPrefabInstanceNodes[i];

                var sourceEntities = sourcePrefabInstanceNode.GetDescendants().OfType<EntityNode>().Select(n => n.EntityRef.Dereference(Registry)).ToList();
                var targetEntities = targetPrefabInstanceNode.GetDescendants().OfType<EntityNode>().Select(n => n.EntityRef.Dereference(Registry)).ToList();
                
                Assert.IsTrue(sourceEntities.Count == targetEntities.Count);

                var targetPrefabInstance = targetPrefabInstanceNode.PrefabInstanceRef.Dereference(Registry);
                
                for (var j = 0; j < sourceEntities.Count; j++)
                {
                    var sourceEntity = sourceEntities[j];
                    var targetEntity = targetEntities[j];

                    if (null == sourceEntity.Instance)
                    {
                        continue;
                    }
                    
                    var targetEntityInstance = new TinyEntityInstance(targetEntity);
                    targetEntityInstance.CopyFrom(sourceEntity.Instance);
                    targetEntityInstance.PrefabInstance = targetPrefabInstance.Ref;
                    targetEntity.Instance = targetEntityInstance;
                    
                    targetPrefabInstance.Entities.Add(targetEntity.Ref);
                    
                    prefabManager.ApplyPrefabToInstance(targetEntity.Instance.Source.Dereference(Registry), targetEntity);
                    prefabManager.CreateBaseline(targetEntity);
                }
            }
        }

        /// <summary>
        /// Updates the `Unity` scene transform bindings for the given node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="parent"></param>
        private void UpdateTransform(EntityNode node, EntityNode parent)
        {
            var entity = node.EntityRef.Dereference(Registry);
            var transform = entity.View.transform;
            transform.SetParent(parent?.EntityRef.Dereference(Registry).View.transform, true);
            TransformInvertedBindings.SyncTransform(transform, entity.View);
        }

        /// <summary>
        /// Creates an entity from an existing `UnityEngine.GameObject`
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public EntityNode CreateFromExisting(Transform transform, Transform parent)
        {
            var entity = Registry.CreateEntity(new TinyId(Guid.NewGuid()), transform.name);

            var entityRef = (TinyEntity.Reference) entity;
            var entityGroup = EntityGroup;
            entity.EntityGroup = entityGroup;
            entityGroup.AddEntityReference(entityRef);
            
            entity.AddComponent(TypeRefs.Core2D.TransformNode);
            entity.View = transform.GetComponent<TinyEntityView>();

            var node = new EntityNode(this, Registry, entity);
            CreateLink(node);
            node.Graph.Add(node);

            if (!parent)
            {
                return node;
            }
            
            var parentView = parent.GetComponent<TinyEntityView>();
            Assert.IsNotNull(parentView);
            var parentNode = FindNode((TinyEntity.Reference) parentView.EntityRef.Dereference(parentView.Registry));
            node.SetParent(parentNode);

            return node;
        }

        public EntityNode CreateEntityNode(ISceneGraphNode parent = null)
        {
            var node = new EntityNode(this, Registry, CreateEntity(parent));
            CreateLink(node);
            Add(node, parent);
            return node;
        }
        
        /// <summary>
        /// Creates a new TinyEntity and adds it to the graph
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public TinyEntity CreateEntity(ISceneGraphNode parent = null)
        {
            Assert.IsTrue(null == parent || this == parent.Graph);

            // Create the entity
            var name = GetUniqueName(parent?.Children.OfType<EntityNode>().Select(c => c.EntityRef) ?? Roots.OfType<EntityNode>().Select(r => r.EntityRef), "Entity");
            var entity = Registry.CreateEntity(new TinyId(Guid.NewGuid()), name);
            var entityRef = (TinyEntity.Reference) entity;
            EntityGroup.AddEntityReference(entityRef);
            
            entity.EntityGroup = EntityGroup;
            entity.AddComponent(TypeRefs.Core2D.TransformNode);

            var view = CreateLink(entity);
            if (parent is EntityNode parentEntityNode)
            {
                var parentEntity = parentEntityNode.EntityRef.Dereference(Registry);
                view.transform.SetParent(parentEntity.View.transform, false);
                entity.Layer = parentEntity.Layer;
                var rectTransformType = TypeRefs.UILayout.RectTransform;
                if (null != parentEntity.GetComponent(rectTransformType))
                {
                    entity.AddComponent(rectTransformType);
                }
            }

            return entity;
        }

        public bool IsRoot(ISceneGraphNode node)
        {
            return Roots.Contains(node);
        }

        public void Unlink()
        {
            var entityRefs = ListPool<TinyEntity.Reference>.Get();
            try
            {
                foreach (var node in Roots.GetDescendants().OfType<EntityNode>())
                {
                    var entity = node.EntityRef.Dereference(Registry);
                    
                    if (null == entity)
                    {
                        continue;
                    }

                    DeleteLink(entity);
                }
            }
            finally
            {
                ListPool<TinyEntity.Reference>.Release(entityRefs);
            }
        }

        private void ImportGraph()
        {
            Roots.Clear();

            var entityCache = DictionaryPool<TinyEntity, EntityNode>.Get();
            var orderedEntities = ListPool<TinyEntity>.Get();

            var entityGroup = EntityGroup;

            try
            {
                // Create the nodes.
                foreach (var entity in entityGroup.Entities.Deref(Registry))
                {
                    // @TODO Remove this
                    entity.EntityGroup = entityGroup;

                    var node = PrefabTransformUtility.IsPrefabInstanceRootTransform(entity) 
                        ? new PrefabInstanceNode(this, Registry, entity.Instance.PrefabInstance.Dereference(Registry), entity) 
                        : new EntityNode(this, Registry, entity);
                    
                    CreateLink(node);
                    entityCache[entity] = node;
                    orderedEntities.Add(entity);
                }
                
                foreach (var entity in orderedEntities)
                {
                    var node = entityCache[entity];
                    
                    // Virtual hierarchy
                    ISceneGraphNode parent;
                    List<ISceneGraphNode> siblings;
                    
                    // Do we have a transform parent?
                    var transformParent = entity.Parent();
                    if (TinyEntity.Reference.None.Id == transformParent.Id)
                    {
                        // We have no parents, we should be part of the root siblings
                        parent = null;
                        siblings = Roots;
                    }
                    else
                    {
                        var parentEntity = transformParent.Dereference(Registry);

                        if (null == parentEntity)
                        {
                            // Something went wrong and we can't find our parent.
                            continue;
                        }

                        if (!entityCache.TryGetValue(parentEntity, out var parentNode))
                        {
                            // Something went wrong and we can't find our parents node
                            continue;
                        }
                        
                        parent = parentNode;
                        siblings = parentNode.Children;
                    }

                    // Insert this node into the graph
                    node.Parent = parent;
                    siblings.Add(node);
                }

            }
            finally
            {
                DictionaryPool<TinyEntity, EntityNode>.Release(entityCache);
                ListPool<TinyEntity>.Release(orderedEntities);
            }

            // Log missing entities
            foreach (var missingRefs in entityGroup.Entities.MissingRef(Registry))
            {
                Console.WriteLine($"SceneGraph failed to load entity Name=[{missingRefs.Name}] Id=[{missingRefs.Id}]");
            }
        }

        public void CommitChanges()
        {
            if (!Changed)
            {
                return;
            }
            
            CommitToTiny();
            CommitToUnity();
            ClearChanged();
        }

        private void CommitToTiny()
        {
            var entityRefs = ListPool<TinyEntity.Reference>.Get();
            var prefabInstanceSetRefs = HashSetPool<TinyPrefabInstance.Reference>.Get();

            try
            {
                // Fix up entity parents
                foreach (var node in Roots.GetDescendants().OfType<EntityNode>())
                {
                    var entity = node.EntityRef.Dereference(Registry);
                    var parent = node.GetFirstAncestorOfType<EntityNode>();
                    entity.SetParent(parent?.EntityRef ?? TinyEntity.Reference.None);
                    entityRefs.Add(node.EntityRef);
                }

                var entityGroup = EntityGroup;
                entityGroup.ClearEntityReferences();
                foreach (var entityRef in entityRefs)
                {
                    entityGroup.AddEntityReference(entityRef);
                }

                entityGroup.PrefabInstances.Clear();
                foreach (var entityRef in entityRefs)
                {
                    var entity = entityRef.Dereference(Registry);
                    if (null == entity.Instance)
                    {
                        continue;
                    }

                    var prefabInstance = entity.Instance.PrefabInstance;

                    if (prefabInstanceSetRefs.Add(prefabInstance))
                    {
                        entityGroup.PrefabInstances.Add(prefabInstance);
                    }
                }
            }
            finally
            {
                ListPool<TinyEntity.Reference>.Release(entityRefs);
                HashSetPool<TinyPrefabInstance.Reference>.Release(prefabInstanceSetRefs);
            }
        }

        private void CommitToUnity()
        {
            var entityGroup = ScratchPad;
            if (!entityGroup.isLoaded || !entityGroup.IsValid())
            {
                return;
            }

            var cache = HashSetPool<TinyEntity.Reference>.Get();

            try
            {
                foreach (var node in Roots.GetDescendants().OfType<EntityNode>())
                {
                    var entity = node.EntityRef.Dereference(Registry);
                    
                    // Setup the view
                    var view = CreateLink(entity);

                    // Fix up the transform hierarchy
                    // @NOTE since we may be dealing with some virtual nodes, find the first entity up the tree that implements a UnityEngine.Transform
                    view.transform.SetParent(node.GetFirstAncestorOfType<ITransformNode>()?.Transform, true);
                    view.transform.SetAsLastSibling();
                    
                    // Track for caching
                    cache.Add(node.EntityRef);
                }
            }
            finally
            {
                HashSetPool<TinyEntity.Reference>.Release(cache);
            }
        }

        private void CacheUnityScene()
        {
            var scene = ScratchPad;

            var roots = ListPool<GameObject>.Get();
            var views = ListPool<TinyEntityView>.Get();
            try
            {
                scene.GetRootGameObjects(roots);
                foreach (var root in roots)
                {
                    root.GetComponentsInChildren(views);
                    foreach (var view in views)
                    {
                        if (null != view && view && !view.EntityRef.Equals(TinyEntity.Reference.None))
                        {
                            m_CachedViewsLookup[view.EntityRef] = view;
                        }
                    }
                }
            }
            finally
            {
                ListPool<TinyEntityView>.Release(views);
                ListPool<GameObject>.Release(roots);
            }
        }

        private void ClearUnityScene()
        {
            m_CachedViewsLookup.Clear();
        }

        private void CreateLinks()
        {
            foreach (var node in Roots)
            {
                CreateLink(node);
            }
        }

        private void CreateLink(ISceneGraphNode node)
        {
            foreach (var child in node.GetDescendants().OfType<EntityNode>())
            {
                CreateLink(child.EntityRef.Dereference(Registry));
            }
        }

        private TinyEntityView CreateLink([NotNull] TinyEntity entity)
        {
            var entityRef = (TinyEntity.Reference)entity;
            var view = entity.View;
            
            if ((null == view || !view) && !TryFindView(entityRef, out view))
            {
                // Could not find any suitable views, create one
                var go = new GameObject(entity.Name);
                entity.View = view = go.AddComponent<TinyEntityView>();
            }
            else
            {
                if (view.ForceRelink)
                {
                    DeleteLink(entity);
                    return CreateLink(entity);
                }
            }

            Assert.IsNotNull(view);
            s_LastKnownView[entityRef] = view;
            entity.View = view;
            entity.View.EntityRef = entityRef;
            view.gameObject.name = entity.Name;
            view.gameObject.SetActive(entity.Enabled);
            view.gameObject.layer = entity.Layer;
            view.Registry = entity.Registry;
            view.Context = Context;

            // At this point, it is not clear if the bindings have been added or not (we may have undo-ed something).
            entity.OnComponentAdded -= HandleComponentAdded;
            entity.OnComponentRemoved -= HandleComponentRemoved;
            entity.OnComponentAdded += HandleComponentAdded;
            entity.OnComponentRemoved += HandleComponentRemoved;

            return view;
        }
        
        private void ReparentLinks()
        {
            foreach (var root in Roots)
            {
                ReparentLink(root);
            }
        }

        private void ReparentLink(ISceneGraphNode node)
        {
            foreach (var child in node.GetDescendants().OfType<EntityNode>())
            {
                var self = child.EntityRef.Dereference(Registry).View.transform;
                var parentNode = child.GetFirstAncestorOfType<EntityNode>();
                
                if (null != parentNode)
                {
                    var parent = parentNode.EntityRef.Dereference(Registry).View.transform;
                    self.SetParent(parent, false);
                }
                
                self.SetAsLastSibling();
            }
        }

        private void DeleteLink(TinyEntity entity)
        {
            entity.OnComponentAdded -= HandleComponentAdded;
            entity.OnComponentRemoved -= HandleComponentRemoved;

            var view = entity.View;
            if (null != view && view && entity.View.gameObject)
            {
                view.Disposed = true;
                Object.DestroyImmediate(entity.View.gameObject, false);
            }

            entity.View = null;
        }

        private void HandleComponentAdded(TinyEntity entity, TinyObject component)
        {
            Bindings.SetConfigurationDirty(entity);
            var type = component.Type.Dereference(Registry);
            if (type.HasAttribute<ComponentCallbackAttribute>())
            {
                var bindings = type.GetAttribute<ComponentCallbackAttribute>().Callback;

                // Invoke callback to perform first time setup hook
                bindings.Run(ComponentCallbackType.OnAddComponent, entity, component);
            }
        }

        private void HandleComponentRemoved(TinyEntity entity, TinyObject component)
        {
            Bindings.SetConfigurationDirty(entity);
            var type = component.Type.Dereference(Registry);
            if (type.HasAttribute<ComponentCallbackAttribute>())
            {
                var bindings = type.GetAttribute<ComponentCallbackAttribute>().Callback;
                
                // Invoke callback to perform teardown hook
                bindings.Run(ComponentCallbackType.OnRemoveComponent, entity, component);
            }
        }

        private void Transfer()
        {
            foreach (var node in Roots.GetDescendants().OfType<EntityNode>())
            {
                Bindings.Transfer(node.EntityRef.Dereference(Registry));
            }
        }

        private bool TryFindView(TinyEntity.Reference entityRef, out TinyEntityView view)
        {
            if (m_CachedViewsLookup.TryGetValue(entityRef, out view) && null != view && view)
            {
                return true;
            }
            if (s_LastKnownView.TryGetValue(entityRef, out view) && null != view && view)
            {
                return true;
            }
            return false;
        }
        
        public EntityNode FindNode(TinyEntity entity)
        {
            return null == entity ? null : FindNode((TinyEntity.Reference)entity);
        }

        public EntityNode FindNode(TinyEntity.Reference entity)
        {
            return Roots.Select(r => FindEntityNodeRecursive(r, entity)).NotNull().FirstOrDefault();
        }
        
        private static EntityNode FindEntityNodeRecursive(ISceneGraphNode node, TinyEntity.Reference entity)
        {
            if (node is EntityNode entityNode && entityNode.EntityRef.Equals(entity))
            {
                return entityNode;
            }

            return node.Children
                .Select(r => FindEntityNodeRecursive(r, entity))
                .NotNull()
                .FirstOrDefault();
        }
        
        private string GetUniqueName(IEnumerable<TinyEntity.Reference> elements, string name)
        {
            var digits = name.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray();
            var baseName = name.Substring(0, name.Length - digits.Length);
            var next = baseName;
            var index = 1;

            while (true)
            {
                if (elements.Deref(Registry).All(element => !string.Equals(element.Name, next)))
                {
                    return next;
                }

                next = $"{baseName}{index++}";
            }
        }
    }
}

