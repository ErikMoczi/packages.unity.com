namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct TwoBoneIKConstraintJob : IAnimationJob
    {
        public TransformHandle root;
        public TransformHandle mid;
        public TransformHandle tip;

        public TransformHandle hint;
        public AnimationJobCache.Index hintWeight;

        public TransformHandle target;
        public AnimationJobCache.Index targetPositionWeight;
        public AnimationJobCache.Index targetRotationWeight;

        public Vector2 linkLengths;

        public AnimationJobCache.Cache cache;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float jobWeight = stream.GetInputWeight(0);
            if (jobWeight > 0f)
            {
                AnimationRuntimeUtils.SolveTwoBoneIK(
                    stream, root, mid, tip, target, hint,
                    cache.GetFloat(targetPositionWeight) * jobWeight,
                    cache.GetFloat(targetRotationWeight) * jobWeight,
                    cache.GetFloat(hintWeight) * jobWeight,
                    linkLengths
                    );
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
        where T : IAnimationJobData, ITwoBoneIKConstraintData
    {
        public override TwoBoneIKConstraintJob Create(Animator animator, T data)
        {
            var job = new TwoBoneIKConstraintJob();
            var cacheBuilder = new AnimationJobCache.CacheBuilder();

            job.root = TransformHandle.Bind(animator, data.root);
            job.mid = TransformHandle.Bind(animator, data.mid);
            job.tip = TransformHandle.Bind(animator, data.tip);
            job.target = TransformHandle.Bind(animator, data.target);

            if (data.hint != null)
                job.hint = TransformHandle.Bind(animator, data.hint);

            job.linkLengths[0] = Vector3.Distance(data.root.position, data.mid.position);
            job.linkLengths[1] = Vector3.Distance(data.mid.position, data.tip.position);
        
            job.targetPositionWeight = cacheBuilder.Add(data.targetPositionWeight);
            job.targetRotationWeight = cacheBuilder.Add(data.targetRotationWeight);
            job.hintWeight = cacheBuilder.Add(data.hintWeight);
            job.cache = cacheBuilder.Create();

            return job;
        }

        public override void Destroy(TwoBoneIKConstraintJob job)
        {
            job.cache.Dispose();
        }

        public override void Update(T data, TwoBoneIKConstraintJob job)
        {
            job.cache.SetFloat(job.targetPositionWeight, data.targetPositionWeight);
            job.cache.SetFloat(job.targetRotationWeight, data.targetRotationWeight);
            job.cache.SetFloat(job.hintWeight, data.hintWeight);
        }
    }
}