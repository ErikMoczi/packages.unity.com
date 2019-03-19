using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using System;

// The BoxTriggerSystem would track positions of all points, which are defined by Entity(Position,BoxIndex),
// and if a point moves from one box to another -- add a TriggerCondition component to the box.
// The trigger system is in charge of removing the TriggerCondition after handling it.
// See MusicTriggerSystem for an example.

namespace Unity.Audio.Megacity
{
    public class BoxTriggerBarrier : EntityCommandBufferSystem
    {
    }

    [UpdateBefore(typeof(BoxTriggerBarrier))]
    class BoxTriggerSystem : JobComponentSystem
    {
        private ComponentGroup m_BBGroup;
        private ComponentGroup m_PointGroup;

        private BoxTriggerBarrier m_Barrier;

        public ChunkEntityEnumerable m_BoundingBoxEnumerable;

        public struct TriggerJob : IJobChunk
        {
            [ReadOnly]
            public ChunkEntityEnumerable m_BoundingBoxEnumerable;

            [ReadOnly]
            public ComponentDataFromEntity<Translation> m_PointPosition;

            [ReadOnly]
            public ComponentDataFromEntity<BoxIndex> m_PointBoxIndex;

            [ReadOnly]
            public ComponentDataFromEntity<BoundingBox> m_BoundingBoxes;

            [ReadOnly]
            public ArchetypeChunkEntityType m_PointEntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Translation> m_PositionType;

            [ReadOnly]
            public ArchetypeChunkComponentType<BoxIndex> m_BoxIndexType;

            public EntityCommandBuffer.Concurrent m_EntityCommandBuffer;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var pointEntities = chunk.GetNativeArray(m_PointEntityType);
                var positionComponents = chunk.GetNativeArray<Translation>(m_PositionType);
                var boxIndexComponents = chunk.GetNativeArray<BoxIndex>(m_BoxIndexType);

                for (int p = 0; p < pointEntities.Length; p++)
                {
                    var pointEntity = pointEntities[p];
                    var position = positionComponents[p].Value;

                    Entity newBoundingBox = Entity.Null;
                    foreach (var boundingBoxEntity in m_BoundingBoxEnumerable)
                    {
                        var bb = m_BoundingBoxes[boundingBoxEntity];

                        // if position is in the box
                        float3 center = bb.center;
                        float3 size = bb.size;
                        float3 d = math.abs(position - center);
                        if (d.x < size.x
                            && d.y < size.y
                            && d.z < size.z)
                        {
                            newBoundingBox = boundingBoxEntity;
                            break;
                        }
                    }

                    var bi = m_PointBoxIndex[pointEntity];
                    if (newBoundingBox != Entity.Null && bi.prevBoundingBox != newBoundingBox)
                    {
                        //Debug.Log ("box trigger from " + bi.prevIndex + " to " + newIndex + " position: " + m_PointGroup.Position[p].Value + " boxposition " + m_BBGroup.BoundingBox[newIndex].center + " " + m_BBGroup.BoundingBox[newIndex].size);
                        bi.prevBoundingBox = bi.currBoundingBox;
                        bi.currBoundingBox = newBoundingBox;
                        m_EntityCommandBuffer.SetComponent(chunkIndex, pointEntity, bi);

                        // add trigger component
                        m_EntityCommandBuffer.AddComponent(chunkIndex, newBoundingBox, new TriggerCondition());
                    }
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            m_BoundingBoxEnumerable.Setup(EntityManager, m_BBGroup, Allocator.Persistent);

            var job = new TriggerJob
            {
                m_BoundingBoxEnumerable = m_BoundingBoxEnumerable,
                m_PointPosition = GetComponentDataFromEntity<Translation>(),
                m_PointBoxIndex = GetComponentDataFromEntity<BoxIndex>(),
                m_BoundingBoxes = GetComponentDataFromEntity<BoundingBox>(),
                m_PointEntityType = GetArchetypeChunkEntityType(),
                m_PositionType = GetArchetypeChunkComponentType<Translation>(true),
                m_BoxIndexType = GetArchetypeChunkComponentType<BoxIndex>(),
                m_EntityCommandBuffer = m_Barrier.CreateCommandBuffer().ToConcurrent()
            };

            var j = job.Schedule(m_PointGroup, inputDeps);
            m_Barrier.AddJobHandleForProducer(j);
            return j;
        }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Barrier = World.GetOrCreateManager<BoxTriggerBarrier>();

            m_BBGroup = GetComponentGroup(
                new EntityArchetypeQuery
                {
                    All = new ComponentType[] { typeof(BoundingBox) },
                    None = new ComponentType[] { typeof(TriggerCondition) },
                    Any = Array.Empty<ComponentType>(),
                });

            m_PointGroup = GetComponentGroup(
                new EntityArchetypeQuery
                {
                    All = new ComponentType[] { typeof(Translation), typeof(BoxIndex) },
                    None = Array.Empty<ComponentType>(),
                    Any = Array.Empty<ComponentType>(),
                });

            m_BoundingBoxEnumerable = new ChunkEntityEnumerable(EntityManager, m_BBGroup, Allocator.TempJob);
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            m_BoundingBoxEnumerable.Dispose();
        }
    }
}
