using UnityEngine;
using Unity.Experimental.Audio;
using UnityEngine.Experimental.Audio;
using System.Collections.Generic;

namespace Unity.Audio.Megacity
{
    // This is unfortunately needed to guard the SampleProvider against being garbage collected, since the ECSoundPlayerNode does not hold a reference to it.
    // We are planning to change the SampleProvider API to be handle-based to avoid this.
    class SampleProviderRegistrar
    {
        static SampleProviderRegistrar instance;

        List<AudioSampleProvider> providers = new List<AudioSampleProvider>();

        public static SampleProviderRegistrar Instance
        {
            get
            {
                if (instance == null)
                    instance = new SampleProviderRegistrar();

                return instance;
            }
        }

        public void Register(AudioSampleProvider prov)
        {
            providers.Add(prov);
        }

        public void Unregister(AudioSampleProvider prov)
        {
            providers.Remove(prov);
        }
    }

    struct ECSoundPlayerNode
    {
        DSPNode m_DSPNode;
        public DSPNode node => m_DSPNode;
        uint m_ProviderID;

        public static ECSoundPlayerNode Create(DSPCommandBlockInterceptor block, AudioSampleProvider prov, float initialPitch = 1)
        {
            return new ECSoundPlayerNode(block, prov, initialPitch);
        }

        ECSoundPlayerNode(DSPCommandBlockInterceptor block, AudioSampleProvider prov, float initialPitch)
        {
            SampleProviderRegistrar.Instance.Register(prov);

            m_DSPNode = block.CreateDSPNode<ECSoundPlayerJob.Params, ECSoundPlayerJob.Provs, ECSoundPlayerJob>();
            block.AddOutletPort(m_DSPNode, 2, SoundFormat.Stereo);

            m_ProviderID = (prov != null) ? prov.id : 0;

            SetupResampling(block, prov == null ? AudioSettings.outputSampleRate : (int)prov.sampleRate, initialPitch);

            block.SetSampleProvider<ECSoundPlayerJob.Params, ECSoundPlayerJob.Provs, ECSoundPlayerJob>(prov, m_DSPNode, ECSoundPlayerJob.Provs.Sample, 0);

            var updateJob = new SoundPlayerUpdateJob();
            block.CreateUpdateRequest<SoundPlayerUpdateJob, ECSoundPlayerJob.Params, ECSoundPlayerJob.Provs, ECSoundPlayerJob>(updateJob, m_DSPNode, req => {
                req.Dispose();
            });
        }

        public void SetupResampling(DSPCommandBlockInterceptor block, int samplerate, float pitch)
        {
            float resamplingFactor = 1;
            resamplingFactor = (float)samplerate / (float)AudioSettings.outputSampleRate;
            float factor = resamplingFactor * pitch;
            block.SetFloat<ECSoundPlayerJob.Params, ECSoundPlayerJob.Provs, ECSoundPlayerJob>(m_DSPNode, ECSoundPlayerJob.Params.Factor, factor, 0);
        }

        public void Dispose(DSPCommandBlockInterceptor block)
        {
            block.SetSampleProvider<ECSoundPlayerJob.Params, ECSoundPlayerJob.Provs, ECSoundPlayerJob>(null, m_DSPNode, ECSoundPlayerJob.Provs.Sample);

            var sp = AudioSampleProvider.Lookup(m_ProviderID, null, 0);
            m_ProviderID = 0;

            SoundPlayerDisposeJob disposer = new SoundPlayerDisposeJob();
            block.CreateUpdateRequest<SoundPlayerDisposeJob, ECSoundPlayerJob.Params, ECSoundPlayerJob.Provs, ECSoundPlayerJob>(disposer, m_DSPNode, (request) =>
            {
                sp?.Dispose();
                request.Dispose();
            });

            block.ReleaseDSPNode(m_DSPNode);

            SampleProviderRegistrar.Instance.Unregister(sp);
        }

        public void Update(DSPCommandBlockInterceptor block, float pitch)
        {
            if (m_ProviderID == 0)
                return;
            var sp = AudioSampleProvider.Lookup(m_ProviderID, null, 0);
            float resamplingFactor = (float)sp.sampleRate / (float)AudioSettings.outputSampleRate;
            float factor = resamplingFactor * pitch;
            block.SetFloat<ECSoundPlayerJob.Params, ECSoundPlayerJob.Provs, ECSoundPlayerJob>(m_DSPNode, ECSoundPlayerJob.Params.Factor, factor, 1000);
        }

        struct SoundPlayerDisposeJob : IAudioJobUpdate<ECSoundPlayerJob.Params, ECSoundPlayerJob.Provs, ECSoundPlayerJob>
        {
            public void Update(ref ECSoundPlayerJob job, ResourceContext context)
            {
            }
        }
    }
}
