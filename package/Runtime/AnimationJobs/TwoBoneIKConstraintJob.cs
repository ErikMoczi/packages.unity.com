namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct TwoBoneIKConstraintJob : IAnimationJob
    {
        public TransformHandle root;
        public TransformHandle mid;
        public TransformHandle tip;

        public TransformHandle hint;
        public CacheIndex hintWeightIdx;

        public TransformHandle target;
        public CacheIndex targetPositionWeightIdx;
        public CacheIndex targetRotationWeightIdx;

        public Vector2 linkLengths;

        public AnimationJobCache cache;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float jobWeight = stream.GetInputWeight(0);
            if (jobWeight > 0f)
            {
                AnimationRuntimeUtils.SolveTwoBoneIK(
                    stream, root, mid, tip, target, hint,
                    cache.GetRaw(targetPositionWeightIdx) * jobWeight,
                    cache.GetRaw(targetRotationWeightIdx) * jobWeight,
                    cache.GetRaw(hintWeightIdx) * jobWeight,
                    linkLengths
                    );
            }
            else
            {
                AnimationRuntimeUtils.PassThrough(stream, root);
                AnimationRuntimeUtils.PassThrough(stream, mid);
                AnimationRuntimeUtils.PassThrough(stream, tip);
            }
        }
    }

    public interface ITwoBoneIKConstraintData
    {
        Transform root { get; }
        Transform mid { get; }
        Transform tip { get; }
        Transform target { get; }
        Transform hint { get; }

        float targetPositionWeight { get; }
        float targetRotationWeight { get; }
        float hintWeight { get; }
    }

    public class TwoBoneIKConstraintJobBinder<T> : AnimationJobBinder<TwoBoneIKConstraintJob, T>
        where T : struct, IAnimationJobData, ITwoBoneIKConstraintData
    {
        public override TwoBoneIKConstraintJob Create(Animator animator, ref T data)
        {
            var job = new TwoBoneIKConstraintJob();
            var cacheBuilder = new AnimationJobCacheBuilder();

            job.root = TransformHandle.Bind(animator, data.root);
            job.mid = TransformHandle.Bind(animator, data.mid);
            job.tip = TransformHandle.Bind(animator, data.tip);
            job.target = TransformHandle.Bind(animator, data.target);

            if (data.hint != null)
                job.hint = TransformHandle.Bind(animator, data.hint);

            job.linkLengths[0] = Vector3.Distance(data.root.position, data.mid.position);
            job.linkLengths[1] = Vector3.Distance(data.mid.position, data.tip.position);
        
            job.targetPositionWeightIdx = cacheBuilder.Add(data.targetPositionWeight);
            job.targetRotationWeightIdx = cacheBuilder.Add(data.targetRotationWeight);
            job.hintWeightIdx = cacheBuilder.Add(data.hintWeight);
            job.cache = cacheBuilder.Build();

            return job;
        }

        public override void Destroy(TwoBoneIKConstraintJob job)
        {
            job.cache.Dispose();
        }

        public override void Update(TwoBoneIKConstraintJob job, ref T data)
        {
            job.cache.SetRaw(data.targetPositionWeight, job.targetPositionWeightIdx);
            job.cache.SetRaw(data.targetRotationWeight, job.targetRotationWeightIdx);
            job.cache.SetRaw(data.hintWeight, job.hintWeightIdx);
        }
    }
}