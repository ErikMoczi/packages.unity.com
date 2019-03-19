using System;
using UnityEngine;
using Unity.Entities;
using Unity.Experimental.Audio;
using Unity.Jobs;

namespace Unity.Audio.Megacity
{
    [UpdateAfter(typeof(AudioBeginFrame)), UpdateBefore(typeof(AudioEndFrame))]
    public class AudioFrame : ComponentSystemGroup { }

    [Serializable]
    public struct AudioMasterParameters
    {
        [Serializable]

        public struct Limiter
        {
            [Range(-60, 20)]
            public float preGainDbs /* = 1.0f */;
            [Range(0, 1000)]
            public float releaseMs/* = 50*/;
            [Range(-60, 0)]
            public float thresholdDBs /* = -1.5f*/;
        }

        public Limiter LimiterParameters;

        [Range(0, 5)]
        public float MasterVolume /* = 1 */;

        public static AudioMasterParameters Defaults()
        {
            AudioMasterParameters ret;

            ret.MasterVolume = 1;

            ret.LimiterParameters.preGainDbs = 1.0f;
            ret.LimiterParameters.releaseMs = 50;
            ret.LimiterParameters.thresholdDBs = -1.5f;

            return ret;
        }
    }

    public class AudioManagerSystem : JobComponentSystem
    {
        public Transform PlayerTransform { get; set; }
        public Transform ListenerTransform { get; set; }

        public bool AudioEnabled
        {
            get { return m_AudioEnabled && PlayerTransform != null && ListenerTransform != null; }
            set { m_AudioEnabled = value; }
        }

        internal DSPGraph WorldGraph => DSPGraph.GetDefaultGraph();
        internal DSPNode MasterChannel => m_LimiterNode;

        internal DSPCommandBlockInterceptor FrameBlock
        {
            get
            {
                if (!m_BlocksAlive)
                    throw new InvalidOperationException("Audio manager frame blocks not available at this time");

                return m_Block;
            }
        }

        DSPConnection m_ConverterConnection;
        DSPNode m_ConverterNode;
        DSPConnection m_MasterConnection;
        DSPNode m_MasterGainNode;
        DSPNode m_LimiterNode;
        bool m_AudioEnabled = false;
        DSPCommandBlockInterceptor m_Block;
        bool m_BlocksAlive = false;

        AudioMasterParameters m_Parameters = AudioMasterParameters.Defaults();

        protected override void OnCreateManager()
        {
            m_Block = DSPCommandBlockInterceptor.CreateCommandBlock(WorldGraph);
            m_BlocksAlive = true;
            m_MasterGainNode = m_Block.CreateDSPNode<GainNodeJob.Params, NoProvs, GainNodeJob>();
            m_Block.AddInletPort(m_MasterGainNode, 2, SoundFormat.Stereo);
            m_Block.AddOutletPort(m_MasterGainNode, 2, SoundFormat.Stereo);
            m_Block.SetFloat<GainNodeJob.Params, NoProvs, GainNodeJob>(m_MasterGainNode, GainNodeJob.Params.Vol1, 0, 0);
            m_Block.SetFloat<GainNodeJob.Params, NoProvs, GainNodeJob>(m_MasterGainNode, GainNodeJob.Params.Vol2, 0, 0);

            if (Application.platform == RuntimePlatform.XboxOne)
            {
                m_ConverterNode = m_Block.CreateDSPNode<StereoTo7Point1NodeJob.Params, NoProvs, StereoTo7Point1NodeJob>();
                m_Block.AddInletPort(m_ConverterNode, 2, SoundFormat.Stereo);
                m_Block.AddOutletPort(m_ConverterNode, 8, SoundFormat.SevenDot1);
                m_ConverterConnection = m_Block.Connect(m_MasterGainNode, 0, m_ConverterNode, 0);
                m_MasterConnection = m_Block.Connect(m_ConverterNode, 0, WorldGraph.GetRootDSP(), 0);
            }
            else
            {
                m_MasterConnection = m_Block.Connect(m_MasterGainNode, 0, WorldGraph.GetRootDSP(), 0);
            }

            m_LimiterNode = m_Block.CreateDSPNode<LimiterNodeJob.Params, LimiterNodeJob.Provs, LimiterNodeJob>();
            m_Block.AddInletPort(m_LimiterNode, 2, SoundFormat.Stereo);
            m_Block.AddOutletPort(m_LimiterNode, 2, SoundFormat.Stereo);
            m_Block.Connect(m_LimiterNode, 0, m_MasterGainNode, 0);

            DSPCommandBlockInterceptor.SetNodeName(m_MasterGainNode, "Master gain", DSPCommandBlockInterceptor.Group.Global);
            DSPCommandBlockInterceptor.SetNodeName(m_LimiterNode, "Limiter", DSPCommandBlockInterceptor.Group.Global);
            DSPCommandBlockInterceptor.SetNodeName(WorldGraph.GetRootDSP(), "Root", DSPCommandBlockInterceptor.Group.Root);

            SwapBuffers();
        }

        public void SetParameters(AudioMasterParameters parameters)
        {
            m_Parameters = parameters;
        }

        internal void SwapBuffers()
        {
            UpdateParameters();
            m_Block.Complete();
            m_Block = DSPCommandBlockInterceptor.CreateCommandBlock(WorldGraph);
        }

        protected override void OnDestroyManager()
        {
            if (m_BlocksAlive)
            {
                if (Application.platform == RuntimePlatform.XboxOne)
                {
                    m_Block.ReleaseDSPNode(m_ConverterNode);
                }

                m_Block.ReleaseDSPNode(m_MasterGainNode);
                m_Block.ReleaseDSPNode(m_LimiterNode);
                m_Block.Complete();
                m_Block = DSPCommandBlockInterceptor.CreateCommandBlock(WorldGraph);
            }

            m_BlocksAlive = false;
        }

        protected override JobHandle OnUpdate(JobHandle deps)
        {
            return deps;
        }

        internal void UpdateParameters(uint lerpLength = 1024)
        {
            if (m_BlocksAlive)
            {
                m_Block.SetFloat<LimiterNodeJob.Params, LimiterNodeJob.Provs, LimiterNodeJob>(
                    m_LimiterNode,
                    LimiterNodeJob.Params.PreGainDBs,
                    m_Parameters.LimiterParameters.preGainDbs,
                    lerpLength
                );

                m_Block.SetFloat<LimiterNodeJob.Params, LimiterNodeJob.Provs, LimiterNodeJob>(
                    m_LimiterNode,
                    LimiterNodeJob.Params.LimitingThresholdDBs,
                    m_Parameters.LimiterParameters.thresholdDBs,
                    lerpLength
                );

                m_Block.SetFloat<LimiterNodeJob.Params, LimiterNodeJob.Provs, LimiterNodeJob>(
                    m_LimiterNode,
                    LimiterNodeJob.Params.ReleaseMs,
                    m_Parameters.LimiterParameters.releaseMs,
                    lerpLength
                );

                m_Block.SetAttenuation(m_MasterConnection, m_Parameters.MasterVolume, m_Parameters.MasterVolume, 0);
            }
        }

        public void SetActive(bool value)
        {
            var volume = value ? 1 : 0;
            m_Block.SetFloat<GainNodeJob.Params, NoProvs, GainNodeJob>(m_MasterGainNode, GainNodeJob.Params.Vol1, volume, 5 * 44100);
            m_Block.SetFloat<GainNodeJob.Params, NoProvs, GainNodeJob>(m_MasterGainNode, GainNodeJob.Params.Vol2, volume, 5 * 44100);
        }
    }

    [UpdateAfter(typeof(AudioManagerSystem))]
    class AudioBeginFrame : JobComponentSystem
    {
        AudioManagerSystem m_Manager;

        protected override JobHandle OnUpdate(JobHandle deps)
        {
            return deps;
        }

        protected override void OnCreateManager()
        {
            m_Manager = World.GetOrCreateManager<AudioManagerSystem>();
        }
    }

    [UpdateAfter(typeof(AudioBeginFrame)), AlwaysUpdateSystem]
    class AudioEndFrame : JobComponentSystem
    {
        AudioManagerSystem m_Manager;

        protected override JobHandle OnUpdate(JobHandle deps)
        {
            m_Manager.SwapBuffers();

            return deps;
        }

        protected override void OnCreateManager()
        {
            m_Manager = World.GetOrCreateManager<AudioManagerSystem>();
        }
    }
}
