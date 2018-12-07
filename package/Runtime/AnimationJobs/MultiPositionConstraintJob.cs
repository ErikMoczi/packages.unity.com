using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct MultiPositionConstraintJob : IAnimationJob
    {
        static readonly float k_Epsilon = 1e-5f;

        public TransformHandle driven;
        public TransformHandle drivenParent;
        public AnimationJobCache.Index drivenOffset;

        public NativeArray<TransformHandle> sources;
        public NativeArray<AnimationJobCache.Index> sourceWeights;
        public NativeArray<Vector3> sourceOffsets;

        public Vector3 axesMask;

        public AnimationJobCache.Cache cache;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float jobWeight = stream.GetInputWeight(0);
            if (jobWeight > 0f)
            {
                float sumWeights = AnimationRuntimeUtils.Sum(cache, sourceWeights);
                if (sumWeights < k_Epsilon)
                    return;

                float weightScale = sumWeights > 1f ? 1f / sumWeights : 1f;

                Vector3 currentWPos = driven.GetPosition(stream);
                Vector3 accumPos = currentWPos;
                for (int i = 0; i < sources.Length; ++i)
                {
                    var normalizedWeight = cache.GetFloat(sourceWeights[i]) * weightScale;
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

                driven.SetLocalPosition(stream, Vector3.Lerp(currentLPos, accumPos + cache.GetVector3(drivenOffset), jobWeight));
            }
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
        where T : IAnimationJobData, IMultiPositionConstraintData
    {
        public override MultiPositionConstraintJob Create(Animator animator, T data)
        {
            var job = new MultiPositionConstraintJob();
            var cacheBuilder = new AnimationJobCache.CacheBuilder();

            job.driven = TransformHandle.Bind(animator, data.constrainedObject);
            job.drivenParent = TransformHandle.Bind(animator, data.constrainedObject.parent);
            job.drivenOffset = cacheBuilder.Add(data.offset);

            var src = data.sourceObjects;
            var srcWeights = data.sourceWeights;
            job.sources = new NativeArray<TransformHandle>(src.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceOffsets = new NativeArray<Vector3>(src.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceWeights = new NativeArray<AnimationJobCache.Index>(src.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            Vector3 drivenPos = data.constrainedObject.position;
            for (int i = 0; i < src.Length; ++i)
            {
                job.sources[i] = TransformHandle.Bind(animator, src[i]);
                job.sourceWeights[i] = cacheBuilder.Add(srcWeights[i]);
                job.sourceOffsets[i] = data.maintainOffset ? (drivenPos - src[i].position) : Vector3.zero;
            }

            job.axesMask = new Vector3(
                System.Convert.ToSingle(data.constrainedXAxis),
                System.Convert.ToSingle(data.constrainedYAxis),
                System.Convert.ToSingle(data.constrainedZAxis)
                );
            job.cache = cacheBuilder.Create();

            return job;
        }

        public override void Destroy(MultiPositionConstraintJob job)
        {
            job.sources.Dispose();
            job.sourceOffsets.Dispose();
            job.sourceWeights.Dispose();
            job.cache.Dispose();
        }

        public override void Update(T data, MultiPositionConstraintJob job)
        {
            job.cache.SetVector3(job.drivenOffset, data.offset);
            job.cache.SetArray(job.sourceWeights.ToArray(), data.sourceWeights);
        }
    }
}