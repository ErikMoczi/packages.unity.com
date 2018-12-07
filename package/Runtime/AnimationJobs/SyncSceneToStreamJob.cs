using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct SyncSceneToStreamJob : IAnimationJob
    {
        public NativeArray<TransformSceneHandle> sceneHandles;
        public NativeArray<TransformStreamHandle> streamHandles;
        public BitArray syncSceneToStream;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            for (int i = 0; i < sceneHandles.Length; ++i)
            {
                var sceneHandle = sceneHandles[i];
                if (syncSceneToStream[i] && sceneHandle.IsValid(stream))
                {
                    var streamHandle = streamHandles[i];
                    streamHandle.SetLocalPosition(stream, sceneHandle.GetLocalPosition(stream));
                    streamHandle.SetLocalRotation(stream, sceneHandle.GetLocalRotation(stream));
                    streamHandle.SetLocalScale(stream, sceneHandle.GetLocalScale(stream));
                }
            }
        }
    }

    public interface ISyncSceneToStreamData
    {
        Transform[] objects { get; }
        bool[] sync { get; }
    }

    public class SyncSceneToStreamJobBinder<T> : AnimationJobBinder<SyncSceneToStreamJob, T>
        where T : struct, IAnimationJobData, ISyncSceneToStreamData
    {
        public override SyncSceneToStreamJob Create(Animator animator, ref T data)
        {
            var job = new SyncSceneToStreamJob();

            var objects = data.objects;
            var sync = data.sync;
            job.sceneHandles = new NativeArray<TransformSceneHandle>(objects.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.streamHandles = new NativeArray<TransformStreamHandle>(objects.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.syncSceneToStream = new BitArray(data.sync);

            for (int i = 0; i < objects.Length; ++i)
            {
                job.sceneHandles[i] = animator.BindSceneTransform(objects[i]);
                job.streamHandles[i] = animator.BindStreamTransform(objects[i]);
            }

            return job;
        }

        public override void Destroy(SyncSceneToStreamJob job)
        {
            job.sceneHandles.Dispose();
            job.streamHandles.Dispose();
            job.syncSceneToStream.Dispose();
        }
    }
}