using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Experimental.Audio;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Audio.Megacity
{
    struct Resampler
    {
        public double pos; // position in the current buffer

        // Resampler with linear interpolation, pulling directly from the prov
        public void ResampleLerpRead(ParameterData<ECSoundPlayerJob.Params> parameters, NativeArray<float> output, NativeArray<float> input, SampleProvider provider)
        {
            for (int i = 0; i < output.Length / 2; i++)
            {
                float factor = parameters.GetFloat(ECSoundPlayerJob.Params.Factor, (uint)i);
                pos += factor;

                double origpos = pos;

                int L = input.Length / 2 - 1;

                // sometimes need to jump more than one buffer, hence the while loop
                while (pos >= L)
                {
                    // move the last sample to the start of buffer
                    input[0] = input[input.Length - 2];
                    input[1] = input[input.Length - 1];

                    // read N-1 samples starting at buffer index 1 - this allows to save the previous sample (last sample of previous buffer).
                    ReadSamples(provider, new NativeSlice<float>(input, 2));

                    pos -= input.Length / 2 - 1;
                }

                double fp = Math.Floor(pos);

                double frac = pos - fp; // fractional part, used for interpolation

                int p = (int)fp; // integer index of the previous sample

                int n = p + 1;

                float prevSampleL = input[p * 2 + 0];
                float prevSampleR = input[p * 2 + 1];
                float sampleL = input[n * 2 + 0];
                float sampleR = input[n * 2 + 1];

                output[i * 2 + 0] = (float)(prevSampleL + (sampleL - prevSampleL) * frac);
                output[i * 2 + 1] = (float)(prevSampleR + (sampleR - prevSampleR) * frac);
            }
        }

        // read either mono or stereo, always convert to stereo interleaved
        public static void ReadSamples(SampleProvider prov, NativeSlice<float> dst)
        {
            if (!prov.Valid)
                return;
            // Read from sampleprovider and convert to interleaved stereo if needed
            if (prov.ChannelCount == 2)
            {
                int res = prov.Read(dst.Slice(0, dst.Length));
                if (res < dst.Length / 2)
                {
                    for (int i = res * 2; i < dst.Length; i++)
                    {
                        dst[i] = 0;
                    }
                }
            }
            else
            {
                int n = dst.Length / 2;
                int res = prov.Read(dst.Slice(0, n));
                if (res < n)
                {
                    for (int i = res; i < n; i++)
                    {
                        dst[i] = 0;
                    }
                }
                for (int i = n - 1; i >= 0; i--)
                {
                    dst[i * 2 + 0] = dst[i];
                    dst[i * 2 + 1] = dst[i];
                }
            }
        }
    }

    [BurstCompile]
    struct ECSoundPlayerJob : IAudioJob<ECSoundPlayerJob.Params, ECSoundPlayerJob.Provs>
    {
        public enum Provs
        {
            Sample
        }

        public enum Params
        {
            Factor,
        }

        public Resampler m_Resampler;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<float> m_ResampleBuffer;

        public void Init(ParameterData<Params> parameters)
        {
        }

        public void Execute(ref ExecuteContext<Params, Provs> ctx)
        {
            var buf = ctx.Outputs.GetSampleBuffer(0);

            var prov = ctx.Providers.GetSampleProvider(Provs.Sample);

            m_Resampler.ResampleLerpRead(ctx.Parameters, buf.Buffer, m_ResampleBuffer, prov);
        }
    }

    struct SoundPlayerUpdateJob : IAudioJobUpdate<ECSoundPlayerJob.Params, ECSoundPlayerJob.Provs, ECSoundPlayerJob>
    {
        public void Update(ref ECSoundPlayerJob audioJob, ResourceContext context)
        {
            if (!audioJob.m_ResampleBuffer.IsCreated)
            {
                audioJob.m_ResampleBuffer = context.AllocateArray<float>(1025 * 2);
                audioJob.m_Resampler.pos = (double)audioJob.m_ResampleBuffer.Length / 2; // set position to "end of buffer", to force pulling data on first iteration
            }
        }
    }

#pragma warning disable 0649
    [Serializable]
    struct ECSoundPlayer : IComponentData
    {
        internal int m_SoundPlayerIndex;

        internal DSPNode m_Source;
        internal DSPNode m_DirectMixGain;
        internal DSPNode m_DirectLPFGain;
        internal DSPNode m_LPF;

        internal DSPConnection m_DirectMixConnection;
        internal DSPConnection m_DirectLPFConnection;
    }
#pragma warning restore 0649

}
