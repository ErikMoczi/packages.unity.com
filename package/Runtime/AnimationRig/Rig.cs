namespace UnityEngine.Animations.Rigging
{
    using Playables;
    using Experimental.Animations;

    [AddComponentMenu("Animation Rigging/Setup/Rig")]
    public class Rig : MonoBehaviour
    {
        [Range(0f, 1f)] public float weight = 1f;

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

        public void UpdateConstraints(AnimationScriptPlayable[] playables, bool enabled)
        {
            if (!isInitialized || playables == null || playables.Length != m_Constraints.Length)
                return;

            var w = weight * System.Convert.ToSingle(enabled);
            for (int i = 0; i < m_Constraints.Length; ++i)
            {
                var constraintWeight = m_Constraints[i].weight * w;
                playables[i].SetInputWeight(0, constraintWeight);

                if (constraintWeight > 0f)
                    m_Constraints[i].UpdateJob(m_Jobs[i]);
            }
        }

        public bool isInitialized { get; private set; }

        public IRigConstraint[] constraints => isInitialized ? m_Constraints : null;

        public IAnimationJob[] jobs => isInitialized ? m_Jobs : null;
    }
}
