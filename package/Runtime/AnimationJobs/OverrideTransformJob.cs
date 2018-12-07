namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct OverrideTransformJob : IAnimationJob
    {
        public enum Space
        {
            World = 0,
            Local = 1,
            Pivot = 2
        }

        public TransformHandle driven;
        public TransformHandle source;
        public AffineTransform sourceInvLocalBindTx;

        public Quaternion sourceToWorldRot;
        public Quaternion sourceToLocalRot;
        public Quaternion sourceToPivotRot;

        public CacheIndex spaceIdx;
        public CacheIndex sourceToCurrSpaceRotIdx;
        public CacheIndex positionIdx;
        public CacheIndex rotationIdx;
        public CacheIndex positionWeightIdx;
        public CacheIndex rotationWeightIdx;

        public AnimationJobCache cache;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float jobWeight = stream.GetInputWeight(0);
            if (jobWeight > 0f)
            {
                AffineTransform overrideTx;
                if (source.IsValid(stream))
                {
                    var sourceLocalTx = new AffineTransform(source.GetLocalPosition(stream), source.GetLocalRotation(stream));
                    var sourceToSpaceRot = cache.Get<Quaternion>(sourceToCurrSpaceRotIdx);
                    overrideTx = Quaternion.Inverse(sourceToSpaceRot) * (sourceInvLocalBindTx * sourceLocalTx) * sourceToSpaceRot;
                }
                else
                    overrideTx = new AffineTransform(cache.Get<Vector3>(positionIdx), Quaternion.Euler(cache.Get<Vector3>(rotationIdx)));

                Space overrideSpace = (Space)cache.GetRaw(spaceIdx);
                var posW = cache.GetRaw(positionWeightIdx) * jobWeight;
                var rotW = cache.GetRaw(rotationWeightIdx) * jobWeight;
                switch (overrideSpace)
                {
                    case Space.World:
                        driven.SetPosition(stream, Vector3.Lerp(driven.GetPosition(stream), overrideTx.translation, posW));
                        driven.SetRotation(stream, Quaternion.Lerp(driven.GetRotation(stream), overrideTx.rotation, rotW));
                        break;
                    case Space.Local:
                        driven.SetLocalPosition(stream, Vector3.Lerp(driven.GetLocalPosition(stream), overrideTx.translation, posW));
                        driven.SetLocalRotation(stream, Quaternion.Lerp(driven.GetLocalRotation(stream), overrideTx.rotation, rotW));
                        break;
                    case Space.Pivot:
                        var drivenLocalTx = new AffineTransform(driven.GetLocalPosition(stream), driven.GetLocalRotation(stream));
                        overrideTx = drivenLocalTx * overrideTx;
                        driven.SetLocalPosition(stream, Vector3.Lerp(drivenLocalTx.translation, overrideTx.translation, posW));
                        driven.SetLocalRotation(stream, Quaternion.Lerp(drivenLocalTx.rotation, overrideTx.rotation, rotW));
                        break;
                    default:
                        break;
                }
            }
            else
                AnimationRuntimeUtils.PassThrough(stream, driven);
        }

        public void UpdateSpace(int space)
        {
            if ((int)cache.GetRaw(spaceIdx) == space)
                return;

            cache.SetRaw(space, spaceIdx);

            Space currSpace = (Space)space;
            if (currSpace == Space.Pivot)
                cache.Set(sourceToPivotRot, sourceToCurrSpaceRotIdx);
            else if (currSpace == Space.Local)
                cache.Set(sourceToLocalRot, sourceToCurrSpaceRotIdx);
            else
                cache.Set(sourceToWorldRot, sourceToCurrSpaceRotIdx);
        }
    }

    public interface IOverrideTransformData
    {
        Transform constrainedObject { get; }
        Transform source { get; }
        Vector3 position { get; }
        Vector3 rotation { get; }
        int space { get; }
        float positionWeight { get; }
        float rotationWeight { get; }
    }

    public class OverrideTransformJobBinder<T> : AnimationJobBinder<OverrideTransformJob, T>
        where T : struct, IAnimationJobData, IOverrideTransformData
    {
        public override OverrideTransformJob Create(Animator animator, ref T data)
        {
            var job = new OverrideTransformJob();
            var cacheBuilder = new AnimationJobCacheBuilder();

            job.driven = TransformHandle.Bind(animator, data.constrainedObject);

            if (data.source != null)
            {
                // Cache source to possible space rotation offsets (world, local and pivot)
                // at bind time so we can switch dynamically between them at runtime.

                job.source = TransformHandle.Bind(animator, data.source);
                var sourceLocalTx = new AffineTransform(data.source.localPosition, data.source.localRotation);
                job.sourceInvLocalBindTx = sourceLocalTx.Inverse();

                var sourceWorldTx = new AffineTransform(data.source.position, data.source.rotation);
                var drivenWorldTx = new AffineTransform(data.constrainedObject.position, data.constrainedObject.rotation);
                job.sourceToWorldRot = sourceWorldTx.Inverse().rotation;
                job.sourceToPivotRot = sourceWorldTx.InverseMul(drivenWorldTx).rotation;

                var drivenParent = data.constrainedObject.parent;
                if (drivenParent != null)
                {
                    var drivenParentWorldTx = new AffineTransform(drivenParent.position, drivenParent.rotation); 
                    job.sourceToLocalRot = sourceWorldTx.InverseMul(drivenParentWorldTx).rotation;
                }
                else
                    job.sourceToLocalRot = job.sourceToPivotRot;
            }

            job.spaceIdx = cacheBuilder.Add(data.space);
            if (data.space == (int)OverrideTransformJob.Space.Pivot)
                job.sourceToCurrSpaceRotIdx = cacheBuilder.Add(job.sourceToPivotRot);
            else if (data.space == (int)OverrideTransformJob.Space.Local)
                job.sourceToCurrSpaceRotIdx = cacheBuilder.Add(job.sourceToLocalRot);
            else
                job.sourceToCurrSpaceRotIdx = cacheBuilder.Add(job.sourceToWorldRot);

            job.positionIdx = cacheBuilder.Add(data.position);
            job.rotationIdx = cacheBuilder.Add(data.rotation);
            job.positionWeightIdx = cacheBuilder.Add(data.positionWeight);
            job.rotationWeightIdx = cacheBuilder.Add(data.rotationWeight);
            job.cache = cacheBuilder.Build();

            return job;
        }

        public override void Destroy(OverrideTransformJob job)
        {
            job.cache.Dispose();
        }

        public override void Update(OverrideTransformJob job, ref T data)
        {
            job.UpdateSpace(data.space);
            job.cache.Set(data.position, job.positionIdx);
            job.cache.Set(data.rotation, job.rotationIdx);
            job.cache.SetRaw(data.positionWeight, job.positionWeightIdx);
            job.cache.SetRaw(data.rotationWeight, job.rotationWeightIdx);
        }
    }
}