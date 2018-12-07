namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;
    using Playables;

    public interface IAnimationJobBinder
    {
        IAnimationJob Create(Animator animator, IAnimationJobData data);
        void Destroy(IAnimationJob job);
        void Update(IAnimationJobData data, IAnimationJob job);
        AnimationScriptPlayable CreatePlayable(PlayableGraph graph, IAnimationJob job);
    }

    public abstract class AnimationJobBinder<TJob, TData> : IAnimationJobBinder
        where TJob : struct, IAnimationJob
        where TData : IAnimationJobData
    {
        public abstract TJob Create(Animator animator, TData data);

        public abstract void Destroy(TJob job);

        public virtual void Update(TData data, TJob job) {}

        IAnimationJob IAnimationJobBinder.Create(Animator animator, IAnimationJobData data)
        {
            Debug.Assert(data is TData);
            return Create(animator, (TData)data);
        }

        void IAnimationJobBinder.Destroy(IAnimationJob job)
        {
            Debug.Assert(job is TJob);
            Destroy((TJob)job);
        }

        void IAnimationJobBinder.Update(IAnimationJobData data, IAnimationJob job)
        {
            Debug.Assert(data is TData && job is TJob);
            Update((TData)data, (TJob)job);
        }

        AnimationScriptPlayable IAnimationJobBinder.CreatePlayable(PlayableGraph graph, IAnimationJob job)
        {
            Debug.Assert(job is TJob);
            return AnimationScriptPlayable.Create(graph, (TJob)job);
        }
    }
}
