namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct BlendConstraintJob : IAnimationJob
    {
        public const int k_BlendTranslationMask = 1 << 0;
        public const int k_BlendRotationMask = 1 << 1;

        public TransformHandle driven;
        public TransformHandle sourceA;
        public TransformHandle sourceB;
        public AnimationJobCache.Index options;
        public AnimationJobCache.Index positionWeight;
        public AnimationJobCache.Index rotationWeight;

        public AnimationJobCache.Cache cache;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float jobWeight = stream.GetInputWeight(0);
            if (jobWeight > 0f)
            {
                var flags = cache.GetInt(options);
                if ((flags & k_BlendTranslationMask) != 0)
                {
                    Vector3 posBlend = Vector3.Lerp(sourceA.GetPosition(stream), sourceB.GetPosition(stream), cache.GetFloat(positionWeight));
                    driven.SetPosition(stream, Vector3.Lerp(driven.GetPosition(stream), posBlend, jobWeight));
                }

                if ((flags & k_BlendRotationMask) != 0)
                {
                    Quaternion rotBlend = Quaternion.Lerp(sourceA.GetRotation(stream), sourceB.GetRotation(stream), cache.GetFloat(rotationWeight));
                    driven.SetRotation(stream, Quaternion.Lerp(driven.GetRotation(stream), rotBlend, jobWeight));
                }
            }
        }

        public static int PackFlags(bool blendT, bool blendR)
        {
            return System.Convert.ToInt32(blendT) | System.Convert.ToInt32(blendR) * k_BlendRotationMask;
        }
    }

    public interface IBlendConstraintData
    {
        Transform constrainedObject { get; }
        Transform sourceA { get; }
        Transform sourceB { get; }
        bool blendPosition { get; }
        bool blendRotation { get; }
        float positionWeight { get; }
        float rotationWeight { get; }
    }

    public class BlendConstraintJobBinder<T> : AnimationJobBinder<BlendConstraintJob, T>
        where T : IAnimationJobData, IBlendConstraintData
    {
        public override BlendConstraintJob Create(Animator animator, T data)
        {
            var job = new BlendConstraintJob();
            var cacheBuilder = new AnimationJobCache.CacheBuilder();

            job.driven = TransformHandle.Bind(animator, data.constrainedObject);
            job.sourceA = TransformHandle.Bind(animator, data.sourceA);
            job.sourceB = TransformHandle.Bind(animator, data.sourceB);
            job.options = cacheBuilder.Add(BlendConstraintJob.PackFlags(data.blendPosition, data.blendRotation));

            job.positionWeight = cacheBuilder.Add(data.positionWeight);
            job.rotationWeight = cacheBuilder.Add(data.rotationWeight);
            job.cache = cacheBuilder.Create();

            return job;
        }

        public override void Destroy(BlendConstraintJob job)
        {
            job.cache.Dispose();
        }

        public override void Update(T data, BlendConstraintJob job)
        {
            job.cache.SetFloat(job.positionWeight, data.positionWeight);
            job.cache.SetFloat(job.rotationWeight, data.rotationWeight);
            job.cache.SetInt(job.options, BlendConstraintJob.PackFlags(data.blendPosition, data.blendRotation));
        }
    }
}