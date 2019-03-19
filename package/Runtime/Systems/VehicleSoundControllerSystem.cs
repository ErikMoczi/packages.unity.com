using Unity.Entities;
using Unity.Collections;
using System;

namespace Unity.Audio.Megacity
{
    [UpdateInGroup(typeof(AudioFrame))]
    class VehicleSoundControllerSystem : ComponentSystem
    {
        ComponentGroup m_UpdateData;
        ComponentGroup m_EnableData;
        ComponentGroup m_DisableData;

        AudioManagerSystem m_AudioManager;

        protected override void OnUpdate()
        {
            if (!m_AudioManager.AudioEnabled)
                return;

            var block = m_AudioManager.FrameBlock;

            using (var enableEntitiesEnumerable = new ChunkEntityEnumerable(EntityManager, m_EnableData, Allocator.TempJob))
            {
                var enableDataControllerSS = GetComponentDataFromEntity<VehicleSoundControllerSS>();
                var enableDataControllerComponentType = GetArchetypeChunkSharedComponentType<VehicleSoundController>();
                ChunkEntityEnumerable.ChunkEntityEnumerator it = enableEntitiesEnumerable.GetEnumerator();
                while(it.MoveNext())
                {
                    var enableDataController = it.GetCurrentSharedData(enableDataControllerComponentType, EntityManager);
                    var sss = new VehicleSoundControllerSSS();
                    sss.OnEnable(enableDataController, m_AudioManager, block);
                    var ss = new VehicleSoundControllerSS();
                    ss.OnEnable(enableDataController, m_AudioManager, block);
                    PostUpdateCommands.AddSharedComponent(it.Current, sss);
                    PostUpdateCommands.AddComponent(it.Current, ss);
                }
            }

            using (var updateEntitiesEnumerable = new ChunkEntityEnumerable(EntityManager, m_UpdateData, Allocator.TempJob))
            {
                var updateDataControllerSS = GetComponentDataFromEntity<VehicleSoundControllerSS>();
                var updateDataControllerSSSComponentType = GetArchetypeChunkSharedComponentType<VehicleSoundControllerSSS>();
                var updateDataControllerComponentType = GetArchetypeChunkSharedComponentType<VehicleSoundController>();
                ChunkEntityEnumerable.ChunkEntityEnumerator it = updateEntitiesEnumerable.GetEnumerator();
                while (it.MoveNext())
                {
                    var updateDataControllerSSS = it.GetCurrentSharedData(updateDataControllerSSSComponentType, EntityManager);
                    var updateDataController = it.GetCurrentSharedData(updateDataControllerComponentType, EntityManager);
                    var copy = updateDataControllerSS[it.Current];
                    copy.OnUpdate(updateDataController, m_AudioManager, block, updateDataControllerSSS);
                    PostUpdateCommands.SetComponent(it.Current, copy);
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
                    var updateDataControllerSSSComponentType = GetArchetypeChunkSharedComponentType<VehicleSoundControllerSSS>();
                    var updateDataControllerComponentType = GetArchetypeChunkSharedComponentType<VehicleSoundController>();
                    ChunkEntityEnumerable.ChunkEntityEnumerator it = updateEntitiesEnumerable.GetEnumerator();
                    while (it.MoveNext())
                    {
                        var updateDataControllerSSS = it.GetCurrentSharedData(updateDataControllerSSSComponentType, EntityManager);
                        updateDataControllerSSS.OnDisable(block);
                    }
                }
                return;
            }
            if (m_DisableData.CalculateLength() > 0)
            {
                using (var disableEntitiesEnumerable = new ChunkEntityEnumerable(EntityManager, m_DisableData, Allocator.TempJob))
                {
                    var disableDataControllerSSSComponentType = GetArchetypeChunkSharedComponentType<VehicleSoundControllerSSS>();
                    var disableDataControllerComponentType = GetArchetypeChunkSharedComponentType<VehicleSoundController>();
                    ChunkEntityEnumerable.ChunkEntityEnumerator it = disableEntitiesEnumerable.GetEnumerator();
                    while (it.MoveNext())
                    {
                        var disableDataControllerSSS = it.GetCurrentSharedData(disableDataControllerSSSComponentType, EntityManager);
                        disableDataControllerSSS.OnDisable(block);

                        if (!processUpdateData) // Argh
                        {
                            PostUpdateCommands.RemoveComponent<VehicleSoundControllerSSS>(it.Current);
                            PostUpdateCommands.RemoveComponent<VehicleSoundControllerSS>(it.Current);
                        }
                    }
                }
            }
        }

        protected override void OnCreateManager()
        {
            m_AudioManager = World.GetOrCreateManager<AudioManagerSystem>();

            m_UpdateData = GetComponentGroup(
                new EntityArchetypeQuery
                {
                    All = new ComponentType[] { typeof(VehicleSoundController), typeof(VehicleSoundControllerSS), typeof(VehicleSoundControllerSSS) },
                    None = Array.Empty<ComponentType>(),
                    Any = Array.Empty<ComponentType>()
                });

            m_EnableData = GetComponentGroup(
                new EntityArchetypeQuery
                {
                    All = new ComponentType[] { typeof(VehicleSoundController) },
                    None = new ComponentType[] { typeof(VehicleSoundControllerSS) },
                    Any = Array.Empty<ComponentType>()
                });

            m_DisableData = GetComponentGroup(
                new EntityArchetypeQuery
                {
                    All = new ComponentType[] { typeof(VehicleSoundControllerSSS) },
                    None = new ComponentType[] { typeof(VehicleSoundController) },
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
