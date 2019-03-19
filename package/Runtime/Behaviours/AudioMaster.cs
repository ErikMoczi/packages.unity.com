using Unity.Entities;
using UnityEngine;

namespace Unity.Audio.Megacity
{
    public class AudioMaster : MonoBehaviour
    {
#pragma warning disable 0649
        public AudioMasterParameters Parameters;
#pragma warning restore 0649

        public bool AutoFadeIn = false;

        AudioManagerSystem m_AudioManger;
        bool m_GameStarted;

        public void GameStarted()
        {
            m_GameStarted = true;
            m_AudioManger.SetActive(true);
        }


        void OnEnable()
        {
            m_AudioManger = World.Active.GetOrCreateManager<AudioManagerSystem>();
            m_AudioManger.AudioEnabled = true;

            if(m_GameStarted || AutoFadeIn)
                m_AudioManger.SetActive(true);
        }

        void Update()
        {
            m_AudioManger.SetParameters(Parameters);
        }

        void OnDisable()
        {
            // These extra checks are needed to guard against MonoBehaviour destruction order that would otherwise cause errors in scenes like _SoundObjects
            if (World.Active != null && World.Active.IsCreated)
            {
                var system = World.Active.GetOrCreateManager<AudioManagerSystem>();
                if (system != null)
                    system.AudioEnabled = false;

                m_AudioManger.SetActive(false);
            }
        }
    }
}
