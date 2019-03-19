using Unity.Entities;
using UnityEngine;

namespace Unity.Audio.Megacity
{
    class AudioMaster : MonoBehaviour
    {
#pragma warning disable 0649
        public AudioMasterParameters Parameters;
#pragma warning restore 0649

        public bool AutoFadeIn = false;

        AudioManagerSystem m_AudioManger;

        void Start()
        {
            m_AudioManger = World.Active.GetOrCreateManager<AudioManagerSystem>();
            m_AudioManger.AudioEnabled = true;

            if(AutoFadeIn)
                m_AudioManger.SetActive(AutoFadeIn);
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
            }
        }
    }
}
