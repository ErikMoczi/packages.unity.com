using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct MultiAimConstraintJob : IWeightedAnimationJob
    {
        const float k_Epsilon = 1e-5f;

        public ReadWriteTransformHandle driven;
        public ReadOnlyTransformHandle drivenParent;
        public Vector3Property drivenOffset;

        public NativeArray<ReadOnlyTransformHandle> sources;
        public NativeArray<Quaternion> sourceOffsets;
        public CacheIndex sourceWeightStartIdx;

        public Vector3 aimAxis;
        public Vector3 axesMask;

        public FloatProperty minLimit;
        public FloatProperty maxLimit;

        public AnimationJobCache cache;

        public FloatProperty jobWeight { get; set; }

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float w = jobWeight.Get(stream);
            if (w > 0f)
            {
                float sumWeights = AnimationRuntimeUtils.Sum(cache, sourceWeightStartIdx, sources.Length);
                if (sumWeights < k_Epsilon)
                    return;

                float weightScale = sumWeights > 1f ? 1f / sumWeights : 1f;

                Vector2 minMaxAngles = new Vector2(minLimit.Get(stream), maxLimit.Get(stream));
                driven.GetGlobalTR(stream, out Vector3 currentWPos, out Quaternion currentWRot);
                Vector3 currentDir = currentWRot * aimAxis;
                Quaternion accumDeltaRot = Quaternion.identity;
                for (int i = 0; i < sources.Length; ++i)
                {
                    var normalizedWeight = cache.GetRaw(sourceWeightStartIdx, i) * weightScale;
                    if (normalizedWeight < k_Epsilon)
                        continue;

                    var src = sources[i];
                    var toDir = src.GetPosition(stream) - currentWPos;
                    var rotToSource = Quaternion.AngleAxis(
                        Mathf.Clamp(Vector3.Angle(currentDir, toDir), minMaxAngles.x, minMaxAngles.y),
                        Vector3.Cross(currentDir, toDir).normalized
                        );

                    accumDeltaRot = Quaternion.Lerp(accumDeltaRot, sourceOffsets[i] * rotToSource, normalizedWeight);
                    sources[i] = src;
                }
                Quaternion newRot = accumDeltaRot * currentWRot;

                // Convert newRot to local space
                if (drivenParent.IsValid(stream))
                    newRot = Quaternion.Inverse(drivenParent.GetRotation(stream)) * newRot;

                Quaternion currentLRot = driven.GetLocalRotation(stream);
                if (Vector3.Dot(axesMask, axesMask) < 3f)
                    newRot = Quaternion.Euler(AnimationRuntimeUtils.Lerp(currentLRot.eulerAngles, newRot.eulerAngles, axesMask));

                var offset = drivenOffset.Get(stream);
                if (Vector3.Dot(offset, offset) > 0f)
                    newRot *= Quaternion.Euler(offset);

                driven.SetLocalRotation(stream, Quaternion.Lerp(currentLRot, newRot, w));
            }
            else
                AnimationRuntimeUtils.PassThrough(stream, driven);
        }
    }

    public interface IMultiAimConstraintData
    {
        Transform constrainedObject { get; }
        Transform[] sourceObjects { get; }
        float[] sourceWeights { get; }
        bool maintainOffset { get; }
        Vector3 aimAxis { get; }

        bool constrainedXAxis { get; }
        bool constrainedYAxis { get; }
        bool constrainedZAxis { get; }

        string offsetVector3Property { get; }
        string minLimitFloatProperty { get; }
        string maxLimitFloatProperty { get; }
    }

    public class MultiAimConstraintJobBinder<T> : AnimationJobBinder<MultiAimConstraintJob, T>
        where T : struct, IAnimationJobData, IMultiAimConstraintData
    {
        public override MultiAimConstraintJob Create(Animator animator, ref T data, Component component)
        {
            var job = new MultiAimConstraintJob();
            var cacheBuilder = new AnimationJobCacheBuilder();

            job.driven = ReadWriteTransformHandle.Bind(animator, data.constrainedObject);
            job.drivenParent = ReadOnlyTransformHandle.Bind(animator, data.constrainedObject.parent);
            job.aimAxis = data.aimAxis;

            var src = data.sourceObjects;
            var srcWeights = data.sourceWeights;
            job.sources = new NativeArray<ReadOnlyTransformHandle>(src.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceOffsets = new NativeArray<Quaternion>(src.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceWeightStartIdx = cacheBuilder.AllocateChunk(src.Length);

            for (int i = 0; i < src.Length; ++i)
            {
                job.sources[i] = ReadOnlyTransformHandle.Bind(animator, src[i]);
                cacheBuilder.SetValue(job.sourceWeightStartIdx, i, srcWeights[i]);
                if (data.maintainOffset)
                {
                    var constrainedAim = data.constrainedObject.rotation * data.aimAxis;
                    job.sourceOffsets[i] = QuaternionExt.FromToRotation(
                        src[i].position - data.constrainedObject.position,
                        constrainedAim
                        );
                }
                else
                    job.sourceOffsets[i] = Quaternion.identity;
            }

            job.minLimit = FloatProperty.Bind(animator, component, data.minLimitFloatProperty);
            job.maxLimit = FloatProperty.Bind(animator, component, data.maxLimitFloatProperty);
            job.drivenOffset = Vector3Property.Bind(animator, component, data.offsetVector3Property);

            job.axesMask = new Vector3(
                System.Convert.ToSingle(data.constrainedXAxis),
                System.Convert.ToSingle(data.constrainedYAxis),
                System.Convert.ToSingle(data.constrainedZAxis)
                );
            job.cache = cacheBuilder.Build();

            return job;
        }

        public override void Destroy(MultiAimConstraintJob job)
        {
            job.sources.Dispose();
            job.sourceOffsets.Dispose();
            job.cache.Dispose();
        }

        public override void Update(MultiAimConstraintJob job, ref T data)
        {
            job.cache.SetArray(data.sourceWeights, job.sourceWeightStartIdx);
        }
    }
}
