using System;
using Unity.Entities;
using UnityEngine;
using Unity.Experimental.Audio;
using UnityEngine.Experimental.Audio;
using Unity.Collections;

namespace Unity.Audio.Megacity
{
    struct MusicPlayerSSS : ISystemStateSharedComponentData
    {
        internal ECSoundPlayerNode[] m_MusicNode;
        internal NativeArray<DSPConnection> m_MusicGain;

        internal void OnEnable(AudioManagerSystem audioManager, DSPCommandBlockInterceptor block, MusicPlayer musicPlayerData)
        {
            int count = musicPlayerData.m_Layers.Length;

            // setup and start all audioclips at the same time, with volume 0
            m_MusicNode = new ECSoundPlayerNode[count];
            m_MusicGain = new NativeArray<DSPConnection>(count, Allocator.Persistent);
            for (int i = 0; i < count; i++)
            {
                m_MusicNode[i] = ECSoundPlayerNode.Create(block, musicPlayerData.m_Layers[i].m_AudioClip.CreateAudioSampleProvider(0, 0, true));
                DSPCommandBlockInterceptor.SetNodeName(m_MusicNode[i].node, musicPlayerData.m_Layers[i].m_AudioClip.name, DSPCommandBlockInterceptor.Group.Music);
                m_MusicGain[i] = block.Connect(m_MusicNode[i].node, 0, audioManager.MasterChannel, 0);
                block.SetAttenuation(m_MusicGain[i], 0.0f);
            }
        }

        internal void OnDisable(DSPCommandBlockInterceptor block)
        {
            for (int i = 0; i < m_MusicNode.Length; i++)
            {
                m_MusicNode[i].Dispose(block);
            }
            m_MusicGain.Dispose();
        }
    }


    struct MusicPlayerSS : ISystemStateComponentData
    {
        public int m_ActiveTrack;

        public int m_PrevTrack; // previous audio clip index, to avoid playing 2 of the same

        public int m_Track1;
        public int m_Track2;

        public float m_Volume1; // always 0->1

        public int m_SetNextTrack; // next track index to transition to, when possible (cross fade ends), or -1 if none

        public float m_MusicVolume;
        public float m_CrossfadeTime;

        internal void OnEnable(AudioManagerSystem audioManager, DSPCommandBlockInterceptor block, MusicPlayer musicPlayerData)
        {
            m_Track1 = -1;
            m_Track2 = -1;
            m_Volume1 = 0;

            m_ActiveTrack = 1;
            m_PrevTrack = -1;
            m_SetNextTrack = -1;

            m_MusicVolume = musicPlayerData.m_MusicVolume;
            m_CrossfadeTime = musicPlayerData.m_CrossfadeTime;
        }

        internal void OnUpdate(DSPCommandBlockInterceptor block, MusicPlayerSSS sss, MusicPlayer musicPlayerData)
        {
            if (m_SetNextTrack >= sss.m_MusicNode.Length)
            {
                m_SetNextTrack = -2;
            }

            // interpolate and set active track
            if (m_SetNextTrack != -1 && m_PrevTrack != m_SetNextTrack && (m_Volume1 == 1 || m_Track1 < 0))
            {
                m_Track2 = m_Track1;
                m_Track1 = m_SetNextTrack;

                m_Volume1 = 0;

                if (m_Track1 >= 0)
                {
                    // fade in track1
                    block.SetAttenuation(sss.m_MusicGain[m_Track1], m_MusicVolume * musicPlayerData.m_Layers[m_Track1].m_Volume, (uint)(m_CrossfadeTime * 44100));
                }
                if (m_Track2 >= 0)
                {
                    // fade out track2
                    block.SetAttenuation(sss.m_MusicGain[m_Track2], 0.0f, (uint)(m_CrossfadeTime * 44100));
                }

                m_PrevTrack = m_SetNextTrack;
                m_SetNextTrack = -1;
            }

            // mix towards m_ActiveTrack
            if (m_Track1 >= 0 || m_Track2 >= 0)
            {
                m_Volume1 += Time.deltaTime / m_CrossfadeTime; // transition over m_CrossfadeTime seconds
                m_Volume1 = Mathf.Clamp(m_Volume1, 0.0f, 1.0f);
            }
        }
    }

    [System.Serializable]
    public class MusicLayer
    {
        public AudioClip m_AudioClip;
        public float m_Volume;
    }

    [Serializable]
    public struct MusicPlayer : ISharedComponentData
    {
        public MusicLayer[] m_Layers;

        [Range(0, 1)]
        public float m_MusicVolume;

        [Range(1, 5)]
        public float m_CrossfadeTime;
    }

    public class MusicPlayerComponent : SharedComponentDataProxy<MusicPlayer>
    {
    }
}
