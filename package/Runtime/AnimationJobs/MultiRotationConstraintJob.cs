using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct MultiRotationConstraintJob : IAnimationJob
    {
        const float k_Epsilon = 1e-5f;

        public TransformHandle driven;
        public TransformHandle drivenParent;
        public CacheIndex drivenOffsetIdx;

        public NativeArray<TransformHandle> sources;
        public NativeArray<Quaternion> sourceOffsets;
        public CacheIndex sourceWeightStartIdx;

        public Vector3 axesMask;

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

                Quaternion currentWRot = driven.GetRotation(stream);
                Quaternion accumRot = currentWRot;
                for (int i = 0; i < sources.Length; ++i)
                {
                    var normalizedWeight = cache.GetRaw(sourceWeightStartIdx, i) * weightScale;
                    if (normalizedWeight < k_Epsilon)
                        continue;

                    accumRot = Quaternion.Lerp(accumRot, sources[i].GetRotation(stream) * sourceOffsets[i], normalizedWeight);
                }

                // Convert accumRot to local space
                if (drivenParent.IsValid(stream))
                    accumRot = Quaternion.Inverse(drivenParent.GetRotation(stream)) * accumRot;

                Quaternion currentLRot = driven.GetLocalRotation(stream);
                if (Vector3.Dot(axesMask, axesMask) < 3f)
                    accumRot = Quaternion.Euler(AnimationRuntimeUtils.Lerp(currentLRot.eulerAngles, accumRot.eulerAngles, axesMask));

                var offset = cache.Get<Vector3>(drivenOffsetIdx);
                if (Vector3.Dot(offset, offset) > 0f)
                    accumRot *= Quaternion.Euler(offset);

                driven.SetLocalRotation(stream, Quaternion.Lerp(currentLRot, accumRot, jobWeight));
            }
            else
                AnimationRuntimeUtils.PassThrough(stream, driven);
        }
    }

    public interface IMultiRotationConstraintData
    {
        Transform constrainedObject { get; }
        Transform[] sourceObjects { get; }
        float[] sourceWeights { get; }
        bool maintainOffset { get; }
        Vector3 offset { get; }

        bool constrainedXAxis { get; }
        bool constrainedYAxis { get; }
        bool constrainedZAxis { get; }
    }

    public class MultiRotationConstraintJobBinder<T> : AnimationJobBinder<MultiRotationConstraintJob, T>
        where T : struct, IAnimationJobData, IMultiRotationConstraintData
    {
        public override MultiRotationConstraintJob Create(Animator animator, ref T data)
        {
            var job = new MultiRotationConstraintJob();
            var cacheBuilder = new AnimationJobCacheBuilder();

            job.driven = TransformHandle.Bind(animator, data.constrainedObject);
            job.drivenParent = TransformHandle.Bind(animator, data.constrainedObject.parent);
            job.drivenOffsetIdx = cacheBuilder.Add(data.offset);

            var src = data.sourceObjects;
            var srcWeights = data.sourceWeights;
            job.sources = new NativeArray<TransformHandle>(src.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceOffsets = new NativeArray<Quaternion>(src.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceWeightStartIdx = cacheBuilder.AllocateChunk(srcWeights.Length);

            Quaternion drivenRot = data.constrainedObject.rotation;
            for (int i = 0; i < src.Length; ++i)
            {
                job.sources[i] = TransformHandle.Bind(animator, src[i]);
                cacheBuilder.SetValue(job.sourceWeightStartIdx, i, srcWeights[i]);
                job.sourceOffsets[i] = data.maintainOffset ?
                    (Quaternion.Inverse(src[i].rotation) * drivenRot) : Quaternion.identity;
            }

            job.axesMask = new Vector3(
                System.Convert.ToSingle(data.constrainedXAxis),
                System.Convert.ToSingle(data.constrainedYAxis),
                System.Convert.ToSingle(data.constrainedZAxis)
                );
            job.cache = cacheBuilder.Build();

            return job;
        }

        public override void Destroy(MultiRotationConstraintJob job)
        {
            job.sources.Dispose();
            job.sourceOffsets.Dispose();
            job.cache.Dispose();
        }

        public override void Update(MultiRotationConstraintJob job, ref T data)
        {
            job.cache.Set(data.offset, job.drivenOffsetIdx);
            job.cache.SetArray(data.sourceWeights, job.sourceWeightStartIdx);
        }
    }
}