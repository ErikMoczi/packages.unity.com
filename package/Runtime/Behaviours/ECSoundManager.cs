using Unity.Entities;
using UnityEngine;

namespace Unity.Audio.Megacity
{
    enum NoProvs
    {
    }

    public class ECSoundManager : MonoBehaviour
    {
        EntityManager m_EntityManager;

        public ECSoundEmitterDefinitionAsset[] m_SoundDefinitions;

        public AudioClip[] m_Clips;

        ECSoundFieldMixSystem m_MixSystem;
        ECSoundSystem m_SoundSystem;
        AudioManagerSystem m_Manager;
        World m_World;

        public void OnEnable()
        {
            m_World = World.Active;

            m_EntityManager = m_World.GetOrCreateManager<EntityManager>();

            for (int i = 0; i < m_SoundDefinitions.Length; i++)
                m_SoundDefinitions[i].Reflect(m_EntityManager);

            m_Manager = m_World.GetOrCreateManager<AudioManagerSystem>();

            var block = DSPCommandBlockInterceptor.CreateCommandBlock(m_Manager.WorldGraph);

            try
            {
                m_SoundSystem = m_World.GetOrCreateManager<ECSoundSystem>();
                m_SoundSystem.AddFieldPlayers(block, m_Clips);

                m_MixSystem = m_World.GetOrCreateManager<ECSoundFieldMixSystem>();
                m_MixSystem.AddFieldPlayers(m_Clips.Length);
            }
            finally
            {
                block.Complete();
            }
        }

        public void OnDisable()
        {
            if(m_World.IsCreated)
                m_SoundSystem.CleanupFieldPlayers(m_Manager.FrameBlock);
        }

        public void Update()
        {
            var entityManager = m_World.GetExistingManager<EntityManager>();
            foreach (var def in m_SoundDefinitions)
                def.Reflect(entityManager);
        }
    }
}
