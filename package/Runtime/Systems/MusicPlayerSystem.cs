using Unity.Entities;
using Unity.Collections;
using System;

namespace Unity.Audio.Megacity
{
    [UpdateInGroup(typeof(AudioFrame))]
    class MusicPlayerSystem : ComponentSystem
    {
        ComponentGroup m_TriggerData;
        ComponentGroup m_UpdateData;
        ComponentGroup m_EnableData;
        ComponentGroup m_DisableData;

        AudioManagerSystem m_AudioManager;

        protected override void OnUpdate()
        {
            if (!m_AudioManager.AudioEnabled)
                return;

            var block = m_AudioManager.FrameBlock;

            var musicPlayerComponentType = GetArchetypeChunkSharedComponentType<MusicPlayer>();
            var musicPlayerSSSComponentType = GetArchetypeChunkSharedComponentType<MusicPlayerSSS>();
            var musicTriggerComponentType = GetArchetypeChunkSharedComponentType<MusicTrigger>();

            using (var enableEntitiesEnumerable = new ChunkEntityEnumerable(EntityManager, m_EnableData, Allocator.TempJob))
            {
                ChunkEntityEnumerable.ChunkEntityEnumerator it = enableEntitiesEnumerable.GetEnumerator();
                while (it.MoveNext())
                {
                    var player = it.GetCurrentSharedData(musicPlayerComponentType, EntityManager);
                    var playerSSS = new MusicPlayerSSS();
                    playerSSS.OnEnable(m_AudioManager, block, player);
                    var playerSS = new MusicPlayerSS();
                    playerSS.OnEnable(m_AudioManager, block, player);
                    PostUpdateCommands.AddSharedComponent(it.Current, playerSSS);
                    PostUpdateCommands.AddComponent(it.Current, playerSS);
                }
            }

            var playerSSFromEntity = GetComponentDataFromEntity<MusicPlayerSS>();

            using (var updateEntitiesEnumerable = new ChunkEntityEnumerable(EntityManager, m_UpdateData, Allocator.TempJob))
            using (var triggerEntitiesEnumerable = new ChunkEntityEnumerable(EntityManager, m_TriggerData, Allocator.TempJob))
            {
                ChunkEntityEnumerable.ChunkEntityEnumerator updateIt = updateEntitiesEnumerable.GetEnumerator();
                while (updateIt.MoveNext())
                {
                    var copy = playerSSFromEntity[updateIt.Current];

                    ChunkEntityEnumerable.ChunkEntityEnumerator triggerIt = triggerEntitiesEnumerable.GetEnumerator();
                    while (triggerIt.MoveNext())
                    {
                        var triggerData = triggerIt.GetCurrentSharedData(musicTriggerComponentType, EntityManager);

                        copy.m_SetNextTrack = triggerData.musicIndex;

                        PostUpdateCommands.RemoveComponent<TriggerCondition>(triggerIt.Current);
                    }

                    var updatePlayer = updateIt.GetCurrentSharedData(musicPlayerComponentType, EntityManager);
                    var updatePlayerSSS = updateIt.GetCurrentSharedData(musicPlayerSSSComponentType, EntityManager); 
                    copy.OnUpdate(block, updatePlayerSSS, updatePlayer);
                    PostUpdateCommands.SetComponent(updateIt.Current, copy);
                }
            }

            Cleanup(block);
        }

        void Cleanup(DSPCommandBlockInterceptor block, bool processUpdateData = false)
        {
            if (processUpdateData && m_UpdateData.CalculateLength() > 0)
            {
                using (var updateEntitiesEnumerable = new ChunkEntityEnumerable(EntityManager, m_UpdateData, Allocator.TempJob))
                {
                    var updateSSSComponentType = GetArchetypeChunkSharedComponentType<MusicPlayerSSS>();
                    var updateComponentType = GetArchetypeChunkSharedComponentType<MusicPlayer>();
                    ChunkEntityEnumerable.ChunkEntityEnumerator it = updateEntitiesEnumerable.GetEnumerator();
                    while (it.MoveNext())
                    {
                        var updateSSS = it.GetCurrentSharedData(updateSSSComponentType, EntityManager);
                        updateSSS.OnDisable(block);
                    }
                }
                return;
            }

            if (m_DisableData.CalculateLength() > 0)
            {
                using (var disableEntitiesEnumerable = new ChunkEntityEnumerable(EntityManager, m_DisableData, Allocator.TempJob))
                {
                    var disableSSSComponentType = GetArchetypeChunkSharedComponentType<MusicPlayerSSS>();
                    var disableComponentType = GetArchetypeChunkSharedComponentType<MusicPlayer>();
                    ChunkEntityEnumerable.ChunkEntityEnumerator it = disableEntitiesEnumerable.GetEnumerator();
                    while (it.MoveNext())
                    {
                        var disableSSS = it.GetCurrentSharedData(disableSSSComponentType, EntityManager);
                        disableSSS.OnDisable(block);
                    }
                }
            }
        }

        protected override void OnCreateManager()
        {
            m_AudioManager = World.GetOrCreateManager<AudioManagerSystem>();

            m_TriggerData = GetComponentGroup(
                new EntityArchetypeQuery
                {
                    All = new ComponentType[] { typeof(TriggerCondition), typeof(MusicTrigger) },
                    None = Array.Empty<ComponentType>(),
                    Any = Array.Empty<ComponentType>()
                });

            m_UpdateData = GetComponentGroup(
                new EntityArchetypeQuery
                {
                    All = new ComponentType[] { typeof(MusicPlayer), typeof(MusicPlayerSSS), typeof(MusicPlayerSS) },
                    None = Array.Empty<ComponentType>(),
                    Any = Array.Empty<ComponentType>()
                });

            m_EnableData = GetComponentGroup(
                new EntityArchetypeQuery
                {
                    All = new ComponentType[] { typeof(MusicPlayer) },
                    None = new ComponentType[] { typeof(MusicPlayerSS) },
                    Any = Array.Empty<ComponentType>()
                });

            m_DisableData = GetComponentGroup(
                new EntityArchetypeQuery
                {
                    All = new ComponentType[] { typeof(MusicPlayerSSS) },
                    None = new ComponentType[] { typeof(MusicPlayer) },
                    Any = Array.Empty<ComponentType>()
                });
        }

        protected override void OnDestroyManager()
        {
            var block = DSPCommandBlockInterceptor.CreateCommandBlock(m_AudioManager.WorldGraph);

            try
            {
                Cleanup(block, true);
            }
            finally
            {
                block.Complete();
            }
        }
    }
}
