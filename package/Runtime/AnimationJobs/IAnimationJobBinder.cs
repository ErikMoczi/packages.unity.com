namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;
    using Playables;

    public interface IAnimationJobBinder
    {
        IAnimationJob Create(Animator animator, IAnimationJobData data);
        void Destroy(IAnimationJob job);
        void Update(IAnimationJob job, IAnimationJobData data);
        AnimationScriptPlayable CreatePlayable(PlayableGraph graph, IAnimationJob job);
    }

    public abstract class AnimationJobBinder<TJob, TData> : IAnimationJobBinder
        where TJob : struct, IAnimationJob
        where TData : struct, IAnimationJobData
    {
        public abstract TJob Create(Animator animator, ref TData data);

        public abstract void Destroy(TJob job);

        public virtual void Update(TJob job, ref TData data) {}

        IAnimationJob IAnimationJobBinder.Create(Animator animator, IAnimationJobData data)
        {
            Debug.Assert(data is TData);
            TData tData = (TData)data;
            return Create(animator, ref tData);
        }

        void IAnimationJobBinder.Destroy(IAnimationJob job)
        {
            Debug.Assert(job is TJob);
            Destroy((TJob)job);
        }

        void IAnimationJobBinder.Update(IAnimationJob job, IAnimationJobData data)
        {
            Debug.Assert(data is TData && job is TJob);
            TData tData = (TData)data;
            Update((TJob)job, ref tData);
        }

        AnimationScriptPlayable IAnimationJobBinder.CreatePlayable(PlayableGraph graph, IAnimationJob job)
        {
            Debug.Assert(job is TJob);
            return AnimationScriptPlayable.Create(graph, (TJob)job);
        }
    }
}
