using UnityEngine;
using Unity.Burst;
using Unity.Experimental.Audio;
using Unity.Mathematics;

namespace Unity.Audio.Megacity
{
    [BurstCompile]
    struct LimiterNodeJob : IAudioJob<LimiterNodeJob.Params, LimiterNodeJob.Provs>
    {
       public enum Params
        {
            PreGainDBs,
            LimitingThresholdDBs,
            ReleaseMs
        }

        public enum Provs
        {
        }

        float m_EnvelopeFollower;

        public void Init(ParameterData<Params> parameters)
        {
        }

        public void Execute(ref ExecuteContext<Params, Provs> ctx)
        {
            var ins = ctx.Inputs.GetSampleBuffer(0).Buffer;
            var side = ins;

            var outsb = ctx.Outputs.GetSampleBuffer(0);
            var outs = outsb.Buffer;
            int channels = (int)outsb.Channels;

            var sampleFrames = outsb.Samples;

            var preGain = math.pow(10, ctx.Parameters.GetFloat(Params.PreGainDBs, 0) / 20);
            var threshold = math.pow(10, ctx.Parameters.GetFloat(Params.LimitingThresholdDBs, 0) / 20);
            threshold *= threshold;

            // how much the envelope decays each sample after the input volume has fallen below threshold
            float decay = math.exp(-1.0f / (ctx.SampleRate * ctx.Parameters.GetFloat(Params.ReleaseMs, 0) / 1000));

            // main loop

            for (int i = 0; i < sampleFrames; i++)
            {
                float sample = 0.0f;
                // reset gain
                float gain = 1.0f;

                // catch the sample we evaluate.
                for (int c = 0; c < channels; c++)
                {
                    var inp = side[i * channels + c];
                    sample = math.max(sample, inp * inp);
                }

                sample *= preGain;

                // check if input volume is above our threshold
                if (sample > threshold)
                {
                    // this is analogous to zero attack (because the envelope is instantly set to the input volume)
                    if (sample > m_EnvelopeFollower)
                        m_EnvelopeFollower = sample;
                }

                // apply gain only if volume envelope is above zero, else gain spins out of control
                if (m_EnvelopeFollower > threshold)
                    gain = threshold / m_EnvelopeFollower;

                // apply release to envelope if input is falling below threshold
                if (sample < threshold)
                    m_EnvelopeFollower *= decay;


                // lastly, gain outputs!
                for (int c = 0; c < channels; c++)
                {
                    outs[i * channels + c] = ins[i * channels + c] * gain * preGain;
                }
            }
        }
    }
}
