using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.Experimental.U2D.Animation
{
    [ExecuteInEditMode]
    [UpdateAfter(typeof(PrepareSkinningSystem))]
    class DeformSpriteSystem : JobComponentSystem
    {
        List<SkinJob> m_Jobs = new List<SkinJob>(16);
        List<SpriteComponent> m_UniqueSpriteComponents = new List<SpriteComponent>(16);
        ComponentGroup m_ComponentGroup;

        protected override void OnCreateManager()
        {
            m_ComponentGroup = GetComponentGroup(typeof(WorldToLocal), typeof(SpriteComponent), typeof(Vertex), typeof(BoneTransform));
        }

        [BurstCompile]
        public struct SkinJob : IJobParallelFor
        {
            // these are readonly per sprite and shared
            [ReadOnly]
            public EntityArray entities;
            [ReadOnly]
            public NativeSlice<float3> vertices;
            [ReadOnly]
            public NativeSlice<BoneWeight> boneWeights;
            [ReadOnly]
            public NativeSlice<float4x4> bindPoses;
            [ReadOnly]
            public ComponentDataArray<WorldToLocal> localToWorldArray;

            // these are calculated per renderer and per instance
            [NativeDisableParallelForRestriction]
            public BufferFromEntity<BoneTransform> boneTransformArray;
            [NativeDisableParallelForRestriction]
            public BufferFromEntity<Vertex> deformedArray;

            public void Execute(int i)
            {
                var rootInv = localToWorldArray[i].Value;
                var boneTransforms = boneTransformArray[entities[i]].Reinterpret<float4x4>().ToNativeArray();
                var deformableVertices = deformedArray[entities[i]].Reinterpret<float3>().ToNativeArray();
                SpriteSkinUtility.Deform(rootInv, vertices, boneWeights, boneTransforms, bindPoses, deformableVertices);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            m_Jobs.Clear();
            m_UniqueSpriteComponents.Clear();
            EntityManager.GetAllUniqueSharedComponentData(m_UniqueSpriteComponents);

            var spriteComponentCount = m_UniqueSpriteComponents.Count;
            var entitiesPerSprite = new NativeArray<int>(spriteComponentCount, Allocator.Temp);

            m_Jobs.Capacity = spriteComponentCount;
            
            for (var i = 0; i < spriteComponentCount; i++)
            {
                var spriteComponent = m_UniqueSpriteComponents[i];
                var sprite = spriteComponent.Value;
                var job = default(SkinJob);
                var entityCount = 0;

                if (sprite != null)
                {
                    m_ComponentGroup.SetFilter(spriteComponent);
                    var filteredEntities = m_ComponentGroup.GetEntityArray();

                    entityCount = filteredEntities.Length;
                    job = new SkinJob
                    {
                        entities = filteredEntities,
                        vertices = sprite.GetVertexAttribute<Vector3>(UnityEngine.Rendering.VertexAttribute.Position).SliceWithStride<float3>(),
                        boneWeights = sprite.GetVertexAttribute<BoneWeight>(UnityEngine.Rendering.VertexAttribute.BlendWeight),
                        bindPoses = new NativeSlice<Matrix4x4>(sprite.GetBindPoses()).SliceWithStride<float4x4>(),
                        localToWorldArray = m_ComponentGroup.GetComponentDataArray<WorldToLocal>(),
                        boneTransformArray = GetBufferFromEntity<BoneTransform>(),
                        deformedArray = GetBufferFromEntity<Vertex>()
                    };
                }

                m_Jobs.Add(job);
                entitiesPerSprite[i] = entityCount;
            }


            if (m_Jobs.Count > 0)
            {
                var jobHandles = new NativeArray<JobHandle>(entitiesPerSprite.Length, Allocator.Temp);
                var prevHandle = inputDeps;
                
                for (var i = 0; i < entitiesPerSprite.Length; ++i)
                {
                    if (entitiesPerSprite[i] > 0)
                    {
                        jobHandles[i] = m_Jobs[i].Schedule(entitiesPerSprite[i], 4, prevHandle);
                        prevHandle = jobHandles[i];
                    }
                }
                
                var combinedHandle = JobHandle.CombineDependencies(jobHandles);
                jobHandles.Dispose();
                entitiesPerSprite.Dispose();

                return combinedHandle;
            }

            return inputDeps;

        }
    }

}