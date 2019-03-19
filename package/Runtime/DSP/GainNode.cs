using Unity.Burst;
using Unity.Experimental.Audio;

namespace Unity.Audio.Megacity
{
    [BurstCompile]
    struct GainNodeJob : IAudioJob<GainNodeJob.Params, NoProvs>
    {
        public enum Params
        {
            Vol1,
            Vol2
        }

        public void Init(ParameterData<GainNodeJob.Params> parameters)
        {
        }

        public void Execute(ref ExecuteContext<GainNodeJob.Params, NoProvs> ctx)
        {
            var inputBuffer = ctx.Inputs.GetSampleBuffer(0);
            var outputBuffer = ctx.Outputs.GetSampleBuffer(0);
            var numChannels = outputBuffer.Channels;
            var sampleFrames = outputBuffer.Samples;
            var src = inputBuffer.Buffer;
            var dst = outputBuffer.Buffer;

            int offset = 0;
            for (uint n = 0; n < sampleFrames; n++)
            {
                dst[offset] = src[offset] * ctx.Parameters.GetFloat(Params.Vol1, n); ++offset;
                dst[offset] = src[offset] * ctx.Parameters.GetFloat(Params.Vol2, n); ++offset;
            }
        }
    }
}
