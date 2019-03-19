using System;
using Unity.Burst;
using UnityEngine;
using Unity.Collections;
using Unity.Experimental.Audio;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Audio.Megacity
{
    struct StateVariableFilter
    {
        public enum FilterType
        {
            Lowpass,
            Highpass,
            Bandpass,
            Bell,
            Notch,
            Lowshelf,
            Highshelf
        }

        DSPNode m_Node;
        NativeArray<NodeJob.Channel> m_Channels;

        public DSPNode node => m_Node;

        public static StateVariableFilter Create(DSPCommandBlockInterceptor block)
        {
            return new StateVariableFilter(block);
        }

        StateVariableFilter(DSPCommandBlockInterceptor block)
        {
            m_Channels = new NativeArray<NodeJob.Channel>(2, Allocator.Persistent);
            for (int c = 0; c < m_Channels.Length; ++c)
            {
                var channel = new NodeJob.Channel();
                m_Channels[c] = channel;
            }

            m_Node = block.CreateDSPNode<NodeJob.Params, NoProvs, NodeJob>();
            block.AddInletPort(m_Node, 2, SoundFormat.Stereo);
            block.AddOutletPort(m_Node, 2, SoundFormat.Stereo);

            var reverbUpdateJob = new UpdateJob();
            reverbUpdateJob.channels = m_Channels;
            block.CreateUpdateRequest<UpdateJob, NodeJob.Params, NoProvs, NodeJob>(reverbUpdateJob, m_Node, req => { req.Dispose(); });

            block.SetFloat<NodeJob.Params, NoProvs, NodeJob>(m_Node, NodeJob.Params.FilterType, (int)FilterType.Lowpass, 0);
            block.SetFloat<NodeJob.Params, NoProvs, NodeJob>(m_Node, NodeJob.Params.Cutoff, 1500.0f, 0);
            block.SetFloat<NodeJob.Params, NoProvs, NodeJob>(m_Node, NodeJob.Params.Q, 0.707f, 0);
            block.SetFloat<NodeJob.Params, NoProvs, NodeJob>(m_Node, NodeJob.Params.GainInDBs, 0.0f, 0);
        }

        public void Dispose(DSPCommandBlockInterceptor block)
        {
            var disposeJob = new DisposeJob();
            var ch = m_Channels;
            m_Channels = default;
            block.CreateUpdateRequest<DisposeJob, NodeJob.Params, NoProvs, NodeJob>(disposeJob, m_Node, req => {
                ch.Dispose();
                req.Dispose();
            });
            block.ReleaseDSPNode(m_Node);
        }

        public struct Coefficients
        {
            public float A, g, k, a1, a2, a3, m0, m1, m2;
        }

        public static Coefficients DesignBell(float fc, float quality, float linearGain)
        {
            float A = linearGain;
            float g = Mathf.Tan(Mathf.PI * fc);
            float k = 1 / (quality * A);
            float a1 = 1 / (1 + g * (g + k));
            float a2 = g * a1;
            float a3 = g * a2;
            float m0 = 1;
            float m1 = k * (A * A - 1);
            float m2 = 0;
            return new Coefficients { A = A, g = g, k = k, a1 = a1, a2 = a2, a3 = a3, m0 = m0, m1 = m1, m2 = m2 };
        }

        public static Coefficients DesignLowpass(float normalizedFrequency, float Q, float linearGain)
        {
            float A = linearGain;
            float g = math.tan(Mathf.PI * normalizedFrequency);
            float k = 1 / Q;
            float a1 = 1 / (1 + g * (g + k));
            float a2 = g * a1;
            float a3 = g * a2;
            float m0 = 0;
            float m1 = 0;
            float m2 = 1;
            return new Coefficients { A = A, g = g, k = k, a1 = a1, a2 = a2, a3 = a3, m0 = m0, m1 = m1, m2 = m2 };
        }

        public static Coefficients DesignBandpass(float normalizedFrequency, float Q, float linearGain)
        {
            var coeffs = Design(FilterType.Lowpass, normalizedFrequency, Q, linearGain);
            coeffs.m1 = 1;
            coeffs.m2 = 0;
            return coeffs;
        }

        public static Coefficients DesignHighpass(float normalizedFrequency, float Q, float linearGain)
        {
            var coeffs = Design(FilterType.Lowpass, normalizedFrequency, Q, linearGain);
            coeffs.m0 = 1;
            coeffs.m1 = -coeffs.k;
            coeffs.m2 = -1;
            return coeffs;
        }

        public static Coefficients DesignNotch(float normalizedFrequency, float Q, float linearGain)
        {
            var coeffs = DesignLowpass(normalizedFrequency, Q, linearGain);
            coeffs.m0 = 1;
            coeffs.m1 = -coeffs.k;
            coeffs.m2 = 0;
            return coeffs;
        }

        public static Coefficients DesignLowshelf(float normalizedFrequency, float Q, float linearGain)
        {
            float A = linearGain;
            float g = Mathf.Tan(Mathf.PI * normalizedFrequency) / Mathf.Sqrt(A);
            float k = 1 / Q;
            float a1 = 1 / (1 + g * (g + k));
            float a2 = g * a1;
            float a3 = g * a2;
            float m0 = 1;
            float m1 = k * (A - 1);
            float m2 = A * A - 1;
            return new Coefficients { A = A, g = g, k = k, a1 = a1, a2 = a2, a3 = a3, m0 = m0, m1 = m1, m2 = m2 };
        }

        public static Coefficients DesignHighshelf(float normalizedFrequency, float Q, float linearGain)
        {
            float A = linearGain;
            float g = Mathf.Tan(Mathf.PI * normalizedFrequency) / Mathf.Sqrt(A);
            float k = 1 / Q;
            float a1 = 1 / (1 + g * (g + k));
            float a2 = g * a1;
            float a3 = g * a2;
            float m0 = A * A;
            float m1 = k * (1 - A) * A;
            float m2 = 1 - A * A;
            return new Coefficients { A = A, g = g, k = k, a1 = a1, a2 = a2, a3 = a3, m0 = m0, m1 = m1, m2 = m2 };
        }

        public static Coefficients Design(FilterType type, float normalizedFrequency, float Q, float linearGain)
        {
            switch (type)
            {
                case FilterType.Lowpass: return DesignLowpass(normalizedFrequency, Q, linearGain);
                case FilterType.Highpass: return DesignHighpass(normalizedFrequency, Q, linearGain);
                case FilterType.Bandpass: return DesignBandpass(normalizedFrequency, Q, linearGain);
                case FilterType.Bell: return DesignBell(normalizedFrequency, Q, linearGain);
                case FilterType.Notch: return DesignNotch(normalizedFrequency, Q, linearGain);
                case FilterType.Lowshelf: return DesignLowshelf(normalizedFrequency, Q, linearGain);
                case FilterType.Highshelf: return DesignHighshelf(normalizedFrequency, Q, linearGain);
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
        }

        public static Coefficients Design(FilterType filterType, float cutoff, float Q, float gainInDBs, float sampleRate)
        {
            var linearGain = Mathf.Pow(10, gainInDBs / 20);
            switch (filterType)
            {
                case FilterType.Lowpass:
                    return DesignLowpass(cutoff / sampleRate, Q, linearGain);
                case FilterType.Highpass:
                    return DesignHighpass(cutoff / sampleRate, Q, linearGain);
                case FilterType.Bandpass:
                    return DesignBandpass(cutoff / sampleRate, Q, linearGain);
                case FilterType.Bell:
                    return DesignBell(cutoff / sampleRate, Q, linearGain);
                case FilterType.Notch:
                    return DesignNotch(cutoff / sampleRate, Q, linearGain);
                case FilterType.Lowshelf:
                    return DesignLowshelf(cutoff / sampleRate, Q, linearGain);
                case FilterType.Highshelf:
                    return DesignHighshelf(cutoff / sampleRate, Q, linearGain);
                default:
                    throw new ArgumentOutOfRangeException("filterType", filterType, null);
            }
        }

        [BurstCompile]
        public struct NodeJob : IAudioJob<NodeJob.Params, NoProvs>
        {
            public struct Channel
            {
                public float z1, z2;
            }

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<Channel> m_Channels;

            public enum Params
            {
                FilterType,
                Cutoff,
                Q,
                GainInDBs
            }

            public void Init(ParameterData<Params> parameters)
            {
            }

            public void Execute(ref ExecuteContext<Params, NoProvs> ctx)
            {
                var inputBuffer = ctx.Inputs.GetSampleBuffer(0);
                var outputBuffer = ctx.Outputs.GetSampleBuffer(0);
                var numChannels = (int)outputBuffer.Channels;
                var sampleFrames = (int)outputBuffer.Samples;
                var numChamples = numChannels * sampleFrames;

                var inBuf = inputBuffer.Buffer;
                var outBuf = outputBuffer.Buffer;

                if (m_Channels.Length == 0)
                {
                    for (int n = 0; n < numChamples; n++)
                        outBuf[n] = 0.0f;
                    return;
                }

                ref ParameterData<Params> parameters = ref ctx.Parameters;
                var coeffs = Design((FilterType)parameters.GetFloat(Params.FilterType, 0), parameters.GetFloat(Params.Cutoff, 0), parameters.GetFloat(Params.Q, 0), parameters.GetFloat(Params.GainInDBs, 0), ctx.m_SampleRate);

                for (int c = 0; c < m_Channels.Length; c++)
                {
                    var z1 = m_Channels[c].z1;
                    var z2 = m_Channels[c].z2;

                    for (int i = 0; i < numChamples; i += numChannels)
                    {
                        var x = inBuf[i + c];

                        var v3 = x - z2;
                        var v1 = coeffs.a1 * z1 + coeffs.a2 * v3;
                        var v2 = z2 + coeffs.a2 * z1 + coeffs.a3 * v3;
                        z1 = 2 * v1 - z1;
                        z2 = 2 * v2 - z2;
                        outBuf[i + c] = coeffs.m0 * x + coeffs.m1 * v1 + coeffs.m2 * v2;
                    }

                    m_Channels[c] = new Channel { z1 = z1, z2 = z2 };
                }
            }
        }

        [BurstCompileAttribute]
        struct UpdateJob : IAudioJobUpdate<NodeJob.Params, NoProvs, NodeJob>
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<NodeJob.Channel> channels;

            public void Update(ref NodeJob audioJob, ResourceContext context)
            {
                audioJob.m_Channels = channels;
            }
        }

        [BurstCompileAttribute]
        struct DisposeJob : IAudioJobUpdate<NodeJob.Params, NoProvs, NodeJob>
        {
            public void Update(ref NodeJob audioJob, ResourceContext context)
            {
            }
        }
    }
}
