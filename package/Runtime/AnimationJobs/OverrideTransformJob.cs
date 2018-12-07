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

        public AnimationJobCache.Index space;
        public AnimationJobCache.Index sourceToCurrSpaceRot;
        public AnimationJobCache.Index position;
        public AnimationJobCache.Index rotation;
        public AnimationJobCache.Index positionWeight;
        public AnimationJobCache.Index rotationWeight;

        public AnimationJobCache.Cache cache;

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
                    var sourceToSpaceRot = cache.GetQuaternion(sourceToCurrSpaceRot);
                    overrideTx = Quaternion.Inverse(sourceToSpaceRot) * (sourceInvLocalBindTx * sourceLocalTx) * sourceToSpaceRot;
                }
                else
                    overrideTx = new AffineTransform(cache.GetVector3(position), Quaternion.Euler(cache.GetVector3(rotation)));

                Space overrideSpace = (Space)cache.GetInt(space);
                var posW = cache.GetFloat(positionWeight) * jobWeight;
                var rotW = cache.GetFloat(rotationWeight) * jobWeight;
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
        }

        public void UpdateSpace(int newSpace)
        {
            if (cache.GetInt(space) == newSpace)
                return;

            cache.SetInt(space, newSpace);

            Space currSpace = (Space)newSpace;
            if (currSpace == Space.Pivot)
                cache.SetQuaternion(sourceToCurrSpaceRot, sourceToPivotRot);
            else if (currSpace == Space.Local)
                cache.SetQuaternion(sourceToCurrSpaceRot, sourceToLocalRot);
            else
                cache.SetQuaternion(sourceToCurrSpaceRot, sourceToWorldRot);
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
        where T : IAnimationJobData, IOverrideTransformData
    {
        public override OverrideTransformJob Create(Animator animator, T data)
        {
            var job = new OverrideTransformJob();
            var cacheBuilder = new AnimationJobCache.CacheBuilder();

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

            job.space = cacheBuilder.Add(data.space);
            if (data.space == (int)OverrideTransformJob.Space.Pivot)
                job.sourceToCurrSpaceRot = cacheBuilder.Add(job.sourceToPivotRot);
            else if (data.space == (int)OverrideTransformJob.Space.Local)
                job.sourceToCurrSpaceRot = cacheBuilder.Add(job.sourceToLocalRot);
            else
                job.sourceToCurrSpaceRot = cacheBuilder.Add(job.sourceToWorldRot);

            job.position = cacheBuilder.Add(data.position);
            job.rotation = cacheBuilder.Add(data.rotation);
            job.positionWeight = cacheBuilder.Add(data.positionWeight);
            job.rotationWeight = cacheBuilder.Add(data.rotationWeight);
            job.cache = cacheBuilder.Create();

            return job;
        }

        public override void Destroy(OverrideTransformJob job)
        {
            job.cache.Dispose();
        }

        public override void Update(T data, OverrideTransformJob job)
        {
            job.UpdateSpace(data.space);
            job.cache.SetVector3(job.position, data.position);
            job.cache.SetVector3(job.rotation, data.rotation);
            job.cache.SetFloat(job.positionWeight, data.positionWeight);
            job.cache.SetFloat(job.rotationWeight, data.rotationWeight);
        }
    }
}