﻿using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Transforms2D
{
    public class MoveForward2DSystem : JobComponentSystem
    {
        [ComputeJobOptimization]
        struct MoveForwardPosition : IJobProcessComponentData<Position2D, Heading2D, MoveSpeed>
        {
            public float dt;
        
            public void Execute(ref Position2D position, [ReadOnly]ref Heading2D heading, [ReadOnly]ref MoveSpeed moveSpeed)
            {
                position.Value += dt * moveSpeed.speed * heading.Value;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var moveForwardPositionJob = new MoveForwardPosition { dt = Time.deltaTime };
            return moveForwardPositionJob.Schedule(this, 64, inputDeps);
        }
    }
}
