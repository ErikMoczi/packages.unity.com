using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Rendering
{
    public struct PerInstanceCullingTag : IComponentData { }

    struct RootLodRequirement : IComponentData
    {
        public LodRequirement LOD;
        public int            InstanceCount;
    }

    struct LodRequirement : IComponentData
    {
        public float3 WorldReferencePosition;
        public float MinDist;
        public float MaxDist;

        public LodRequirement(MeshLODGroupComponent lodGroup, LocalToWorld localToWorld, int lodMask)
        {
            var referencePoint = math.transform(localToWorld.Value, lodGroup.LocalReferencePoint);
            float minDist = 0.0f;
            float maxDist = 0.0f;
            if ((lodMask & 0x01) == 0x01)
            {
                minDist = 0.0f;
                maxDist = lodGroup.LODDistances0.x;
            }
            else if ((lodMask & 0x02) == 0x02)
            {
                minDist = lodGroup.LODDistances0.x;
                maxDist = lodGroup.LODDistances0.y;
            }
            else if ((lodMask & 0x04) == 0x04)
            {
                minDist = lodGroup.LODDistances0.y;
                maxDist = lodGroup.LODDistances0.z;
            }
            else if ((lodMask & 0x08) == 0x08)
            {
                minDist = lodGroup.LODDistances0.z;
                maxDist = lodGroup.LODDistances0.w;
            }
            else if ((lodMask & 0x10) == 0x10)
            {
                minDist = lodGroup.LODDistances0.w;
                maxDist = lodGroup.LODDistances1.x;
            }
            else if ((lodMask & 0x20) == 0x20)
            {
                minDist = lodGroup.LODDistances1.x;
                maxDist = lodGroup.LODDistances1.y;
            }
            else if ((lodMask & 0x40) == 0x40)
            {
                minDist = lodGroup.LODDistances1.y;
                maxDist = lodGroup.LODDistances1.z;
            }
            else if ((lodMask & 0x80) == 0x80)
            {
                minDist = lodGroup.LODDistances1.z;
                maxDist = lodGroup.LODDistances1.w;
            }

            WorldReferencePosition = referencePoint;
            MinDist = minDist;
            MaxDist = maxDist;
        }
    }

    [UpdateAfter(typeof(RenderBoundsUpdateSystem))]
    [ExecuteAlways]
    public class LodRequirementsUpdateSystem : JobComponentSystem
    {
        ComponentGroup m_MissingWorldMeshRenderBounds;
        ComponentGroup m_MissingChunkWorldMeshRenderBounds;

        ComponentGroup m_Group;
        ComponentGroup m_MissingRootLodRequirement;
        ComponentGroup m_MissingLodRequirement;

        [BurstCompile]
        struct UpdateLodRequirementsJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion]
            public NativeArray<ArchetypeChunk> Chunks;

            [ReadOnly] public ComponentDataFromEntity<MeshLODGroupComponent>    MeshLODGroupComponent;

            [ReadOnly] public ArchetypeChunkComponentType<MeshLODComponent>     MeshLODComponent;
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld>         LocalToWorld;
            [ReadOnly] public ComponentDataFromEntity<LocalToWorld>             LocalToWorldLookup;
            
            public ArchetypeChunkComponentType<LodRequirement>                  LodRequirement;
            public ArchetypeChunkComponentType<RootLodRequirement>              RootLodRequirement;


            public void Execute(int index)
            {
                var chunk = Chunks[index];

                //@TODO: Delta change...
                var lodRequirement = chunk.GetNativeArray(LodRequirement);
                var rootLodRequirement = chunk.GetNativeArray(RootLodRequirement);
                var meshLods = chunk.GetNativeArray(MeshLODComponent);
                var localToWorlds = chunk.GetNativeArray(LocalToWorld);
                var instanceCount = chunk.Count;

//                var requirementCount = 0;
//                var lastLodGroupEntity = Entity.Null;
//                var lastLodGroupMask = 0;
//                var uniqueLodCount = 0;

                for (int i = 0; i < instanceCount; i++)
                {
                    var meshLod = meshLods[i];
                    var localToWorld = localToWorlds[i];
                    var lodGroupEntity = meshLod.Group;
                    var lodMask = meshLod.LODMask;
                    var lodGroup = MeshLODGroupComponent[lodGroupEntity];
/*
                    var sameAsLast = (lodGroupEntity == lastLodGroupEntity) && (lastLodGroupMask == lodMask);
                    if (!sameAsLast)
                    {
                        uniqueLodCount++;
                        lastLodGroupEntity = lodGroupEntity;
                        lastLodGroupMask = lodMask;
                    }
*/
                    lodRequirement[i] = new LodRequirement(lodGroup, localToWorld, lodMask);
                }

                var rootLodIndex = -1;
                var lastLodRootMask = 0;
                var lastLodRootGroupEntity = Entity.Null;

                for (int i = 0; i < instanceCount; i++)
                {
                    var meshLod = meshLods[i];
                    var lodGroupEntity = meshLod.Group;
                    var lodGroup = MeshLODGroupComponent[lodGroupEntity];
                    var parentMask = lodGroup.ParentMask;
                    var parentGroupEntity = lodGroup.ParentGroup;
                    var changedRoot = parentGroupEntity != lastLodRootGroupEntity || parentMask != lastLodRootMask || i == 0;

                    if (changedRoot)
                    {
                        rootLodIndex++;
                        RootLodRequirement rootLod;
                        rootLod.InstanceCount = 1;

                        if (parentGroupEntity == Entity.Null)
                        {
                            rootLod.LOD.WorldReferencePosition = new float3(0, 0, 0);
                            rootLod.LOD.MinDist = 0;
                            rootLod.LOD.MaxDist = 64000.0f;
                        }
                        else
                        {
                            var parentLodGroup = MeshLODGroupComponent[parentGroupEntity];
                            rootLod.LOD = new LodRequirement(parentLodGroup, LocalToWorldLookup[parentGroupEntity], parentMask);
                            rootLod.InstanceCount = 1;

                            if (parentLodGroup.ParentGroup != Entity.Null)
                                throw new System.NotImplementedException("Deep HLOD is not supported yet");
                        }

                        rootLodRequirement[rootLodIndex] = rootLod;
                        lastLodRootGroupEntity = parentGroupEntity;
                        lastLodRootMask = parentMask;
                    }
                    else
                    {
                        var lastRoot = rootLodRequirement[rootLodIndex];
                        lastRoot.InstanceCount++;
                        rootLodRequirement[rootLodIndex] = lastRoot;
                    }
                }

/*
                var foundRootInstanceCount = 0;
                for (int i = 0; i < rootLodIndex + 1; i++)
                {
                    var lastRoot = rootLodRequirement[i];
                    foundRootInstanceCount += lastRoot.InstanceCount;
                }

                if (chunk.Count != foundRootInstanceCount)
                {
                    throw new System.ArgumentException("Out of bounds");
                }
*/
            }
        }

        public void AllowFrozenHack()
        {
            m_MissingLodRequirement = GetComponentGroup(typeof(MeshLODComponent), ComponentType.Subtractive<LodRequirement>());
            m_MissingRootLodRequirement = GetComponentGroup(typeof(MeshLODComponent), ComponentType.Subtractive<RootLodRequirement>());
            m_Group = GetComponentGroup(typeof(LocalToWorld), typeof(LodRequirement), typeof(RootLodRequirement));
        }


        protected override void OnCreateManager()
        {
            m_MissingLodRequirement = GetComponentGroup(typeof(MeshLODComponent), ComponentType.Subtractive<LodRequirement>(), ComponentType.Subtractive<Frozen>());
            m_MissingRootLodRequirement = GetComponentGroup(typeof(MeshLODComponent), ComponentType.Subtractive<RootLodRequirement>(), ComponentType.Subtractive<Frozen>());
            m_Group = GetComponentGroup(typeof(LocalToWorld), typeof(LodRequirement), typeof(RootLodRequirement), ComponentType.Subtractive<Frozen>());
        }

        protected override JobHandle OnUpdate(JobHandle dependency)
        {
            EntityManager.AddComponent(m_MissingLodRequirement, typeof(LodRequirement));
            EntityManager.AddComponent(m_MissingRootLodRequirement, typeof(RootLodRequirement));

            var updateLodJob = new UpdateLodRequirementsJob
            {
                Chunks = m_Group.CreateArchetypeChunkArray(Allocator.TempJob),
                MeshLODGroupComponent = GetComponentDataFromEntity<MeshLODGroupComponent>(true),
                MeshLODComponent = GetArchetypeChunkComponentType<MeshLODComponent>(true),
                LocalToWorld = GetArchetypeChunkComponentType<LocalToWorld>(true),
                LocalToWorldLookup = GetComponentDataFromEntity<LocalToWorld>(true),
                LodRequirement = GetArchetypeChunkComponentType<LodRequirement>(),
                RootLodRequirement = GetArchetypeChunkComponentType<RootLodRequirement>(),
            };
            return updateLodJob.Schedule(updateLodJob.Chunks.Length, 1, dependency);
        }
    }
}
