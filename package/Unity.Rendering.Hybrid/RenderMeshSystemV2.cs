#if USE_BATCH_RENDERER_GROUP
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace Unity.Rendering
{
    [BurstCompile]
    struct GatherChunkRenderers : IJobParallelFor
    {
        [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
        [ReadOnly] public ArchetypeChunkSharedComponentType<RenderMesh> RenderMeshType;
        public NativeArray<int> ChunkRenderer;

        public void Execute(int chunkIndex)
        {
            var chunk = Chunks[chunkIndex];
            var sharedIndex = chunk.GetSharedComponentIndex(RenderMeshType);
            ChunkRenderer[chunkIndex] = sharedIndex;
        }
    }
    
    [BurstCompile]
    struct GatherChunkSubScenes : IJobParallelFor
    {
        [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
        [ReadOnly] public ArchetypeChunkSharedComponentType<SubsceneTag> SubsceneTagType;
        public NativeArray<int> ChunkSubscene;

        public void Execute(int chunkIndex)
        {
            var chunk = Chunks[chunkIndex];
            var sharedIndex = chunk.GetSharedComponentIndex(SubsceneTagType);
            ChunkSubscene[chunkIndex] = sharedIndex;
        }
    }

    [BurstCompile]
    struct FilterSubsceneChunks : IJob
    {
        [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
        [ReadOnly] public ArchetypeChunkComponentType<ChunkWorldRenderBounds> ChunkWorldRenderBoundsType;
        [ReadOnly] public ArchetypeChunkComponentType<WorldRenderBounds> WorldRenderBoundsType;
        [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> LocalToWorldType;
        [ReadOnly] public ArchetypeChunkSharedComponentType<RenderMesh> RenderMeshType;
        [ReadOnly] public ArchetypeChunkComponentType<Frozen> FrozenType;
        
        public NativeArray<ArchetypeChunk> SubsceneChunks;
        public NativeArray<int> SubsceneChunkCount;

        public void Execute()
        {
            int subsceneChunkCount = 0;
            for (var j = 0; j < Chunks.Length; j++)
            {
                var chunk = Chunks[j];

                if (chunk.Invalid())
                    continue;

                if (!chunk.Has(ChunkWorldRenderBoundsType))
                    continue;

                if (!chunk.Has(WorldRenderBoundsType))
                    continue;

                if (!chunk.Has(LocalToWorldType))
                    continue;

                if (!chunk.Has(RenderMeshType))
                    continue;

                if (!chunk.Has(FrozenType))
                    continue;

                SubsceneChunks[subsceneChunkCount] = chunk;
                subsceneChunkCount++;
            }

            SubsceneChunkCount[0] = subsceneChunkCount;
        }
    }

    struct SubSceneTagOrderVersion
    {
        public SubsceneTag Scene;
        public int Version;
    }

    /// <summary>
    /// Renders all Entities containing both RenderMesh & LocalToWorld components.
    /// </summary>
    [ExecuteAlways]
    //@TODO: Necessary due to empty component group. When Component group and archetype chunks are unified this should be removed
    [AlwaysUpdateSystem]
    [UpdateAfter(typeof(LodRequirementsUpdateSystem))]
    public class RenderMeshSystemV2 : ComponentSystem
    {
        int m_LastFrozenChunksOrderVersion = -1;

        ComponentGroup m_FrozenGroup;
        ComponentGroup m_DynamicGroup;
        ComponentGroup m_SubsceneLoadedGroup;

        ComponentGroup m_CullingJobDependencyGroup;
        InstancedRenderMeshBatchGroup m_InstancedRenderMeshBatchGroup;

        NativeHashMap<SubsceneTag, int> m_SubsceneTagVersion;
        NativeList<SubSceneTagOrderVersion> m_LastKnownSubsceneTagVersion;

        protected override void OnCreateManager()
        {
            m_FrozenGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = Array.Empty<ComponentType>(),
                All = new ComponentType[]
                    {typeof(ChunkWorldRenderBounds), typeof(WorldRenderBounds), typeof(LocalToWorld), typeof(RenderMesh), typeof(Frozen), typeof(MeshLODComponent), typeof(SubsceneTag)}
            });
            m_DynamicGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = new ComponentType[] { typeof(Frozen) },
                All = new ComponentType[]
                    {typeof(WorldRenderBounds), typeof(LocalToWorld), typeof(RenderMesh)  }
            });
            m_SubsceneLoadedGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = Array.Empty<ComponentType>(),
                All = new ComponentType[] {typeof(SubsceneLoadedTag)}
            });

            // This component group must include all types that are being used by the culling job
            m_CullingJobDependencyGroup = GetComponentGroup(typeof(RootLodRequirement), typeof(LodRequirement), typeof(WorldRenderBounds));

            m_InstancedRenderMeshBatchGroup = new InstancedRenderMeshBatchGroup(EntityManager, this, m_CullingJobDependencyGroup);
            m_SubsceneTagVersion = new NativeHashMap<SubsceneTag, int>(1000,Allocator.Persistent);
            m_LastKnownSubsceneTagVersion = new NativeList<SubSceneTagOrderVersion>(Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            m_InstancedRenderMeshBatchGroup.CompleteJobs();
            m_InstancedRenderMeshBatchGroup.Dispose();
            m_SubsceneTagVersion.Dispose();
            m_LastKnownSubsceneTagVersion.Dispose();
        }

        public void CacheMeshBatchRendererGroup(SubsceneTag tag, NativeArray<ArchetypeChunk> chunks, int chunkCount)
        {
            var RenderMeshType = GetArchetypeChunkSharedComponentType<RenderMesh>();
            var meshInstanceFlippedTagType = GetArchetypeChunkComponentType<RenderMeshFlippedWindingTag>();

            Profiler.BeginSample("Sort Shared Renderers");
            var chunkRenderer = new NativeArray<int>(chunkCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var sortedChunks = new NativeArraySharedValues<int>(chunkRenderer, Allocator.TempJob);

            var gatherChunkRenderersJob = new GatherChunkRenderers
            {
                Chunks = chunks,
                RenderMeshType = RenderMeshType,
                ChunkRenderer = chunkRenderer
            };
            var gatherChunkRenderersJobHandle = gatherChunkRenderersJob.Schedule(chunkCount, 64);
            var sortedChunksJobHandle = sortedChunks.Schedule(gatherChunkRenderersJobHandle);
            sortedChunksJobHandle.Complete();
            Profiler.EndSample();

            var sharedRenderCount = sortedChunks.SharedValueCount;
            var sharedRendererCounts = sortedChunks.GetSharedValueIndexCountArray();
            var sortedChunkIndices = sortedChunks.GetSortedIndices();

            m_InstancedRenderMeshBatchGroup.BeginBatchGroup();
            Profiler.BeginSample("Add New Batches");
            {
                var sortedChunkIndex = 0;
                for (int i = 0; i < sharedRenderCount; i++)
                {
                    var startSortedChunkIndex = sortedChunkIndex;
                    var endSortedChunkIndex = startSortedChunkIndex + sharedRendererCounts[i];

                    while (sortedChunkIndex < endSortedChunkIndex)
                    {
                        var chunkIndex = sortedChunkIndices[sortedChunkIndex];
                        var chunk = chunks[chunkIndex];
                        var rendererSharedComponentIndex = chunk.GetSharedComponentIndex(RenderMeshType);
                        var remainingEntitySlots = 1023;
                        var flippedWinding = chunk.Has(meshInstanceFlippedTagType);
                        int instanceCount = chunk.Count;
                        int startSortedIndex = sortedChunkIndex;
                        int batchChunkCount = 1;

                        remainingEntitySlots -= chunk.Count;
                        sortedChunkIndex++;

                        while (remainingEntitySlots > 0)
                        {
                            if (sortedChunkIndex >= endSortedChunkIndex)
                                break;

                            var nextChunkIndex = sortedChunkIndices[sortedChunkIndex];
                            var nextChunk = chunks[nextChunkIndex];
                            if (nextChunk.Count > remainingEntitySlots)
                                break;

                            var nextFlippedWinding = nextChunk.Has(meshInstanceFlippedTagType);
                            if (nextFlippedWinding != flippedWinding)
                                break;

                            remainingEntitySlots -= nextChunk.Count;
                            instanceCount += nextChunk.Count;
                            batchChunkCount++;
                            sortedChunkIndex++;
                        }

                        m_InstancedRenderMeshBatchGroup.AddBatch(tag, rendererSharedComponentIndex, instanceCount, chunks, sortedChunkIndices, startSortedIndex, batchChunkCount, flippedWinding );
                    }
                }
            }
            Profiler.EndSample();
            m_InstancedRenderMeshBatchGroup.EndBatchGroup(tag, chunks, sortedChunkIndices);

            chunkRenderer.Dispose();
            sortedChunks.Dispose();
        }

        void UpdateFrozenRenderBatches()
        {
            var frozenOrderVersion = EntityManager.GetComponentOrderVersion<Frozen>();
            var staticChunksOrderVersion = frozenOrderVersion;
            if (staticChunksOrderVersion == m_LastFrozenChunksOrderVersion)
                return;

            for (int i = 0; i < m_LastKnownSubsceneTagVersion.Length; i++)
            {
                var scene = m_LastKnownSubsceneTagVersion[i].Scene;
                var version = m_LastKnownSubsceneTagVersion[i].Version;

                if (EntityManager.GetSharedComponentOrderVersion(scene) != version)
                {
                    // Debug.Log($"Removing scene:{scene:X8} batches");
                    Profiler.BeginSample("Remove Subscene");
                    m_SubsceneTagVersion.Remove(scene);
                    m_InstancedRenderMeshBatchGroup.RemoveTag(scene);
                    Profiler.EndSample();
                }
            }
            
            m_LastKnownSubsceneTagVersion.Clear();
            
            var subsceneLoadedChunks = m_SubsceneLoadedGroup.CreateArchetypeChunkArray(Allocator.TempJob);
            var subsceneTagType = GetArchetypeChunkSharedComponentType<SubsceneTag>();
            var chunkWorldRenderBoundsType = GetArchetypeChunkComponentType<ChunkWorldRenderBounds>();
            var worldRenderBoundsType = GetArchetypeChunkComponentType<WorldRenderBounds>();
            var localToWorldType = GetArchetypeChunkComponentType<LocalToWorld>();
            var RenderMeshType = GetArchetypeChunkSharedComponentType<RenderMesh>();
            var frozenType = GetArchetypeChunkComponentType<Frozen>();

            for (var i = 0; i < subsceneLoadedChunks.Length; i++)
            {
                var subsceneTypesChunk = subsceneLoadedChunks[i];
                var subsceneTagSharedComoonentIndex = subsceneTypesChunk.GetSharedComponentIndex(subsceneTagType);
                var subsceneTag = EntityManager.GetSharedComponentData<SubsceneTag>(subsceneTagSharedComoonentIndex);
                int subsceneTagVersion = EntityManager.GetSharedComponentOrderVersion(subsceneTag);

                m_LastKnownSubsceneTagVersion.Add(new SubSceneTagOrderVersion
                {
                    Scene = subsceneTag,
                    Version = subsceneTagVersion
                });

                var alreadyTrackingSubscene = m_SubsceneTagVersion.TryGetValue(subsceneTag, out subsceneTagVersion);
                if (alreadyTrackingSubscene)
                    continue;

                var subsceneChunkTracker = World.GetOrCreateManager<SubsceneChunkTracker>();
                var subsceneChunks = subsceneChunkTracker.Chunks[subsceneTag];
                var filteredChunks = new NativeArray<ArchetypeChunk>(subsceneChunks.Length, Allocator.TempJob,
                    NativeArrayOptions.UninitializedMemory);
                var filteredChunkCount = new NativeArray<int>(1, Allocator.TempJob);

                var filterSubsceneChunksJob = new FilterSubsceneChunks
                {
                    Chunks = subsceneChunks,
                    SubsceneChunks = filteredChunks,
                    SubsceneChunkCount = filteredChunkCount,
                    ChunkWorldRenderBoundsType = chunkWorldRenderBoundsType,
                    WorldRenderBoundsType = worldRenderBoundsType,
                    LocalToWorldType = localToWorldType,
                    RenderMeshType = RenderMeshType,
                    FrozenType = frozenType
                };
                var filterSubsceneChunksJobHandle = filterSubsceneChunksJob.Schedule();
                filterSubsceneChunksJobHandle.Complete();

                m_SubsceneTagVersion.TryAdd(subsceneTag, subsceneTagVersion);

                Profiler.BeginSample("CacheMeshBatchRenderGroup");
                CacheMeshBatchRendererGroup(subsceneTag, filteredChunks, filteredChunkCount[0]);
                Profiler.EndSample();

                filteredChunks.Dispose();
                filteredChunkCount.Dispose();
            }
            subsceneLoadedChunks.Dispose();

            m_LastFrozenChunksOrderVersion = staticChunksOrderVersion;
        }

        void UpdateDynamicRenderBatches()
        {
            m_InstancedRenderMeshBatchGroup.RemoveTag(new SubsceneTag());

            Profiler.BeginSample("CreateArchetypeChunkArray");
            var chunks = m_DynamicGroup.CreateArchetypeChunkArray(Allocator.TempJob);
            Profiler.EndSample();

            if (chunks.Length > 0)
            {
                CacheMeshBatchRendererGroup(new SubsceneTag(), chunks, chunks.Length);
            }
            chunks.Dispose();
        }

        protected override void OnUpdate()
        {
            m_InstancedRenderMeshBatchGroup.CompleteJobs();
            m_InstancedRenderMeshBatchGroup.ResetLod();

            Profiler.BeginSample("UpdateFrozenRenderBatches");
            UpdateFrozenRenderBatches();
            Profiler.EndSample();

            Profiler.BeginSample("UpdateDynamicRenderBatches");
            UpdateDynamicRenderBatches();
            Profiler.EndSample();
        }
    }
}
#endif
