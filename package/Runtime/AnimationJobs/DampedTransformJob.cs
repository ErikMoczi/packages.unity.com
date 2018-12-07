namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct DampedTransformJob : IAnimationJob
    {
        static readonly float k_DampFactor = 30f;

        public TransformHandle driven;
        public TransformHandle source;
        public AffineTransform localBindTx;
        public AnimationJobCache.Index dampPosition;
        public AnimationJobCache.Index dampRotation;
        public Vector3 aimBindAxis;

        public AffineTransform prevDrivenTx;

        public AnimationJobCache.Cache cache;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float jobWeight = stream.GetInputWeight(0);
            if (jobWeight > 0f)
            {
                AffineTransform sourceTx = new AffineTransform(source.GetPosition(stream), source.GetRotation(stream));
                AffineTransform targetTx = sourceTx * localBindTx;

                Vector3 pos = Vector3.Lerp(prevDrivenTx.translation, targetTx.translation, k_DampFactor * cache.GetFloat(dampPosition) * stream.deltaTime);
                pos = Vector3.Lerp(driven.GetPosition(stream), pos, jobWeight);

                Quaternion drivenRot = driven.GetRotation(stream);
                if (Vector3.Dot(aimBindAxis, aimBindAxis) > 0f)
                {
                    var fromDir = drivenRot * aimBindAxis;
                    var toDir = sourceTx.translation - pos;
                    targetTx.rotation = Quaternion.AngleAxis(Vector3.Angle(fromDir, toDir), Vector3.Cross(fromDir, toDir).normalized) * drivenRot;
                }

                Quaternion rot = Quaternion.Lerp(prevDrivenTx.rotation, targetTx.rotation, k_DampFactor * cache.GetFloat(dampRotation) * stream.deltaTime);
                rot = Quaternion.Lerp(drivenRot, rot, jobWeight);

                driven.SetPosition(stream, pos);
                driven.SetRotation(stream, rot);
                prevDrivenTx.translation = pos;
                prevDrivenTx.rotation = rot;
            }
        }
    }

    public interface IDampedTransformData
    {
        Transform constrainedObject { get; }
        Transform source { get; }
        float dampPosition { get; }
        float dampRotation { get; }
        bool maintainAim { get; }
    }

    public class DampedTransformJobBinder<T> : AnimationJobBinder<DampedTransformJob, T>
        where T : IAnimationJobData, IDampedTransformData
    {
        public override DampedTransformJob Create(Animator animator, T data)
        {
            var job = new DampedTransformJob();
            var cacheBuilder = new AnimationJobCache.CacheBuilder();

            job.driven = TransformHandle.Bind(animator, data.constrainedObject);
            job.source = TransformHandle.Bind(animator, data.source);

            var drivenTx = new AffineTransform(data.constrainedObject.position, data.constrainedObject.rotation);
            var sourceTx = new AffineTransform(data.source.position, data.source.rotation);

            job.localBindTx = sourceTx.InverseMul(drivenTx);
            job.prevDrivenTx = drivenTx;
            job.dampPosition = cacheBuilder.Add(data.dampPosition);
            job.dampRotation = cacheBuilder.Add(data.dampRotation);

            if (data.maintainAim && AnimationRuntimeUtils.SqrDistance(data.constrainedObject.position, data.source.position) > 0f)
                job.aimBindAxis = Quaternion.Inverse(data.constrainedObject.rotation) * (sourceTx.translation - drivenTx.translation).normalized;
            else
                job.aimBindAxis = Vector3.zero;

            job.cache = cacheBuilder.Create();
            return job;
        }

        public override void Destroy(DampedTransformJob job)
        {
            job.cache.Dispose();
        }

        public override void Update(T data, DampedTransformJob job)
        {
            job.cache.SetFloat(job.dampPosition, data.dampPosition);
            job.cache.SetFloat(job.dampRotation, data.dampRotation);
        }
    }
}