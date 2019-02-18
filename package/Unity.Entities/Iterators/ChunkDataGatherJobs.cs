﻿using System;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Unity.Entities
{
    [BurstCompile]
    unsafe struct GatherChunks : IJobParallelFor
    {
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<EntityArchetype> Archetypes;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> Offsets;
        [NativeDisableParallelForRestriction]
        public NativeArray<ArchetypeChunk> Chunks;

        public void Execute(int index)
        {
            var archetype = Archetypes[index];
            var chunkCount = archetype.Archetype->Chunks.Count;
            var offset = Offsets[index];
            var dstChunksPtr = (Chunk**) Chunks.GetUnsafePtr();
            UnsafeUtility.MemCpy(dstChunksPtr + offset, archetype.Archetype->Chunks.p, chunkCount * sizeof(Chunk*));
        }
    }

    [BurstCompile]
    unsafe struct GatherEntitiesJob : IJobChunk
    {
        public NativeArray<Entity> Entities;
        [ReadOnly]public ArchetypeChunkEntityType EntityType;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int entityOffset)
        {
            var destinationPtr = (Entity*)Entities.GetUnsafePtr() + entityOffset;
            var sourcePtr = chunk.GetNativeArray(EntityType).GetUnsafeReadOnlyPtr();
            var copySizeInBytes = sizeof(Entity) * chunk.Count;

            UnsafeUtility.MemCpy(destinationPtr, sourcePtr, copySizeInBytes);
        }
    }
    
    [BurstCompile]
    unsafe struct GatherComponentDataJob<T> : IJobChunk
        where T : struct,IComponentData
    {
        public NativeArray<T> ComponentData;
        [ReadOnly]public ArchetypeChunkComponentType<T> ComponentType;
        
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int entityOffset)
        {
            var sourcePtr = chunk.GetNativeArray(ComponentType).GetUnsafeReadOnlyPtr();
            var destinationPtr = (byte*) ComponentData.GetUnsafePtr() + UnsafeUtility.SizeOf<T>() * entityOffset;          
            var copySizeInBytes = UnsafeUtility.SizeOf<T>() * chunk.Count;
            
            UnsafeUtility.MemCpy(destinationPtr, sourcePtr, copySizeInBytes);
        }
    }
    
    [BurstCompile]
    unsafe struct CopyComponentArrayToChunks<T> : IJobChunk
        where T : struct,IComponentData
    {
        [ReadOnly]
        public NativeArray<T> ComponentData;
        public ArchetypeChunkComponentType<T> ComponentType;
        
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int entityOffset)
        {
            var destinationPtr = chunk.GetNativeArray(ComponentType).GetUnsafePtr();
            var srcPtr = (byte*) ComponentData.GetUnsafeReadOnlyPtr() + UnsafeUtility.SizeOf<T>() * entityOffset;          
            var copySizeInBytes = UnsafeUtility.SizeOf<T>() * chunk.Count;
            
            UnsafeUtility.MemCpy(destinationPtr, srcPtr, copySizeInBytes);
        }
    }
    
}