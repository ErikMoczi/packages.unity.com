using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Unity.Properties;
using Unity.Tiny.Attributes;
using UnityEditor;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace Unity.Tiny
{
    internal interface IPrefabManager : IContextManager
    {
        TinyPrefabInstance Instantiate(TinyEntityGroup source);

        /// <summary>
        /// Creates a baseline entity to track modifications against
        /// </summary>
        void CreateBaseline(TinyEntity instance);

        /// <summary>
        /// Immediately records modifications for an entity instance
        /// </summary>
        void RecordEntityInstanceModifications(TinyEntity entity);
        
        /// <summary>
        /// Applies the correct prefab attributes to any reflected components
        /// </summary>
        void ApplyPrefabAttributesToInstance(TinyEntity prefab, TinyEntity instance);
        
        /// <summary>
        /// Applies the prefab state onto the instance
        /// </summary>
        void ApplyPrefabToInstance(TinyEntity prefab, TinyEntity instance);
        
        /// <summary>
        /// Applies all modifications from the given instance to it's source prefab
        /// </summary>
        void ApplyInstanceToPrefab(TinyPrefabInstance instance);
        
        /// <summary>
        /// Removes all modifications on the given instance
        /// </summary>
        void RevertInstanceToPrefab(TinyPrefabInstance instance);
        
        /// <summary>
        /// Applies entity level modifications to the prefab and clears them from the instance.
        /// </summary>
        void ApplyEntityModificationsToPrefab(EntityModificationFlags flags, TinyEntity entity);

        /// <summary>
        /// Applies component level modifications to the prefab and clears them from the instance.
        /// </summary>
        void ApplyComponentModificationsToPrefab(IEnumerable<IPropertyModification> modifications, TinyEntity entity);

        /// <summary>
        /// Reverts entity level modifications from the given instance to their prefab value(s), and clears the modifications.
        /// </summary>
        void RevertEntityModificationsForInstance(EntityModificationFlags flags, TinyEntity entity);
        
        /// <summary>
        /// Reverts component level modifications from the given instance to their prefab value(s), and clears the modifications.
        /// </summary>
        void RevertComponentModificationsForInstance(IEnumerable<IPropertyModification> modifications, TinyEntity entity);

        /// <summary>
        /// Applies an added entity to the source prefab
        /// </summary>
        void ApplyAddedEntityToPrefab(TinyPrefabInstance instance, TinyEntity entity);
        
        /// <summary>
        /// Applies a removed entity to the source prefab
        /// </summary>
        void ApplyRemovedEntityToPrefab(TinyPrefabInstance instance, TinyEntity entity);

        /// <summary>
        /// Reverts a removed component from a prefab instance
        /// </summary>
        void RevertRemovedComponentForInstance(TinyEntity entity, TinyType.Reference type);
    }
    
    [UsedImplicitly]
    [ContextManager(ContextUsage.All)]
    internal class PrefabManager : ContextManager, IPrefabManager
    {
        /// <summary>
        /// Special types that are ALWAYS overriden for instances
        ///
        /// Parenting information and position within the scene should be PER instance and should NEVER reflect a prefab
        /// </summary>
        private static readonly TinyType.Reference[] s_RootIgnoredTypes = new []
        {
            TypeRefs.Core2D.TransformNode,
            TypeRefs.Core2D.TransformLocalPosition
        };
        
        /// <summary>
        /// Maximum amount of time spent per update on PrefabInstance change processing
        /// </summary>
        private const int KPrefabInstanceChangeProcessTime = 16;
       
        /// <summary>
        /// Registry to store the last known version of each prefab instance (NOT PREFABS THEMSELVES)
        ///
        /// This is used as a baseline to generate a diff (modifications) for instances
        /// this registry is kept up to date after modifications are extracted
        /// </summary>
        private readonly TinyRegistry m_BaselineRegistry = new TinyRegistry();
        
        /// <summary>
        /// Queue of instances to be updated (this queue is time-sliced)
        /// </summary>
        private readonly List<TinyPrefabInstance.Reference> m_PrefabInstanceChangeQueue = new List<TinyPrefabInstance.Reference>();
        
        private IBindingsManager m_BindingsManager;
        private readonly TinyCaretaker m_Caretaker;

        public PrefabManager(TinyContext context)
            : base(context)
        {
            m_Caretaker = context.Caretaker;
            m_Caretaker.OnWillGenerateMemento += HandleWillGenerateMemento;
        }

        public override void Load()
        {
#if UNITY_EDITOR
            EditorApplication.update += Update;
#endif            
            m_BindingsManager = Context.GetManager<IBindingsManager>();
            
            var undo = Context.GetManager<IUndoManager>();
            
            undo.OnBeginUndo += HandleBeginUndoRedo; 
            undo.OnBeginRedo += HandleBeginUndoRedo;
            undo.OnUndoPerformed += HandleUndoPerformed;   
             
            undo.OnEndUndo += HandleEndUndoRedo; 
            undo.OnEndRedo += HandleEndUndoRedo;
            undo.OnRedoPerformed += HandleRedoPerformed;
            
            // @TODO [PREFAB] Optimization - This should only be loaded instances
            // Create a `copy` of all prefab instances and register baselines
            foreach (var instance in Registry.FindAllByType<TinyPrefabInstance>())
            {
                foreach (var @ref in instance.Entities)
                {
                    var entity = @ref.Dereference(Registry);
                    if (null != entity)
                    {
                        // Record the initial baseline
                        RecordEntityInstanceModifications(entity);
                    }
                }
            }
            
            base.Load();
        }

        public override void Unload()
        {
#if UNITY_EDITOR
            EditorApplication.update -= Update;
#endif    
            base.Unload();
        }

        private void Update()
        {
            if (m_PrefabInstanceChangeQueue.Count <= 0)
            {
                return;
            }

            var changed = false;
            var watch = Stopwatch.StartNew();

            while (m_PrefabInstanceChangeQueue.Count > 0 && watch.ElapsedMilliseconds < KPrefabInstanceChangeProcessTime)
            {
                var @ref = m_PrefabInstanceChangeQueue[m_PrefabInstanceChangeQueue.Count - 1];
                var instance = @ref.Dereference(Registry);
                m_PrefabInstanceChangeQueue.RemoveAt(m_PrefabInstanceChangeQueue.Count - 1);
                
                if (Context.GetManager<IEntityGroupManager>().LoadedEntityGroups.Contains(instance.EntityGroup))
                {
                    changed |= SyncPrefabInstance(instance);
                }
            }

            // If we had any sort of structural change (i.e. Entity was created or destroyed), the data model must be fully re-built
            if (changed)
            {
                // Re-run all bindings since we are not sure what has actually changed
                m_BindingsManager.SetAllDirty();
                TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                m_BindingsManager.TransferAll();
            }
        }

        private void HandleBeginUndoRedo()
        {
           m_Caretaker.OnWillGenerateMemento -= HandleWillGenerateMemento;
        }
        
        private void HandleEndUndoRedo()
        {    
            m_Caretaker.OnWillGenerateMemento += HandleWillGenerateMemento;
        }

        private void HandleUndoPerformed(HashSet<Change> changes)
        { 
            foreach (var change in changes)
            {
                Restore(change, change.PreviousVersion);
            }
        }

        private void HandleRedoPerformed(HashSet<Change> changes)
        { 
            foreach (var change in changes)
            {
                Restore(change, change.NextVersion);
            }
        }

        private void Restore(Change change, IMemento version)
        {
            if (change.RegistryObject is TinyEntityGroup previousGroup)
            {
                if (GetPrefabInstances(previousGroup.Ref).Any())
                {
                    // A change was made to a prefab entity
                    // Queue an update fo all instances of this prefab
                    QueueSyncPrefabInstancesForGroup(previousGroup.Ref);
                }
            }
            else
            {
                var entity = Registry.FindById<TinyEntity>(change.Id);

                switch (entity)
                {
                    case null when !(change.RegistryObject is TinyEntity):
                        return;
                    case null:
                        entity = (TinyEntity) change.RegistryObject;
                        break;
                }

                if (null != entity.Instance)
                {
                    var target = m_BaselineRegistry.FindById<TinyEntity>(change.Id);

                    if (null == target)
                    {
                        return;
                    }
                
                    if (null == version)
                    {
                        m_BaselineRegistry.Unregister(change.Id);
                    }
                    else
                    {
                        target.Restore(version);
                    }

                    return;
                }

                var group = GetEntityGroupForEntity(entity.Ref);
            
                if (group != null && GetPrefabInstances(group.Ref).Any())
                {
                    // A change was made to a prefab entity
                    // Queue an update fo all instances of this prefab
                    QueueSyncPrefabInstancesForGroup(group.Ref);
                }
            }
        }

        /// <summary>
        /// Invoked when any object in the data model changes before the memento is generated
        /// </summary>
        /// <param name="originator">Object that has been changed</param>
        private void HandleWillGenerateMemento(IOriginator originator)
        {   
            switch (originator)
            {
                case TinyEntity entity when entity.HasEntityInstanceComponent():
                {
                    // This is a simple instance modification
                    // Track the change and perform bookkeeping on the baseline registry
                    RecordEntityInstanceModifications(entity);
                }
                break;
                
                case TinyEntity entity when entity.EntityGroup != null && GetPrefabInstances(entity.EntityGroup.Ref).Any():
                {
                    // A change was made to a prefab entity
                    // Queue an update for all instances of this prefab
                    QueueSyncPrefabInstancesForGroup(entity.EntityGroup.Ref);
                }
                break;
                
                case TinyEntityGroup entityGroup when GetPrefabInstances(entityGroup.Ref).Any():
                {
                    // We are making a change to a prefab group.
                    // This usually means a hierarchical change (e.g. An entity was deleted or created)
                    // Queue an update fo all instances of this prefab (this will re-sync the hierarchy)
                    QueueSyncPrefabInstancesForGroup(entityGroup.Ref); 
                }
                break;
            }
        }

        public void RecordEntityInstanceModifications(TinyEntity entity)
        {
            RegisterTypesForEntity(entity);

            // Pull our baseline entity to compare against from our local registry
            if (m_BaselineRegistry.FindById(entity.Id) is TinyEntity entityBaseline)
            {
                // Record top level entity changes
                if (entity.Enabled != entityBaseline.Enabled)
                {
                    entity.Instance.EntityModificationFlags |= EntityModificationFlags.Enabled;
                }
                
                if (entity.Name != entityBaseline.Name)
                {
                    entity.Instance.EntityModificationFlags |= EntityModificationFlags.Name;
                }
                
                if (entity.Layer != entityBaseline.Layer)
                {
                    entity.Instance.EntityModificationFlags |= EntityModificationFlags.Layer;
                }
                
                if (entity.Static != entityBaseline.Static)
                {
                    entity.Instance.EntityModificationFlags |= EntityModificationFlags.Static;
                }

                var isPrefabInstanceRootTransform = PrefabTransformUtility.IsPrefabInstanceRootTransform(entity);

                // @NOTE Removed components from the instance are handled in 
                //       TinyEntity.RemoveComponent. They will be implicitly skipped here
                foreach (var component in entity.Components)
                {
                    if (component.Type.Equals(TypeRefs.Core2D.TransformNode) && !isPrefabInstanceRootTransform)
                    {
                        // Guard against recording transform hierarchy overrides for children.
                        // This can lead to some weird behaviours.
                        continue;
                    }
                    
                    // Pull the last known instance of this component
                    var componentBaseline = entityBaseline.GetComponent(component.Type);

                    if (null == componentBaseline)
                    {
                        // This component was added this frame in some way
                        componentBaseline = entityBaseline.AddComponent(component.Type);

                        // Get the component into the baseline prefab state
                        ApplyComponentModifications(entity.Instance.Modifications, componentBaseline);
                    }

                    if (entity.Instance.Source.Dereference(Registry).HasComponent(component.Type))
                    {
                        // Visit/diff both components and record modifications
                        var visitor = new PrefabModificationVisitor(entity.Instance, component.Type);
                        visitor.PushContainer(component.Properties);
                        componentBaseline.Properties.Visit(visitor);
                    }
                }
            }
            else
            {
                // This is the first time we encounter this entity
                // This should normally be called on project load
                // Create a new baseline entity
                entityBaseline = m_BaselineRegistry.CreateEntity(entity.Id, entity.Name);
            }

            // Take an in memory copy of the entity to diff next frame
            // @IMPORTANT The baseline is taken each time the entity is changed in some way
            // @NOTE Even if we are NOT recording modifications the baseline must still be updated
            entityBaseline.Enabled = entity.Enabled;
            entityBaseline.Name = entity.Name;
            entityBaseline.Layer = entity.Layer;
            entityBaseline.Static = entity.Static;
            
            foreach (var component in entity.Components)
            {
                var componentBaseline = entityBaseline.GetOrAddComponent(component.Type);
                PropertyContainer.Transfer(component, componentBaseline);
            }
        }
        
        private void QueueSyncPrefabInstancesForGroup(TinyEntityGroup.Reference prefab)
        {
            foreach (var instance in GetPrefabInstances(prefab))
            {
                if (m_PrefabInstanceChangeQueue.Contains(instance.Ref))
                {
                    continue;
                }

                m_PrefabInstanceChangeQueue.Add(instance.Ref);
            }
        }
        
        /// <summary>
        /// Brute force algorithm to sync a prefab instance with it's source
        /// </summary>
        /// <param name="prefabInstance">The instance to sync</param>
        /// <returns>True if a data model change was made; false otherwise</returns>
        private bool SyncPrefabInstance(TinyPrefabInstance prefabInstance)
        {
            var dataModelChanged = false;
            
            // Load the group from the main registry
            var instanceGroup = prefabInstance.EntityGroup.Dereference(Registry);
            var prefabGroup = prefabInstance.PrefabEntityGroup.Dereference(Registry);

            if (null == instanceGroup || null == prefabGroup)
            {
                return false;
            }
            
            var createdEntities = HashSetPool<TinyEntity.Reference>.Get();
            var entityReferenceRemap = DictionaryPool<TinyEntity.Reference, TinyEntity.Reference>.Get();
            
            try
            {
                // Push all entities from the original prefab group to the created set
                foreach (var @ref in prefabGroup.Entities)
                {
                    createdEntities.Add(@ref);
                }
    
                // Sync components
                for (var i = 0; i < prefabInstance.Entities.Count; i++)
                {
                    var instanceReference = prefabInstance.Entities[i];
                    var instance = instanceReference.Dereference(Registry);
    
                    if (null == instance)
                    {
                        prefabInstance.Entities.RemoveAt(i--);
                        continue;
                    }
    
                    // This entity has a matching instance for this `PrefabInstance`
                    createdEntities.Remove(instance.Instance.Source);
    
                    // Find the matching Source for this EntityInstance
                    var prefab = instance.Instance.Source.Dereference(Registry);
    
                    // The source prefab is missing OR has changed groups, destroy it
                    if (null == prefab || prefab.EntityGroup != prefabGroup)
                    {
                        // Destroy the entity instance and cleanup
                        instanceGroup.RemoveEntityReference(instanceReference);
                        Registry.Unregister(instanceReference.Id);
                        m_BaselineRegistry.Unregister(instanceReference.Id);
                        
                        // Remove the entity from the PrefabInstance
                        prefabInstance.Entities.RemoveAt(i);
    
                        // Entity has been destroyed, data model has been changed
                        dataModelChanged = true;

                        i--;
                        continue;
                    }
    
                    // Sync components and re-apply modifications
                    ApplyPrefabToInstance(prefab, instance);
    
                    // Keep remap information for this instance since we don't know the exact changes
                    entityReferenceRemap.Add(prefab.Ref, instanceReference);
                }
    
                // Sync new entities
                foreach (var prefabReference in createdEntities)
                {
                    var prefab = prefabReference.Dereference(Registry);
                    
                    // @NOTE Instantiate will sync prefab components
                    var instance = CreateEntityInstance(prefab, prefabInstance.Ref);
                        
                    instanceGroup.AddEntityReference(instance.Ref);
                    prefabInstance.Entities.Add(instance.Ref);
                        
                    // Keep remap information
                    entityReferenceRemap.Add(prefabReference,  instance.Ref);
                    
                    // Entity has been destroyed, data model has been changed
                    dataModelChanged = true;
                }
    
                // Sync references (remapping)
                var remapVisitor = new EntityRemapVisitor(entityReferenceRemap);
                foreach (var instanceReference in prefabInstance.Entities)
                {
                    var instance = instanceReference.Dereference(Registry);
                    
                    foreach (var component in instance.Components)
                    {
                        component.Visit(remapVisitor);
                    }
                }

                // Sync roots
                foreach (var instanceReference in prefabInstance.Entities)
                {
                    var instance = instanceReference.Dereference(Registry);
                    
                    // Keep root parents in sync
                    if (instance.Parent().Equals(TinyEntity.Reference.None))
                    {
                        instance.SetParent(prefabInstance.Parent);
                    }
                }

                // Sync baseline
                foreach (var instanceReference in prefabInstance.Entities)
                {
                    var instance = instanceReference.Dereference(Registry);
                    var baseline = instanceReference.Dereference(m_BaselineRegistry) ?? m_BaselineRegistry.CreateEntity(instance.Id, instance.Name);
    
                    RegisterTypesForEntity(instance);
    
                    baseline.Enabled = instance.Enabled;
                    baseline.Name = instance.Name;
                    baseline.Layer = instance.Layer;
                    baseline.Static = instance.Static;
                    
                    // Keep the baseline entity in sync so these changes are not recorded as `PrefabModifications`
                    // Ideally we want to use Save and Restore but in practice it's just too slow
                    // entityBaseline.Restore(entity.Save())
                    foreach (var component in instance.Components)
                    {
                        var componentBaseline = baseline.GetOrAddComponent(component.Type);
                        PropertyContainer.Transfer(component, componentBaseline);
                    }
                }

                // Sync ordering/structure
                dataModelChanged |= ReorderPrefabInstance(Registry, prefabInstance, createdEntities.Count > 0);
            }
            finally
            {
                DictionaryPool<TinyEntity.Reference, TinyEntity.Reference>.Release(entityReferenceRemap);
                HashSetPool<TinyEntity.Reference>.Release(createdEntities);
            }

            // @HACK 
            // We should be returning `dataModelChanged` here
            // However there is an issue where we have no efficient way to determine that a structural change was made to the prefab (re-parenting)
            return true;
        }

        /// <summary>
        /// Re-orders the prefab instance and entity group to match the prefab structure
        /// </summary>
        private static bool ReorderPrefabInstance(IRegistry registry, TinyPrefabInstance prefabInstance, bool forced = false)
        {
            var instanceGroup = prefabInstance.EntityGroup.Dereference(registry);
            var prefabGroup = prefabInstance.PrefabEntityGroup.Dereference(registry);
            
            // Preserve ordering
            var needsReordering = forced || prefabInstance.Entities
                                      .Select(instanceReference => instanceReference.Dereference(registry))
                                      .Select(instance => prefabGroup.Entities.IndexOf(instance.Instance.Source))
                                      .Where((prefabGroupIndex, i) => i != prefabGroupIndex)
                                      .Any();

            if (!needsReordering)
            {
                return false;
            }
            
            var entities = new TinyEntity.Reference[prefabInstance.Entities.Count];
            var indices = new int[prefabInstance.Entities.Count];
            
            // Rebuild the instance entities set in the correct order
            foreach (var instanceReference in prefabInstance.Entities)
            {
                var instance = instanceReference.Dereference(registry);
                
                var prefabGroupIndex = prefabGroup.Entities.IndexOf(instance.Instance.Source);
                var instanceGroupIndex = instanceGroup.Entities.IndexOf(instanceReference);

                entities[prefabGroupIndex] = instanceReference;
                indices[prefabGroupIndex] = instanceGroupIndex;
            }
            
            // Sort the indices for the instance correctly
            Array.Sort(indices);

            // Wipe out the instance group and prefab instance
            prefabInstance.Entities.Clear();
            foreach (var entity in entities)
            {
                instanceGroup.RemoveEntityReference(entity);
            }

            // Re-add in the correct order
            for (var i = 0; i < entities.Length; i++)
            {
                prefabInstance.Entities.Add(entities[i]);
                instanceGroup.Entities.Insert(indices[i], entities[i]);
            }

            return true;
        }

        /// <summary>
        /// This method ensures that our baseline registry types match the live registry types
        /// </summary>
        /// <param name="entity"></param>
        private void RegisterTypesForEntity(IRegistryObject entity)
        {
            foreach (var type in EnumerateTypes(entity.Registry, entity as TinyEntity))
            {
                var baselineType = m_BaselineRegistry.FindById(type.Id);
                
                if (null != baselineType && ReferenceEquals(baselineType, type))
                {
                    continue;
                }
                
                m_BaselineRegistry.Register(type);
            }
        }

        private static IEnumerable<TinyType> EnumerateTypes(IRegistry registry, TinyEntity entity)
        {
            foreach (var component in entity.Components)
            {
                var componentType = component.Type.Dereference(registry);

                yield return componentType;
                
                foreach (var fieldType in EnumerateFieldTypes(registry, componentType))
                {
                    yield return fieldType;
                }
            }
        }

        private static IEnumerable<TinyType> EnumerateFieldTypes(IRegistry registry, TinyType type)
        {
            foreach (var field in type.Fields)
            {
                var fieldType = field.FieldType.Dereference(registry);

                if (fieldType.IsPrimitive)
                {
                    continue;
                }

                yield return fieldType;

                foreach (var inner in EnumerateFieldTypes(registry, fieldType))
                {
                    yield return inner;
                }
            }
        }

        internal static void RemapEntities(IEnumerable<TinyEntity> entities, IDictionary<TinyEntity.Reference, TinyEntity.Reference> remap)
        {
            var visitor = new EntityRemapVisitor(remap);

            foreach (var entity in entities)
            {
                foreach (var component in entity.Components)
                {
                    component.Visit(visitor);
                }
            }
        }

        /// <summary>
        /// Creates a prefab from the given entities
        /// </summary>
        /// <param name="group"></param>
        /// <param name="entities"></param>
        private void CreatePrefab(TinyEntityGroup group, IEnumerable<TinyEntity> entities)
        {
            var prefabEntities = ListPool<TinyEntity>.Get();

            try
            {
                var remap = new Dictionary<TinyEntity.Reference, TinyEntity.Reference>();

                foreach (var sourceEntity in entities)
                {
                    var isPrefabInstanceRootTransform = prefabEntities.Count == 0;
                    var prefabEntity = Registry.CreateEntity(TinyId.New(), sourceEntity.Name);
                    var migration = new MigrationContainer(sourceEntity);

                    // Mask specific properties
                    migration.Remove("Id");
                    migration.Remove("Name");
                    migration.Remove("Components");

                    // Copy entity properties
                    PropertyContainer.Transfer(migration, prefabEntity);

                    foreach (var sourceComponent in sourceEntity.Components)
                    {
                        var prefabComponent = prefabEntity.AddComponent(sourceComponent.Type);

                        if (isPrefabInstanceRootTransform)
                        {
                            if (sourceComponent.Type.Equals(TypeRefs.Core2D.TransformNode) ||
                                sourceComponent.Type.Equals(TypeRefs.Core2D.TransformLocalPosition))
                            {
                                continue;
                            }
                        }
                        
                        prefabComponent.CopyFrom(sourceComponent);
                    }
                    
                    prefabEntity.EntityGroup = group;

                    group.AddEntityReference(prefabEntity.Ref);
                    prefabEntities.Add(prefabEntity);
                    remap.Add(sourceEntity.Ref, prefabEntity.Ref);
                }

                RemapEntities(prefabEntities, remap);
            }
            finally
            {
                ListPool<TinyEntity>.Release(prefabEntities);
            }
        }

        /// <summary>
        /// Creates a prefab from the given entities and converts the to an instance of that prefab
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="entities"></param>
        /// <returns></returns>
        public void CreatePrefabAndConvertToInstance(TinyEntityGroup prefab, IEnumerable<TinyEntity> entities)
        {
            // Create the prefab itself
            CreatePrefab(prefab, entities);
            
            // Create the instance of the prefab
            var instance = Registry.CreatePrefabInstance(TinyId.New(), prefab.Name);

            // Add the instance to the entity group
            var instanceGroup = entities.Select(e => e.EntityGroup).NotNull().First();
            instanceGroup.PrefabInstances.Add(instance.Ref);
            
            instance.PrefabEntityGroup = prefab.Ref;
            instance.EntityGroup = instanceGroup.Ref;

            // Initialize `EntityInstance` objects and add to the `PrefabInstance`
            var i = 0;
            foreach (var instanceEntity in entities)
            {
                var prefabEntity = prefab.Entities[i++];
                
                instanceEntity.Instance = new TinyEntityInstance(instanceEntity)
                {
                    Source = prefabEntity,
                    PrefabInstance = instance.Ref
                };
                    
                instance.Entities.Add(instanceEntity.Ref);

                // If this is our root entity
                if (i == 1)
                {
                    // Record modifications immediately for special components
                    foreach (var ignoredType in s_RootIgnoredTypes)
                    {
                        if (!instanceEntity.HasComponent(ignoredType))
                        {
                            continue;
                        }
                        
                        var visitor = new PrefabModificationVisitor(instanceEntity.Instance, ignoredType);
                        visitor.PushContainer(instanceEntity.GetComponent(ignoredType).Properties);
                        prefabEntity.Dereference(Registry).GetComponent(ignoredType).Properties.Visit(visitor);
                    }
                    
                }
            }

            // Queue up a sync for this instance
            m_PrefabInstanceChangeQueue.Add(instance.Ref);
        }

        /// <summary>
        /// Creates a new instance of the given prefab
        /// </summary>
        /// <param name="source">The source prefab</param>
        /// <returns>Instance of the prefab</returns>
        public TinyPrefabInstance Instantiate(TinyEntityGroup source)
        {
            var instance = Registry.CreatePrefabInstance(TinyId.New(), source.Name);
            instance.PrefabEntityGroup = source.Ref;
            
            var entities = new TinyEntity[source.Entities.Count];
            var remap = new Dictionary<TinyEntity.Reference, TinyEntity.Reference>();
                
            for (var i = 0; i < source.Entities.Count; i++)
            {
                var prefab = source.Entities[i].Dereference(Registry);
                var entity = CreateEntityInstance(prefab, instance.Ref);
                remap.Add(prefab.Ref, entity.Ref);
                instance.Entities.Add(entity.Ref);

                entities[i] = entity;
                entities[i].Instance.PrefabInstance = instance.Ref;
            }

            RemapEntities(entities, remap);
            
            return instance;
        }

        /// <summary>
        /// Creates a new instance of the given prefab
        /// </summary>
        /// <param name="source">The source prefab</param>
        /// <param name="prefabInstance"></param>
        /// <returns>Instance of the prefab</returns>
        private TinyEntity CreateEntityInstance(TinyEntity source, TinyPrefabInstance.Reference prefabInstance)
        {
            var instance = Registry.CreateEntity(TinyId.New(), source.Name);
            instance.Instance = new TinyEntityInstance(instance)
            {
                Source = source.Ref,
                PrefabInstance = prefabInstance
            };
    
            var migration = new MigrationContainer(source);

            // Mask specific properties
            migration.Remove("Id");
            migration.Remove("Name");
            migration.Remove("Components");

            // Copy entity properties
            PropertyContainer.Transfer(migration, instance);

            ApplyPrefabToInstance(source, instance);

            // Take an in memory copy of the entity to diff next frame
            CreateBaseline(instance);

            return instance;
        }

        public void CreateBaseline(TinyEntity instance)
        {
            var baseline = m_BaselineRegistry.CreateEntity(instance.Id, instance.Name);

            baseline.Enabled = instance.Enabled;
            baseline.Name = instance.Name;
            baseline.Layer = instance.Layer;
            baseline.Static = instance.Static;
            
            foreach (var component in instance.Components)
            {
                var componentBaseline = baseline.AddComponent(component.Type);
                PropertyContainer.Transfer(component, componentBaseline);
            }
        }
        
        /// <summary>
        /// Sets up the prefab instance 
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="instance"></param>
        public void ApplyPrefabToInstance(TinyEntity prefab, TinyEntity instance)
        {
            if (null == prefab)
            {
                return;
            }

            // Apply top level entity modifications to the instance
            ApplyEntityModificationsToInstance(prefab, instance);
            ApplyPrefabComponentsToInstance(prefab, instance);
            ApplyComponentModifications(instance.Instance.Modifications, instance);
        }

        public void ApplyPrefabAttributesToInstance(TinyEntity prefab, TinyEntity instance)
        {
            var prefabInstance = instance.Instance;

            Assert.IsNotNull(prefabInstance);
            
            foreach (var prefabComponent in prefab.Components)
            {
                var type = prefabComponent.Type;
                
                if (prefabInstance.RemovedComponents.Contains(type))
                {
                    // This is an explicitly removed component
                    // Don't sync anything to the instance
                    continue;
                }
                
                var instanceComponent = instance.GetComponent(type);
                
                // Don't serialize this component
                PrefabAttributeUtility.AddPrefabComponentAttributes(instanceComponent);
            }
        }
        
        private static void ApplyEntityModificationsToInstance(TinyEntity prefab, TinyEntity instance)
        {
            if (!instance.Instance.EntityModificationFlags.HasFlag(EntityModificationFlags.Enabled))
            {
                instance.Enabled = prefab.Enabled;
            }
            
            if (!instance.Instance.EntityModificationFlags.HasFlag(EntityModificationFlags.Name))
            {
                instance.Name = prefab.Name;
            }
            
            if (!instance.Instance.EntityModificationFlags.HasFlag(EntityModificationFlags.Layer))
            {
                instance.Layer = prefab.Layer;
            }
            
            if (!instance.Instance.EntityModificationFlags.HasFlag(EntityModificationFlags.Static))
            {
                instance.Static = prefab.Static;
            }
        }
        
        /// <summary>
        /// Adds all prefab components to the given prefab instance using the prefab values
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="instance"></param>
        private static void ApplyPrefabComponentsToInstance(TinyEntity prefab, TinyEntity instance)
        {
            var prefabInstance = instance.Instance;

            Assert.IsNotNull(prefabInstance);

            var offset = 0;
            
            for (var i = 0; i < prefab.Components.Count; i++)
            {
                var prefabComponent = prefab.Components[i];
                var type = prefabComponent.Type;
                
                if (prefabInstance.RemovedComponents.Contains(type))
                {
                    // This is an explicitly removed component
                    // Don't sync anything to the instance
                    offset++;
                    continue;
                }

                TinyObject instanceComponent;

                var srcIndex = instance.GetComponentIndex(type);
                var dstIndex = i - offset;
                
                if (srcIndex == -1)
                {
                    instanceComponent = instance.AddComponent(type);

                    // Fix-up to position in the components array
                    instance.Components.RemoveAt(instance.Components.Count - 1);
                    instance.Components.Insert(dstIndex, instanceComponent);
                }
                else if (srcIndex != dstIndex)
                {
                    instanceComponent = instance.Components[srcIndex];
                    instance.Components.RemoveAt(srcIndex);
                    instance.Components.Insert(dstIndex, instanceComponent);
                }
                else
                {
                    instanceComponent = instance.Components[srcIndex];
                }

                // Don't serialize this component
                PrefabAttributeUtility.AddPrefabComponentAttributes(instanceComponent);
                instanceComponent.CopyFrom(prefabComponent);
            }

            // Support removing components from prefab instances
            for (var i = 0; i < instance.Components.Count; i++)
            {
                var component = instance.Components[i];
                
                // @TODO We need a way of detecting a component as being an instance (that is undo-redo compliant)
                // There is an issue when applying an override component to a prefab and undo-ing
                // The component will be removed instead of reverting to an override component
                if (component.HasAttribute<NonSerializedInContext>() && !prefab.HasComponent(component.Type))
                {
                    instance.Components.RemoveAt(i--);
                }
            }

            for (var i = 0; i < prefabInstance.RemovedComponents.Count; i++)
            {
                var removedComponent = prefabInstance.RemovedComponents[i];
                
                // This is a removed component that NO longer exists on the prefab
                // We can safely clean it up
                if (!prefab.HasComponent(removedComponent))
                {
                    prefabInstance.RemovedComponents.RemoveAt(i--);
                }
            }
        }

        /// <inheritdoc />
        public void ApplyInstanceToPrefab(TinyPrefabInstance instance)
        {
            foreach (var entity in instance.Entities.Deref(Registry))
            {
                var prefabEntity = entity.Instance.Source.Dereference(Registry);

                // Apply any added components
                foreach (var added in entity.Components.Where(c => !prefabEntity.HasComponent(c.Type)))
                {
                    var prefabComponent = prefabEntity.AddComponent(added.Type);
                    prefabComponent.CopyFrom(added);
                }
                
                // Apply any removed components
                foreach (var removed in entity.Instance.RemovedComponents)
                {
                    prefabEntity.RemoveComponent(removed);
                }
                
                // Apply component modifications
                ApplyComponentModificationsToPrefab(entity.Instance.Modifications, entity);
                
                // Apply entity modifications
                ApplyEntityModificationsToPrefab(EntityModificationFlags.All, entity);
            }

            // @TODO Need to add any overriden children.
            //       In order to do this we need efficient access to the transform hierarchy which is currently only in the `Unity.Tiny.Editor` assembly
            //       For now we will use a custom method to pull out the entities based on transform node. (It's not the most optimal but gets the job done)
            var entities = GetEntitiesForInstance(Registry, instance);

            // Apply any newly added entity overrides
            ApplyAddedEntitiesToPrefab(instance, entities);

            // Apply any removed entity overrides
            ApplyRemovedEntitiesToPrefab(instance, entities);

            // Perform any remapping (i.e. any overriden fields on the instance that point to local entities)
            RemapPrefabFromInstance(instance);

            // Apply top-level naming
            var prefabGroup = instance.PrefabEntityGroup.Dereference(Registry);
            var root = instance.Entities.FirstOrDefault().Dereference(Registry);
            instance.Name = root.Name;
            prefabGroup.Name = root.Name;
            
            QueueSyncPrefabInstancesForGroup(instance.PrefabEntityGroup);
        }

        private static void RemapPrefabFromInstance(TinyPrefabInstance prefabInstance)
        {
            var remap = DictionaryPool<TinyEntity.Reference, TinyEntity.Reference>.Get();
            var entities = ListPool<TinyEntity>.Get();

            try
            {
                for (var i = 0; i < prefabInstance.Entities.Count; i++)
                {
                    var entity = prefabInstance.Entities[i].Dereference(prefabInstance.Registry);
                    var prefab = entity.Instance.Source.Dereference(prefabInstance.Registry);
                    remap.Add(entity.Ref, prefab.Ref);
                    
                    entities.Add(prefab);
                }
                
                RemapEntities(entities, remap);
            }
            finally
            {
                DictionaryPool<TinyEntity.Reference, TinyEntity.Reference>.Release(remap);
                ListPool<TinyEntity>.Release(entities);
            }
        }
        
        internal static void RemapInstanceFromPrefab(TinyPrefabInstance prefabInstance)
        {
            var remap = DictionaryPool<TinyEntity.Reference, TinyEntity.Reference>.Get();
            var entities = ListPool<TinyEntity>.Get();

            try
            {
                for (var i = 0; i < prefabInstance.Entities.Count; i++)
                {
                    var entity = prefabInstance.Entities[i].Dereference(prefabInstance.Registry);
                    var prefab = entity.Instance.Source.Dereference(prefabInstance.Registry);
                    remap.Add(prefab.Ref, entity.Ref);
                    
                    entities.Add(entity);
                }
                
                RemapEntities(entities, remap);
            }
            finally
            {
                DictionaryPool<TinyEntity.Reference, TinyEntity.Reference>.Release(remap);
                ListPool<TinyEntity>.Release(entities);
            }
        }

        public void RevertInstanceToPrefab(TinyPrefabInstance instance)
        {
            foreach (var entity in instance.Entities.Deref(Registry))
            {
                RevertEntityModificationsForInstance(EntityModificationFlags.All, entity);
                RevertComponentModificationsForInstance(entity.Instance.Modifications, entity);

                var removed = entity.Instance.RemovedComponents;
                foreach (var component in removed)
                {
                    RevertRemovedComponentForInstance(entity, component);
                }
            }
        }
        
        /// <summary>
        /// Returns all entities that are part of the given prefab instance (including override entities)
        /// </summary>
        private static IList<TinyEntity> GetEntitiesForInstance(IRegistry registry, TinyPrefabInstance instance)
        {
            var root = instance.Entities.First().Dereference(registry);
            var group = root.EntityGroup;
            var startIndex = group.Entities.IndexOf(root.Ref);

            var entities = new List<TinyEntity>();
            
            var validParents = HashSetPool<TinyEntity.Reference>.Get();
            var ignoredParents = HashSetPool<TinyEntity.Reference>.Get();

            validParents.Add(root.Ref);

            try
            {
                // Run through the tree starting at our known root
                for (var i = startIndex + 1; i < group.Entities.Count; i++)
                {
                    var candidate = group.Entities[i].Dereference(registry);

                    if (null != candidate.Instance && !candidate.Instance.PrefabInstance.Equals(instance.Ref))
                    {
                        // This entity is part of another prefab instance, skip
                        // Track as an ignore so we don't early out
                        ignoredParents.Add(candidate.Ref);
                        continue;
                    }
                    
                    var parent = candidate.Parent();

                    if (ignoredParents.Contains(parent))
                    {
                        // This node is part of an ignored tree
                        ignoredParents.Add(candidate.Ref);
                        continue;
                    }

                    if (validParents.Contains(parent))
                    {
                        validParents.Add(candidate.Ref);
                        continue;
                    }

                    // We have encountered a node that is not part of our tree in any way
                    // Assuming the entities are ordered correctly we are able to early exit here
                    break;
                }
                
                entities.AddRange(validParents.Select(e => e.Dereference(registry)));
            }
            finally
            {
                HashSetPool<TinyEntity.Reference>.Release(validParents);
                HashSetPool<TinyEntity.Reference>.Release(ignoredParents);
            }

            return entities;
        }
        
        /// <inheritdoc />
        public void ApplyEntityModificationsToPrefab(EntityModificationFlags flags, TinyEntity entity)
        {
            var instance = entity.Instance;

            if (null == instance)
            {
                return;
            }

            var prefab = entity.Instance.Source.Dereference(entity.Registry);

            if (null == prefab)
            {
                return;
            }

            if (flags.HasFlag(EntityModificationFlags.Enabled))
            {
                prefab.Enabled = entity.Enabled;
                instance.EntityModificationFlags &= ~EntityModificationFlags.Enabled;
            }
        
            if (flags.HasFlag(EntityModificationFlags.Name))
            {
                prefab.Name = entity.Name;
                instance.EntityModificationFlags &= ~EntityModificationFlags.Name;
            }
        
            if (flags.HasFlag(EntityModificationFlags.Layer))
            {
                prefab.Layer = entity.Layer;
                instance.EntityModificationFlags &= ~EntityModificationFlags.Layer;
            }
            
            if (flags.HasFlag(EntityModificationFlags.Static))
            {
                prefab.Static = entity.Static;
                instance.EntityModificationFlags &= ~EntityModificationFlags.Static;
            }
        }
        
        /// <inheritdoc />
        public void ApplyComponentModificationsToPrefab(IEnumerable<IPropertyModification> modifications, TinyEntity entity)
        {
            var instance = entity.Instance;

            if (null == instance)
            {
                return;
            }

            var prefab = entity.Instance.Source.Dereference(entity.Registry);

            if (null == prefab)
            {
                return;
            }

            var isPrefabInstanceRootTransform = PrefabTransformUtility.IsPrefabInstanceRootTransform(entity);

            var toRemove = ListPool<IPropertyModification>.Get();

            try
            {
                foreach (var modification in modifications)
                {
                    if (isPrefabInstanceRootTransform && s_RootIgnoredTypes.Contains(modification.Target))
                    {
                        // This is a special type that should NOT be recorded for the root entity
                        // (e.g. position and parenting information should ALWAYS be overriden by an instance)
                        continue;
                    }
                    
                    var path = modification.GetFullPath();

                    var prefabComponent = prefab.GetComponent(modification.Target);
                    var instanceComponent = entity.GetComponent(modification.Target);

                    if (null == prefabComponent || null == instanceComponent)
                    {
                        continue;
                    }

                    var prefabResolution = path.Resolve(prefabComponent);
                    var instanceResolution = path.Resolve(instanceComponent);

                    if (!prefabResolution.success || !instanceResolution.success)
                    {
                        toRemove.Add(modification);
                        return;
                    }

                    switch (prefabResolution.property)
                    {
                        case IValueClassProperty valueClassProperty:
                            valueClassProperty.SetObjectValue(prefabResolution.container, instanceResolution.value);
                            break;
                        case IListClassProperty listClassProperty:
                            listClassProperty.SetObjectAt(prefabResolution.container, prefabResolution.listIndex, instanceResolution.value);
                            break;
                    }

                    toRemove.Add(modification);
                }
                
                foreach (var modification in toRemove)
                {
                    instance.Modifications.Remove(modification);
                }
            }
            finally
            {
                ListPool<IPropertyModification>.Release(toRemove);
            }
            
            RemapPrefabFromInstance(entity.Instance.PrefabInstance.Dereference(Registry));
        }

        public void ApplyAddedEntityToPrefab(TinyPrefabInstance instance, TinyEntity entity)
        {
            // This method will return all entities that are part of our instance (including overrides but ignoring other prefabs)
            var entities = GetEntitiesForInstance(Registry, instance);
            
            // Make sure this entity is part of the instance hierarchy
            Assert.IsTrue(entities.Contains(entity));

            // Filter out any override entities (except for the one we are adding)
            var target = entities.Where(e => e.HasEntityInstanceComponent() || e == entity).ToList();
            
            ApplyAddedEntitiesToPrefab(instance, target);
            
            // Perform any remapping (i.e. any overriden fields on the instance that point to local entities)
            RemapPrefabFromInstance(instance);
            
            QueueSyncPrefabInstancesForGroup(instance.PrefabEntityGroup);
        }

        public void ApplyRemovedEntityToPrefab(TinyPrefabInstance instance, TinyEntity entity)
        {
            // This method will return all entities that are part of our instance (including overrides but ignoring other prefabs)
            var entities = GetEntitiesForInstance(Registry, instance);
            
            ApplyRemovedEntitiesToPrefab(instance, entities);
            
            QueueSyncPrefabInstancesForGroup(instance.PrefabEntityGroup);
        }

        public void RevertRemovedComponentForInstance(TinyEntity entity, TinyType.Reference type)
        {
            entity.Instance.RemovedComponents.Remove(type);

            var component = entity.AddComponent(type);
            var prefab = entity.Instance.Source.Dereference(entity.Registry).GetComponent(type);

            component.CopyFrom(prefab);
        }

        /// <summary>
        /// Applies any newly added entities in the given list to the prefab group
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="instanceEntities"></param>
        private void ApplyAddedEntitiesToPrefab(TinyPrefabInstance instance, IList<TinyEntity> instanceEntities)
        {
            var prefabGroup = instance.PrefabEntityGroup.Dereference(Registry);
            
            for (var i = 0; i < instanceEntities.Count; i++)
            {
                var entityInstance = instanceEntities[i].Instance;

                if (null != entityInstance)
                {
                    // This is a prefab instance itself
                    continue;
                }

                // This is a newly added entity
                // Create it with data from the source
                var prefabEntity = Registry.CreateEntity(TinyId.New(), instanceEntities[i].Name);
                prefabEntity.EntityGroup = prefabGroup;

                foreach (var component in instanceEntities[i].Components)
                {
                    var prefabComponent = prefabEntity.AddComponent(component.Type);
                    prefabComponent.CopyFrom(component);
                }
                
                instanceEntities[i].Instance = new TinyEntityInstance(instanceEntities[i])
                {
                    Source = prefabEntity.Ref,
                    PrefabInstance = instance.Ref
                };
                
                ApplyPrefabAttributesToInstance(prefabEntity, instanceEntities[i]);
                
                CreateBaseline(instanceEntities[i]);

                // This feels very fragile... add some assertions?
                prefabGroup.Entities.Insert(i, prefabEntity.Ref);
                instance.Entities.Insert(i, instanceEntities[i].Ref);
            }
        }

        private void ApplyRemovedEntitiesToPrefab(TinyPrefabInstance instance, IList<TinyEntity> instanceEntities)
        {
            var prefabGroup = instance.PrefabEntityGroup.Dereference(Registry);

            for (int prefabEntityIndex = 0, instanceEntityIndex = 0; prefabEntityIndex < prefabGroup.Entities.Count; prefabEntityIndex++, instanceEntityIndex++)
            {
                var prefabEntity = prefabGroup.Entities[prefabEntityIndex];

                if (instanceEntityIndex < instanceEntities.Count && instanceEntities[instanceEntityIndex].Instance.Source.Equals(prefabEntity))
                {
                    continue;
                }
                
                prefabGroup.Entities.RemoveAt(prefabEntityIndex--);
                Registry.Unregister(prefabEntity.Id);
            }
        }
        
        /// <inheritdoc />
        public void RevertEntityModificationsForInstance(EntityModificationFlags flags, TinyEntity entity)
        {
            var instance = entity.Instance;

            if (null == instance)
            {
                return;
            }

            var prefab = entity.Instance.Source.Dereference(entity.Registry);

            if (null == prefab)
            {
                return;
            }

            var baseline = m_BaselineRegistry.FindById<TinyEntity>(entity.Id);

            if (flags.HasFlag(EntityModificationFlags.Enabled))
            {
                entity.Enabled = prefab.Enabled;
                baseline.Enabled = prefab.Enabled;
                instance.EntityModificationFlags &= ~EntityModificationFlags.Enabled;
            }
        
            if (flags.HasFlag(EntityModificationFlags.Name))
            {
                entity.Name = prefab.Name;
                baseline.Name = prefab.Name;
                instance.EntityModificationFlags &= ~EntityModificationFlags.Name;
            }
        
            if (flags.HasFlag(EntityModificationFlags.Layer))
            {
                entity.Layer = prefab.Layer;
                baseline.Layer = prefab.Layer;
                instance.EntityModificationFlags &= ~EntityModificationFlags.Layer;
            }
            
            if (flags.HasFlag(EntityModificationFlags.Static))
            {
                entity.Static = prefab.Static;
                baseline.Static = prefab.Static;
                instance.EntityModificationFlags &= ~EntityModificationFlags.Static;
            }
        }
        
        /// <inheritdoc />
        public void RevertComponentModificationsForInstance(IEnumerable<IPropertyModification> modifications, TinyEntity entity)
        {
            var instance = entity.Instance;

            if (null == instance)
            {
                return;
            }

            var prefab = entity.Instance.Source.Dereference(entity.Registry);

            if (null == prefab)
            {
                return;
            }

            var baseline = m_BaselineRegistry.FindById<TinyEntity>(entity.Id);

            var toRemove = ListPool<IPropertyModification>.Get();

            try
            {
                foreach (var modification in modifications)
                {
                    var path = modification.GetFullPath();

                    var prefabComponent = prefab.GetComponent(modification.Target);
                    var instanceComponent = entity.GetComponent(modification.Target);
                    var baselineComponent = baseline.GetComponent(modification.Target);

                    if (null == prefabComponent || null == instanceComponent)
                    {
                        continue;
                    }

                    var prefabResolution = path.Resolve(prefabComponent);
                    var instanceResolution = path.Resolve(instanceComponent);
                    var baselineResolution = path.Resolve(baselineComponent);

                    if (!prefabResolution.success || !instanceResolution.success)
                    {
                        toRemove.Add(modification);
                        return;
                    }

                    switch (instanceResolution.property)
                    {
                        case IValueClassProperty valueClassProperty:
                            valueClassProperty.SetObjectValue(instanceResolution.container, prefabResolution.value);
                            valueClassProperty.SetObjectValue(baselineResolution.container, prefabResolution.value);
                            break;
                        case IListClassProperty listClassProperty:
                            listClassProperty.SetObjectAt(instanceResolution.container, instanceResolution.listIndex, prefabResolution.value);
                            listClassProperty.SetObjectAt(baselineResolution.container, baselineResolution.listIndex, prefabResolution.value);
                            break;
                    }

                    toRemove.Add(modification);
                }

                foreach (var modification in toRemove)
                {
                    instance.Modifications.Remove(modification);
                }
            }
            finally
            {
                ListPool<IPropertyModification>.Release(toRemove);
            }
        }
        
        /// <summary>
        /// Applies a set of property modifications to the given entity
        /// </summary>
        /// <param name="modifications">Set of property modifications</param>
        /// <param name="entity">Target entity</param>
        private static void ApplyComponentModifications(IEnumerable<IPropertyModification> modifications, TinyEntity entity)
        {
            foreach (var modification in modifications)
            {
                var type = modification.Target;
                
                var component = entity.GetComponent(type);

                if (null == component)
                {
                    // We can silently bail out here
                    // This can happen if the prefab instance has removed the component
                    continue;
                }
                
                ApplyModificationToComponent(modification, component);
            }
        }
        
        /// <summary>
        /// Applies the given set of modifications to the given component
        ///
        /// @NOTE Only modifications with the matching target (e.g. `modification.Target == component.Type`) will be applied
        /// </summary>
        /// <param name="modifications">Set of modifications to apply</param>
        /// <param name="component">Target component</param>
        public void ApplyComponentModifications(IEnumerable<IPropertyModification> modifications, TinyObject component)
        {
            foreach (var modification in modifications)
            {
                var type = modification.Target;

                if (!component.Type.Equals(type))
                {
                    continue;
                }

                ApplyModificationToComponent(modification, component);
            }
        }

        /// <summary>
        /// Applies a single modification to a component
        ///
        /// @NOTE No validation is performed on the `modification.Target`
        /// </summary>
        /// <param name="modification"></param>
        /// <param name="component"></param>
        /// <returns>True; if the modification was successfully applied</returns>
        private static bool ApplyModificationToComponent(IPropertyModification modification, TinyObject component)
        {
            var path = modification.GetFullPath();
            var resolution = path.Resolve(component);

            if (!resolution.success)
            {
                // Path could not be resolved
                // This usually means the field was removed or renamed
                // @TODO Can we recover from this?
                Debug.LogWarning($"[PrefabManager] Failed to resolve modification Component=[{component.Type.Name}] Path=[{path}]");
                return false;
            }

            if (PropertyModificationConverter.GetSerializedTypeId((resolution.property as IValueProperty)?.ValueType) != modification.TypeCode)
            {
                // There is a type mismatch between the modification and the property itself
                // @TODO Can we recover from this?
                Debug.LogWarning($"[PrefabManager] Failed to resolve modification Component=[{component.Type.Name}] Path=[{path}]");
                return false;
            }
                
            switch (resolution.property)
            {
                case IValueClassProperty valueClassProperty:
                    valueClassProperty.SetObjectValue(resolution.container, modification.Value);
                    break;
                case IListClassProperty listClassProperty:
                    listClassProperty.SetObjectAt(resolution.container, resolution.listIndex, modification.Value);
                    break;
            }

            return true;
        }
        
        /// <summary>
        /// Returns all `PrefabInstances` that have been spawned of the given EntityGroup
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        private IEnumerable<TinyPrefabInstance> GetPrefabInstances(TinyEntityGroup.Reference prefab)
        {
            return Registry.FindAllByType<TinyPrefabInstance>().Where(instance => instance.PrefabEntityGroup.Equals(prefab));
        }

        /// <summary>
        /// Returns the `EntityGroup` that the given reference is a part of
        /// </summary>
        /// <param name="entityRef"></param>
        /// <returns></returns>
        private TinyEntityGroup GetEntityGroupForEntity(TinyEntity.Reference entityRef)
        {
            // @TODO [PREFAB] Use acceleration structure!
            foreach (var group in Registry.FindAllByType<TinyEntityGroup>())
            {
                if (group.Entities.Contains(entityRef))
                {
                    return group;
                }
            }

            return null;
        }

        /// <summary>
        /// Compresses a property path to remove the `Properties` and `Items` elements
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static string CompressPropertyPath(PropertyPath path)
        {
            var builder = new System.Text.StringBuilder(16);
            var skip = false;

            for (var i = 0; i < path.PartsCount; i++)
            {
                var part = path[i];
                
                // List items get promoted one level up
                if (part.IsListItem)
                {
                    builder.Append("[" + part.listIndex + "]");
                    skip = true;
                    continue;
                }

                if (i < path.PartsCount - 1 && skip)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('.');
                }
                
                builder.Append(part.propertyName);
                skip = !skip;
            }
            
            return builder.ToString();
        }

        /// <summary>
        /// Expands a property path to include the `Properties` and `Items` elements
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static PropertyPath ExpandPropertyPath(string input)
        {
            var path = new PropertyPath(input);
            var result = new PropertyPath("Properties");

            for (var i = 0; i < path.PartsCount; i++)
            {
                var part = path[i];
                var name = part.propertyName;

                // Special case for custom fields
                // e.g. array.Count 
                if (name.StartsWith(PrefabAttributes.CustomField.Prefix))
                {
                    var l = PrefabAttributes.CustomField.Prefix.Length;
                    name = name.Substring(l, name.Length - l);
                }
                
                result.Push(name);

                // Expand list types 
                // myArray[0] -> myArray.Items[0]
                if (part.IsListItem)
                {
                    result.Push("Items", part.listIndex);
                }

                // Expand tiny object types
                // myStruct.foo -> myStruct.Properties.foo
                //
                // @NOTE This step is skipped if our next property is a custom field
                // e.g. myArray.Count should NOT be expanded
                if (i < path.PartsCount - 1 && !path[i + 1].propertyName.StartsWith(PrefabAttributes.CustomField.Prefix))
                {
                    result.Push("Properties");
                }
            }

            return result;
        }
        
        /// <summary>
        /// Returns true if the currently visited (property, index) is contained in the given (path, root) 
        /// </summary>
        /// <param name="path">Expanded property path</param>
        /// <param name="root">Start point for the search (typically the component)</param>
        /// <param name="targetProperty"></param>
        /// <param name="targetListIndex"></param>
        /// <returns>True if the currently visited (container, property, index) is contained in the given (path, root)(</returns>
        internal static bool IsModified(PropertyPath path, IPropertyContainer root, IProperty targetProperty, int targetListIndex = -1)
        {
            var currentContainer = root;
            
            for (var i = 0; i < path.PartsCount; i++)
            {
                var part = path[i];
                
                var currentProperty = currentContainer?.PropertyBag.FindProperty(part.propertyName);
                
                if (currentProperty == null)
                {
                    break;
                }

                if (part.listIndex >= 0)
                {
                    if (!(currentProperty is IListClassProperty listProperty) || listProperty.Count(currentContainer) <= part.listIndex)
                    {
                        break;
                    }
                    
                    if (ReferenceEquals(currentProperty, targetProperty) && (targetListIndex == -1 || part.listIndex == targetListIndex))
                    {
                        return true;
                    }
                    
                    currentContainer = listProperty.GetObjectAt(currentContainer, part.listIndex) as IPropertyContainer;
                }
                else
                {
                    if (ReferenceEquals(currentProperty, targetProperty))
                    {
                        return true;
                    }

                    currentContainer = (currentProperty as IValueProperty)?.GetObjectValue(currentContainer) as IPropertyContainer;
                }
            }

            return false;
        }
    }
}