using System;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    public struct ForEachComponentGroupFilter : IDisposable
    {
        internal NativeArray<ComponentChunkIterator> ItemIterator;
        internal NativeArray<int> ItemLength;

        internal int IndexInComponentGroup;
        internal NativeArray<int> SharedComponentIndex;

        internal ArchetypeManager TypeManager;

        public int Length => ItemIterator.Length;

        public void Dispose()
        {
            for (int i = 0; i < SharedComponentIndex.Length; ++i)
                TypeManager.GetSharedComponentDataManager().RemoveReference(SharedComponentIndex[i]);
            ItemIterator.Dispose();
            ItemLength.Dispose();
            SharedComponentIndex.Dispose();
        }
    }
    unsafe struct ComponentGroupData
    {
        readonly EntityGroupData*             m_GroupData;
        readonly EntityDataManager*           m_EntityDataManager;
        ComponentGroupFilter                  m_Filter;                          
        
        internal ComponentGroupData(EntityGroupData* groupData, EntityDataManager* entityDataManager)
        {
            m_GroupData = groupData;
            m_EntityDataManager = entityDataManager;
            m_Filter = default(ComponentGroupFilter);
        }
        
        public void SetFilter(ArchetypeManager typeManager, ref ComponentGroupFilter filter)
        {
            Assert.IsTrue(filter.SharedComponentFilterCount <= 2 && filter.SharedComponentFilterCount >= 0);
            
            ResetFilter(typeManager);
            m_Filter = filter;
        }
        
        internal void ResetFilter(ArchetypeManager typeManager)
        {
            var filteredCount = m_Filter.SharedComponentFilterCount;

            fixed (int* sharedComponentIndexPtr = m_Filter.SharedComponentIndex)
            {
                for (var i=0; i<filteredCount; ++i)
                    typeManager.GetSharedComponentDataManager().RemoveReference(sharedComponentIndexPtr[i]);
            }

            m_Filter.SharedComponentFilterCount = 0;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle GetSafetyHandle(ComponentJobSafetyManager safetyManager, int indexInComponentGroup)
        {
            var type = m_GroupData->RequiredComponents + indexInComponentGroup;
            var isReadOnly = type->AccessModeType == ComponentType.AccessMode.ReadOnly;
            return safetyManager.GetSafetyHandle(type->TypeIndex, isReadOnly);
        }
#endif

        public bool IsEmptyIgnoreFilter
        {
            get
            {
                for (var match = m_GroupData->FirstMatchingArchetype; match != null; match = match->Next)
                {
                    if (match->Archetype->EntityCount > 0)
                        return false;
                }

                return true;
            }
        }

        internal void GetComponentChunkIterator(out int outLength, out ComponentChunkIterator outIterator)
        {
            MatchingArchetypes* firstArchetype = null;
            Chunk* firstNonEmptyChunk;
            
            ComponentChunkIterator.CalculateInitialChunkIterator(m_GroupData->FirstMatchingArchetype, ref m_Filter, out firstArchetype, out firstNonEmptyChunk, out outLength);
            outIterator = new ComponentChunkIterator(firstArchetype, firstNonEmptyChunk, ref m_Filter);
        }

        internal void GetComponentChunkIterators(ForEachComponentGroupFilter forEachFilter)
        {
            var numFilters = forEachFilter.SharedComponentIndex.Length;

            var firstArchetype = new NativeArray<IntPtr>(numFilters, Allocator.Temp);
            var firstNonEmptyChunk = new NativeArray<IntPtr>(numFilters, Allocator.Temp);

            ComponentChunkIterator.CalculateInitialChunkIterators(m_GroupData->FirstMatchingArchetype, forEachFilter.IndexInComponentGroup, forEachFilter.SharedComponentIndex,
                firstArchetype, firstNonEmptyChunk, forEachFilter.ItemLength);
            var filter = new ComponentGroupFilter();
            filter.SharedComponentFilterCount = 1;
            filter.IndexInComponentGroup[0] = forEachFilter.IndexInComponentGroup;
            for (int i = 0; i < numFilters; ++i)
            {
                filter.SharedComponentIndex[0] = forEachFilter.SharedComponentIndex[i];
                forEachFilter.ItemIterator[i] = new ComponentChunkIterator((MatchingArchetypes*) firstArchetype[i],
                    (Chunk*) firstNonEmptyChunk[i], ref filter);
            }

            firstArchetype.Dispose();
            firstNonEmptyChunk.Dispose();
        }

        internal int GetIndexInComponentGroup(int componentType)
        {
            var componentIndex = 0;
            while (componentIndex < m_GroupData->RequiredComponentsCount && m_GroupData->RequiredComponents[componentIndex].TypeIndex != componentType)
                ++componentIndex;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (componentIndex >= m_GroupData->RequiredComponentsCount)
                throw new InvalidOperationException(
                    $"Trying to get iterator for {TypeManager.GetType(componentType)} but the required component type was not declared in the EntityGroup.");
#endif
            return componentIndex;
        }

        internal int ComponentTypeIndex(int indexInComponentGroup)
        {
            return m_GroupData->RequiredComponents[indexInComponentGroup].TypeIndex;
        }

        public bool CompareComponents(ComponentType[] componentTypes)
        {
            fixed (ComponentType* ptr = componentTypes)
            {
                return CompareComponents(ptr, componentTypes.Length);
            }
        }

        internal bool CompareComponents(ComponentType* componentTypes, int count)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            for (var k = 0; k < count; ++k)
            {
                if (componentTypes[k].TypeIndex == TypeManager.GetTypeIndex<Entity>())
                    throw new System.ArgumentException("ComponentGroup.CompareComponents may not include typeof(Entity), it is implicit");
            }
#endif

            // ComponentGroups are constructed including the Entity ID
            int requiredCount = m_GroupData->RequiredComponentsCount;
            if (count != requiredCount - 1)
                return false;

            for (var k = 0; k < count; ++k)
            {
                int i;
                for (i = 1; i < requiredCount ; ++i)
                {
                    if (m_GroupData->RequiredComponents[i] == componentTypes[k])
                        break;
                }

                if (i == requiredCount)
                    return false;
            }

            return true;
        }

        public ComponentType[] Types
        {
            get
            {
                var types = new List<ComponentType> ();
                for (var i = 0; i < m_GroupData->RequiredComponentsCount; ++i)
                    types.Add(m_GroupData->RequiredComponents[i]);

                return types.ToArray();
            }
        }
        
        public int GetCombinedComponentOrderVersion()
        {
            int version = 0;
            
            for (var i = 0; i < m_GroupData->RequiredComponentsCount; ++i)
            {
                version += m_EntityDataManager->GetComponentTypeOrderVersion(m_GroupData->RequiredComponents[i].TypeIndex);
            }
            
            return version;
        }


        internal void CompleteDependency(ComponentJobSafetyManager safetyManager)
        {
            safetyManager.CompleteDependencies(m_GroupData->ReaderTypes, m_GroupData->ReaderTypesCount, m_GroupData->WriterTypes, m_GroupData->WriterTypesCount);
        }

        internal JobHandle GetDependency(ComponentJobSafetyManager safetyManager)
        {
            return safetyManager.GetDependency(m_GroupData->ReaderTypes, m_GroupData->ReaderTypesCount, m_GroupData->WriterTypes, m_GroupData->WriterTypesCount);
        }

        internal void AddDependency(ComponentJobSafetyManager safetyManager, JobHandle job)
        {
            safetyManager.AddDependency(m_GroupData->ReaderTypes, m_GroupData->ReaderTypesCount, m_GroupData->WriterTypes, m_GroupData->WriterTypesCount, job);
        }
    }

    public unsafe class ComponentGroup : IDisposable
    {
        readonly ComponentJobSafetyManager    m_SafetyManager;
        readonly ArchetypeManager             m_TypeManager;
        readonly EntityDataManager*           m_EntityDataManager;
        ComponentGroupData                    m_ComponentGroupData;

        // TODO: this is temporary, used to cache some state to avoid recomputing the TransformAccessArray. We need to improve this.
        internal IDisposable m_CachedState;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal string                       DisallowDisposing = null;
#endif
        internal EntityDataManager* EntityDataManager => m_EntityDataManager;

        internal ComponentGroup(EntityGroupData* groupData, ComponentJobSafetyManager safetyManager, ArchetypeManager typeManager, EntityDataManager* entityDataManager )
        {
            m_ComponentGroupData = new ComponentGroupData(groupData,entityDataManager);
            m_SafetyManager = safetyManager;
            m_TypeManager = typeManager;
            m_EntityDataManager = entityDataManager;
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (DisallowDisposing  != null)
                throw new System.ArgumentException(DisallowDisposing);
#endif

            if (m_CachedState != null)
                m_CachedState.Dispose();

            m_ComponentGroupData.ResetFilter(m_TypeManager);
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle GetSafetyHandle(int indexInComponentGroup) => m_ComponentGroupData.GetSafetyHandle(m_SafetyManager, indexInComponentGroup);
#endif

        public bool IsEmptyIgnoreFilter => m_ComponentGroupData.IsEmptyIgnoreFilter;

        internal void GetComponentChunkIterator(out int length, out ComponentChunkIterator iterator) =>
            m_ComponentGroupData.GetComponentChunkIterator(out length, out iterator);

        internal int GetIndexInComponentGroup(int componentType)
        {
            return m_ComponentGroupData.GetIndexInComponentGroup(componentType);
        }

        internal void GetComponentDataArray<T>(ref ComponentChunkIterator iterator, int indexInComponentGroup, int length, out ComponentDataArray<T> output) where T : struct, IComponentData
        {
            iterator.IndexInComponentGroup = indexInComponentGroup;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            output = new ComponentDataArray<T>(iterator, length, GetSafetyHandle(indexInComponentGroup));
#else
			output = new ComponentDataArray<T>(iterator, length);
#endif
        }

        public ComponentDataArray<T> GetComponentDataArray<T>() where T : struct, IComponentData
        {
            int length;
            ComponentChunkIterator iterator;
            GetComponentChunkIterator(out length, out iterator);
            var indexInComponentGroup = GetIndexInComponentGroup(TypeManager.GetTypeIndex<T>());

            ComponentDataArray<T> res;
            GetComponentDataArray<T>(ref iterator, indexInComponentGroup, length, out res);
            return res;
        }

        public ComponentDataArray<T> GetComponentDataArray<T>(ForEachComponentGroupFilter filter, int filterIdx) where T : struct, IComponentData
        {
            var indexInComponentGroup = GetIndexInComponentGroup(TypeManager.GetTypeIndex<T>());

            ComponentDataArray<T> res;
            ComponentChunkIterator iterator = filter.ItemIterator[filterIdx];
            GetComponentDataArray<T>(ref iterator, indexInComponentGroup, filter.ItemLength[filterIdx], out res);
            return res;
        }

        internal void GetSharedComponentDataArray<T>(ref ComponentChunkIterator iterator, int indexInComponentGroup, int length, out SharedComponentDataArray<T> output) where T : struct, ISharedComponentData
        {
            iterator.IndexInComponentGroup = indexInComponentGroup;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var typeIndex = m_ComponentGroupData.ComponentTypeIndex(indexInComponentGroup);
            output = new SharedComponentDataArray<T>(m_TypeManager.GetSharedComponentDataManager(), indexInComponentGroup, iterator, length, m_SafetyManager.GetSafetyHandle(typeIndex, true));
#else
			output = new SharedComponentDataArray<T>(m_TypeManager.GetSharedComponentDataManager(), indexInComponentGroup, iterator, length);
#endif
        }

        public SharedComponentDataArray<T> GetSharedComponentDataArray<T>() where T : struct, ISharedComponentData
        {
            int length;
            ComponentChunkIterator iterator;
            GetComponentChunkIterator(out length, out iterator);
            var indexInComponentGroup = GetIndexInComponentGroup(TypeManager.GetTypeIndex<T>());

            SharedComponentDataArray<T> res;
            GetSharedComponentDataArray<T>(ref iterator, indexInComponentGroup, length, out res);
            return res;
        }

        internal void GetFixedArrayArray<T>(ref ComponentChunkIterator iterator, int indexInComponentGroup, int length, out FixedArrayArray<T> output) where T : struct
        {
            iterator.IndexInComponentGroup = indexInComponentGroup;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            output = new FixedArrayArray<T>(iterator, length, GetSafetyHandle(indexInComponentGroup));
#else
			output = new FixedArrayArray<T>(iterator, length);
#endif
        }

        public FixedArrayArray<T> GetFixedArrayArray<T>() where T : struct
        {
            int length;
            ComponentChunkIterator iterator;
            GetComponentChunkIterator(out length, out iterator);
            var indexInComponentGroup = GetIndexInComponentGroup(TypeManager.GetTypeIndex<T>());

            FixedArrayArray<T> res;
            GetFixedArrayArray<T>(ref iterator, indexInComponentGroup, length, out res);
            return res;
        }

        internal void GetEntityArray(ref ComponentChunkIterator iterator, int length, out EntityArray output)
        {
            iterator.IndexInComponentGroup = 0;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            output = new EntityArray(iterator, length, m_SafetyManager.GetSafetyHandle(TypeManager.GetTypeIndex<Entity>(), true));
#else
			output = new EntityArray(iterator, length);
#endif
        }

        public EntityArray GetEntityArray()
        {
            int length;
            ComponentChunkIterator iterator;
            GetComponentChunkIterator(out length, out iterator);

            EntityArray res;
            GetEntityArray(ref iterator, length, out res);
            return res;
        }

        public int CalculateLength()
        {
            int length;
            ComponentChunkIterator iterator;
            GetComponentChunkIterator(out length, out iterator);
            return length;
        }

        public bool CompareComponents(ComponentType[] componentTypes) =>
            m_ComponentGroupData.CompareComponents(componentTypes);

        internal bool CompareComponents(ComponentType* componentTypes, int count) =>
            m_ComponentGroupData.CompareComponents(componentTypes,count);

        public ComponentType[] Types => m_ComponentGroupData.Types;

        internal ArchetypeManager ArchetypeManager => m_TypeManager;

        public void ResetFilter()
        {
            m_ComponentGroupData.ResetFilter(m_TypeManager);
        }

        public void SetFilter<SharedComponent1>(SharedComponent1 sharedComponent1)
            where SharedComponent1 : struct, ISharedComponentData
        {
            ComponentGroupFilter filter;
            filter.SharedComponentFilterCount = 1;
            filter.IndexInComponentGroup[0] = GetIndexInComponentGroup(TypeManager.GetTypeIndex<SharedComponent1>());
            filter.SharedComponentIndex[0] = m_TypeManager.GetSharedComponentDataManager().InsertSharedComponent(sharedComponent1);

            m_ComponentGroupData.SetFilter(m_TypeManager, ref filter);
        }
        public ForEachComponentGroupFilter CreateForEachFilter<SharedComponent1>(List<SharedComponent1> sharedComponent1)
            where SharedComponent1 : struct, ISharedComponentData
        {
            var forEachFilter = new ForEachComponentGroupFilter();
            forEachFilter.TypeManager = m_TypeManager;
            forEachFilter.ItemIterator = new NativeArray<ComponentChunkIterator>(sharedComponent1.Count, Allocator.Temp);
            forEachFilter.ItemLength = new NativeArray<int>(sharedComponent1.Count, Allocator.Temp);
            forEachFilter.SharedComponentIndex = new NativeArray<int>(sharedComponent1.Count, Allocator.Temp);
            forEachFilter.IndexInComponentGroup = GetIndexInComponentGroup(TypeManager.GetTypeIndex<SharedComponent1>());
            for (int i = 0; i < sharedComponent1.Count; ++i)
            {
                forEachFilter.SharedComponentIndex[i] = m_TypeManager.GetSharedComponentDataManager()
                    .InsertSharedComponent(sharedComponent1[i]);
            }

            m_ComponentGroupData.GetComponentChunkIterators(forEachFilter);

            return forEachFilter;
        }

        internal void SetFilter<SharedComponent1,SharedComponent2>(SharedComponent1 sharedComponent1, SharedComponent2 sharedComponent2)
            where SharedComponent1 : struct, ISharedComponentData
            where SharedComponent2 : struct, ISharedComponentData
        {
            ComponentGroupFilter filter;
            filter.SharedComponentFilterCount = 2;
            filter.IndexInComponentGroup[0] = GetIndexInComponentGroup(TypeManager.GetTypeIndex<SharedComponent1>());
            filter.SharedComponentIndex[0] = m_TypeManager.GetSharedComponentDataManager().InsertSharedComponent(sharedComponent1);

            filter.IndexInComponentGroup[1] = GetIndexInComponentGroup(TypeManager.GetTypeIndex<SharedComponent2>());
            filter.SharedComponentIndex[1] = m_TypeManager.GetSharedComponentDataManager().InsertSharedComponent(sharedComponent2);
            
            m_ComponentGroupData.SetFilter(m_TypeManager, ref filter);
        }
        
        public void CompleteDependency() => m_ComponentGroupData.CompleteDependency(m_SafetyManager);
        public JobHandle GetDependency() => m_ComponentGroupData.GetDependency(m_SafetyManager);
        public void AddDependency(JobHandle job) => m_ComponentGroupData.AddDependency(m_SafetyManager, job);

        public int GetCombinedComponentOrderVersion()
        {
            return m_ComponentGroupData.GetCombinedComponentOrderVersion();
        }

        internal ArchetypeManager GetArchetypeManager()
        {
            return m_TypeManager;
        }
    }
}
