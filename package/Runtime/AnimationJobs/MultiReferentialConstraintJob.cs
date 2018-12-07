using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct MultiReferentialConstraintJob : IAnimationJob
    {
        public CacheIndex driverIdx;
        public NativeArray<TransformHandle> sources;
        public NativeArray<AffineTransform> sourceBindTx;
        public NativeArray<AffineTransform> offsetTx;

        public AnimationJobCache cache;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float jobWeight = stream.GetInputWeight(0);
            if (jobWeight > 0f)
            {
                int driver = (int)cache.GetRaw(driverIdx);
                var driverTx = new AffineTransform(
                    sources[driver].GetPosition(stream),
                    sources[driver].GetRotation(stream)
                    );

                int offset = 0;
                for (int i = 0; i < sources.Length; ++i)
                {
                    if (i == driver)
                        continue;

                    var tx = driverTx * offsetTx[offset];
                    sources[i].SetPosition(stream, Vector3.Lerp(sources[i].GetPosition(stream), tx.translation, jobWeight));
                    sources[i].SetRotation(stream, Quaternion.Lerp(sources[i].GetRotation(stream), tx.rotation, jobWeight));
                    offset++;
                }
            }
            else
            {
                for (int i = 0; i < sources.Length; ++i)
                    AnimationRuntimeUtils.PassThrough(stream, sources[i]);
            }
        }

        public void UpdateOffsets()
        {
            int offset = 0;
            int driver = (int)cache.GetRaw(driverIdx);
            var invDriverTx = sourceBindTx[driver].Inverse();
            for (int i = 0; i < sourceBindTx.Length; ++i)
            {
                if (i == driver)
                    continue;

                offsetTx[offset] = invDriverTx * sourceBindTx[i];
                offset++;
            }
        }
    }

    public interface IMultiReferentialConstraintData
    {
        int driver { get; }
        Transform[] sourceObjects { get; }
    }

    public class MultiReferentialConstraintJobBinder<T> : AnimationJobBinder<MultiReferentialConstraintJob, T>
        where T : struct, IAnimationJobData, IMultiReferentialConstraintData
    {
        public override MultiReferentialConstraintJob Create(Animator animator, ref T data)
        {
            var job = new MultiReferentialConstraintJob();
            var cacheBuilder = new AnimationJobCacheBuilder();

            job.driverIdx = cacheBuilder.Add(data.driver);

            var sources = data.sourceObjects;
            job.sources = new NativeArray<TransformHandle>(sources.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceBindTx = new NativeArray<AffineTransform>(sources.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.offsetTx = new NativeArray<AffineTransform>(sources.Length - 1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < sources.Length; ++i)
            {
                job.sources[i] = TransformHandle.Bind(animator, sources[i].transform);
                job.sourceBindTx[i] = new AffineTransform(sources[i].position, sources[i].rotation);
            }
            job.cache = cacheBuilder.Build();
            job.UpdateOffsets();

            return job;
        }

        public override void Destroy(MultiReferentialConstraintJob job)
        {
            job.sources.Dispose();
            job.sourceBindTx.Dispose();
            job.offsetTx.Dispose();
            job.cache.Dispose();
        }

        public override void Update(MultiReferentialConstraintJob job, ref T data)
        {
            if (data.driver != (int)job.cache.GetRaw(job.driverIdx))
            {
                job.cache.SetRaw(data.driver, job.driverIdx);
                job.UpdateOffsets();
            }
        }
    }
}