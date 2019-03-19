using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Experimental.Audio;
using UnityEngine.Experimental.Audio;
using Unity.Collections;
using System;
using System.Collections.Generic;

namespace Unity.Audio.Megacity
{
    class SamplePlaybackBarrier : EntityCommandBufferSystem {}

    [UpdateInGroup(typeof(AudioFrame))]
    public class SamplePlaybackSystem : ComponentSystem
    {
        struct State : ISystemStateComponentData
        {
            public ECSoundPlayerNode SamplePlayer;
            public DSPConnection Connection;
        }

        ComponentGroup m_NewSamplesGroup;
        ComponentGroup m_AliveSamplesGroup;
        ComponentGroup m_DeadSamplesGroup;

        NativeArray<Entity> m_AliveSamples;
        NativeArray<Entity> m_DeadSamples;

        SamplePlaybackBarrier m_Barrier;
        AudioManagerSystem m_AudioManager;

        protected override void OnUpdate()
        {
            if (!m_AudioManager.AudioEnabled)
                return;

            var block = m_AudioManager.FrameBlock;

            // TODO: Query from m_WorldGraph
            const uint dspBufferSize = 1024;

            UpdateEntities(block, PostUpdateCommands, dspBufferSize);
        }

        protected override void OnCreateManager()
        {
            m_Barrier = World.GetOrCreateManager<SamplePlaybackBarrier>();
            m_AudioManager = World.GetOrCreateManager<AudioManagerSystem>();

            m_NewSamplesGroup = GetComponentGroup(
                new EntityArchetypeQuery
                {
                    All = new ComponentType[] { ComponentType.ReadOnly(typeof(SharedAudioClip)), typeof(SamplePlayback) },
                    None = new ComponentType[] { typeof(State) },
                    Any = Array.Empty<ComponentType>()
                });

            m_AliveSamplesGroup = GetComponentGroup(
                new EntityArchetypeQuery
                {
                    All = new ComponentType[] { typeof(SamplePlayback), ComponentType.ReadOnly(typeof(State)) },
                    None = Array.Empty<ComponentType>(),
                    Any = Array.Empty<ComponentType>()
                });

            m_DeadSamplesGroup = GetComponentGroup(
                new EntityArchetypeQuery
                {
                    All = new ComponentType[] { typeof(State) },
                    None = new ComponentType[] { ComponentType.ReadOnly(typeof(SamplePlayback)) },
                    Any = Array.Empty<ComponentType>()
                });
        }

        protected override void OnDestroyManager()
        {
            var block = DSPCommandBlockInterceptor.CreateCommandBlock(m_AudioManager.WorldGraph);

            // TODO: Hack to correctly cleanup incoherent system states in ECS
            try
            {
                var entityToState = GetComponentDataFromEntity<State>(false);

                ChunkUtils.ReallocateEntitiesFromChunkArray(ref m_AliveSamples, EntityManager, m_AliveSamplesGroup, Allocator.Persistent);
                for (int i = 0; i < m_AliveSamples.Length; ++i)
                {
                    DestroySamplePlayback(block, entityToState[m_AliveSamples[i]]);
                }

                ChunkUtils.ReallocateEntitiesFromChunkArray(ref m_DeadSamples, EntityManager, m_DeadSamplesGroup, Allocator.Persistent);
                for (int i = 0; i < m_DeadSamples.Length; ++i)
                {
                    DestroySamplePlayback(block, entityToState[m_DeadSamples[i]]);
                }
            }
            finally
            {
                block.Complete();
            }

            if (m_AliveSamples.IsCreated)
                m_AliveSamples.Dispose();

            if (m_DeadSamples.IsCreated)
                m_DeadSamples.Dispose();
        }

        Dictionary<int, AudioClip> m_AudioClips = new Dictionary<int, AudioClip>();

        public void AddClip(AudioClip clip)
        {
            int instanceID = clip.GetInstanceID();
            if (!m_AudioClips.ContainsKey(instanceID))
                m_AudioClips[instanceID] = clip;
        }

        void UpdateEntities(DSPCommandBlockInterceptor block, EntityCommandBuffer ecb, uint dspBufferSize)
        {
            var sampleToState = GetComponentDataFromEntity<State>(false);
            var sampleToPlayback = GetComponentDataFromEntity<SamplePlayback>(false);
            var sampleToClip = GetComponentDataFromEntity<SharedAudioClip>(false);

            using (var newSamplesEnumerable = new ChunkEntityEnumerable(EntityManager, m_NewSamplesGroup, Allocator.TempJob))
            {
                foreach (var sample in newSamplesEnumerable)
                {
                    if (!sampleToClip.Exists(sample) || !sampleToPlayback.Exists(sample))
                        continue;

                    int instanceID = sampleToClip[sample].ClipInstanceID;

                    if (!m_AudioClips.ContainsKey(instanceID))
                    {
                        Debug.LogError("Cannot find AudioClip for instance ID " + instanceID);
                        continue;
                    }

                    var clip = m_AudioClips[instanceID];
                    var playback = CreateSamplePlayback(block, sampleToPlayback[sample], clip, dspBufferSize);
                    ecb.AddComponent(sample, playback);
                }
            }

            using (var aliveSamplesEnumerable = new ChunkEntityEnumerable(EntityManager, m_AliveSamplesGroup, Allocator.TempJob))
            {
                foreach (var sample in aliveSamplesEnumerable)
                {
                    if (!sampleToState.Exists(sample) || !sampleToPlayback.Exists(sample))
                        continue;

                    var p = sampleToPlayback[sample];
                    var state = sampleToState[sample];
                    block.SetAttenuation(state.Connection, p.Volume * p.Left, p.Volume * p.Right, dspBufferSize);
                }
            }

            using (var deadSamplesEnumerable = new ChunkEntityEnumerable(EntityManager, m_DeadSamplesGroup, Allocator.TempJob))
            {
                foreach (var sample in deadSamplesEnumerable)
                {
                    if (!sampleToState.Exists(sample))
                        continue;

                    var state = sampleToState[sample];
                    DestroySamplePlayback(block, state);
                    ecb.RemoveComponent(sample, ComponentType.ReadWrite<State>());
                }
            }
        }

        State CreateSamplePlayback(DSPCommandBlockInterceptor block, SamplePlayback playback, AudioClip clip, uint dspBufferSize)
        {
            var s = new State
            {
                SamplePlayer = ECSoundPlayerNode.Create(block, clip.CreateAudioSampleProvider(0, 0, playback.Loop != 0)),
            };

            DSPCommandBlockInterceptor.SetNodeName(s.SamplePlayer.node, clip.name, DSPCommandBlockInterceptor.Group.Events);

            s.Connection = block.Connect(s.SamplePlayer.node, 0, m_AudioManager.MasterChannel, 0);

            // update initial parameters
            s.SamplePlayer.Update(block, playback.Pitch);
            block.SetAttenuation(s.Connection, playback.Volume * playback.Left, playback.Volume * playback.Right, dspBufferSize);
            return s;
        }

        void DestroySamplePlayback(DSPCommandBlockInterceptor block, State state)
        {
            state.SamplePlayer.Dispose(block);
        }
    }
}
