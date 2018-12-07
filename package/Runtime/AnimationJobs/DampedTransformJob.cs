namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct DampedTransformJob : IAnimationJob
    {
        const float k_DampFactor = 40f;

        public TransformHandle driven;
        public TransformHandle source;
        public AffineTransform localBindTx;

        public Vector3 aimBindAxis;
        public AffineTransform prevDrivenTx;

        public CacheIndex dampPositionIdx;
        public CacheIndex dampRotationIdx;
        public AnimationJobCache cache;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float jobWeight = stream.GetInputWeight(0);
            if (jobWeight > 0f)
            {
                var sourceTx = new AffineTransform(source.GetPosition(stream), source.GetRotation(stream));
                var targetTx = sourceTx * localBindTx;

                var drivenPos = driven.GetPosition(stream);
                targetTx.translation = Vector3.Lerp(drivenPos, targetTx.translation, jobWeight);
                var factorDeltaTime = k_DampFactor * stream.deltaTime;
                var dampPosW = 1f - cache.GetRaw(dampPositionIdx);
                var finalPos = Vector3.Lerp(prevDrivenTx.translation, targetTx.translation, dampPosW * dampPosW * factorDeltaTime);

                var drivenRot = driven.GetRotation(stream);
                if (Vector3.Dot(aimBindAxis, aimBindAxis) > 0f)
                {
                    var fromDir = drivenRot * aimBindAxis;
                    var toDir = sourceTx.translation - finalPos;
                    targetTx.rotation = Quaternion.AngleAxis(Vector3.Angle(fromDir, toDir), Vector3.Cross(fromDir, toDir).normalized) * drivenRot;
                }
                targetTx.rotation = Quaternion.Lerp(drivenRot, targetTx.rotation, jobWeight);
                var dampRotW = 1f - cache.GetRaw(dampRotationIdx);
                var finalRot = Quaternion.Lerp(prevDrivenTx.rotation, targetTx.rotation, dampRotW * dampRotW * factorDeltaTime);

                driven.SetPosition(stream, finalPos);
                driven.SetRotation(stream, finalRot);
                prevDrivenTx.translation = finalPos;
                prevDrivenTx.rotation = finalRot;
            }
            else
                AnimationRuntimeUtils.PassThrough(stream, driven);
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
        where T : struct, IAnimationJobData, IDampedTransformData
    {
        public override DampedTransformJob Create(Animator animator, ref T data)
        {
            var job = new DampedTransformJob();
            var cacheBuilder = new AnimationJobCacheBuilder();

            job.driven = TransformHandle.Bind(animator, data.constrainedObject);
            job.source = TransformHandle.Bind(animator, data.source);

            var drivenTx = new AffineTransform(data.constrainedObject.position, data.constrainedObject.rotation);
            var sourceTx = new AffineTransform(data.source.position, data.source.rotation);

            job.localBindTx = sourceTx.InverseMul(drivenTx);
            job.prevDrivenTx = drivenTx;
            job.dampPositionIdx = cacheBuilder.Add(data.dampPosition);
            job.dampRotationIdx = cacheBuilder.Add(data.dampRotation);

            if (data.maintainAim && AnimationRuntimeUtils.SqrDistance(data.constrainedObject.position, data.source.position) > 0f)
                job.aimBindAxis = Quaternion.Inverse(data.constrainedObject.rotation) * (sourceTx.translation - drivenTx.translation).normalized;
            else
                job.aimBindAxis = Vector3.zero;

            job.cache = cacheBuilder.Build();
            return job;
        }

        public override void Destroy(DampedTransformJob job)
        {
            job.cache.Dispose();
        }

        public override void Update(DampedTransformJob job, ref T data)
        {
            job.cache.SetRaw(data.dampPosition, job.dampPositionIdx);
            job.cache.SetRaw(data.dampRotation, job.dampRotationIdx);
        }
    }
}