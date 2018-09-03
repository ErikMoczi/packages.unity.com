using System;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
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
        
        public void SetFilter(ArchetypeManager typeManager, ComponentGroupFilter filter)
        {
            Assert.IsTrue(filter.FilterCount <= 2 && filter.FilterCount >= 0);
            
            ResetFilter(typeManager);
            m_Filter = filter;
        }
        
        internal void ResetFilter(ArchetypeManager typeManager)
        {
            var filteredCount = m_Filter.FilterCount;

            fixed (int* sharedComponentIndexPtr = m_Filter.SharedComponentIndex)
            {
                for (var i=0; i<filteredCount; ++i)
                    typeManager.GetSharedComponentDataManager().RemoveReference(sharedComponentIndexPtr[i]);
            }
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
            // Update the archetype segments
            var length = 0;
            MatchingArchetypes* first = null;
            Chunk* firstNonEmptyChunk = null;
            if (!m_Filter.HasFilter)
            {
                for (var match = m_GroupData->FirstMatchingArchetype; match != null; match = match->Next)
                {
                    if (match->Archetype->EntityCount > 0)
                    {
                        length += match->Archetype->EntityCount;
                        if (first == null)
                            first = match;
                    }
                }
                if (first != null)
                    firstNonEmptyChunk = (Chunk*)first->Archetype->ChunkList.Begin;
            }
            else
            {
                for (var match = m_GroupData->FirstMatchingArchetype; match != null; match = match->Next)
                {
                    if (match->Archetype->EntityCount <= 0)
                        continue;

                    var archeType = match->Archetype;
                    for (var c = (Chunk*)archeType->ChunkList.Begin; c != archeType->ChunkList.End; c = (Chunk*)c->ChunkListNode.Next)
                    {
                        if (!c->MatchesFilter(match, ref m_Filter))
                            continue;

                        if (c->Count <= 0)
                            continue;

                        length += c->Count;
                        if (first != null)
                            continue;

                        first = match;
                        firstNonEmptyChunk = c;
                    }
                }
            }

            outLength = length;

            outIterator = first == null
                ? new ComponentChunkIterator(null, 0, null, default(ComponentGroupFilter))
                : new ComponentChunkIterator(first, length, firstNonEmptyChunk, m_Filter);
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

        public Type[] Types
        {
            get
            {
                var types = new List<Type> ();
                for (var i = 0; i < m_GroupData->RequiredComponentsCount; ++i)
                {
                    if (m_GroupData->RequiredComponents[i].AccessModeType != ComponentType.AccessMode.Subtractive)
                        types.Add(TypeManager.GetType(m_GroupData->RequiredComponents[i].TypeIndex));
                }

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

        internal int EntityIndex(Entity entity)
        {
            Chunk* entityChunk;
            int entityChunkIndex;

            m_EntityDataManager->GetComponentChunk(entity, out entityChunk, out entityChunkIndex);
            var entityArchetype = m_EntityDataManager->GetArchetype(entity);

            int entityStartIndex = 0;
            var matchingArchetype = m_GroupData->FirstMatchingArchetype;
            while (true)
            {
                var archetype = matchingArchetype->Archetype;
                if (!m_Filter.HasFilter && archetype != entityArchetype)
                {
                    entityStartIndex += archetype->EntityCount;
                }
                else
                {
                    for (var c = (Chunk*)archetype->ChunkList.Begin; c != archetype->ChunkList.End; c = (Chunk*)c->ChunkListNode.Next)
                    {
                        if (c->Count <= 0)
                            continue;

                        if (m_Filter.HasFilter && !c->MatchesFilter(matchingArchetype, ref m_Filter))
                            continue;

                        if (c == entityChunk)
                            return entityStartIndex + entityChunkIndex;

                        entityStartIndex += c->Count;
                    }
                }

                if (matchingArchetype == m_GroupData->LastMatchingArchetype)
                    break;

                matchingArchetype = matchingArchetype->Next;
                if (matchingArchetype == null)
                    break;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            throw new IndexOutOfRangeException($"Entity {entity} is out of range of ComponentGroup.");
#else
            return -1;
#endif
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

        internal void GetIndexFromEntity(out IndexFromEntity output)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            output = new IndexFromEntity(m_ComponentGroupData, m_SafetyManager.GetSafetyHandle(TypeManager.GetTypeIndex<Entity>(), true));
#else
            output = new IndexFromEntity(m_ComponentGroupData);
#endif
        }

        internal IndexFromEntity GetIndexFromEntity()
        {
            IndexFromEntity res;
            GetIndexFromEntity(out res);
            return res;
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

        //@TODO: This should really be just ComponentType[] ...
        public Type[] Types => m_ComponentGroupData.Types;

        internal ArchetypeManager ArchetypeManager => m_TypeManager;

        public void ResetFilter()
        {
            m_ComponentGroupData.ResetFilter(m_TypeManager);
        }

        public void SetFilter<SharedComponent1>(SharedComponent1 sharedComponent1)
            where SharedComponent1 : struct, ISharedComponentData
        {
            ComponentGroupFilter filter;
            filter.FilterCount = 1;
            filter.IndexInComponentGroup[0] = GetIndexInComponentGroup(TypeManager.GetTypeIndex<SharedComponent1>());
            filter.SharedComponentIndex[0] = m_TypeManager.GetSharedComponentDataManager().InsertSharedComponent(sharedComponent1);

            m_ComponentGroupData.SetFilter(m_TypeManager, filter);
        }

        internal void SetFilter<SharedComponent1,SharedComponent2>(SharedComponent1 sharedComponent1, SharedComponent2 sharedComponent2)
            where SharedComponent1 : struct, ISharedComponentData
            where SharedComponent2 : struct, ISharedComponentData
        {
            ComponentGroupFilter filter;
            filter.FilterCount = 2;
            filter.IndexInComponentGroup[0] = GetIndexInComponentGroup(TypeManager.GetTypeIndex<SharedComponent1>());
            filter.SharedComponentIndex[0] = m_TypeManager.GetSharedComponentDataManager().InsertSharedComponent(sharedComponent1);

            filter.IndexInComponentGroup[1] = GetIndexInComponentGroup(TypeManager.GetTypeIndex<SharedComponent2>());
            filter.SharedComponentIndex[1] = m_TypeManager.GetSharedComponentDataManager().InsertSharedComponent(sharedComponent2);
            
            m_ComponentGroupData.SetFilter(m_TypeManager, filter);
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
