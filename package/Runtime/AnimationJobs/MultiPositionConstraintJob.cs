﻿using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct MultiPositionConstraintJob : IAnimationJob
    {
        const float k_Epsilon = 1e-5f;

        public TransformHandle driven;
        public TransformHandle drivenParent;
        public CacheIndex drivenOffsetIdx;

        public NativeArray<TransformHandle> sources;
        public NativeArray<Vector3> sourceOffsets;
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

                Vector3 currentWPos = driven.GetPosition(stream);
                Vector3 accumPos = currentWPos;
                for (int i = 0; i < sources.Length; ++i)
                {
                    var normalizedWeight = cache.GetRaw(sourceWeightStartIdx, i) * weightScale;
                    if (normalizedWeight < k_Epsilon)
                        continue;

                    accumPos += (sources[i].GetPosition(stream) + sourceOffsets[i] - currentWPos) * normalizedWeight;
                }

                // Convert accumPos to local space
                if (drivenParent.IsValid(stream))
                {
                    var parentTx = new AffineTransform(drivenParent.GetPosition(stream), drivenParent.GetRotation(stream));
                    accumPos = parentTx.InverseTransform(accumPos);
                }

                Vector3 currentLPos = driven.GetLocalPosition(stream);
                if (Vector3.Dot(axesMask, axesMask) < 3f)
                    accumPos = AnimationRuntimeUtils.Lerp(currentLPos, accumPos, axesMask);

                driven.SetLocalPosition(stream, Vector3.Lerp(currentLPos, accumPos + cache.Get<Vector3>(drivenOffsetIdx), jobWeight));
            }
            else
                AnimationRuntimeUtils.PassThrough(stream, driven);
        }
    }

    public interface IMultiPositionConstraintData
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

    public class MultiPositionConstraintJobBinder<T> : AnimationJobBinder<MultiPositionConstraintJob, T>
        where T : struct, IAnimationJobData, IMultiPositionConstraintData
    {
        public override MultiPositionConstraintJob Create(Animator animator, ref T data)
        {
            var job = new MultiPositionConstraintJob();
            var cacheBuilder = new AnimationJobCacheBuilder();

            job.driven = TransformHandle.Bind(animator, data.constrainedObject);
            job.drivenParent = TransformHandle.Bind(animator, data.constrainedObject.parent);
            job.drivenOffsetIdx = cacheBuilder.Add(data.offset);

            var src = data.sourceObjects;
            var srcWeights = data.sourceWeights;
            job.sources = new NativeArray<TransformHandle>(src.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceOffsets = new NativeArray<Vector3>(src.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceWeightStartIdx = cacheBuilder.AllocateChunk(src.Length);

            Vector3 drivenPos = data.constrainedObject.position;
            for (int i = 0; i < src.Length; ++i)
            {
                job.sources[i] = TransformHandle.Bind(animator, src[i]);
                cacheBuilder.SetValue(job.sourceWeightStartIdx, i, srcWeights[i]);
                job.sourceOffsets[i] = data.maintainOffset ? (drivenPos - src[i].position) : Vector3.zero;
            }

            job.axesMask = new Vector3(
                System.Convert.ToSingle(data.constrainedXAxis),
                System.Convert.ToSingle(data.constrainedYAxis),
                System.Convert.ToSingle(data.constrainedZAxis)
                );
            job.cache = cacheBuilder.Build();

            return job;
        }

        public override void Destroy(MultiPositionConstraintJob job)
        {
            job.sources.Dispose();
            job.sourceOffsets.Dispose();
            job.cache.Dispose();
        }

        public override void Update(MultiPositionConstraintJob job, ref T data)
        {
            job.cache.Set(data.offset, job.drivenOffsetIdx);
            job.cache.SetArray(data.sourceWeights, job.sourceWeightStartIdx);
        }
    }
}