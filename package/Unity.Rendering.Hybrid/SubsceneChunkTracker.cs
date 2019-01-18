using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities.Serialization;
using Unity.Jobs;

namespace Unity.Entities
{
    public struct SubsceneLoadedTag : IComponentData
    {
    }

    public class SubsceneChunkTracker : JobComponentSystem
    {
        public Dictionary<SubsceneTag, NativeArray<ArchetypeChunk>> Chunks;

        protected override void OnCreateManager()
        {
            Chunks = new Dictionary<SubsceneTag, NativeArray<ArchetypeChunk>>();
        }

        protected override void OnDestroyManager()
        {
            foreach(var item in Chunks)
            {
                item.Value.Dispose();
            }
            Chunks.Clear();
            
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return inputDeps;
        }
    }
}
