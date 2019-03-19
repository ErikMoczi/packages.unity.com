using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using System;

namespace Unity.Audio.Megacity
{
    public struct ChunkUtils
    {
        public static int ReallocateEntitiesFromChunkArray(ref NativeArray<Entity> entities, EntityManager entityManager, ComponentGroup group, Allocator allocator, int maxElements = 0x7FFFFFFF)
        {
            var chunks = group.CreateArchetypeChunkArray(Allocator.TempJob);
            int count = ReallocateEntitiesFromChunkArray(ref entities, entityManager, chunks, allocator, maxElements);
            chunks.Dispose();

            return count;
        }

        public static int ReallocateEntitiesFromChunkArray(ref NativeArray<Entity> entities, EntityManager entityManager, NativeArray<ArchetypeChunk> chunks, Allocator allocator, int maxElements = 0x7FFFFFFF)
        {
            if (entities.IsCreated)
                entities.Dispose();

            int numElements = 0;
            for (int i = 0; i < chunks.Length; i++)
            {
                numElements += chunks[i].Count;
                if (numElements >= maxElements)
                {
                    numElements = maxElements;
                    break;
                }
            }

            if (numElements == 0)
            {
                entities = new NativeArray<Entity>(0, allocator);
                return 0;
            }

            entities = new NativeArray<Entity>(numElements, allocator);

            var entityType = entityManager.GetArchetypeChunkEntityType();

            int offset = 0;
            for (int c = 0; c < chunks.Length; c++)
            {
                var chunk = chunks[c];
                var entitiesInChunk = chunk.GetNativeArray(entityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (offset == numElements)
                        break;
                    entities[offset++] = entitiesInChunk[i];
                }
            }

            return numElements;
        }
    }

    public struct ChunkEntityEnumerable : IDisposable
    {
        ArchetypeChunkEntityType entityType;
        NativeArray<ArchetypeChunk> chunks;

        public ChunkEntityEnumerable(bool dummy)
        {
            entityType = default;
            chunks = default;
        }

        public ChunkEntityEnumerable(EntityManager entityManager, ComponentGroup group, Allocator allocator)
        {
            entityType = default;
            chunks = default;

            Setup(entityManager, group, allocator);
        }

        public void Setup(EntityManager entityManager, ComponentGroup group, Allocator allocator)
        {
            if (chunks.IsCreated)
                chunks.Dispose();

            entityType = entityManager.GetArchetypeChunkEntityType();
            chunks = group.CreateArchetypeChunkArray(allocator);
        }

        public bool Empty
        {
            get
            {
                for (int n = 0; n < chunks.Length; n++)
                    if (chunks[n].Count > 0)
                        return false;
                return true;
            }
        }

        public struct ChunkEntityEnumerator
        {
            internal ArchetypeChunkEntityType entityType;
            internal NativeArray<ArchetypeChunk> chunks;
            internal int chunkIndex;
            internal int elementIndex;
            internal NativeArray<Entity> currChunk;
            internal int currChunkLength;

            public Entity Current
            {
                get
                {
                    return currChunk[elementIndex];
                }
            }

            public T GetCurrentSharedData<T>(ArchetypeChunkSharedComponentType<T> componentType, EntityManager entityManager) where T : struct, ISharedComponentData
            {
                return chunks[chunkIndex].GetSharedComponentData<T>(componentType, entityManager);
            }

            public bool MoveNext()
            {
                if (++elementIndex >= currChunkLength)
                {
                    if (++chunkIndex >= chunks.Length)
                    {
                        return false;
                    }
                    else
                    {
                        elementIndex = 0;
                        currChunk = chunks[chunkIndex].GetNativeArray(entityType);
                        currChunkLength = currChunk.Length;
                    }
                }

                return true;
            }
        }

        public ChunkEntityEnumerator GetEnumerator()
        {
            return new ChunkEntityEnumerator
            {
                entityType = entityType,
                chunks = chunks,
                chunkIndex = 0,
                elementIndex = -1,
                currChunk = (chunks.Length == 0) ? new NativeArray<Entity>() : chunks[0].GetNativeArray(entityType),
                currChunkLength = (chunks.Length == 0) ? 0 : chunks[0].Count
            };
        }

        public void Dispose()
        {
            if (chunks.IsCreated)
                chunks.Dispose();
        }
    }
}
