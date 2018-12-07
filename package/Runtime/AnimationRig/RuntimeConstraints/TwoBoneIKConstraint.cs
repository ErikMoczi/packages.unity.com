namespace UnityEngine.Animations.Rigging
{
    [System.Serializable]
    public struct TwoBoneIKConstraintData : IAnimationJobData, ITwoBoneIKConstraintData, IRigReferenceSync
    {
        [SerializeField] JobTransform m_Root;
        [SerializeField] JobTransform m_Mid;
        [SerializeField] JobTransform m_Tip;
        [SerializeField] JobTransform m_Target;
        [SerializeField] JobTransform m_Hint;

        [SerializeField, Range(0f, 1f)] float m_TargetPositionWeight;
        [SerializeField, Range(0f, 1f)] float m_TargetRotationWeight;
        [SerializeField, Range(0f, 1f)] float m_HintWeight;

        public JobTransform root { get => m_Root; set => m_Root = value; }
        public JobTransform mid { get => m_Mid; set => m_Mid = value; }
        public JobTransform tip { get => m_Tip; set => m_Tip = value; }
        public JobTransform target { get => m_Target; set => m_Target = value; }
        public JobTransform hint { get => m_Hint; set => m_Hint = value; }

        public float targetPositionWeight { get => m_TargetPositionWeight; set => m_TargetPositionWeight = Mathf.Clamp01(value); }
        public float targetRotationWeight { get => m_TargetRotationWeight; set => m_TargetRotationWeight = Mathf.Clamp01(value); } 
        public float hintWeight { get => m_HintWeight; set => m_HintWeight = Mathf.Clamp01(value); }

        Transform ITwoBoneIKConstraintData.root => m_Root.transform;
        Transform ITwoBoneIKConstraintData.mid => m_Mid.transform;
        Transform ITwoBoneIKConstraintData.tip => m_Tip.transform;
        Transform ITwoBoneIKConstraintData.target => m_Target.transform;
        Transform ITwoBoneIKConstraintData.hint => m_Hint.transform;

        bool IAnimationJobData.IsValid() => !(m_Tip.transform == null || m_Mid.transform == null || m_Root.transform == null || m_Target.transform == null);

        void IAnimationJobData.SetDefaultValues()
        {
            m_Root = JobTransform.defaultNoSync;
            m_Mid = JobTransform.defaultNoSync;
            m_Tip = JobTransform.defaultNoSync;
            m_Target = JobTransform.defaultSync;
            m_Hint = JobTransform.defaultSync;
            m_TargetPositionWeight = 1f;
            m_TargetRotationWeight = 1f;
            m_HintWeight = 1f;
        }

        JobTransform[] IRigReferenceSync.allReferences
        {
            get
            {
                if (!m_Hint.transform)
                    return new JobTransform[] { m_Root, m_Mid, m_Tip, m_Target };
                else
                    return new JobTransform[] { m_Root, m_Mid, m_Tip, m_Target, m_Hint };
            }
        }
    }

    [AddComponentMenu("Animation Rigging/Two Bone IK Constraint")]
    public class TwoBoneIKConstraint : RuntimeRigConstraint<
        TwoBoneIKConstraintJob,
        TwoBoneIKConstraintData,
        TwoBoneIKConstraintJobBinder<TwoBoneIKConstraintData>
        >
    {
    #if UNITY_EDITOR
    #pragma warning disable 0414
        [SerializeField, HideInInspector] bool m_SourceObjectsGUIToggle;
        [SerializeField, HideInInspector] bool m_SettingsGUIToggle;
    #endif
    }
}