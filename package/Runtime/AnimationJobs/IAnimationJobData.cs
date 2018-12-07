namespace UnityEngine.Animations.Rigging
{
    public interface IAnimationJobData
    {
        bool IsValid();
        IAnimationJobBinder binder { get; }
    }
}