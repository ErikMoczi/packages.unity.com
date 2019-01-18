using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Rendering
{
    [UpdateAfter(typeof(EndFrameBarrier))]
    [ExecuteAlways]
    public class CreateMissingRenderBoundsFromMeshRenderer : ComponentSystem
    {
        ComponentGroup m_MissingRenderBounds;
        
        protected override void OnCreateManager()
        {
            m_MissingRenderBounds = GetComponentGroup(ComponentType.Subtractive<Frozen>(), ComponentType.Subtractive<RenderBounds>(), typeof(RenderMesh));
        }
        
        protected override void OnUpdate()
        {
            var sharedComponents = m_MissingRenderBounds.GetSharedComponentDataArray<RenderMesh>();
            var entities = m_MissingRenderBounds.GetEntityArray();
            for (int i = 0; i != sharedComponents.Length; i++)
            {
                var meshRenderer = sharedComponents[i];
                if (meshRenderer.mesh != null)
                    PostUpdateCommands.AddComponent(entities[i], new RenderBounds { Value = meshRenderer.mesh.bounds });
            }
        }
    }

    [UpdateAfter(typeof(CreateMissingRenderBoundsFromMeshRenderer))]
    [ExecuteAlways]
    public class RenderBoundsUpdateSystem : JobComponentSystem
    {
        ComponentGroup m_MissingWorldRenderBounds;
        ComponentGroup m_MissingChunkWorldRenderBounds;

        EntityArchetypeQuery m_Query;

        [BurstCompile]
        struct BoundsJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion]
            public NativeArray<ArchetypeChunk> Chunks;
            
            [ReadOnly] public ArchetypeChunkComponentType<RenderBounds> RendererBounds;
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> LocalToWorld;
            public ArchetypeChunkComponentType<WorldRenderBounds> WorldRenderBounds;
            public ArchetypeChunkComponentType<ChunkWorldRenderBounds> ChunkWorldRenderBounds;

            WorldRenderBounds Transform(LocalToWorld transform, RenderBounds localBounds)
            {
                return new WorldRenderBounds { Value = AABB.Transform(transform.Value, localBounds.Value) }; 
            }

            ChunkWorldRenderBounds CombineBoundingVolume(NativeArray<WorldRenderBounds> instanceBounds)
            {
                var minMax = MinMaxAABB.Empty;
                for (int i = 0; i < instanceBounds.Length; i++)
                    minMax.Encapsulate(instanceBounds[i].Value);

                return new ChunkWorldRenderBounds { Value = minMax };
            }

            public void Execute(int index)
            {
                ArchetypeChunk chunk = Chunks[index];

                //@TODO: Delta change...
                var worldBounds = chunk.GetNativeArray(WorldRenderBounds);
                var chunkWorldMeshRendererBounds = chunk.GetNativeArray(ChunkWorldRenderBounds);

                if (chunk.Has(RendererBounds))
                {
                    var localBounds = chunk.GetNativeArray(RendererBounds);
                    var localToWorld =  chunk.GetNativeArray(LocalToWorld);
                    for (int i = 0; i != localBounds.Length; i++)
                        worldBounds[i] = Transform(localToWorld[i], localBounds[i]);
                }

                chunkWorldMeshRendererBounds[0] = CombineBoundingVolume(worldBounds);
            }
        }

        public void AllowFrozenHack()
        {
            m_MissingWorldRenderBounds = GetComponentGroup(typeof(RenderBounds), typeof(LocalToWorld), ComponentType.Subtractive<WorldRenderBounds>());
            m_MissingChunkWorldRenderBounds = GetComponentGroup(typeof(WorldRenderBounds), typeof(LocalToWorld), ComponentType.Subtractive<ChunkWorldRenderBounds>());
        
            //@TODO: For controlling if system should update or not... Merge with m_Query once ComponentGroup is unified
            GetComponentGroup(typeof(WorldRenderBounds), typeof(LocalToWorld));
        
            m_Query = new EntityArchetypeQuery
            {
                All = new ComponentType[] { typeof(LocalToWorld), typeof(WorldRenderBounds), typeof(ChunkWorldRenderBounds) },
                None = new ComponentType[] { },
                Any = new ComponentType[0]
            };
        }

        protected override void OnCreateManager()
        {
            m_MissingWorldRenderBounds = GetComponentGroup(typeof(RenderBounds), typeof(LocalToWorld), ComponentType.Subtractive<WorldRenderBounds>(), ComponentType.Subtractive<Frozen>());
            m_MissingChunkWorldRenderBounds = GetComponentGroup(typeof(WorldRenderBounds), typeof(LocalToWorld), ComponentType.Subtractive<ChunkWorldRenderBounds>(), ComponentType.Subtractive<Frozen>());
            
            //@TODO: For controlling if system should update or not... Merge with m_Query once ComponentGroup is unified
            GetComponentGroup(typeof(WorldRenderBounds), typeof(LocalToWorld), ComponentType.Subtractive<Frozen>());
            
            m_Query = new EntityArchetypeQuery
            {
                All = new ComponentType[] { typeof(LocalToWorld), typeof(WorldRenderBounds), typeof(ChunkWorldRenderBounds) },
                None = new ComponentType[] { typeof(Frozen) },
                Any = new ComponentType[0]
            };
        }
      
        protected override JobHandle OnUpdate(JobHandle dependency)
        {
            EntityManager.AddComponent(m_MissingWorldRenderBounds, typeof(WorldRenderBounds));
            EntityManager.AddComponent(m_MissingChunkWorldRenderBounds, typeof(ChunkWorldRenderBounds));
            
            var boundsJob = new BoundsJob
            {
                Chunks = EntityManager.CreateArchetypeChunkArray(m_Query, Allocator.TempJob),
                RendererBounds = GetArchetypeChunkComponentType<RenderBounds>(true),
                LocalToWorld = GetArchetypeChunkComponentType<LocalToWorld>(true),
                WorldRenderBounds = GetArchetypeChunkComponentType<WorldRenderBounds>(),
                ChunkWorldRenderBounds = GetArchetypeChunkComponentType<ChunkWorldRenderBounds>(),
            };
            return boundsJob.Schedule(boundsJob.Chunks.Length, 1, dependency);
        }
        
#if false
        public void DrawGizmos()
        {
            var boundsGroup = GetComponentGroup(typeof(LocalToWorld), typeof(WorldMeshRenderBounds), typeof(MeshRenderBounds));
            var localToWorlds = boundsGroup.GetComponentDataArray<LocalToWorld>();
            var worldBounds = boundsGroup.GetComponentDataArray<WorldMeshRenderBounds>();
            var localBounds = boundsGroup.GetComponentDataArray<MeshRenderBounds>();
            boundsGroup.CompleteDependency();

            Gizmos.matrix =Matrix4x4.identity;
            Gizmos.color = Color.green;
            for (int i = 0; i != worldBounds.Length; i++)
            {
                Gizmos.DrawWireCube(worldBounds[i].Value.Center, worldBounds[i].Value.Size);
            }

            Gizmos.color = Color.blue;
            for (int i = 0; i != localToWorlds.Length; i++)
            {
                Gizmos.matrix = new Matrix4x4(localToWorlds[i].Value.c0, localToWorlds[i].Value.c1, localToWorlds[i].Value.c2, localToWorlds[i].Value.c3);
                Gizmos.DrawWireCube(localBounds[i].Value.Center, localBounds[i].Value.Size);
            }
        }
        
        //@TODO: We really need a system level gizmo callback.  
        [UnityEditor.DrawGizmo(UnityEditor.GizmoType.NonSelected)]
        public static void DrawGizmos(Light light, UnityEditor.GizmoType type)
        {
            if (light.type == LightType.Directional && light.isActiveAndEnabled)
            {
                var renderer = Entities.World.Active.GetExistingManager<MeshRenderBoundsUpdateSystem>();
                renderer.DrawGizmos();
            }
        }
    #endif
    }
}
