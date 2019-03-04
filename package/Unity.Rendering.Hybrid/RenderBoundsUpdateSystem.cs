using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Rendering
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [ExecuteAlways]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.EntitySceneOptimizations)]
    public class CreateMissingRenderBoundsFromMeshRenderer : ComponentSystem
    {
        ComponentGroup m_MissingRenderBounds;

        protected override void OnCreateManager()
        {
            m_MissingRenderBounds = GetComponentGroup(
                ComponentType.Exclude<Frozen>(), 
                ComponentType.Exclude<RenderBounds>(), 
                ComponentType.ReadWrite<RenderMesh>());
        }

        protected override void OnUpdate()
        {
            var chunks = m_MissingRenderBounds.CreateArchetypeChunkArray(Allocator.TempJob);
            for (int i = 0; i < chunks.Length; ++i)
            {
                var chunk = chunks[i];
                var sharedComponent = chunk.GetSharedComponentData(GetArchetypeChunkSharedComponentType<RenderMesh>(), EntityManager);
                if (sharedComponent.mesh != null)
                {
                    var entities = chunk.GetNativeArray(GetArchetypeChunkEntityType());
                    for (int j = 0; j < chunk.Count; ++j)
                    {
                        PostUpdateCommands.AddComponent(entities[j], new RenderBounds { Value = sharedComponent.mesh.bounds.ToAABB() });
                    }
                }
            }
            chunks.Dispose();
        }
    }

    /// <summary>
    /// Updates WorldRenderBounds for anything that has LocalToWorld and RenderBounds (and ensures WorldRenderBounds exists)
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(CreateMissingRenderBoundsFromMeshRenderer))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.EntitySceneOptimizations)]
    [ExecuteAlways]
    public class RenderBoundsUpdateSystem : JobComponentSystem
    {
        ComponentGroup m_MissingWorldRenderBounds;
        ComponentGroup m_WorldRenderBounds;

        [BurstCompile]
        struct BoundsJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion]
            public NativeArray<ArchetypeChunk> Chunks;

            [ReadOnly] public ArchetypeChunkComponentType<RenderBounds> RendererBounds;
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> LocalToWorld;
            public ArchetypeChunkComponentType<WorldRenderBounds> WorldRenderBounds;

            WorldRenderBounds Transform(LocalToWorld transform, RenderBounds localBounds)
            {
                return new WorldRenderBounds { Value = AABB.Transform(transform.Value, localBounds.Value) };
            }

            public void Execute(int index)
            {
                ArchetypeChunk chunk = Chunks[index];

                //@TODO: Delta change...
                var worldBounds = chunk.GetNativeArray(WorldRenderBounds);

                if (chunk.Has(RendererBounds))
                {
                    var localBounds = chunk.GetNativeArray(RendererBounds);
                    var localToWorld = chunk.GetNativeArray(LocalToWorld);
                    for (int i = 0; i != localBounds.Length; i++)
                        worldBounds[i] = Transform(localToWorld[i], localBounds[i]);
                }
            }
        }

        public void AllowFrozenHack()
        {
            m_MissingWorldRenderBounds = GetComponentGroup(typeof(RenderBounds), typeof(LocalToWorld), ComponentType.Exclude<WorldRenderBounds>());

            //@TODO: For controlling if system should update or not... Merge with m_Query once ComponentGroup is unified
            m_WorldRenderBounds = GetComponentGroup(
                typeof(WorldRenderBounds), 
                ComponentType.ReadOnly<LocalToWorld>());
        }

        protected override void OnCreateManager()
        {
            m_MissingWorldRenderBounds = GetComponentGroup(
                typeof(RenderBounds), 
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.Exclude<WorldRenderBounds>(),
                ComponentType.Exclude<Frozen>());

            //@TODO: For controlling if system should update or not... Merge with m_Query once ComponentGroup is unified
            m_WorldRenderBounds = GetComponentGroup(
                typeof(WorldRenderBounds), 
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.Exclude<Frozen>());

        }

        protected override JobHandle OnUpdate(JobHandle dependency)
        {
            EntityManager.AddComponent(m_MissingWorldRenderBounds, typeof(WorldRenderBounds));

            var boundsJob = new BoundsJob
            {
                Chunks = m_WorldRenderBounds.CreateArchetypeChunkArray(Allocator.TempJob),
                RendererBounds = GetArchetypeChunkComponentType<RenderBounds>(true),
                LocalToWorld = GetArchetypeChunkComponentType<LocalToWorld>(true),
                WorldRenderBounds = GetArchetypeChunkComponentType<WorldRenderBounds>(),
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
