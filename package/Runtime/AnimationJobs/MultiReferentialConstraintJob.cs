using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct MultiReferentialConstraintJob : IAnimationJob
    {
        public AnimationJobCache.Index driver;
        public NativeArray<TransformHandle> sources;
        public NativeArray<AffineTransform> sourceBindTx;
        public NativeArray<AffineTransform> offsetTx;

        public AnimationJobCache.Cache cache;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float jobWeight = stream.GetInputWeight(0);
            if (jobWeight > 0f)
            {
                int driverIdx = cache.GetInt(driver);
                var driverTx = new AffineTransform(
                    sources[driverIdx].GetPosition(stream),
                    sources[driverIdx].GetRotation(stream)
                    );

                int offset = 0;
                for (int i = 0; i < sources.Length; ++i)
                {
                    if (i == driverIdx)
                        continue;

                    var tx = driverTx * offsetTx[offset];
                    sources[i].SetPosition(stream, Vector3.Lerp(sources[i].GetPosition(stream), tx.translation, jobWeight));
                    sources[i].SetRotation(stream, Quaternion.Lerp(sources[i].GetRotation(stream), tx.rotation, jobWeight));
                    offset++;
                }
            }
        }

        public void UpdateOffsets()
        {
            int offset = 0;
            int driverIdx = cache.GetInt(driver);
            var invDriverTx = sourceBindTx[driverIdx].Inverse();
            for (int i = 0; i < sourceBindTx.Length; ++i)
            {
                if (i == driverIdx)
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
        where T : IAnimationJobData, IMultiReferentialConstraintData
    {
        public override MultiReferentialConstraintJob Create(Animator animator, T data)
        {
            var job = new MultiReferentialConstraintJob();
            var cacheBuilder = new AnimationJobCache.CacheBuilder();

            job.driver = cacheBuilder.Add(data.driver);

            var sources = data.sourceObjects;
            job.sources = new NativeArray<TransformHandle>(sources.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceBindTx = new NativeArray<AffineTransform>(sources.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.offsetTx = new NativeArray<AffineTransform>(sources.Length - 1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < sources.Length; ++i)
            {
                job.sources[i] = TransformHandle.Bind(animator, sources[i].transform);
                job.sourceBindTx[i] = new AffineTransform(sources[i].position, sources[i].rotation);
            }
            job.cache = cacheBuilder.Create();
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

        public override void Update(T data, MultiReferentialConstraintJob job)
        {
            if (data.driver != job.cache.GetInt(job.driver))
            {
                job.cache.SetInt(job.driver, data.driver);
                job.UpdateOffsets();
            }
        }
    }
}