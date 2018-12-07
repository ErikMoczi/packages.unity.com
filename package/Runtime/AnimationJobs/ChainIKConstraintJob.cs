using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct ChainIKConstraintJob : IAnimationJob
    {
        public NativeArray<TransformHandle> chain;
        public TransformHandle target;

        public NativeArray<float> linkLengths;
        public NativeArray<Vector3> linkPositions;

        public AnimationJobCache.Index chainRotationWeight;
        public AnimationJobCache.Index tipRotationWeight;
        public AnimationJobCache.Index tolerance;
        public AnimationJobCache.Index maxIterations;

        public AnimationJobCache.Cache cache;

        public float maxReach;

        internal static readonly float k_Epsilon = 1e-6f;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float jobWeight = stream.GetInputWeight(0);
            if (jobWeight > 0f)
            {
                for (int i = 0; i < chain.Length; ++i)
                    linkPositions[i] = chain[i].GetPosition(stream);

                int tipIndex = chain.Length - 1;
                if (AnimationRuntimeUtils.SolveFABRIK(linkPositions, linkLengths, target.GetPosition(stream),
                    cache.GetFloat(tolerance), maxReach, cache.GetInt(maxIterations)))
                {
                    var chainRWeight = cache.GetFloat(chainRotationWeight) * jobWeight;
                    for (int i = 0; i < tipIndex; ++i)
                    {
                        var prevDir = chain[i + 1].GetPosition(stream) - chain[i].GetPosition(stream);
                        var newDir = linkPositions[i + 1] - linkPositions[i];
                        var angle = Vector3.Angle(prevDir, Vector3.Lerp(prevDir, newDir, chainRWeight));

                        if (angle > k_Epsilon)
                        {
                            var axis = Vector3.Cross(prevDir, newDir).normalized;
                            chain[i].SetRotation(stream, Quaternion.AngleAxis(angle, axis) * chain[i].GetRotation(stream));
                        }
                    }
                }

                chain[tipIndex].SetRotation(
                    stream, Quaternion.Lerp(chain[tipIndex].GetRotation(stream), target.GetRotation(stream), cache.GetFloat(tipRotationWeight) * jobWeight)
                    );
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
    }

    public class ChainIKConstraintJobBinder<T> : AnimationJobBinder<ChainIKConstraintJob, T>
        where T : IAnimationJobData, IChainIKConstraintData
    {
        public override ChainIKConstraintJob Create(Animator animator, T data)
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

            var cacheBuilder = new AnimationJobCache.CacheBuilder();
            job.chainRotationWeight = cacheBuilder.Add(data.chainRotationWeight);
            job.tipRotationWeight = cacheBuilder.Add(data.tipRotationWeight);
            job.maxIterations = cacheBuilder.Add(data.maxIterations);
            job.tolerance = cacheBuilder.Add(data.tolerance);
            job.cache = cacheBuilder.Create();

            return job;
        }

        public override void Destroy(ChainIKConstraintJob job)
        {
            job.chain.Dispose();
            job.linkLengths.Dispose();
            job.linkPositions.Dispose();
            job.cache.Dispose();
        }

        public override void Update(T data, ChainIKConstraintJob job)
        {
            job.cache.SetFloat(job.chainRotationWeight, data.chainRotationWeight);
            job.cache.SetFloat(job.tipRotationWeight, data.tipRotationWeight);
            job.cache.SetInt(job.maxIterations, data.maxIterations);
            job.cache.SetFloat(job.tolerance, data.tolerance);
        }
    }
}
