using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct TwistCorrectionJob : IAnimationJob
    {
        public TransformHandle source;
        public Quaternion sourceInverseBindRotation;
        public Vector3 axisMask;
    
        public NativeArray<TransformHandle> twistNodes;
        public NativeArray<AnimationJobCache.Index> twistWeights;

        public AnimationJobCache.Cache cache;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float jobWeight = stream.GetInputWeight(0);
            if (jobWeight > 0f)
            {
                if (twistNodes.Length == 0)
                    return;

                Quaternion twistRot = TwistRotation(axisMask, sourceInverseBindRotation * source.GetLocalRotation(stream));
                Quaternion invTwistRot = Quaternion.Inverse(twistRot);
                for (int i = 0; i < twistNodes.Length; ++i)
                {
                    var w = cache.GetFloat(twistWeights[i]) * 2f - 1f;
                    Quaternion rot = Quaternion.Lerp(Quaternion.identity, Mathf.Sign(w) > 0f ? twistRot : invTwistRot, Mathf.Abs(w));
                    twistNodes[i].SetLocalRotation(stream, Quaternion.Lerp(twistNodes[i].GetLocalRotation(stream), rot, jobWeight));
                }
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
        where T : IAnimationJobData, ITwistCorrectionData
    {
        public override TwistCorrectionJob Create(Animator animator, T data)
        {
            var job = new TwistCorrectionJob();
            var cacheBuilder = new AnimationJobCache.CacheBuilder();

            job.source = TransformHandle.Bind(animator, data.source);
            job.sourceInverseBindRotation = Quaternion.Inverse(data.source.localRotation);
            job.axisMask = data.twistAxis;

            var twistNodes = data.twistNodes;
            var twistNodeWeights = data.twistNodeWeights;
            job.twistNodes = new NativeArray<TransformHandle>(twistNodes.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.twistWeights = new NativeArray<AnimationJobCache.Index>(twistNodes.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < twistNodes.Length; ++i)
            {
                job.twistNodes[i] = TransformHandle.Bind(animator, twistNodes[i]);
                job.twistWeights[i] = cacheBuilder.Add(twistNodeWeights[i]);
            }
            job.cache = cacheBuilder.Create();

            return job;
        }

        public override void Destroy(TwistCorrectionJob job)
        {
            job.twistNodes.Dispose();
            job.twistWeights.Dispose();
            job.cache.Dispose();
        }

        public override void Update(T data, TwistCorrectionJob job)
        {
            job.cache.SetArray(job.twistWeights.ToArray(), data.twistNodeWeights);
        }
    }
}