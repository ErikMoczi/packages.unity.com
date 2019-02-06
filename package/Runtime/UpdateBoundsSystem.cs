using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Jobs;
using Unity.Transforms;
using UnityEngine.Experimental.U2D.Common;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.U2D.Animation
{
    [UnityEngine.ExecuteInEditMode]
    [UpdateAfter(typeof(DeformSpriteSystem))]
    public class UpdateBoundsSystem : JobComponentSystem
    {
        ComponentGroup m_ComponentGroup;

        protected override void OnCreateManager()
        {
            m_ComponentGroup = GetComponentGroup(typeof(SpriteSkin), typeof(SpriteComponent));
        }

        struct Bounds
        {
            public float4 center;
            public float4 extents;
        }

        [BurstCompile]
        struct CalculateBoundsJob : IJobParallelFor
        {
            [ReadOnly, DeallocateOnJobCompletion]
            public NativeArray<float4x4> worldToLocalArray;
            [ReadOnly, DeallocateOnJobCompletion]
            public NativeArray<float4x4> rootLocalToWorldArray;
            public NativeArray<Bounds> boundsArray;
            public void Execute(int i)
            {
                var matrix = math.mul(worldToLocalArray[i], rootLocalToWorldArray[i]);
                var center = boundsArray[i].center;
                var extents = boundsArray[i].extents;
                var p0 = math.mul(matrix, center + new float4(-extents.x, -extents.y, extents.z, extents.w));
                var p1 = math.mul(matrix, center + new float4(-extents.x, extents.y, extents.z, extents.w));
                var p2 = math.mul(matrix, center + extents);
                var p3 = math.mul(matrix, center + new float4(extents.x, -extents.y, extents.z, extents.w));
                var min = math.min(p0, math.min(p1, math.min(p2, p3)));
                var max = math.max(p0, math.max(p1, math.max(p2, p3)));
                extents = (max - min) * 0.5f;
                center = min + extents;
                boundsArray[i] = new Bounds()
                {
                    center = center,
                    extents = extents
                };
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var spriteSkinComponents = m_ComponentGroup.GetComponentArray<SpriteSkin>();
            var spriteComponents = m_ComponentGroup.GetSharedComponentDataArray<SpriteComponent>();
            var worldToLocalArray = new NativeArray<float4x4>(spriteSkinComponents.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var rootLocalToWorldArray = new NativeArray<float4x4>(spriteSkinComponents.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var boundsArray = new NativeArray<Bounds>(spriteSkinComponents.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            for (var i = 0; i < spriteSkinComponents.Length; ++i)
            {
                var spriteSkin = spriteSkinComponents[i];
                var sprite = spriteComponents[i].Value;

                if (spriteSkin == null || sprite == null)
                    continue;
                
                worldToLocalArray[i] = spriteSkin.transform.worldToLocalMatrix;
                rootLocalToWorldArray[i] = spriteSkin.rootBone.localToWorldMatrix;

                var unityBounds = spriteSkin.bounds;
                boundsArray[i] = new Bounds ()
                {
                    center = new float4(unityBounds.center, 1),
                    extents = new float4(unityBounds.extents, 0),
                };
            }

            var jobHandle = new CalculateBoundsJob()
            {
                worldToLocalArray = worldToLocalArray,
                rootLocalToWorldArray = rootLocalToWorldArray,
                boundsArray = boundsArray
            }.Schedule(spriteSkinComponents.Length, 32);
            
            jobHandle.Complete();

            for (var i = 0; i < spriteSkinComponents.Length; ++i)
            {
                var spriteSkin = spriteSkinComponents[i];
                var sprite = spriteComponents[i].Value;

                if (spriteSkin == null || sprite == null)
                    continue;
                
                var center = boundsArray[i].center;
                var extents = boundsArray[i].extents;
                var bounds = new UnityEngine.Bounds();
                bounds.center = new Vector3(center.x, center.y, center.z);
                bounds.extents = new Vector3(extents.x, extents.y, extents.z);
                InternalEngineBridge.SetLocalAABB(spriteSkin.spriteRenderer, bounds);
            }

            boundsArray.Dispose();

            return inputDeps;
        }
    }
}
