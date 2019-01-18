#if !USE_BATCH_RENDERER_GROUP
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace Unity.Rendering
{
    // #TODO Bulk add/remove SystemStateComponentData

    /// <summary>
    /// Renders all Entities containing both RenderMesh & LocalToWorld components.
    /// </summary>
    [ExecuteAlways]
    [UpdateAfter(typeof(RenderBoundsUpdateSystem))]
    public class RenderMeshSystem : ComponentSystem
    {
        public Camera ActiveCamera;

        private int m_LastFrozenChunksOrderVersion = -1;
        private int m_LastDynamicChunksOrderVersion = -1;
        private int m_LastLocalToWorldOrderVersion = -1;

        private NativeArray<ArchetypeChunk> m_FrozenChunks;
        private NativeArray<ArchetypeChunk> m_DynamicChunks;
        private NativeArray<WorldRenderBounds> m_FrozenChunkBounds;
        
        // Instance renderer takes only batches of 1023
        Matrix4x4[] m_MatricesArray = new Matrix4x4[1023];
        private NativeArray<float4> m_Planes;
        
        ComponentGroup m_FrozenChunksQuery;
        ComponentGroup m_DynamicChunksQuery;

        static unsafe void CopyTo(NativeSlice<VisibleLocalToWorld> transforms, int count, Matrix4x4[] outMatrices, int offset)
        {
            // @TODO: This is using unsafe code because the Unity DrawInstances API takes a Matrix4x4[] instead of NativeArray.
            Assert.AreEqual(sizeof(Matrix4x4), sizeof(VisibleLocalToWorld));
            fixed (Matrix4x4* resultMatrices = outMatrices)
            {
                VisibleLocalToWorld* sourceMatrices = (VisibleLocalToWorld*) transforms.GetUnsafeReadOnlyPtr();
                UnsafeUtility.MemCpy(resultMatrices + offset, sourceMatrices , UnsafeUtility.SizeOf<Matrix4x4>() * count);
            }
        }
        
        protected override void OnCreateManager()
        {
            m_FrozenChunksQuery = GetComponentGroup(new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = Array.Empty<ComponentType>(),
                All = new ComponentType[] {typeof(LocalToWorld), typeof(RenderMesh), typeof(VisibleLocalToWorld), typeof(Frozen)}
            });
            m_DynamicChunksQuery = GetComponentGroup(new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = new ComponentType[] {typeof(Frozen)},
                All = new ComponentType[] {typeof(LocalToWorld), typeof(RenderMesh), typeof(VisibleLocalToWorld)}
            });
            
            GetComponentGroup(new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = new ComponentType[] { typeof(VisibleLocalToWorld) },
                All = new ComponentType[] { typeof(RenderMesh), typeof(LocalToWorld) }
            });

            m_Planes = new NativeArray<float4>(6, Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            if (m_FrozenChunks.IsCreated)
            {
                m_FrozenChunks.Dispose();
            }
            if (m_FrozenChunkBounds.IsCreated)
            {
                m_FrozenChunkBounds.Dispose();
            }
            if (m_DynamicChunks.IsCreated)
            {
                m_DynamicChunks.Dispose();
            }

            m_Planes.Dispose();
        }

        [BurstCompile]
        struct UpdateChunkBounds : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly] public ArchetypeChunkComponentType<WorldRenderBounds> WorldRenderBoundsType;
            public NativeArray<WorldRenderBounds> ChunkBounds;

            public void Execute(int index)
            {
                var chunk = Chunks[index];

                var instanceBounds = chunk.GetNativeArray(WorldRenderBoundsType);
                if (instanceBounds.Length == 0)
                    return;

                // TODO: Improve this approach
                // See: https://www.inf.ethz.ch/personal/emo/DoctThesisFiles/fischer05.pdf

                var chunkBounds = (Bounds)instanceBounds[0].Value;
                for (int j = 1; j < instanceBounds.Length; j++)
                {
                    chunkBounds.Encapsulate(instanceBounds[j].Value);
                }

                ChunkBounds[index] = new WorldRenderBounds {Value = chunkBounds};
            }

        }
        
        [BurstCompile]
        unsafe struct CullLODToVisible : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly] public ComponentDataFromEntity<ActiveLODGroupMask> ActiveLODGroupMask;
            [ReadOnly] public ArchetypeChunkComponentType<MeshLODComponent> MeshLODComponentType;
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> LocalToWorldType;
            [ReadOnly] public ArchetypeChunkComponentType<WorldRenderBounds> WorldRenderBoundsType;
            [NativeDisableUnsafePtrRestriction]
            [ReadOnly] public WorldRenderBounds* ChunkBounds;
            [ReadOnly] public NativeArray<float4> Planes;
            public ArchetypeChunkComponentType<VisibleLocalToWorld> VisibleLocalToWorldType;
            public NativeArray<int> ChunkVisibleCount;

            float4x4* GetVisibleOutputBuffer(ArchetypeChunk chunk)
            {
                var chunkVisibleLocalToWorld = chunk.GetNativeArray(VisibleLocalToWorldType);
                return (float4x4*)chunkVisibleLocalToWorld.GetUnsafePtr();
            }
            
            float4x4* GetLocalToWorldSourceBuffer(ArchetypeChunk chunk)
            {
                var chunkLocalToWorld = chunk.GetNativeArray(LocalToWorldType);
                
                if (chunkLocalToWorld.Length > 0)
                    return (float4x4*) chunkLocalToWorld.GetUnsafeReadOnlyPtr();
                else
                    return null;
            }

            void VisibleIn(int index)
            {
                var chunk = Chunks[index];
                var chunkEntityCount = chunk.Count;
                var chunkVisibleCount = 0;
                var chunkLODs = chunk.GetNativeArray(MeshLODComponentType);
                var hasMeshLODComponentType = chunkLODs.Length > 0;

                float4x4* dstPtr = GetVisibleOutputBuffer(chunk);
                float4x4* srcPtr = GetLocalToWorldSourceBuffer(chunk);
                if (srcPtr == null)
                    return;

                if (!hasMeshLODComponentType)
                {
                    for (int i = 0; i < chunkEntityCount; i++)
                    {
                        UnsafeUtility.MemCpy(dstPtr + chunkVisibleCount + i, srcPtr + i, UnsafeUtility.SizeOf<float4x4>());
                    }

                    chunkVisibleCount = chunkEntityCount;
                }
                else
                {
                    for (int i = 0; i < chunkEntityCount; i++)
                    {
                        var instanceLOD = chunkLODs[i];
                        var instanceLODValid = (ActiveLODGroupMask[instanceLOD.Group].LODMask & instanceLOD.LODMask) != 0;
                        if (instanceLODValid)
                        {
                            UnsafeUtility.MemCpy(dstPtr + chunkVisibleCount, srcPtr + i, UnsafeUtility.SizeOf<float4x4>());
                            chunkVisibleCount++;
                        }
                    }
                }

                ChunkVisibleCount[index] = chunkVisibleCount;
            }

            void VisiblePartial(int index)
            {
                var chunk = Chunks[index];
                var chunkEntityCount = chunk.Count;
                var chunkVisibleCount = 0;
                var chunkLODs = chunk.GetNativeArray(MeshLODComponentType);
                var chunkBounds = chunk.GetNativeArray(WorldRenderBoundsType);
                var hasMeshLODComponentType = chunkLODs.Length > 0;
                var hasWorldRenderBounds = chunkBounds.Length > 0;
                
                float4x4* dstPtr = GetVisibleOutputBuffer(chunk);
                float4x4* srcPtr = GetLocalToWorldSourceBuffer(chunk);
                if (srcPtr == null)
                    return;

                // 00 (-WorldRenderBounds -MeshLODComponentType)
                if ((!hasWorldRenderBounds) && (!hasMeshLODComponentType))
                {
                    for (int i = 0; i < chunkEntityCount; i++)
                    {
                        UnsafeUtility.MemCpy(dstPtr + chunkVisibleCount + i, srcPtr + i, UnsafeUtility.SizeOf<float4x4>());
                    }

                    chunkVisibleCount = chunkEntityCount;
                }
                // 01 (-WorldRenderBounds +MeshLODComponentType)
                else if ((!hasWorldRenderBounds) && (hasMeshLODComponentType))
                {
                    for (int i = 0; i < chunkEntityCount; i++)
                    {
                        var instanceLOD = chunkLODs[i];
                        var instanceLODValid = (ActiveLODGroupMask[instanceLOD.Group].LODMask & instanceLOD.LODMask) != 0;
                        if (instanceLODValid)
                        {
                            UnsafeUtility.MemCpy(dstPtr + chunkVisibleCount, srcPtr + i, UnsafeUtility.SizeOf<float4x4>());
                            chunkVisibleCount++;
                        }
                    }
                }
                // 10 (+WorldRenderBounds -MeshLODComponentType)
                else if ((hasWorldRenderBounds) && (!hasMeshLODComponentType))
                {
                    for (int i = 0; i < chunkEntityCount; i++)
                    {
                        var instanceBounds = chunkBounds[i];
                        var instanceCullValid = (FrustumPlanes.Intersect(Planes, instanceBounds.Value) != FrustumPlanes.IntersectResult.Out);

                        if (instanceCullValid)
                        {
                            UnsafeUtility.MemCpy(dstPtr + chunkVisibleCount, srcPtr + i, UnsafeUtility.SizeOf<float4x4>());
                            chunkVisibleCount++;
                        }
                    }
                }
                // 11 (+WorldRenderBounds +MeshLODComponentType)
                else
                {
                    
                    for (int i = 0; i < chunkEntityCount; i++)
                    {
                        var instanceLOD = chunkLODs[i];
                        var instanceLODValid = (ActiveLODGroupMask[instanceLOD.Group].LODMask & instanceLOD.LODMask) != 0;
                        if (instanceLODValid)
                        {
                            var instanceBounds = chunkBounds[i];
                            var instanceCullValid = (FrustumPlanes.Intersect(Planes, instanceBounds.Value) != FrustumPlanes.IntersectResult.Out);
                            if (instanceCullValid)
                            {
                                UnsafeUtility.MemCpy(dstPtr + chunkVisibleCount, srcPtr + i, UnsafeUtility.SizeOf<float4x4>());
                                chunkVisibleCount++;
                            }
                        }
                    }
                }

                ChunkVisibleCount[index] = chunkVisibleCount;
            }

            public void Execute(int index)
            {
                if (ChunkBounds == null)
                {
                    VisiblePartial(index);
                    return;
                }
                
                var chunk = Chunks[index];
                
                var hasWorldRenderBounds = chunk.Has(WorldRenderBoundsType);
                if (!hasWorldRenderBounds)
                {
                    VisibleIn(index);
                    return;
                }
                
                var chunkBounds = ChunkBounds[index];
                var chunkInsideResult = FrustumPlanes.Intersect(Planes, chunkBounds.Value);
                if (chunkInsideResult == FrustumPlanes.IntersectResult.Out)
                {
                    ChunkVisibleCount[index] = 0;
                }
                else if (chunkInsideResult == FrustumPlanes.IntersectResult.In)
                {
                    VisibleIn(index);
                }
                else
                {
                    VisiblePartial(index);
                }
            }
        };
        
        [BurstCompile]
        struct MapChunkRenderers : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly] public ArchetypeChunkSharedComponentType<RenderMesh> RenderMeshType;
            public NativeMultiHashMap<int, int>.Concurrent ChunkRendererMap;

            public void Execute(int index)
            {
                var chunk = Chunks[index];
                var rendererSharedComponentIndex = chunk.GetSharedComponentIndex(RenderMeshType);
                ChunkRendererMap.Add(rendererSharedComponentIndex, index);
            }
        };

        [BurstCompile]
        struct GatherSortedChunks : IJob
        {
            [ReadOnly] public NativeMultiHashMap<int, int> ChunkRendererMap;
            public int SharedComponentCount;
            public NativeArray<ArchetypeChunk> SortedChunks;
            public NativeArray<ArchetypeChunk> Chunks;

            public void Execute()
            {
                int sortedIndex = 0;
                for (int i = 0; i < SharedComponentCount; i++)
                {
                    int chunkIndex = 0;

                    NativeMultiHashMapIterator<int> it;
                    if (!ChunkRendererMap.TryGetFirstValue(i, out chunkIndex, out it))
                        continue;
                    do
                    {
                        SortedChunks[sortedIndex] = Chunks[chunkIndex];
                        sortedIndex++;
                    } while (ChunkRendererMap.TryGetNextValue(out chunkIndex, ref it));
                }
            }
        };

        [BurstCompile]
        unsafe struct PackVisibleChunkIndices : IJob
        {
            [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly] public NativeArray<int> ChunkVisibleCount;
            public NativeArray<int> PackedChunkIndices;
            [NativeDisableUnsafePtrRestriction]
            public int* PackedChunkCount;

            public void Execute()
            {
                var packedChunkCount = 0;
                for (int i = 0; i < Chunks.Length; i++)
                {
                    if (ChunkVisibleCount[i] > 0)
                    {
                        PackedChunkIndices[packedChunkCount] = i;
                        packedChunkCount++;
                    }
                }
                *PackedChunkCount = packedChunkCount;
            }

        }
        
        unsafe void UpdateFrozenInstanceRenderer()
        {
            if (m_FrozenChunks.Length == 0)
            {
                return;
            }
            
            Profiler.BeginSample("Gather Types");
            var localToWorldType = GetArchetypeChunkComponentType<LocalToWorld>(true);
            var visibleLocalToWorldType = GetArchetypeChunkComponentType<VisibleLocalToWorld>(false);
            var RenderMeshType = GetArchetypeChunkSharedComponentType<RenderMesh>();
            var flippedWindingTagType = GetArchetypeChunkComponentType<RenderMeshFlippedWindingTag>();
            var WorldRenderBoundsType = GetArchetypeChunkComponentType<WorldRenderBounds>(true);
            var meshLODComponentType = GetArchetypeChunkComponentType<MeshLODComponent>(true);
            var activeLODGroupMask = GetComponentDataFromEntity<ActiveLODGroupMask>(true);

            Profiler.EndSample();
            
            Profiler.BeginSample("Allocate Temp Data");
            var chunkVisibleCount   = new NativeArray<int>(m_FrozenChunks.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var packedChunkIndices  = new NativeArray<int>(m_FrozenChunks.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            Profiler.EndSample();
                
            var cullLODToVisibleJob = new CullLODToVisible
            {
                Chunks = m_FrozenChunks,
                ActiveLODGroupMask = activeLODGroupMask,
                MeshLODComponentType = meshLODComponentType,
                LocalToWorldType = localToWorldType,
                WorldRenderBoundsType = WorldRenderBoundsType,
                ChunkBounds = (WorldRenderBounds*)m_FrozenChunkBounds.GetUnsafePtr(),
                Planes = m_Planes,
                VisibleLocalToWorldType = visibleLocalToWorldType,
                ChunkVisibleCount = chunkVisibleCount,
            };
            var cullLODToVisibleJobHandle = cullLODToVisibleJob.Schedule(m_FrozenChunks.Length, 64);

            var packedChunkCount = 0;
            var packVisibleChunkIndicesJob = new PackVisibleChunkIndices
            {
                Chunks = m_FrozenChunks,
                ChunkVisibleCount =  chunkVisibleCount,
                PackedChunkIndices = packedChunkIndices,
                PackedChunkCount = &packedChunkCount
            };
            var packVisibleChunkIndicesJobHandle = packVisibleChunkIndicesJob.Schedule(cullLODToVisibleJobHandle);
            packVisibleChunkIndicesJobHandle.Complete();
                
            Profiler.BeginSample("Process DrawMeshInstanced");
            var drawCount = 0;
            var lastRendererIndex = -1;
            var batchCount = 0;
            var flippedWinding = false;

            for (int i = 0; i < packedChunkCount; i++)
            {
                var chunkIndex = packedChunkIndices[i];
                var chunk = m_FrozenChunks[chunkIndex];
                var rendererIndex = chunk.GetSharedComponentIndex(RenderMeshType);
                var activeCount = chunkVisibleCount[chunkIndex];
                var rendererChanged = rendererIndex != lastRendererIndex;
                var fullBatch = ((batchCount + activeCount) > 1023);
                var visibleTransforms = chunk.GetNativeArray(visibleLocalToWorldType);

                var newFlippedWinding = chunk.Has(flippedWindingTagType);

                if ((fullBatch || rendererChanged || (newFlippedWinding != flippedWinding)) && (batchCount > 0))
                {
                    RenderBatch(lastRendererIndex, batchCount);

                    drawCount++;
                    batchCount = 0;
                }

                CopyTo(visibleTransforms, activeCount, m_MatricesArray, batchCount);

                flippedWinding = newFlippedWinding;
                batchCount += activeCount;
                lastRendererIndex = rendererIndex;
            }

            if (batchCount > 0)
            {
                RenderBatch(lastRendererIndex, batchCount);

                drawCount++;
            }
            Profiler.EndSample();
            
            packedChunkIndices.Dispose();
            chunkVisibleCount.Dispose();
        }
        
        unsafe void UpdateDynamicInstanceRenderer()
        {
            if (m_DynamicChunks.Length == 0)
            {
                return;
            }
            
            Profiler.BeginSample("Gather Types");
            var localToWorldType = GetArchetypeChunkComponentType<LocalToWorld>(true);
            var visibleLocalToWorldType = GetArchetypeChunkComponentType<VisibleLocalToWorld>(false);
            var RenderMeshType = GetArchetypeChunkSharedComponentType<RenderMesh>();
            var flippedWindingTagType = GetArchetypeChunkComponentType<RenderMeshFlippedWindingTag>();
            var WorldRenderBoundsType = GetArchetypeChunkComponentType<WorldRenderBounds>(true);
            var meshLODComponentType = GetArchetypeChunkComponentType<MeshLODComponent>(true);
            var activeLODGroupMask = GetComponentDataFromEntity<ActiveLODGroupMask>(true);
            Profiler.EndSample();
            
            Profiler.BeginSample("Allocate Temp Data");
            var chunkVisibleCount   = new NativeArray<int>(m_DynamicChunks.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var packedChunkIndices  = new NativeArray<int>(m_DynamicChunks.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            Profiler.EndSample();
                
            var cullLODToVisibleJob = new CullLODToVisible
            {
                Chunks = m_DynamicChunks,
                ActiveLODGroupMask = activeLODGroupMask,
                MeshLODComponentType = meshLODComponentType,
                LocalToWorldType = localToWorldType,
                WorldRenderBoundsType = WorldRenderBoundsType,
                ChunkBounds = null,
                Planes = m_Planes,
                VisibleLocalToWorldType = visibleLocalToWorldType,
                ChunkVisibleCount = chunkVisibleCount,
            };
            var cullLODToVisibleJobHandle = cullLODToVisibleJob.Schedule(m_DynamicChunks.Length, 64);

            var packedChunkCount = 0;
            var packVisibleChunkIndicesJob = new PackVisibleChunkIndices
            {
                Chunks = m_DynamicChunks,
                ChunkVisibleCount =  chunkVisibleCount,
                PackedChunkIndices = packedChunkIndices,
                PackedChunkCount = &packedChunkCount
            };
            var packVisibleChunkIndicesJobHandle = packVisibleChunkIndicesJob.Schedule(cullLODToVisibleJobHandle);
            packVisibleChunkIndicesJobHandle.Complete();
                
            Profiler.BeginSample("Process DrawMeshInstanced");
            var drawCount = 0;
            var lastRendererIndex = -1;
            var batchCount = 0;
            var flippedWinding = false;

            for (int i = 0; i < packedChunkCount; i++)
            {
                var chunkIndex = packedChunkIndices[i];
                var chunk = m_DynamicChunks[chunkIndex];
                var rendererIndex = chunk.GetSharedComponentIndex(RenderMeshType);
                var activeCount = chunkVisibleCount[chunkIndex];
                var rendererChanged = rendererIndex != lastRendererIndex;
                var fullBatch = ((batchCount + activeCount) > 1023);
                var visibleTransforms = chunk.GetNativeArray(visibleLocalToWorldType);

                var newFlippedWinding = chunk.Has(flippedWindingTagType);

                if ((fullBatch || rendererChanged || (newFlippedWinding != flippedWinding)) && (batchCount > 0))
                {
                    RenderBatch(lastRendererIndex, batchCount);

                    drawCount++;
                    batchCount = 0;
                }

                CopyTo(visibleTransforms, activeCount, m_MatricesArray, batchCount);

                flippedWinding = newFlippedWinding;
                batchCount += activeCount;
                lastRendererIndex = rendererIndex;
            }

            if (batchCount > 0)
            {
                RenderBatch(lastRendererIndex, batchCount);

                drawCount++;
            }
            Profiler.EndSample();
            
            packedChunkIndices.Dispose();
            chunkVisibleCount.Dispose();
        }
        
        void RenderBatch(int lastRendererIndex, int batchCount)
        {
            var renderer = EntityManager.GetSharedComponentData<RenderMesh>(lastRendererIndex);
            if (renderer.mesh && renderer.material)
            {
                if (renderer.material.enableInstancing)
                {
                    Graphics.DrawMeshInstanced(renderer.mesh, renderer.subMesh, renderer.material, m_MatricesArray,
                        batchCount, null, renderer.castShadows, renderer.receiveShadows, renderer.layer, ActiveCamera);
                }
                else
                {
                    for (int i = 0; i != batchCount; i++)
                    {
                        Graphics.DrawMesh(renderer.mesh, m_MatricesArray[i], renderer.material, renderer.layer, ActiveCamera, renderer.subMesh, null, renderer.castShadows, renderer.receiveShadows);
                    }

                    //@TODO : temporarily disabled because it spams the console about Resources/unity_builtin_extra
                    //@TODO : also, it doesn't work in the player because of AssetDatabase
//                    if (batchCount >= 2)
//                        Debug.LogWarning($"Please enable GPU instancing for better performance ({renderer.material})\n{AssetDatabase.GetAssetPath(renderer.material)}", renderer.material);
                }
            }
        }
        
        void UpdateFrozenChunkCache()
        {
            var visibleLocalToWorldOrderVersion = EntityManager.GetComponentOrderVersion<VisibleLocalToWorld>();
            var frozenOrderVersion = EntityManager.GetComponentOrderVersion<Frozen>();
            var staticChunksOrderVersion = math.min(visibleLocalToWorldOrderVersion, frozenOrderVersion);
            if (staticChunksOrderVersion == m_LastFrozenChunksOrderVersion)
                return;
            
            // Dispose
            if (m_FrozenChunks.IsCreated)
            {
                m_FrozenChunks.Dispose();
            }
            if (m_FrozenChunkBounds.IsCreated)
            {
                m_FrozenChunkBounds.Dispose();
            }
            
            var sharedComponentCount = EntityManager.GetSharedComponentCount();
            var RenderMeshType = GetArchetypeChunkSharedComponentType<RenderMesh>();
            var WorldRenderBoundsType = GetArchetypeChunkComponentType<WorldRenderBounds>(true);
            
            // Allocate temp data
            var chunkRendererMap = new NativeMultiHashMap<int, int>(100000, Allocator.TempJob);
            var foundArchetypes = new NativeList<EntityArchetype>(Allocator.TempJob);

            Profiler.BeginSample("CreateArchetypeChunkArray");
            var chunks = m_FrozenChunksQuery.CreateArchetypeChunkArray(Allocator.TempJob);
            Profiler.EndSample();
            
            m_FrozenChunks = new NativeArray<ArchetypeChunk>(chunks.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_FrozenChunkBounds = new NativeArray<WorldRenderBounds>(chunks.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            
            var mapChunkRenderersJob = new MapChunkRenderers
            {
                Chunks = chunks,
                RenderMeshType = RenderMeshType,
                ChunkRendererMap = chunkRendererMap.ToConcurrent()
            };
            var mapChunkRenderersJobHandle = mapChunkRenderersJob.Schedule(chunks.Length, 64);
            
            var gatherSortedChunksJob = new GatherSortedChunks
            {
                ChunkRendererMap = chunkRendererMap,
                SharedComponentCount = sharedComponentCount,
                SortedChunks = m_FrozenChunks,
                Chunks = chunks
            };
            var gatherSortedChunksJobHandle = gatherSortedChunksJob.Schedule(mapChunkRenderersJobHandle);
            
            var updateChangedChunkBoundsJob = new UpdateChunkBounds
            {
                Chunks = m_FrozenChunks,
                WorldRenderBoundsType = WorldRenderBoundsType,
                ChunkBounds = m_FrozenChunkBounds
            };
            var updateChangedChunkBoundsJobHandle = updateChangedChunkBoundsJob.Schedule(chunks.Length, 64, gatherSortedChunksJobHandle);
            updateChangedChunkBoundsJobHandle.Complete();
            
            foundArchetypes.Dispose();
            chunkRendererMap.Dispose();
            chunks.Dispose();

            m_LastFrozenChunksOrderVersion = staticChunksOrderVersion;
        }
        
        void UpdateDynamicChunkCache()
        {
            var dynamicChunksOrderVersion = EntityManager.GetComponentOrderVersion<VisibleLocalToWorld>();
            if (dynamicChunksOrderVersion == m_LastDynamicChunksOrderVersion)
                return;
            
            // Dispose
            if (m_DynamicChunks.IsCreated)
            {
                m_DynamicChunks.Dispose();
            }
            
            var sharedComponentCount = EntityManager.GetSharedComponentCount();
            var RenderMeshType = GetArchetypeChunkSharedComponentType<RenderMesh>();
            
            // Allocate temp data
            var chunkRendererMap = new NativeMultiHashMap<int, int>(100000, Allocator.TempJob);
            var foundArchetypes = new NativeList<EntityArchetype>(Allocator.TempJob);

            Profiler.BeginSample("CreateArchetypeChunkArray");
            var chunks = m_DynamicChunksQuery.CreateArchetypeChunkArray(Allocator.TempJob);
            Profiler.EndSample();
            
            m_DynamicChunks = new NativeArray<ArchetypeChunk>(chunks.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            
            var mapChunkRenderersJob = new MapChunkRenderers
            {
                Chunks = chunks,
                RenderMeshType = RenderMeshType,
                ChunkRendererMap = chunkRendererMap.ToConcurrent()
            };
            var mapChunkRenderersJobHandle = mapChunkRenderersJob.Schedule(chunks.Length, 64);
            
            var gatherSortedChunksJob = new GatherSortedChunks
            {
                ChunkRendererMap = chunkRendererMap,
                SharedComponentCount = sharedComponentCount,
                SortedChunks = m_DynamicChunks,
                Chunks = chunks
            };
            var gatherSortedChunksJobHandle = gatherSortedChunksJob.Schedule(mapChunkRenderersJobHandle);
            gatherSortedChunksJobHandle.Complete();
            
            foundArchetypes.Dispose();
            chunkRendererMap.Dispose();
            chunks.Dispose();

            m_LastDynamicChunksOrderVersion = dynamicChunksOrderVersion;
        }

        void UpdateMissingVisibleLocalToWorld()
        {
            var localToWorldOrderVersion = EntityManager.GetComponentOrderVersion<LocalToWorld>();
            if (localToWorldOrderVersion == m_LastLocalToWorldOrderVersion)
                return;
            
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            
            var query = new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = new ComponentType[] {typeof(VisibleLocalToWorld)},
                All = new ComponentType[] {typeof(RenderMesh), typeof(LocalToWorld)}
            };
            var entityType = GetArchetypeChunkEntityType();
            var chunks = EntityManager.CreateArchetypeChunkArray(query, Allocator.TempJob);
            for (int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                var entities = chunk.GetNativeArray(entityType);
                for (int j = 0; j < chunk.Count; j++)
                {
                    var entity = entities[j];
                    entityCommandBuffer.AddComponent(entity,default(VisibleLocalToWorld));
                }
            }
            
            entityCommandBuffer.Playback(EntityManager);
            entityCommandBuffer.Dispose();
            chunks.Dispose();

            m_LastLocalToWorldOrderVersion = localToWorldOrderVersion;
        }

        protected override void OnUpdate()
        {
            if (ActiveCamera != null)
            {
                FrustumPlanes.FromCamera(ActiveCamera, m_Planes);

                UpdateMissingVisibleLocalToWorld();

                Profiler.BeginSample("UpdateFrozenChunkCache");
                UpdateFrozenChunkCache();
                Profiler.EndSample();
                
                Profiler.BeginSample("UpdateDynamicChunkCache");
                UpdateDynamicChunkCache();
                Profiler.EndSample();

                Profiler.BeginSample("UpdateFrozenInstanceRenderer");
                UpdateFrozenInstanceRenderer();
                Profiler.EndSample();
                
                Profiler.BeginSample("UpdateDynamicInstanceRenderer");
                UpdateDynamicInstanceRenderer();
                Profiler.EndSample();
            }
        }
    }
}
#endif
