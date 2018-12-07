namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public class RuntimeRigConstraint<TJob, TData, TBinder> : MonoBehaviour, IRigConstraint
        where TJob : struct, IAnimationJob
        where TData : struct, IAnimationJobData
        where TBinder : AnimationJobBinder<TJob, TData>, new()
    {
        [SerializeField, Range(0f, 1f)]
        protected float m_Weight = 1f;

        [SerializeField]
        protected TData m_Data;

        static readonly TBinder s_Binder = new TBinder();

        public void Reset()
        {
            m_Weight = 1f;
            m_Data.SetDefaultValues();
        }

        public bool IsValid() => m_Data.IsValid();

        public ref TData data => ref m_Data;

        public float weight { get => m_Weight; set => m_Weight = value; }

        public IAnimationJob CreateJob(Animator animator) => s_Binder.Create(animator, ref m_Data);

        public void DestroyJob(IAnimationJob job) => s_Binder.Destroy((TJob)job);

        public void UpdateJob(IAnimationJob job) => s_Binder.Update((TJob)job, ref m_Data);

        IAnimationJobBinder IRigConstraint.binder => s_Binder;
        IAnimationJobData IRigConstraint.data => m_Data;
    }
}