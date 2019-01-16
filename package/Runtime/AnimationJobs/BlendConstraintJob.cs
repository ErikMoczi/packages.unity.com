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
        public AffineTransform sourceAOffset;
        public AffineTransform sourceBOffset;

        public CacheIndex optionsIdx;
        public CacheIndex positionWeightIdx;
        public CacheIndex rotationWeightIdx;
        public AnimationJobCache cache;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float jobWeight = stream.GetInputWeight(0);
            if (jobWeight > 0f)
            {
                var flags = (int)cache.GetRaw(optionsIdx);
                if ((flags & k_BlendTranslationMask) != 0)
                {
                    Vector3 posBlend = Vector3.Lerp(
                        sourceA.GetPosition(stream) + sourceAOffset.translation,
                        sourceB.GetPosition(stream) + sourceBOffset.translation,
                        cache.GetRaw(positionWeightIdx)
                        );
                    driven.SetPosition(stream, Vector3.Lerp(driven.GetPosition(stream), posBlend, jobWeight));
                }
                else
                    driven.SetLocalPosition(stream, driven.GetLocalPosition(stream));

                if ((flags & k_BlendRotationMask) != 0)
                {
                    Quaternion rotBlend = Quaternion.Lerp(
                        sourceA.GetRotation(stream) * sourceAOffset.rotation,
                        sourceB.GetRotation(stream) * sourceBOffset.rotation,
                        cache.GetRaw(rotationWeightIdx)
                        );
                    driven.SetRotation(stream, Quaternion.Lerp(driven.GetRotation(stream), rotBlend, jobWeight));
                }
                else
                    driven.SetLocalRotation(stream, driven.GetLocalRotation(stream));
            }
            else
                AnimationRuntimeUtils.PassThrough(stream, driven);
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

        bool maintainPositionOffsets { get; }
        bool maintainRotationOffsets { get; }
    }

    public class BlendConstraintJobBinder<T> : AnimationJobBinder<BlendConstraintJob, T>
        where T : struct, IAnimationJobData, IBlendConstraintData
    {
        public override BlendConstraintJob Create(Animator animator, ref T data)
        {
            var job = new BlendConstraintJob();
            var cacheBuilder = new AnimationJobCacheBuilder();

            job.driven = TransformHandle.Bind(animator, data.constrainedObject);
            job.sourceA = TransformHandle.Bind(animator, data.sourceA);
            job.sourceB = TransformHandle.Bind(animator, data.sourceB);
            job.optionsIdx = cacheBuilder.Add(BlendConstraintJob.PackFlags(data.blendPosition, data.blendRotation));

            job.sourceAOffset = job.sourceBOffset = AffineTransform.identity;
            if (data.maintainPositionOffsets)
            {
                var drivenPos = data.constrainedObject.position;
                job.sourceAOffset.translation = drivenPos - data.sourceA.position;
                job.sourceBOffset.translation = drivenPos - data.sourceB.position;
            }

            if (data.maintainRotationOffsets)
            {
                var drivenRot = data.constrainedObject.rotation;
                job.sourceAOffset.rotation = Quaternion.Inverse(data.sourceA.rotation) * drivenRot;
                job.sourceBOffset.rotation = Quaternion.Inverse(data.sourceB.rotation) * drivenRot;
            }

            job.positionWeightIdx = cacheBuilder.Add(data.positionWeight);
            job.rotationWeightIdx = cacheBuilder.Add(data.rotationWeight);
            job.cache = cacheBuilder.Build();

            return job;
        }

        public override void Destroy(BlendConstraintJob job)
        {
            job.cache.Dispose();
        }

        public override void Update(BlendConstraintJob job, ref T data)
        {
            job.cache.SetRaw(data.positionWeight, job.positionWeightIdx);
            job.cache.SetRaw(data.rotationWeight, job.rotationWeightIdx);
            job.cache.SetRaw(BlendConstraintJob.PackFlags(data.blendPosition, data.blendRotation), job.optionsIdx);
        }
    }
}