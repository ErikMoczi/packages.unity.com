using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct TwistCorrectionJob : IWeightedAnimationJob
    {
        public ReadOnlyTransformHandle source;
        public Quaternion sourceInverseBindRotation;
        public Vector3 axisMask;
    
        public NativeArray<ReadWriteTransformHandle> twistNodes;
        public CacheIndex twistWeightStartIdx;

        public AnimationJobCache cache;

        public FloatProperty jobWeight { get; set; }

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float w = jobWeight.Get(stream);
            if (w > 0f)
            {
                if (twistNodes.Length == 0)
                    return;

                Quaternion twistRot = TwistRotation(axisMask, sourceInverseBindRotation * source.GetLocalRotation(stream));
                Quaternion invTwistRot = Quaternion.Inverse(twistRot);
                for (int i = 0; i < twistNodes.Length; ++i)
                {
                    float twistWeight = Mathf.Clamp(cache.GetRaw(twistWeightStartIdx, i), -1f, 1f);
                    Quaternion rot = Quaternion.Lerp(Quaternion.identity, Mathf.Sign(twistWeight) < 0f ? invTwistRot : twistRot, Mathf.Abs(twistWeight));
                    twistNodes[i].SetLocalRotation(stream, Quaternion.Lerp(twistNodes[i].GetLocalRotation(stream), rot, w));
                }
            }
            else
            {
                for (int i = 0; i < twistNodes.Length; ++i)
                    AnimationRuntimeUtils.PassThrough(stream, twistNodes[i]);
            }
        }

        static Quaternion TwistRotation(Vector3 axis, Quaternion rot)
        {
            return new Quaternion(axis.x * rot.x, axis.y * rot.y, axis.z * rot.z, rot.w);
        }
    }

    public interface ITwistCorrectionData
    {
        Transform source { get; }
        Transform[] twistNodes { get; }
        float[] twistNodeWeights { get; }
        Vector3 twistAxis { get; }
    }

    public class TwistCorrectionJobBinder<T> : AnimationJobBinder<TwistCorrectionJob, T>
        where T : struct, IAnimationJobData, ITwistCorrectionData
    {
        public override TwistCorrectionJob Create(Animator animator, ref T data, Component component)
        {
            var job = new TwistCorrectionJob();
            var cacheBuilder = new AnimationJobCacheBuilder();

            job.source = ReadOnlyTransformHandle.Bind(animator, data.source);
            job.sourceInverseBindRotation = Quaternion.Inverse(data.source.localRotation);
            job.axisMask = data.twistAxis;

            var twistNodes = data.twistNodes;
            var twistNodeWeights = data.twistNodeWeights;
            job.twistNodes = new NativeArray<ReadWriteTransformHandle>(twistNodes.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.twistWeightStartIdx = cacheBuilder.AllocateChunk(twistNodes.Length);

            for (int i = 0; i < twistNodes.Length; ++i)
            {
                job.twistNodes[i] = ReadWriteTransformHandle.Bind(animator, twistNodes[i]);
                cacheBuilder.SetValue(job.twistWeightStartIdx, i, twistNodeWeights[i]);
            }
            job.cache = cacheBuilder.Build();

            return job;
        }

        public override void Destroy(TwistCorrectionJob job)
        {
            job.twistNodes.Dispose();
            job.cache.Dispose();
        }

        public override void Update(TwistCorrectionJob job, ref T data)
        {
            job.cache.SetArray(data.twistNodeWeights, job.twistWeightStartIdx);
        }
    }
}