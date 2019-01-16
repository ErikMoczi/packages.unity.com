using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct MultiParentConstraintJob : IAnimationJob
    {
        const float k_Epsilon = 1e-5f;

        public TransformHandle driven;
        public TransformHandle drivenParent;

        public NativeArray<TransformHandle> sources;
        public NativeArray<AffineTransform> sourceOffsets;
        public CacheIndex sourceWeightStartIdx;

        public Vector3 positionAxesMask;
        public Vector3 rotationAxesMask;

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
                Quaternion currentWRot = driven.GetRotation(stream);
                var accumTx = new AffineTransform(currentWPos, currentWRot);
                for (int i = 0; i < sources.Length; ++i)
                {
                    var normalizedWeight = cache.GetRaw(sourceWeightStartIdx, i) * weightScale;
                    if (normalizedWeight < k_Epsilon)
                        continue;

                    var sourceTx = new AffineTransform(sources[i].GetPosition(stream), sources[i].GetRotation(stream));
                    sourceTx *= sourceOffsets[i];

                    accumTx.rotation = Quaternion.Lerp(accumTx.rotation, sourceTx.rotation, normalizedWeight);
                    accumTx.translation += (sourceTx.translation - currentWPos) * normalizedWeight;
                }

                // Convert accumTx to local space
                if (drivenParent.IsValid(stream))
                {
                    var parentTx = new AffineTransform(drivenParent.GetPosition(stream), drivenParent.GetRotation(stream));
                    accumTx = parentTx.InverseMul(accumTx);
                }

                Vector3 currentLPos = driven.GetLocalPosition(stream);
                Quaternion currentLRot = driven.GetLocalRotation(stream);
                if (Vector3.Dot(positionAxesMask, positionAxesMask) < 3f)
                    accumTx.translation = AnimationRuntimeUtils.Lerp(currentLPos, accumTx.translation, positionAxesMask);
                if (Vector3.Dot(rotationAxesMask, rotationAxesMask) < 3f)
                    accumTx.rotation = Quaternion.Euler(AnimationRuntimeUtils.Lerp(currentLRot.eulerAngles, accumTx.rotation.eulerAngles, rotationAxesMask));

                driven.SetLocalPosition(stream, Vector3.Lerp(currentLPos, accumTx.translation, jobWeight));
                driven.SetLocalRotation(stream, Quaternion.Lerp(currentLRot, accumTx.rotation, jobWeight));
            }
            else
                AnimationRuntimeUtils.PassThrough(stream, driven);
        }
    }

    public interface IMultiParentConstraintData
    {
        Transform constrainedObject { get; }
        Transform[] sourceObjects { get; }
        float[] sourceWeights { get; }
        bool maintainPositionOffset { get; }
        bool maintainRotationOffset { get; }

        bool constrainedPositionXAxis { get; }
        bool constrainedPositionYAxis { get; }
        bool constrainedPositionZAxis { get; }
        bool constrainedRotationXAxis { get; }
        bool constrainedRotationYAxis { get; }
        bool constrainedRotationZAxis { get; }
    }

    public class MultiParentConstraintJobBinder<T> : AnimationJobBinder<MultiParentConstraintJob, T>
        where T : struct, IAnimationJobData, IMultiParentConstraintData
    {
        public override MultiParentConstraintJob Create(Animator animator, ref T data)
        {
            var job = new MultiParentConstraintJob();
            var cacheBuilder = new AnimationJobCacheBuilder();

            job.driven = TransformHandle.Bind(animator, data.constrainedObject);
            job.drivenParent = TransformHandle.Bind(animator, data.constrainedObject.parent);

            var src = data.sourceObjects;
            var srcWeights = data.sourceWeights;
            job.sources = new NativeArray<TransformHandle>(src.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceOffsets = new NativeArray<AffineTransform>(src.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceWeightStartIdx = cacheBuilder.AllocateChunk(srcWeights.Length);

            var drivenTx = new AffineTransform(data.constrainedObject.position, data.constrainedObject.rotation);
            for (int i = 0; i < src.Length; ++i)
            {
                job.sources[i] = TransformHandle.Bind(animator, src[i]);
                cacheBuilder.SetValue(job.sourceWeightStartIdx, i, srcWeights[i]);

                var srcTx = new AffineTransform(src[i].position, src[i].rotation);
                var srcOffset = AffineTransform.identity;
                var tmp = srcTx.InverseMul(drivenTx);

                if (data.maintainPositionOffset)
                    srcOffset.translation = tmp.translation;
                if (data.maintainRotationOffset)
                    srcOffset.rotation = tmp.rotation;

                job.sourceOffsets[i] = srcOffset;
            }

            job.positionAxesMask = new Vector3(
                System.Convert.ToSingle(data.constrainedPositionXAxis),
                System.Convert.ToSingle(data.constrainedPositionYAxis),
                System.Convert.ToSingle(data.constrainedPositionZAxis)
                );
            job.rotationAxesMask = new Vector3(
                System.Convert.ToSingle(data.constrainedRotationXAxis),
                System.Convert.ToSingle(data.constrainedRotationYAxis),
                System.Convert.ToSingle(data.constrainedRotationZAxis)
                );
            job.cache = cacheBuilder.Build();

            return job;
        }

        public override void Destroy(MultiParentConstraintJob job)
        {
            job.sources.Dispose();
            job.sourceOffsets.Dispose();
            job.cache.Dispose();
        }

        public override void Update(MultiParentConstraintJob job, ref T data)
        {
            job.cache.SetArray(data.sourceWeights, job.sourceWeightStartIdx);
        }
    }
}