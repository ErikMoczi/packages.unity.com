namespace UnityEngine.Animations.Rigging
{
    [System.Serializable]
    public struct DampedTransformData : IAnimationJobData, IDampedTransformData, IRigReferenceSync
    {
        [SerializeField] JobTransform m_ConstrainedObject;
        [SerializeField] JobTransform m_Source;
        [SerializeField, Range(0f, 1f)] float m_DampPosition;
        [SerializeField, Range(0f, 1f)] float m_DampRotation;
        [SerializeField] bool m_MaintainAim;

        public JobTransform constrainedObject { get => m_ConstrainedObject; set => m_ConstrainedObject = value; }
        public JobTransform sourceObject { get => m_Source; set => m_Source = value; }
        public float dampPosition { get => m_DampPosition; set => m_DampPosition = Mathf.Clamp01(value); }
        public float dampRotation { get => m_DampRotation; set => m_DampRotation = Mathf.Clamp01(value); }
        public bool maintainAim { get => m_MaintainAim; set => m_MaintainAim = value; }

        Transform IDampedTransformData.constrainedObject => m_ConstrainedObject.transform;
        Transform IDampedTransformData.source => m_Source.transform;

        bool IAnimationJobData.IsValid() => !(m_ConstrainedObject.transform == null || m_Source.transform == null);

        void IAnimationJobData.SetDefaultValues()
        {
            m_ConstrainedObject = JobTransform.defaultNoSync;
            m_Source = JobTransform.defaultNoSync;
            m_DampPosition = 0.5f;
            m_DampRotation = 0.5f;
            m_MaintainAim = true;
        }

        JobTransform[] IRigReferenceSync.allReferences => new JobTransform[] { m_ConstrainedObject, m_Source };
    }

    [AddComponentMenu("Animation Rigging/Damped Transform")]
    public class DampedTransform : RuntimeRigConstraint<
        DampedTransformJob,
        DampedTransformData,
        DampedTransformJobBinder<DampedTransformData>
        >
    {
    #if UNITY_EDITOR
    #pragma warning disable 0414
        [SerializeField, HideInInspector] bool m_SourceObjectsGUIToggle;
        [SerializeField, HideInInspector] bool m_SettingsGUIToggle;
    #endif
    }
}