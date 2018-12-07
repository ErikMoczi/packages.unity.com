using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct MultiAimConstraintJob : IAnimationJob
    {
        const float k_Epsilon = 1e-5f;

        public TransformHandle driven;
        public TransformHandle drivenParent;
        public CacheIndex drivenOffset;

        public NativeArray<TransformHandle> sources;
        public NativeArray<Quaternion> sourceOffsets;
        public CacheIndex sourceWeightStartIdx;

        public Vector3 aimAxis;
        public Vector3 axesMask;
        public CacheIndex limitsIdx;

        public AnimationJobCache cache;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float jobWeight = stream.GetInputWeight(0);
            if (jobWeight > 0f)
            {
                float sumWeights = AnimationRuntimeUtils.Sum(cache, sourceWeightStartIdx, sources.Length);
                if (sumWeights < k_Epsilon)
                    return;

                float weightScale = sumWeights > 1f ? 1f / sumWeights : 1f;

                Vector2 minMaxAngles = cache.Get<Vector2>(limitsIdx);
                Vector3 currentWPos = driven.GetPosition(stream);
                Quaternion currentWRot = driven.GetRotation(stream);
                Vector3 currentDir = currentWRot * aimAxis;
                Quaternion accumDeltaRot = Quaternion.identity;
                for (int i = 0; i < sources.Length; ++i)
                {
                    var normalizedWeight = cache.GetRaw(sourceWeightStartIdx, i) * weightScale;
                    if (normalizedWeight < k_Epsilon)
                        continue;

                    var toDir = sources[i].GetPosition(stream) - currentWPos;
                    var rotToSource = Quaternion.AngleAxis(
                        Mathf.Clamp(Vector3.Angle(currentDir, toDir), minMaxAngles.x, minMaxAngles.y),
                        Vector3.Cross(currentDir, toDir).normalized
                        );

                    accumDeltaRot = Quaternion.Lerp(accumDeltaRot, sourceOffsets[i] * rotToSource, normalizedWeight);
                }
                Quaternion newRot = accumDeltaRot * currentWRot;

                // Convert newRot to local space
                if (drivenParent.IsValid(stream))
                    newRot = Quaternion.Inverse(drivenParent.GetRotation(stream)) * newRot;

                Quaternion currentLRot = driven.GetLocalRotation(stream);
                if (Vector3.Dot(axesMask, axesMask) < 3f)
                    newRot = Quaternion.Euler(AnimationRuntimeUtils.Lerp(currentLRot.eulerAngles, newRot.eulerAngles, axesMask));

                var offset = cache.Get<Vector3>(drivenOffset);
                if (Vector3.Dot(offset, offset) > 0f)
                    newRot *= Quaternion.Euler(offset);

                driven.SetLocalRotation(stream, Quaternion.Lerp(currentLRot, newRot, jobWeight));
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
        Vector3 offset { get; }
        Vector3 aimAxis { get; }
        Vector2 limits { get; }

        bool constrainedXAxis { get; }
        bool constrainedYAxis { get; }
        bool constrainedZAxis { get; }
    }

    public class MultiAimConstraintJobBinder<T> : AnimationJobBinder<MultiAimConstraintJob, T>
        where T : struct, IAnimationJobData, IMultiAimConstraintData
    {
        public override MultiAimConstraintJob Create(Animator animator, ref T data)
        {
            var job = new MultiAimConstraintJob();
            var cacheBuilder = new AnimationJobCacheBuilder();

            job.driven = TransformHandle.Bind(animator, data.constrainedObject);
            job.drivenParent = TransformHandle.Bind(animator, data.constrainedObject.parent);
            job.drivenOffset = cacheBuilder.Add(data.offset);
            job.limitsIdx = cacheBuilder.Add(data.limits);
            job.aimAxis = data.aimAxis;

            var src = data.sourceObjects;
            var srcWeights = data.sourceWeights;
            job.sources = new NativeArray<TransformHandle>(src.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceOffsets = new NativeArray<Quaternion>(src.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceWeightStartIdx = cacheBuilder.AllocateChunk(src.Length);

            for (int i = 0; i < src.Length; ++i)
            {
                job.sources[i] = TransformHandle.Bind(animator, src[i]);
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
            job.cache.Set(data.offset, job.drivenOffset);
            job.cache.SetArray(data.sourceWeights, job.sourceWeightStartIdx);
            job.cache.Set(data.limits, job.limitsIdx);
        }
    }
}