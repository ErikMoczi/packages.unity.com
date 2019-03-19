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
        World m_World;

        public void OnEnable()
        {
            m_World = World.Active;

            m_EntityManager = m_World.GetOrCreateManager<EntityManager>();

            for (int i = 0; i < m_SoundDefinitions.Length; i++)
                m_SoundDefinitions[i].Reflect(m_EntityManager);

            var soundSystem = m_World.GetOrCreateManager<ECSoundSystem>();
            soundSystem.AddFieldPlayers(m_Clips);

            m_MixSystem = m_World.GetOrCreateManager<ECSoundFieldMixSystem>();
            m_MixSystem.AddFieldPlayers(m_Clips.Length);
        }

        public void OnDisable()
        {
        }

        public void Update()
        {
            var entityManager = m_World.GetExistingManager<EntityManager>();
            foreach (var def in m_SoundDefinitions)
                def.Reflect(entityManager);
        }
    }
}
