using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct MultiRotationConstraintJob : IWeightedAnimationJob
    {
        const float k_Epsilon = 1e-5f;

        public ReadWriteTransformHandle driven;
        public ReadOnlyTransformHandle drivenParent;
        public Vector3Property drivenOffset;

        public NativeArray<ReadOnlyTransformHandle> sources;
        public NativeArray<Quaternion> sourceOffsets;
        public CacheIndex sourceWeightStartIdx;

        public Vector3 axesMask;

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

                Quaternion currentWRot = driven.GetRotation(stream);
                Quaternion accumRot = currentWRot;
                for (int i = 0; i < sources.Length; ++i)
                {
                    var normalizedWeight = cache.GetRaw(sourceWeightStartIdx, i) * weightScale;
                    if (normalizedWeight < k_Epsilon)
                        continue;

                    var src = sources[i];
                    accumRot = Quaternion.Lerp(accumRot, src.GetRotation(stream) * sourceOffsets[i], normalizedWeight);
                    sources[i] = src;
                }

                // Convert accumRot to local space
                if (drivenParent.IsValid(stream))
                    accumRot = Quaternion.Inverse(drivenParent.GetRotation(stream)) * accumRot;

                Quaternion currentLRot = driven.GetLocalRotation(stream);
                if (Vector3.Dot(axesMask, axesMask) < 3f)
                    accumRot = Quaternion.Euler(AnimationRuntimeUtils.Lerp(currentLRot.eulerAngles, accumRot.eulerAngles, axesMask));

                var offset = drivenOffset.Get(stream);
                if (Vector3.Dot(offset, offset) > 0f)
                    accumRot *= Quaternion.Euler(offset);

                driven.SetLocalRotation(stream, Quaternion.Lerp(currentLRot, accumRot, w));
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

        string offsetVector3Property { get; }

        bool constrainedXAxis { get; }
        bool constrainedYAxis { get; }
        bool constrainedZAxis { get; }
    }

    public class MultiRotationConstraintJobBinder<T> : AnimationJobBinder<MultiRotationConstraintJob, T>
        where T : struct, IAnimationJobData, IMultiRotationConstraintData
    {
        public override MultiRotationConstraintJob Create(Animator animator, ref T data, Component component)
        {
            var job = new MultiRotationConstraintJob();
            var cacheBuilder = new AnimationJobCacheBuilder();

            job.driven = ReadWriteTransformHandle.Bind(animator, data.constrainedObject);
            job.drivenParent = ReadOnlyTransformHandle.Bind(animator, data.constrainedObject.parent);
            job.drivenOffset = Vector3Property.Bind(animator, component, data.offsetVector3Property);

            var src = data.sourceObjects;
            var srcWeights = data.sourceWeights;
            job.sources = new NativeArray<ReadOnlyTransformHandle>(src.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceOffsets = new NativeArray<Quaternion>(src.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceWeightStartIdx = cacheBuilder.AllocateChunk(srcWeights.Length);

            Quaternion drivenRot = data.constrainedObject.rotation;
            for (int i = 0; i < src.Length; ++i)
            {
                job.sources[i] = ReadOnlyTransformHandle.Bind(animator, src[i]);
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
            job.cache.SetArray(data.sourceWeights, job.sourceWeightStartIdx);
        }
    }
}