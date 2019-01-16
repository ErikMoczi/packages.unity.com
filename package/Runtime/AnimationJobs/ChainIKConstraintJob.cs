using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct ChainIKConstraintJob : IAnimationJob
    {
        public NativeArray<TransformHandle> chain;
        public TransformHandle target;
        public AffineTransform targetOffset;

        public NativeArray<float> linkLengths;
        public NativeArray<Vector3> linkPositions;

        public CacheIndex chainRotationWeightIdx;
        public CacheIndex tipRotationWeightIdx;
        public CacheIndex toleranceIdx;
        public CacheIndex maxIterationsIdx;
        public AnimationJobCache cache;

        public float maxReach;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float jobWeight = stream.GetInputWeight(0);
            if (jobWeight > 0f)
            {
                for (int i = 0; i < chain.Length; ++i)
                    linkPositions[i] = chain[i].GetPosition(stream);

                int tipIndex = chain.Length - 1;
                if (AnimationRuntimeUtils.SolveFABRIK(linkPositions, linkLengths, target.GetPosition(stream) + targetOffset.translation,
                    cache.GetRaw(toleranceIdx), maxReach, (int)cache.GetRaw(maxIterationsIdx)))
                {
                    var chainRWeight = cache.GetRaw(chainRotationWeightIdx) * jobWeight;
                    for (int i = 0; i < tipIndex; ++i)
                    {
                        var prevDir = chain[i + 1].GetPosition(stream) - chain[i].GetPosition(stream);
                        var newDir = linkPositions[i + 1] - linkPositions[i];
                        chain[i].SetRotation(stream, QuaternionExt.FromToRotation(prevDir, newDir) * chain[i].GetRotation(stream));
                    }
                }

                chain[tipIndex].SetRotation(
                    stream,
                    Quaternion.Lerp(
                        chain[tipIndex].GetRotation(stream),
                        target.GetRotation(stream) * targetOffset.rotation,
                        cache.GetRaw(tipRotationWeightIdx) * jobWeight
                        )
                    );
            }
            else
            {
                for (int i = 0; i < chain.Length; ++i)
                    AnimationRuntimeUtils.PassThrough(stream, chain[i]);
            }
        }
    }

    public interface IChainIKConstraintData
    {
        Transform root { get; }
        Transform tip { get; }
        Transform target { get; }
        float chainRotationWeight { get; }
        float tipRotationWeight { get; }
        int maxIterations { get; }
        float tolerance { get; }
        bool maintainTargetPositionOffset { get; }
        bool maintainTargetRotationOffset { get; }
    }

    public class ChainIKConstraintJobBinder<T> : AnimationJobBinder<ChainIKConstraintJob, T>
        where T : struct, IAnimationJobData, IChainIKConstraintData
    {
        public override ChainIKConstraintJob Create(Animator animator, ref T data)
        {
            List<Transform> chain = new List<Transform>();
            Transform tmp = data.tip;
            while (tmp != data.root)
            {
                chain.Add(tmp);
                tmp = tmp.parent;
            }
            chain.Add(data.root);
            chain.Reverse();

            var job = new ChainIKConstraintJob();
            job.chain = new NativeArray<TransformHandle>(chain.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.linkLengths = new NativeArray<float>(chain.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.linkPositions = new NativeArray<Vector3>(chain.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.maxReach = 0f;

            int tipIndex = chain.Count - 1;
            for (int i = 0; i < chain.Count; ++i)
            {
                job.chain[i] = TransformHandle.Bind(animator, chain[i]);
                job.linkLengths[i] = (i != tipIndex) ? Vector3.Distance(chain[i].position, chain[i + 1].position) : 0f;
                job.maxReach += job.linkLengths[i];
            }

            job.target = TransformHandle.Bind(animator, data.target);
            job.targetOffset = AffineTransform.identity;
            if (data.maintainTargetPositionOffset)
                job.targetOffset.translation = data.tip.position - data.target.position;
            if (data.maintainTargetRotationOffset)
                job.targetOffset.rotation = Quaternion.Inverse(data.target.rotation) * data.tip.rotation;

            var cacheBuilder = new AnimationJobCacheBuilder();
            job.chainRotationWeightIdx = cacheBuilder.Add(data.chainRotationWeight);
            job.tipRotationWeightIdx = cacheBuilder.Add(data.tipRotationWeight);
            job.maxIterationsIdx = cacheBuilder.Add(data.maxIterations);
            job.toleranceIdx = cacheBuilder.Add(data.tolerance);
            job.cache = cacheBuilder.Build();

            return job;
        }

        public override void Destroy(ChainIKConstraintJob job)
        {
            job.chain.Dispose();
            job.linkLengths.Dispose();
            job.linkPositions.Dispose();
            job.cache.Dispose();
        }

        public override void Update(ChainIKConstraintJob job, ref T data)
        {
            job.cache.SetRaw(data.chainRotationWeight, job.chainRotationWeightIdx);
            job.cache.SetRaw(data.tipRotationWeight, job.tipRotationWeightIdx);
            job.cache.SetRaw(data.maxIterations, job.maxIterationsIdx);
            job.cache.SetRaw(data.tolerance, job.toleranceIdx);
        }
    }
}
