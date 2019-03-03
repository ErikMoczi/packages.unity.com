namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    [DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Setup/Rig")]
    public class Rig : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)]
        protected float m_Weight = 1f;

        private IRigConstraint[] m_Constraints;
        private IAnimationJob[]  m_Jobs;

        public bool Initialize(Animator animator)
        {
            if (isInitialized)
                return true;

            m_Constraints = RigUtils.GetConstraints(this);
            if (m_Constraints == null)
                return false;

            m_Jobs = RigUtils.CreateAnimationJobs(animator, m_Constraints);

            return (isInitialized = true);
        }

        public void Destroy()
        {
            if (!isInitialized)
                return;

            RigUtils.DestroyAnimationJobs(m_Constraints, m_Jobs);
            m_Constraints = null;
            m_Jobs = null;

            isInitialized = false;
        }

        public void UpdateConstraints()
        {
            if (!isInitialized)
                return;

            for (int i = 0, count = m_Constraints.Length; i < count; ++i)
                m_Constraints[i].UpdateJob(m_Jobs[i]);
        }

        public bool isInitialized { get; private set; }

        public float weight { get => m_Weight; set => m_Weight = Mathf.Clamp01(value); }

        public IRigConstraint[] constraints => isInitialized ? m_Constraints : null;

        public IAnimationJob[] jobs => isInitialized ? m_Jobs : null;
    }
}
