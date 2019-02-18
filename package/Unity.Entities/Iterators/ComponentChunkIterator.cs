﻿using System;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Unity.Entities
{
    [Flags]
    internal enum FilterType
    {
        None,
        SharedComponent,
        Changed
    }

    //@TODO: Use field offset / union here... There seems to be an issue in mono preventing it...
    internal unsafe struct ComponentGroupFilter
    {
        public struct SharedComponentData
        {
            public int Count;
            public fixed int IndexInComponentGroup[2];
            public fixed int SharedComponentIndex[2];
        }

        // Saves the index of ComponentTypes in this group that have changed.
        public struct ChangedFilter
        {
            public const int Capacity = 2;

            public int Count;
            public fixed int IndexInComponentGroup[2];
        }

        public FilterType Type;
        public uint RequiredChangeVersion;

        public SharedComponentData Shared;
        public ChangedFilter Changed;

        public bool RequiresMatchesFilter
        {
            get { return Type != FilterType.None; }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public void AssertValid()
        {
            if ((Type & FilterType.SharedComponent) != 0)
                Assert.IsTrue(Shared.Count <= 2 && Shared.Count > 0);
            else if ((Type & FilterType.Changed) != 0)
                Assert.IsTrue(Changed.Count <= 2 && Changed.Count > 0);
        }
#endif
    }

    internal unsafe struct ComponentChunkCache
    {
        [NativeDisableUnsafePtrRestriction] public void* CachedPtr;
        public int CachedBeginIndex;
        public int CachedEndIndex;
        public int CachedSizeOf;
        public bool IsWriting;
    }

    /// <summary>
    ///     Enables iteration over chunks belonging to a set of archetypes.
    /// </summary>
    internal unsafe struct ComponentChunkIterator
    {
        private readonly MatchingArchetypeList m_MatchingArchetypeList;
        private int m_CurrentMatchingArchetypeIndex;

        private int m_CurrentMatchingArchetypeIndexNext => m_CurrentMatchingArchetypeIndex - 1;

        private int m_FirstMatchingArchetypeIndex => m_MatchingArchetypeList.Count - 1;

        private MatchingArchetype* m_CurrentMatchingArchetype => m_MatchingArchetypeList.p[m_CurrentMatchingArchetypeIndex];

        [NativeDisableUnsafePtrRestriction] private Chunk** m_CurrentChunk;


        private int m_CurrentArchetypeEntityIndex;
        private int m_CurrentChunkEntityIndex;

        private int m_CurrentArchetypeIndex;
        private int m_CurrentChunkIndex;

        private ComponentGroupFilter m_Filter;

        private readonly uint m_GlobalSystemVersion;

        public int IndexInComponentGroup;

        internal int GetSharedComponentFromCurrentChunk(int sharedComponentIndex)
        {
            var archetype = m_CurrentMatchingArchetype->Archetype;
            var indexInArchetype = m_CurrentMatchingArchetype->IndexInArchetype[sharedComponentIndex];
            var sharedComponentOffset = indexInArchetype - archetype->FirstSharedComponent;
            return (*m_CurrentChunk)->GetSharedComponentValue(sharedComponentOffset);
        }

        public ComponentChunkIterator(MatchingArchetypeList match, uint globalSystemVersion,
            ref ComponentGroupFilter filter)
        {
            m_MatchingArchetypeList = match;
            m_CurrentMatchingArchetypeIndex = match.Count - 1;
            IndexInComponentGroup = -1;
            m_CurrentChunk = null;
            m_CurrentArchetypeIndex = m_CurrentArchetypeEntityIndex = int.MaxValue; // This will trigger UpdateCacheResolvedIndex to update the cache on first access
            m_CurrentChunkIndex = m_CurrentChunkEntityIndex = 0;
            m_GlobalSystemVersion = globalSystemVersion;
            m_Filter = filter;
        }

        public object GetManagedObject(ArchetypeManager typeMan, int typeIndexInArchetype, int cachedBeginIndex,
            int index)
        {
            return typeMan.GetManagedObject(*m_CurrentChunk, typeIndexInArchetype, index - cachedBeginIndex);
        }

        public object GetManagedObject(ArchetypeManager typeMan, int cachedBeginIndex, int index)
        {
            return typeMan.GetManagedObject(*m_CurrentChunk,
                m_CurrentMatchingArchetype->IndexInArchetype[IndexInComponentGroup], index - cachedBeginIndex);
        }

        public object[] GetManagedObjectRange(ArchetypeManager typeMan, int cachedBeginIndex, int index,
            out int rangeStart, out int rangeLength)
        {
            var objs = typeMan.GetManagedObjectRange(*m_CurrentChunk,
                m_CurrentMatchingArchetype->IndexInArchetype[IndexInComponentGroup], out rangeStart,
                out rangeLength);
            rangeStart += index - cachedBeginIndex;
            rangeLength -= index - cachedBeginIndex;
            return objs;
        }

        /// <summary>
        ///     Total number of chunks in a given MatchingArchetype list.
        /// </summary>
        /// <param name="matchingArchetypes">List of matching archetypes.</param>
        /// <returns>Number of chunks in a list of archetypes.</returns>
        internal static int CalculateNumberOfChunksWithoutFiltering(MatchingArchetypeList matchingArchetypes)
        {
            var chunkCount = 0;

            for (var m = matchingArchetypes.Count - 1; m >= 0; --m)
            {
                var match = matchingArchetypes.p[m];
                chunkCount += match->Archetype->Chunks.Count;
            }

            return chunkCount;
        }

        /// <summary>
        ///     Creates a NativeArray with all the chunks in a given archetype.
        /// </summary>
        /// <param name="matchingArchetypes">List of matching archetypes.</param>
        /// <param name="allocator">Allocator to use for the array.</param>
        /// <param name="jobHandle">Handle to the GatherChunks job used to fill the output array.</param>
        /// <returns>NativeArray of all the chunks in the matchingArchetypes list.</returns>
        public static NativeArray<ArchetypeChunk> CreateArchetypeChunkArray(MatchingArchetypeList matchingArchetypes, Allocator allocator, out JobHandle jobHandle)
        {
            var archetypeCount = matchingArchetypes.Count;

            var archetypes = new NativeArray<EntityArchetype>(archetypeCount, Allocator.TempJob);
            var offsets = new NativeArray<int>(archetypeCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var chunkCount = 0;
            {
                int i = 0;
                var length = 0;
                for (var m = matchingArchetypes.Count - 1; m >= 0; --m)
                {
                    var match = matchingArchetypes.p[m];
                    var archetype = match->Archetype;
                    archetypes[i] = new EntityArchetype

                    {
                        Archetype = archetype
                    };
                    offsets[i] = length;
                    length += archetype->Chunks.Count;
                    i++;

                }
                chunkCount = length;
            }

            var chunks = new NativeArray<ArchetypeChunk>(chunkCount, allocator, NativeArrayOptions.UninitializedMemory);
            var gatherChunksJob = new GatherChunks
            {
                Archetypes = archetypes,
                Offsets = offsets,
                Chunks = chunks
            };
            var gatherChunksJobHandle = gatherChunksJob.Schedule(archetypeCount,1);
            jobHandle = gatherChunksJobHandle;

            return chunks;
        }

        /// <summary>
        ///     Creates a NativeArray containing the entities in a given ComponentGroup.
        /// </summary>
        /// <param name="matchingArchetypes">List of matching archetypes.</param>
        /// <param name="allocator">Allocator to use for the array.</param>
        /// <param name="type">An atomic safety handle required by GatherEntitiesJob so it can call GetNativeArray() on chunks.</param>
        /// <param name="componentGroup">ComponentGroup to gather entities from.</param>
        /// <param name="filter">ComponentGroupFilter for calculating the length of the output array.</param>
        /// <param name="jobHandle">Handle to the GatherEntitiesJob job used to fill the output array.</param>
        /// <param name="dependsOn">Handle to a job this GatherEntitiesJob must wait on.</param>
        /// <returns>NativeArray of the entities in a given ComponentGroup.</returns>
        public static NativeArray<Entity> CreateEntityArray(MatchingArchetypeList matchingArchetypes,
            Allocator allocator,
            ArchetypeChunkEntityType type,
            ComponentGroup componentGroup,
            ref ComponentGroupFilter filter,
            out JobHandle jobHandle,
            JobHandle dependsOn)

        {
            var entityCount = CalculateLength(matchingArchetypes, ref filter);

            var job = new GatherEntitiesJob
            {
                EntityType = type,
                Entities = new NativeArray<Entity>(entityCount, allocator)
            };
            jobHandle = job.Schedule(componentGroup, dependsOn);

            return job.Entities;
        }

        public static NativeArray<T> CreateComponentDataArray<T>(MatchingArchetypeList matchingArchetypes,
            Allocator allocator,
            ArchetypeChunkComponentType<T> type,
            ComponentGroup componentGroup,
            ref ComponentGroupFilter filter,
            out JobHandle jobHandle,
            JobHandle dependsOn)
            where T :struct, IComponentData
        {
            var entityCount = CalculateLength(matchingArchetypes, ref filter);

            var job = new GatherComponentDataJob<T>
            {
                ComponentData = new NativeArray<T>(entityCount, allocator),
                ComponentType = type
            };
            jobHandle = job.Schedule(componentGroup, dependsOn);

            return job.ComponentData;
        }

        public static void CopyFromComponentDataArray<T>(MatchingArchetypeList matchingArchetypes,
            NativeArray<T> componentDataArray,
            ArchetypeChunkComponentType<T> type,
            ComponentGroup componentGroup,
            ref ComponentGroupFilter filter,
            out JobHandle jobHandle,
            JobHandle dependsOn)
            where T :struct, IComponentData
        {
            var job = new CopyComponentArrayToChunks<T>
            {
                ComponentData = componentDataArray,
                ComponentType = type
            };
            jobHandle = job.Schedule(componentGroup, dependsOn);
        }

        /// <summary>
        ///     Total number of entities contained in a given MatchingArchetype list.
        /// </summary>
        /// <param name="matchingArchetypes">List of matching archetypes.</param>
        /// <param name="filter">ComponentGroupFilter to use when calculating total number of entities.</param>
        /// <returns>Number of entities</returns>
        public static int CalculateLength(MatchingArchetypeList matchingArchetypes, ref ComponentGroupFilter filter)
        {
            // Update the archetype segments
            var length = 0;
            if (!filter.RequiresMatchesFilter)
            {
                for (var m = matchingArchetypes.Count - 1; m >= 0; --m)
                {
                    var match = matchingArchetypes.p[m];
                    length += match->Archetype->EntityCount;
                }
            }
            else
            {
                for (var m = matchingArchetypes.Count - 1; m >= 0; --m)
                {
                    var match = matchingArchetypes.p[m];
                    if (match->Archetype->EntityCount <= 0)
                        continue;

                    var archeType = match->Archetype;
                    for(var ci = 0; ci < archeType->Chunks.Count; ++ci)
                    {
                        var c = archeType->Chunks.p[ci];
                        if (!c->MatchesFilter(match, ref filter))
                            continue;

                        Assert.IsTrue(c->Count > 0);

                        length += c->Count;
                    }
                }

            }

            return length;
        }

        private void MoveToNextMatchingChunk()
        {
            var m = m_CurrentMatchingArchetypeIndex;
            var c = m_CurrentChunk;
            var e = m_MatchingArchetypeList.p[m]->Archetype->Chunks.p + m_MatchingArchetypeList.p[m]->Archetype->Chunks.Count;

            do
            {
                c = c + 1;
                while (c == e)
                {
                    m_CurrentArchetypeEntityIndex += m_CurrentChunkEntityIndex;
                    m_CurrentChunkEntityIndex = 0;
                    m = m - 1;
                    if (m < 0)
                    {
                        m_CurrentMatchingArchetypeIndex = m;
                        m_CurrentChunk = null;
                        return;
                    }

                    c = m_MatchingArchetypeList.p[m]->Archetype->Chunks.p;
                    e = m_MatchingArchetypeList.p[m]->Archetype->Chunks.p + m_MatchingArchetypeList.p[m]->Archetype->Chunks.Count;
                }
            } while (!((*c)->MatchesFilter(m_MatchingArchetypeList.p[m], ref m_Filter) && (*c)->Capacity > 0));

            m_CurrentMatchingArchetypeIndex = m;
            m_CurrentChunk = c;
        }

        public void MoveToEntityIndex(int index)
        {
            if (!m_Filter.RequiresMatchesFilter)
            {
                if (index < m_CurrentArchetypeEntityIndex)
                {
                    m_CurrentMatchingArchetypeIndex = m_FirstMatchingArchetypeIndex;
                    m_CurrentArchetypeEntityIndex = 0;
                    m_CurrentChunk = m_CurrentMatchingArchetype->Archetype->Chunks.p;
                    // m_CurrentChunk might point to an invalid chunk if the first matching archetype has no chunks
                    // the while loop below will move to the first archetype that has any entities
                    m_CurrentChunkEntityIndex = 0;
                }

                while (index >= m_CurrentArchetypeEntityIndex + m_CurrentMatchingArchetype->Archetype->EntityCount)
                {
                    m_CurrentArchetypeEntityIndex += m_CurrentMatchingArchetype->Archetype->EntityCount;
                    m_CurrentMatchingArchetypeIndex = m_CurrentMatchingArchetypeIndexNext;
                    m_CurrentChunk = m_CurrentMatchingArchetype->Archetype->Chunks.p;
                    m_CurrentChunkEntityIndex = 0;
                }

                index -= m_CurrentArchetypeEntityIndex;
                if (index < m_CurrentChunkEntityIndex)
                {
                    m_CurrentChunk = m_CurrentMatchingArchetype->Archetype->Chunks.p;
                    m_CurrentChunkEntityIndex = 0;
                }

                while (index >= m_CurrentChunkEntityIndex + (*m_CurrentChunk)->Count)
                {
                    m_CurrentChunkEntityIndex += (*m_CurrentChunk)->Count;
                    m_CurrentChunk = m_CurrentChunk + 1;
                }
            }
            else
            {
                if (index < m_CurrentArchetypeEntityIndex + m_CurrentChunkEntityIndex)
                {
                    if (index < m_CurrentArchetypeEntityIndex)
                    {
                        m_CurrentMatchingArchetypeIndex = m_FirstMatchingArchetypeIndex;
                        m_CurrentArchetypeEntityIndex = 0;
                    }

                    m_CurrentChunk = m_CurrentMatchingArchetype->Archetype->Chunks.p - 1;
                    // m_CurrentChunk now points to an invalid chunk but since the chunk list is circular
                    // it effectively points to the chunk before the first
                    // MoveToNextMatchingChunk will move it to a valid chunk if any exists
                    m_CurrentChunkEntityIndex = 0;
                    MoveToNextMatchingChunk();
                }

                while (index >= m_CurrentArchetypeEntityIndex + m_CurrentChunkEntityIndex + (*m_CurrentChunk)->Count)
                {
                    m_CurrentChunkEntityIndex += (*m_CurrentChunk)->Count;
                    MoveToNextMatchingChunk();
                }
            }
        }

        public void MoveToChunkWithoutFiltering(int index)
        {
            if (index < m_CurrentArchetypeIndex)
            {
                m_CurrentMatchingArchetypeIndex = m_FirstMatchingArchetypeIndex;
                m_CurrentChunk = m_CurrentMatchingArchetype->Archetype->Chunks.p;
                m_CurrentArchetypeIndex = m_CurrentArchetypeEntityIndex = 0;
                m_CurrentChunkIndex = m_CurrentChunkEntityIndex = 0;
            }

            while (index >= m_CurrentArchetypeIndex + m_CurrentMatchingArchetype->Archetype->Chunks.Count)
            {
                m_CurrentArchetypeEntityIndex += m_CurrentMatchingArchetype->Archetype->EntityCount;
                m_CurrentArchetypeIndex += m_CurrentMatchingArchetype->Archetype->Chunks.Count;

                m_CurrentMatchingArchetypeIndex = m_CurrentMatchingArchetypeIndexNext;
                m_CurrentChunk = m_CurrentMatchingArchetype->Archetype->Chunks.p;

                m_CurrentChunkIndex = m_CurrentChunkEntityIndex = 0;
            }

            index -= m_CurrentArchetypeIndex;
            if (index < m_CurrentChunkIndex)
            {
                m_CurrentChunk = m_CurrentMatchingArchetype->Archetype->Chunks.p;
                m_CurrentChunkIndex = m_CurrentChunkEntityIndex = 0;
            }

            while (index >= m_CurrentChunkIndex + 1)
            {
                m_CurrentChunkEntityIndex += (*m_CurrentChunk)->Count;
                m_CurrentChunkIndex += 1;

                m_CurrentChunk = m_CurrentChunk + 1;
            }
        }

        public bool MatchesFilter()
        {
            return (*m_CurrentChunk)->MatchesFilter(m_CurrentMatchingArchetype, ref m_Filter);
        }

        public bool RequiresFilter()
        {
            return m_Filter.RequiresMatchesFilter;
        }

        public int GetIndexInArchetypeFromCurrentChunk(int indexInComponentGroup)
        {
            return m_CurrentMatchingArchetype->IndexInArchetype[indexInComponentGroup];
        }

        public void UpdateCacheToCurrentChunk(out ComponentChunkCache cache, bool isWriting, int indexInComponentGroup)
        {
            var archetype = m_CurrentMatchingArchetype->Archetype;

            int indexInArchetype = m_CurrentMatchingArchetype->IndexInArchetype[indexInComponentGroup];

            cache.CachedBeginIndex = m_CurrentChunkEntityIndex + m_CurrentArchetypeEntityIndex;
            cache.CachedEndIndex = cache.CachedBeginIndex + (*m_CurrentChunk)->Count;
            cache.CachedSizeOf = archetype->SizeOfs[indexInArchetype];
            cache.CachedPtr = (*m_CurrentChunk)->Buffer + archetype->Offsets[indexInArchetype] -
                              cache.CachedBeginIndex * cache.CachedSizeOf;

            cache.IsWriting = isWriting;
            if (isWriting)
                (*m_CurrentChunk)->SetChangeVersion(indexInArchetype, m_GlobalSystemVersion);
        }

        public int GetCurrentChunkCount()
        {
            return (*m_CurrentChunk)->Count;
        }

        public void GetCurrentChunkRange(out int beginIndex, out int endIndex)
        {
            if (m_Filter.RequiresMatchesFilter)
            {
                beginIndex = GetIndexOfFirstEntityInCurrentChunk();
            }
            else
            {
                beginIndex = m_CurrentChunkEntityIndex + m_CurrentArchetypeEntityIndex;
            }

            endIndex = beginIndex + (*m_CurrentChunk)->Count;
        }

        public void* GetCurrentChunkComponentDataPtr(bool isWriting, int indexInComponentGroup)
        {
            var archetype = m_CurrentMatchingArchetype->Archetype;

            int indexInArchetype = m_CurrentMatchingArchetype->IndexInArchetype[indexInComponentGroup];

            if (isWriting)
                (*m_CurrentChunk)->SetChangeVersion(indexInArchetype, m_GlobalSystemVersion);

            return (*m_CurrentChunk)->Buffer + archetype->Offsets[indexInArchetype];
        }

        public void UpdateChangeVersion()
        {
            int indexInArchetype = m_CurrentMatchingArchetype->IndexInArchetype[IndexInComponentGroup];
            (*m_CurrentChunk)->SetChangeVersion(indexInArchetype, m_GlobalSystemVersion);
        }

        public void MoveToEntityIndexAndUpdateCache(int index, out ComponentChunkCache cache, bool isWriting)
        {
            Assert.IsTrue(-1 != IndexInComponentGroup);
            MoveToEntityIndex(index);
            UpdateCacheToCurrentChunk(out cache, isWriting, IndexInComponentGroup);
        }

        internal ArchetypeChunk GetCurrentChunk()
        {
            return new ArchetypeChunk
            {
                m_Chunk = (*m_CurrentChunk)
            };
        }

        // Determines how many chunks of a particular archetype we must iterate through while filtering
        // If the chunk is in the current archetype, we can calculate # of iterations
        // If the chunk is not in the current archetype, just loop over all chunks in the current archetype
        private int CalculateFilteredIterationChunkCount(MatchingArchetype* match)
        {
            var archetype = match->Archetype;
            var chunkCount = match == m_CurrentMatchingArchetype ? m_CurrentChunkIndex : archetype->Chunks.Count;
            return chunkCount;
        }

        // todo: shouldn't be recalculating this for every chunk. Find a way to cache this.
        internal void GetFilteredChunkAndEntityIndices(out int chunkIndex, out int entityIndex)
        {
            if (!RequiresFilter())
            {
                chunkIndex = m_CurrentArchetypeIndex + m_CurrentChunkIndex;
                entityIndex = m_CurrentArchetypeEntityIndex + m_CurrentChunkEntityIndex;
                return;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!(*m_CurrentChunk)->MatchesFilter(m_CurrentMatchingArchetype, ref m_Filter))
            {
                throw new InvalidOperationException("Trying to get chunk index and entity offset on a chunk that doesn't match the current filter");
            }
#endif

            chunkIndex = 0;
            entityIndex = 0;

            for(var m = m_FirstMatchingArchetypeIndex; m != m_CurrentMatchingArchetypeIndexNext; --m)
            {
                var match = m_MatchingArchetypeList.p[m];
                if (match->Archetype->EntityCount <= 0)
                    continue;

                var chunkCount = CalculateFilteredIterationChunkCount(match);
                var archetype = match->Archetype;

                for (var chunkIndexInArchetype = 0; chunkIndexInArchetype < chunkCount; ++chunkIndexInArchetype)
                {
                    var chunk = archetype->Chunks.p[chunkIndexInArchetype];
                    if (!chunk->MatchesFilter(match, ref m_Filter))
                        continue;

                    entityIndex += chunk->Count;
                    chunkIndex++;
                }
            }
        }

        internal int GetIndexOfFirstEntityInCurrentChunk()
        {
            var index = 0;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!(*m_CurrentChunk)->MatchesFilter(m_CurrentMatchingArchetype, ref m_Filter))
            {
                throw new InvalidOperationException("Trying to get chunk index and entity offset on a chunk that doesn't match the current filter");
            }
#endif

            for(var m = m_FirstMatchingArchetypeIndex; m != m_CurrentMatchingArchetypeIndexNext; --m)
            {
                var match = m_MatchingArchetypeList.p[m];
                if (match->Archetype->EntityCount <= 0)
                    continue;

                var chunkCount = CalculateFilteredIterationChunkCount(match);
                var archetype = match->Archetype;

                for (var chunkIndex = 0; chunkIndex < chunkCount; ++chunkIndex)
                {
                    var chunk = archetype->Chunks.p[chunkIndex];
                    if (!chunk->MatchesFilter(match, ref m_Filter))
                        continue;

                    index += chunk->Count;
                }
            }

            return index;
        }
    }
}
