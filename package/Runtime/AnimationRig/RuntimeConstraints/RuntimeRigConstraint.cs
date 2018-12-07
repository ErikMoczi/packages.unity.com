namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public class RuntimeRigConstraint<T> : MonoBehaviour, IRigConstraint
        where T : IAnimationJobData, new()
    {
        [SerializeField, Range(0f, 1f)]
        protected float m_Weight = 1f;

        [SerializeField]
        protected T m_Data;

        public bool IsValid() => data.IsValid();

        public T data
        {
            get
            {
                if (m_Data == null)
                    m_Data = new T();

                return m_Data;
            }

            set => m_Data = value;
        }

        public float weight { get => m_Weight; set => m_Weight = value; }

        public IAnimationJob CreateJob(Animator animator) => data.binder.Create(animator, data);
        public void DestroyJob(IAnimationJob job) => data.binder.Destroy(job);
        public void UpdateJob(IAnimationJob job) => data.binder.Update(data, job);

        IAnimationJobData IRigConstraint.data => data;
    }
}