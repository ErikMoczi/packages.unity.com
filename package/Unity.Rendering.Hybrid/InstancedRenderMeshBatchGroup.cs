#if USE_BATCH_RENDERER_GROUP
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Unity.Rendering
{
    struct CopyChunkParams
    {
        public int SourceIndex;
        public int DestinationIndex;
    }

    [BurstCompile]
    struct SwapRemovedChunks : IJob
    {
        [ReadOnly] public NativeList<CopyChunkParams> ChunkParams;
        public NativeArray<ArchetypeChunk> Chunks;
        public NativeArray<int> ChunkInstanceCounts;
        public NativeArray<ChunkWorldRenderBounds> ChunkWorldRenderBounds;
        public int kMaxChunksPerBatchCount;

        public void Execute()
        {
            for (int i = 0; i < ChunkParams.Length; i++)
            {
                var chunkParams = ChunkParams[i];
                var sourceIndex = chunkParams.SourceIndex;
                var destinationIndex = chunkParams.DestinationIndex;

                for (int j = 0; j < kMaxChunksPerBatchCount; j++)
                {
                    Chunks[(destinationIndex * kMaxChunksPerBatchCount) + j] = Chunks[(sourceIndex * kMaxChunksPerBatchCount) + j];
                    ChunkInstanceCounts[(destinationIndex * kMaxChunksPerBatchCount) + j] = ChunkInstanceCounts[(sourceIndex * kMaxChunksPerBatchCount) + j];
                    ChunkWorldRenderBounds[(destinationIndex * kMaxChunksPerBatchCount) + j] = ChunkWorldRenderBounds[(sourceIndex * kMaxChunksPerBatchCount) + j];
                }
            }
        }
    }

    [BurstCompile]
    struct SwapRemovedBatches : IJob
    {
        public int BatchCount;
        public SubsceneTag Tag;
        public NativeArray<int> InstanceCounts;
        public NativeArray<SubsceneTag> Tags;
        public NativeArray<int> LodSkip;
        public NativeList<CopyChunkParams> CopyList;
        
        public void Execute()
        {
            var batchCount = BatchCount;
            for (int i = batchCount-2; i >= 0; i--)
            {
                var shouldRemove = Tags[i].Equals(Tag);
                if (!shouldRemove)
                    continue;

                var lastIndex = batchCount - 1;
                CopyList.Add(new CopyChunkParams
                {
                    SourceIndex = lastIndex,
                    DestinationIndex = i
                });

                // Swap last batch in to this position
                Tags[i] = Tags[lastIndex];
                LodSkip[i] = LodSkip[lastIndex];
                InstanceCounts[i] = InstanceCounts[lastIndex];
                batchCount--;
            }
        }
    }

    unsafe struct BatchSource
    {
        public int SortedChunkStartIndex;
        public int ChunkCount;
        public int BatchIndex;
        public float4x4* BatchMatrices;
    }

    [BurstCompile]
    unsafe struct CopySourceChunks : IJobParallelFor
    {
        [ReadOnly] public NativeArray<BatchSource> BatchSource;
        [ReadOnly] public NativeArray<ArchetypeChunk> SourceChunks;
        [ReadOnly] public NativeArray<int> SortedChunkIndices;
        public int kMaxChunksPerBatchCount;
        [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> LocalToWorldType;
        [NativeDisableParallelForRestriction] public NativeArray<ArchetypeChunk> BatchGroupChunks;
        [NativeDisableParallelForRestriction] public NativeArray<int> BatchGroupChunkInstanceCounts;
        [NativeDisableParallelForRestriction] public NativeArray<ChunkWorldRenderBounds> BatchGroupChunkWorldRenderBounds;
        [ReadOnly] public ArchetypeChunkComponentType<ChunkWorldRenderBounds> ChunkWorldRenderBoundsType;

        public void Execute(int batchSourceIndex)
        {
            var batchSource = BatchSource[batchSourceIndex];
            var sortedChunkStartIndex = batchSource.SortedChunkStartIndex;
            var chunkCount = batchSource.ChunkCount;
            var batchIndex = batchSource.BatchIndex;
            var dstMatrices = batchSource.BatchMatrices;
            var instanceIndex = 0;

            for (int i = 0; i < chunkCount; i++)
            {
                var chunkIndex = SortedChunkIndices[sortedChunkStartIndex + i];
                var chunk = SourceChunks[chunkIndex];

                BatchGroupChunks[(batchIndex * kMaxChunksPerBatchCount) + i] = chunk;
                BatchGroupChunkInstanceCounts[(batchIndex * kMaxChunksPerBatchCount) + i] = chunk.Count;
                if (chunk.Has(ChunkWorldRenderBoundsType))
                {
                    var chunkBounds = chunk.GetNativeArray(ChunkWorldRenderBoundsType);
                    BatchGroupChunkWorldRenderBounds[(batchIndex * kMaxChunksPerBatchCount) + i] = chunkBounds[0];
                }
                else
                {
                    var bigBounds = new ChunkWorldRenderBounds
                    {
                        Value = new AABB
                        {
                            Center = new float3(0.0f, 0.0f, 0.0f),
                            Extents = new float3(16738.0f, 16738.0f, 16738.0f)
                        }
                    };
                    BatchGroupChunkWorldRenderBounds[(batchIndex * kMaxChunksPerBatchCount) + i] = bigBounds;
                }

                var localToWorld = chunk.GetNativeArray(LocalToWorldType);
                var chunkInstanceCount = chunk.Count;

                var matrixSizeOf = UnsafeUtility.SizeOf<float4x4>();
                float4x4* srcMatrices = (float4x4*) localToWorld.GetUnsafeReadOnlyPtr();

                UnsafeUtility.MemCpy(dstMatrices + instanceIndex, srcMatrices, matrixSizeOf * chunkInstanceCount);
                instanceIndex += chunkInstanceCount;
            }
        }
    }

    unsafe struct ChunkInstanceLodEnabled
    {
        public fixed ulong Enabled[2];
    }

    [BurstCompile]
    unsafe struct SelectLodEnabled : IJobParallelFor
    {
        [ReadOnly] public LODGroupExtensions.LODParams LODParams;
        [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
        [ReadOnly] public NativeArray<int> LodSkip;
        [ReadOnly] public ArchetypeChunkComponentType<RootLodRequirement> RootLodRequirements;
        [ReadOnly] public ArchetypeChunkComponentType<LodRequirement> InstanceLodRequirements;
        [ReadOnly] public NativeArray<int> InstanceCounts;
        public int kMaxChunksPerBatchCount;
        [NativeDisableParallelForRestriction] public NativeArray<ChunkInstanceLodEnabled> ChunkInstanceLodEnableds;

        public void Execute(int batchIndex)
        {
            var chunkIndex = batchIndex * kMaxChunksPerBatchCount;
            var batchInstanceCount = InstanceCounts[batchIndex];
            var processedInstanceCount = 0;
            var lodSkip = LodSkip[batchIndex];

            while (processedInstanceCount < batchInstanceCount)
            {
                var chunk = Chunks[chunkIndex];
                Assert.IsTrue(chunk.Archetype.Valid);
                var chunkInstanceCount = chunk.Count;

                var rootLodRequirements = chunk.GetNativeArray(RootLodRequirements);
                var instanceLodRequirements = chunk.GetNativeArray(InstanceLodRequirements);

                var chunkEntityLodEnabled = ChunkInstanceLodEnableds[chunkIndex];
                ulong* lodEnabled = chunkEntityLodEnabled.Enabled;

                if (!chunk.Has(RootLodRequirements) || !chunk.Has(InstanceLodRequirements))
                {
                    lodEnabled[0] = ~0UL;
                    lodEnabled[1] = ~0UL;
                }
                else
                {
                    lodEnabled[0] = 0;
                    lodEnabled[1] = 0;

                    var chunkInstanceIndex = 0;
                    var rootIndex = 0;

                    while (chunkInstanceIndex < chunkInstanceCount)
                    {
                        var rootLodRequirement = rootLodRequirements[rootIndex];
                        var rootInstanceCount = rootLodRequirement.InstanceCount;
                        var rootLodDistance = math.lengthsq(LODParams.cameraPos - rootLodRequirement.LOD.WorldReferencePosition) * LODParams.screenRelativeMetric;
                        float rootMinDist = rootLodRequirement.LOD.MinDist;
                        if (lodSkip == 1)
                            rootMinDist = 0;


                        var rootLodIntersect =
                            (rootLodDistance < rootLodRequirement.LOD.MaxDist * rootLodRequirement.LOD.MaxDist) &&
                            (rootLodDistance >= rootMinDist * rootMinDist);

                        if (rootLodIntersect)
                        {
                            for (int i = 0; i < rootInstanceCount; i++)
                            {
                                var instanceLodRequirement =  instanceLodRequirements[chunkInstanceIndex + i];
                                var instanceLodDistance =
                                    (math.lengthsq(LODParams.cameraPos - instanceLodRequirement.WorldReferencePosition)) * LODParams.screenRelativeMetric;
                                var instanceLodIntersect =
                                    (instanceLodDistance < instanceLodRequirement.MaxDist * instanceLodRequirement.MaxDist) &&
                                    (instanceLodDistance >= instanceLodRequirement.MinDist * instanceLodRequirement.MinDist);

                                if (instanceLodIntersect)
                                {
                                    var index = chunkInstanceIndex + i;
                                    var wordIndex = index >> 6;
                                    var bitIndex = index & 0x3f;
                                    var lodWord = lodEnabled[wordIndex];

                                    lodWord |= 1UL << bitIndex;
                                    lodEnabled[wordIndex] = lodWord;
                                }
                            }
                        }

                        chunkInstanceIndex += rootInstanceCount;
                        rootIndex++;
                    }
                }

                ChunkInstanceLodEnableds[chunkIndex] = chunkEntityLodEnabled;
                chunkIndex++;
                processedInstanceCount += chunkInstanceCount;
            }
        }
    }

    [BurstCompile]
    unsafe struct SimpleCullingJob : IJobParallelFor
    {
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float4> Planes;
        [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
        [ReadOnly] public NativeArray<int> ChunkInstanceCounts;
        [ReadOnly] public NativeArray<ChunkWorldRenderBounds> ChunkWorldRenderBounds;

        [ReadOnly] public ArchetypeChunkComponentType<WorldRenderBounds> BoundsComponent;
        [ReadOnly] public NativeArray<ChunkInstanceLodEnabled> ChunkInstanceLodEnableds;
        public int kMaxChunksPerBatchCount;
        [ReadOnly] public NativeArray<int> InstanceCounts;

        [NativeDisableParallelForRestriction] public NativeArray<int> IndexList;
        public NativeArray<BatchVisibility> Batches;

        public void Execute(int batchIndex)
        {
            var batch = Batches[batchIndex];
            var chunkIndex = batchIndex * kMaxChunksPerBatchCount;
            var batchInstanceCount = InstanceCounts[batchIndex];
            var processedInstanceCount = 0;
            int batchOutputOffset = batch.offset;
            int batchOutputCount = 0;

            while (processedInstanceCount < batchInstanceCount)
            {
                var chunkInstanceCount = ChunkInstanceCounts[chunkIndex];
                var chunkEntityLodEnabled = ChunkInstanceLodEnableds[chunkIndex];
                ulong* lodEnabled = chunkEntityLodEnabled.Enabled;
                var anyLodEnabled = (lodEnabled[0] | lodEnabled[1]) != 0;

                if (anyLodEnabled)
                {
                    var chunkBounds = ChunkWorldRenderBounds[chunkIndex];
                    var chunkIn = FrustumPlanes.Intersect(Planes, chunkBounds.Value);

                    if (chunkIn != FrustumPlanes.IntersectResult.Out)
                    {
                        var chunk = Chunks[chunkIndex];
                        var chunkInstanceBounds = chunk.GetNativeArray(BoundsComponent);

                        for (int i = 0; i < chunkInstanceCount; i++)
                        {
                            var wordIndex = i >> 6;
                            var bitIndex = i & 0x3f;
                            var lodWord = lodEnabled[wordIndex];
                            var lodSet = (lodWord & (1UL << bitIndex)) != 0;
                            var visible = lodSet;
                            if (visible && chunkIn == FrustumPlanes.IntersectResult.Partial)
                            {
                                visible = FrustumPlanes.Intersect(Planes, chunkInstanceBounds[i].Value) != FrustumPlanes.IntersectResult.Out;
                            }

                            if (visible)
                            {
                                IndexList[batchOutputOffset + batchOutputCount] = processedInstanceCount + i;
                                batchOutputCount++;
                            }
                        }
                    }
                }

                chunkIndex++;
                processedInstanceCount += chunkInstanceCount;
            }

            batch.visibleCount = batchOutputCount;
            Batches[batchIndex] = batch;
        }
    }

    public class InstancedRenderMeshBatchGroup
    {
        int kMaxBatchCount = 64 * 1024;
        int kMaxChunksPerBatchCount = 128;

        EntityManager m_EntityManager;
        ComponentSystemBase m_ComponentSystem;
        JobHandle m_CullingJobDependency;
        JobHandle m_LODDependency;
        ComponentGroup m_CullingJobDependencyGroup;
        BatchRendererGroup m_BatchRendererGroup;

        NativeArray<ArchetypeChunk> m_Chunks;
        NativeArray<int> m_ChunkInstanceCounts;
        NativeArray<ChunkWorldRenderBounds> m_BatchGroupChunkWorldRenderBounds;
        NativeArray<int> m_InstanceCounts;
        NativeArray<SubsceneTag> m_Tags;
        NativeArray<int> m_LodSkip;
        int m_BatchCount;

        NativeArray<ChunkInstanceLodEnabled> m_ChunkInstanceLodEnableds;
        private bool m_ResetLod;

        LODGroupExtensions.LODParams m_PrevLODParams;

        NativeArray<BatchSource> m_BatchSource;
        int m_BatchSourceCount;

        ProfilerMarker m_RemoveBatchMarker;
        
        

        public InstancedRenderMeshBatchGroup(EntityManager entityManager, ComponentSystemBase componentSystem, ComponentGroup cullingJobDependencyGroup)
        {
            m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling);
            m_EntityManager = entityManager;
            m_ComponentSystem = componentSystem;
            m_CullingJobDependencyGroup = cullingJobDependencyGroup;
            m_Chunks = new NativeArray<ArchetypeChunk>(kMaxBatchCount*kMaxChunksPerBatchCount,Allocator.Persistent,NativeArrayOptions.UninitializedMemory);
            m_ChunkInstanceCounts = new NativeArray<int>(kMaxBatchCount*kMaxChunksPerBatchCount,Allocator.Persistent,NativeArrayOptions.UninitializedMemory);
            m_BatchGroupChunkWorldRenderBounds = new NativeArray<ChunkWorldRenderBounds>( kMaxBatchCount * kMaxChunksPerBatchCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_InstanceCounts = new NativeArray<int>(kMaxBatchCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_Tags = new NativeArray<SubsceneTag>(kMaxBatchCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_LodSkip = new NativeArray<int>(kMaxBatchCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_ChunkInstanceLodEnableds = new NativeArray<ChunkInstanceLodEnabled>(kMaxBatchCount*kMaxChunksPerBatchCount,Allocator.Persistent,NativeArrayOptions.UninitializedMemory);
            m_BatchSource = new NativeArray<BatchSource>(kMaxBatchCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_BatchCount = 0;
            m_ResetLod = true;
            
            m_RemoveBatchMarker = new ProfilerMarker("BatchRendererGroup.Remove");
        }

        public void Dispose()
        {
            m_BatchRendererGroup.Dispose();
            m_Chunks.Dispose();
            m_ChunkInstanceCounts.Dispose();
            m_BatchGroupChunkWorldRenderBounds.Dispose();
            m_InstanceCounts.Dispose();
            m_Tags.Dispose();
            m_LodSkip.Dispose();
            m_ChunkInstanceLodEnableds.Dispose();
            m_BatchSource.Dispose();
            m_BatchCount = 0;
            m_BatchSourceCount = 0;
            m_ResetLod = true;
        }

        public void Clear()
        {
            m_BatchRendererGroup.Dispose();
            m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling);
            m_PrevLODParams = new LODGroupExtensions.LODParams();
            m_ResetLod = true;
            m_BatchCount = 0;
            m_BatchSourceCount = 0;
        }

        public void ResetLod()
        {
            m_PrevLODParams = new LODGroupExtensions.LODParams();
            m_ResetLod = true;
        }

        public JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext)
        {
            if (m_BatchCount == 0)
                return new JobHandle();;

            var batchCount = cullingContext.batchVisibility.Length;
            if (batchCount == 0)
                return new JobHandle();;

            var lodParams = LODGroupExtensions.CalculateLODParams(cullingContext.lodParameters);
            if (lodParams.isOrtho)
                throw new System.NotImplementedException();

            Profiler.BeginSample("OnPerformCulling");

            int cullingPlaneCount = cullingContext.cullingPlanes.Length;
            var planes = new NativeArray<float4>(cullingPlaneCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < cullingPlaneCount; i++)
                planes[i] = new float4(cullingContext.cullingPlanes[i].normal,
                    cullingContext.cullingPlanes[i].distance);

            JobHandle cullingDependency;
            var resetLod = m_ResetLod || (!lodParams.Equals(m_PrevLODParams));
            if (resetLod)
            {
                var selectLodEnabledJob = new SelectLodEnabled
                {
                    LODParams = lodParams,
                    Chunks = m_Chunks,
                    RootLodRequirements = m_ComponentSystem.GetArchetypeChunkComponentType<RootLodRequirement>(true),
                    InstanceLodRequirements = m_ComponentSystem.GetArchetypeChunkComponentType<LodRequirement>(true),
                    ChunkInstanceLodEnableds = m_ChunkInstanceLodEnableds,
                    InstanceCounts = m_InstanceCounts,
                    LodSkip = m_LodSkip,
                    kMaxChunksPerBatchCount = kMaxChunksPerBatchCount
                };

                // Depend on all component ata we access + previous jobs since we are writing to a single 
                // m_ChunkInstanceLodEnableds array.
                var lodJobDependency = JobHandle.CombineDependencies(m_CullingJobDependency, m_CullingJobDependencyGroup.GetDependency());
                
                cullingDependency = m_LODDependency = selectLodEnabledJob.Schedule(batchCount, 1, lodJobDependency);
                m_PrevLODParams = lodParams;
                m_ResetLod = false;
            }
            else
            {
                // Depend on all component ata we access + previous m_LODDependency job 
                cullingDependency = JobHandle.CombineDependencies(m_LODDependency, m_CullingJobDependencyGroup.GetDependency());
            }

            var simpleCullingJob = new SimpleCullingJob
            {
                Planes = planes,
                Chunks = m_Chunks,
                ChunkInstanceCounts = m_ChunkInstanceCounts,
                ChunkWorldRenderBounds = m_BatchGroupChunkWorldRenderBounds,
                BoundsComponent = m_ComponentSystem.GetArchetypeChunkComponentType<WorldRenderBounds>(true),
                ChunkInstanceLodEnableds = m_ChunkInstanceLodEnableds,
                kMaxChunksPerBatchCount = kMaxChunksPerBatchCount,
                InstanceCounts = m_InstanceCounts,
                IndexList = cullingContext.visibleIndices,
                Batches = cullingContext.batchVisibility
            };
            var simpleCullingJobHandle = simpleCullingJob.Schedule(batchCount, 1, cullingDependency);
            DidScheduleCullingJob(simpleCullingJobHandle);

            Profiler.EndSample();
            return simpleCullingJobHandle;
        }

        public void BeginBatchGroup()
        {
            m_BatchSourceCount = 0;
        }

        public unsafe void AddBatch(SubsceneTag tag, int rendererSharedComponentIndex, int batchInstanceCount, NativeArray<ArchetypeChunk> chunks, NativeArray<int> sortedChunkIndices, int startSortedIndex, int chunkCount, bool flippedWinding)
        {
            if (chunkCount > kMaxChunksPerBatchCount)
                throw new System.ArgumentException("Too many chunks per batch");

            var bigBounds = new Bounds(new Vector3(0, 0, 0), new Vector3(16738.0f, 16738.0f, 16738.0f));

            var rendererSharedComponent = m_EntityManager.GetSharedComponentData<RenderMesh>(rendererSharedComponentIndex);
            var mesh = rendererSharedComponent.mesh;
            var material = rendererSharedComponent.material;
            var castShadows = rendererSharedComponent.castShadows;
            var receiveShadows = rendererSharedComponent.receiveShadows;
            var subMeshIndex = rendererSharedComponent.subMesh;

            if (mesh == null || material == null)
            {
                return;
            }

            Profiler.BeginSample("AddBatch");
            int batchIndex = m_BatchRendererGroup.AddBatch(mesh, subMeshIndex, material, 0, castShadows, receiveShadows, flippedWinding, bigBounds, batchInstanceCount, null, null);
            var matrices = m_BatchRendererGroup.GetBatchMatrices(batchIndex);
            Profiler.EndSample();

            var batchSource = new BatchSource
            {
                SortedChunkStartIndex = startSortedIndex,
                ChunkCount = chunkCount,
                BatchIndex = batchIndex,
                BatchMatrices = (float4x4*)matrices.GetUnsafePtr(),
            };

            m_Tags[batchIndex] = tag;
            m_LodSkip[batchIndex] = (tag.SubsectionIndex == 0) ? 1 : 0; 
            m_BatchSource[m_BatchSourceCount] = batchSource;
            m_BatchSourceCount++;

            m_InstanceCounts[batchIndex] = batchInstanceCount;
            m_BatchCount = batchIndex+1;
        }

        public void EndBatchGroup(SubsceneTag tag, NativeArray<ArchetypeChunk> chunks, NativeArray<int> sortedChunkIndices)
        {
            var localToWorldType = m_ComponentSystem.GetArchetypeChunkComponentType<LocalToWorld>(true);
            var copySourceCnunksJob = new CopySourceChunks
            {
                BatchSource = m_BatchSource,
                SourceChunks = chunks,
                SortedChunkIndices = sortedChunkIndices,
                kMaxChunksPerBatchCount = kMaxChunksPerBatchCount,
                LocalToWorldType = localToWorldType,
                BatchGroupChunks = m_Chunks,
                BatchGroupChunkInstanceCounts = m_ChunkInstanceCounts,
                BatchGroupChunkWorldRenderBounds = m_BatchGroupChunkWorldRenderBounds,
                ChunkWorldRenderBoundsType= m_ComponentSystem.GetArchetypeChunkComponentType<ChunkWorldRenderBounds>(true)
            };
            var copySourceChunksJobHandle = copySourceCnunksJob.Schedule(m_BatchSourceCount, 1);
            copySourceChunksJobHandle.Complete();
            
            // Clear LOD bias 
            if (tag.SubsectionIndex > 0)
            {
                for (int i = 0; i < m_BatchCount; i++)
                {
                    if (m_Tags[i].Location == tag.Location)
                    {
                        m_LodSkip[i] = 0;
                    }
                }
            }
            
            m_BatchSourceCount = 0;
        }

        public void RemoveTag(SubsceneTag tag)
        {
            // Reset LOD bias 
            if (tag.SubsectionIndex > 0)
            {
                for (int i = 0; i < m_BatchCount; i++)
                {
                    if (m_Tags[i].Location == tag.Location)
                    {
                        m_LodSkip[i] = 1;
                    }
                }
            }
        
            Profiler.BeginSample("RemoveTag");
            // remove any trailing tags
            for (int i = m_BatchCount-1; i >= 0; i--)
            {
                var shouldRemove = m_Tags[i].Equals(tag);
                if (!shouldRemove)
                    break;

                m_RemoveBatchMarker.Begin();
                m_BatchRendererGroup.RemoveBatch(i);
                m_RemoveBatchMarker.End();

                m_BatchCount--;
            }

            var copyList = new NativeList<CopyChunkParams>(Allocator.TempJob);

            var swapRemovedBatchesJob = new SwapRemovedBatches
            {
                BatchCount = m_BatchCount,
                Tag = tag,
                InstanceCounts = m_InstanceCounts,
                Tags = m_Tags,
                LodSkip = m_LodSkip,
                CopyList = copyList
            };
            var swapRemovedBatchesJobHandle = swapRemovedBatchesJob.Schedule();
            var swapRemovedChunksJob = new SwapRemovedChunks
            {
                ChunkParams = copyList,
                Chunks = m_Chunks,
                ChunkInstanceCounts = m_ChunkInstanceCounts,
                kMaxChunksPerBatchCount = kMaxChunksPerBatchCount,
                ChunkWorldRenderBounds = m_BatchGroupChunkWorldRenderBounds
            };
            var swapRemovedChunksJobHandle = swapRemovedChunksJob.Schedule(swapRemovedBatchesJobHandle);

            swapRemovedBatchesJobHandle.Complete();

            using (m_RemoveBatchMarker.Auto())
            {
                for (int i = 0; i < copyList.Length; i++)
                    m_BatchRendererGroup.RemoveBatch(copyList[i].DestinationIndex);
            }
            
            m_BatchCount -= copyList.Length;
            
            swapRemovedChunksJobHandle.Complete();
            copyList.Dispose();
            
            Profiler.EndSample();
        }

        public void CompleteJobs()
        {
            m_CullingJobDependency.Complete();
            m_CullingJobDependencyGroup.CompleteDependency();
        }


        void DidScheduleCullingJob(JobHandle job)
        {
            m_CullingJobDependency = JobHandle.CombineDependencies(job, m_CullingJobDependency);
            m_CullingJobDependencyGroup.AddDependency(job);
        }

    }
}
#endif
