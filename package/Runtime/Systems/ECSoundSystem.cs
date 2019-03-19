using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Experimental.Audio;
using UnityEngine;
using UnityEngine.Experimental.Audio;
using UnityEngine.Profiling;

namespace Unity.Audio.Megacity
{
    [Unity.Burst.BurstCompile]
    [UpdateInGroup(typeof(AudioFrame)), UpdateAfter(typeof(ECSoundFieldMixSystem)), UpdateAfter(typeof(VehicleSoundControllerSystem))]
    class ECSoundSystem : ComponentSystem
    {
        ComponentGroup m_SoundPlayerEntityGroup;

        AudioManagerSystem m_AudioManager;

        NativeArray<Entity> m_SoundFieldPlayerEntities;

        ECSoundPlayerNode[] m_SoundPlayerNodes;
        StateVariableFilter[] m_PlayerLPF;

        public void AddFieldPlayers(AudioClip[] allClips)
        {
            var block = DSPCommandBlockInterceptor.CreateCommandBlock(m_AudioManager.WorldGraph);

            m_SoundPlayerNodes = new ECSoundPlayerNode[allClips.Length];

            m_PlayerLPF = new StateVariableFilter[allClips.Length];

            var soundPlayerEntities = new NativeArray<Entity>(allClips.Length, Allocator.Persistent);

            try
            {
                var rootNode = m_AudioManager.MasterChannel;

                for (int n = 0; n < allClips.Length; n++)
                {
                    var entity = EntityManager.CreateEntity();
                    var player = new ECSoundPlayer();
                    EntityManager.AddComponentData(entity, player);
                    EntityManager.AddComponentData(entity, new ECSoundFieldFinalMix());

                    m_SoundPlayerNodes[n] = ECSoundPlayerNode.Create(block, allClips[n].CreateAudioSampleProvider(0, 0, true));
                    player.m_Source = m_SoundPlayerNodes[n].node;

                    m_PlayerLPF[n] = StateVariableFilter.Create(block);
                    player.m_LPF = m_PlayerLPF[n].node;
                    block.AddInletPort(player.m_LPF, 2, SoundFormat.Stereo);
                    block.AddOutletPort(player.m_LPF, 2, SoundFormat.Stereo);

                    DSPCommandBlockInterceptor.SetNodeName(player.m_Source, allClips[n].name, DSPCommandBlockInterceptor.Group.Ambience);
                    DSPCommandBlockInterceptor.SetNodeName(player.m_LPF, "Lowpass filter", DSPCommandBlockInterceptor.Group.Ambience);

                    player.m_SoundPlayerIndex = n;

                    player.m_DirectMixConnection = block.Connect(player.m_Source, 0, rootNode, 0);
                    var lpfConnection = block.Connect(player.m_Source, 0, player.m_LPF, 0);
                    player.m_DirectLPFConnection = block.Connect(player.m_LPF, 0, rootNode, 0);

                    // A lot of sources start at full level (1.0), resulting in a mix that clips. So
                    // explicitly ease in from 0.0, since the default connection attenuation is 1.0.
                    block.SetAttenuation(player.m_DirectMixConnection, 0.0F);
                    block.SetAttenuation(player.m_DirectLPFConnection, 0.0F);

                    EntityManager.SetComponentData(entity, player);

                    soundPlayerEntities[n] = entity;
                }

                m_SoundFieldPlayerEntities = soundPlayerEntities;
            }
            finally
            {
                block.Complete();
            }
        }

        protected override void OnUpdate()
        {
            if (!m_AudioManager.AudioEnabled)
                return;

            var block = m_AudioManager.FrameBlock;

            Profiler.BeginSample("ECSoundSystem Apply Mix -- Chunk iteration");
            var finalMixFromEntity = GetComponentDataFromEntity<ECSoundFieldFinalMix>(true); // This has to happen here and doesn't work when used as a member variable initialized in OnCreateManager.
            var playerFromEntity = GetComponentDataFromEntity<ECSoundPlayer>(true);
            using (var entityIterable = new ChunkEntityEnumerable(EntityManager, m_SoundPlayerEntityGroup, Allocator.TempJob))
            {
                foreach (var entity in entityIterable)
                {
                    var mix = finalMixFromEntity[entity];
                    var player = playerFromEntity[entity];
                    Profiler.BeginSample("ECSoundSystem Apply Mix -- DSP Apply");
                    ApplyConnectionAttenuation(block, ref player, ref mix, 1024);
                    Profiler.EndSample();
                }
            }
            Profiler.EndSample();
        }

        protected override void OnCreateManager()
        {
            m_AudioManager = World.GetOrCreateManager<AudioManagerSystem>();

            m_SoundPlayerEntityGroup = GetComponentGroup(
                new EntityArchetypeQuery
                {
                    All = new ComponentType[] { typeof(ECSoundFieldFinalMix) },
                    None = Array.Empty<ComponentType>(),
                    Any = Array.Empty<ComponentType>(),
                });
        }

        protected override void OnDestroyManager()
        {
            var block = DSPCommandBlockInterceptor.CreateCommandBlock(m_AudioManager.WorldGraph);

            try
            {
                // Make sure no DSP graphs are running at this point that consume data from this array.
                var playerFromEntity = GetComponentDataFromEntity<ECSoundPlayer>(true);
                for (int n = m_SoundFieldPlayerEntities.Length - 1; n >= 0; n--)
                {
                    var player = playerFromEntity[m_SoundFieldPlayerEntities[n]];
                    m_PlayerLPF[n].Dispose(block);
                    m_SoundPlayerNodes[n].Dispose(block);
                }
            }
            finally
            {
                block.Complete();
                if (m_SoundFieldPlayerEntities.IsCreated)
                    m_SoundFieldPlayerEntities.Dispose();
            }
        }

        void ApplyConnectionAttenuation(DSPCommandBlockInterceptor block, ref ECSoundPlayer player, ref ECSoundFieldFinalMix mix, uint lerpSamples)
        {
            block.SetAttenuation(player.m_DirectMixConnection, mix.data.directL, mix.data.directR, lerpSamples);
            block.SetAttenuation(player.m_DirectLPFConnection, mix.data.directL_LPF, mix.data.directR_LPF, lerpSamples);
        }
    }
}
