namespace UnityEngine.Animations.Rigging
{
    [System.Serializable]
    public class ChainIKConstraintData : IAnimationJobData, IChainIKConstraintData, IRigReferenceSync
    {
        [SerializeField] JobTransform m_Root;
        [SerializeField] JobTransform m_Tip;
        [SerializeField] JobTransform m_Target;

        [SerializeField, Range(0f, 1f)] float m_ChainRotationWeight = 1f;
        [SerializeField, Range(0f, 1f)] float m_TipRotationWeight = 1f;
        [SerializeField, Range(1, 50)] int m_MaxIterations = 15;
        [SerializeField, Range(0f, 0.01f)] float m_Tolerance = 0.001f;

        public ChainIKConstraintData()
        {
            m_Target = JobTransform.defaultSync;
        }

        public JobTransform root { get => m_Root; set => m_Root = value; }
        public JobTransform tip { get => m_Tip; set => m_Tip = value; }
        public JobTransform target { get => m_Target; set => m_Target = value; }
        public float chainRotationWeight { get => m_ChainRotationWeight; set => m_ChainRotationWeight = Mathf.Clamp01(value); }
        public float tipRotationWeight { get => m_TipRotationWeight; set => m_TipRotationWeight = Mathf.Clamp01(value); }
        public int maxIterations { get => m_MaxIterations; set => m_MaxIterations = Mathf.Clamp(value, 1, 50); }
        public float tolerance { get => m_Tolerance; set => m_Tolerance = Mathf.Clamp(value, 0f, 0.01f); }

        Transform IChainIKConstraintData.root => m_Root.transform;
        Transform IChainIKConstraintData.tip => m_Tip.transform;
        Transform IChainIKConstraintData.target => m_Target.transform;

        bool IAnimationJobData.IsValid()
        {
            if (m_Root.transform == null || m_Tip.transform == null || m_Target.transform == null)
                return false;

            int count = 1;
            Transform tmp = m_Tip.transform;
            while (tmp != null && tmp != m_Root.transform)
            {
                tmp = tmp.parent;
                ++count;
            }

            return (tmp.transform == m_Root.transform && count > 2);
        }

        IAnimationJobBinder IAnimationJobData.binder { get; } = new ChainIKConstraintJobBinder<ChainIKConstraintData>();

        JobTransform[] IRigReferenceSync.allReferences => new JobTransform[] { m_Root, m_Tip, m_Target };
    }

    [AddComponentMenu("Runtime Rigging/Chain IK Constraint")]
    public class ChainIKConstraint : RuntimeRigConstraint<ChainIKConstraintData>
    {
    #if UNITY_EDITOR
    #pragma warning disable 0414
        [SerializeField, HideInInspector] bool m_SourceObjectsGUIToggle;
        [SerializeField, HideInInspector] bool m_SettingsGUIToggle;
    #endif
    }
}