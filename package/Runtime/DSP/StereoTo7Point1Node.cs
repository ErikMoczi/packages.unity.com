using Unity.Burst;
using Unity.Experimental.Audio;

namespace Unity.Audio.Megacity
{
    [BurstCompile]
    struct StereoTo7Point1NodeJob : IAudioJob<StereoTo7Point1NodeJob.Params, NoProvs>
    {
        public enum Params
        {
        }

        public void Init(ParameterData<StereoTo7Point1NodeJob.Params> parameters)
        {
        }

        public void Execute(ref ExecuteContext<StereoTo7Point1NodeJob.Params, NoProvs> ctx)
        {
            var inputBuffer = ctx.Inputs.GetSampleBuffer(0);
            var outputBuffer = ctx.Outputs.GetSampleBuffer(0);

            var numChannels = outputBuffer.Channels;
            var sampleFrames = outputBuffer.Samples;

            var src = inputBuffer.Buffer;
            var dst = outputBuffer.Buffer;

            int srcOffset = 0;
            int dstOffset = 0;

            for (uint n = 0; n < sampleFrames; n++)
            {
                dst[dstOffset++] = src[srcOffset++];
                dst[dstOffset++] = src[srcOffset++];
                dst[dstOffset++] = 0.0f;
                dst[dstOffset++] = 0.0f;
                dst[dstOffset++] = 0.0f;
                dst[dstOffset++] = 0.0f;
                dst[dstOffset++] = 0.0f;
                dst[dstOffset++] = 0.0f;
            }
        }
    }
}
