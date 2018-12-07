namespace UnityEngine.Animations.Rigging
{
    [System.Serializable]
    public struct OverrideTransformData : IAnimationJobData, IOverrideTransformData, IRigReferenceSync
    {
        [System.Serializable]
        public enum Space
        {
            World = OverrideTransformJob.Space.World,
            Local = OverrideTransformJob.Space.Local,
            Pivot = OverrideTransformJob.Space.Pivot
        }

        [SerializeField] JobTransform m_ConstrainedObject;
        [SerializeField] JobTransform m_OverrideSource;
        [SerializeField] Vector3 m_OverridePosition;
        [SerializeField] Vector3 m_OverrideRotation;
        [SerializeField] Space m_Space;
        [SerializeField, Range(0f, 1f)] float m_PositionWeight;
        [SerializeField, Range(0f, 1f)] float m_RotationWeight;

        public JobTransform constrainedObject { get => m_ConstrainedObject; set => m_ConstrainedObject = value; }
        public JobTransform sourceObject { get => m_OverrideSource; set => m_OverrideSource = value; }
        public Space space { get => m_Space; set => m_Space = value; }
        public Vector3 position { get => m_OverridePosition; set => m_OverridePosition = value; }
        public Vector3 rotation { get => m_OverrideRotation; set => m_OverrideRotation = value; }
        public float positionWeight { get => m_PositionWeight; set => m_PositionWeight = Mathf.Clamp01(value); }
        public float rotationWeight { get => m_RotationWeight; set => m_RotationWeight = Mathf.Clamp01(value); }

        Transform IOverrideTransformData.constrainedObject => m_ConstrainedObject.transform;
        Transform IOverrideTransformData.source => m_OverrideSource.transform;
        int IOverrideTransformData.space => (int)m_Space;

        bool IAnimationJobData.IsValid() => m_ConstrainedObject.transform != null;

        void IAnimationJobData.SetDefaultValues()
        {
            m_ConstrainedObject = JobTransform.defaultNoSync;
            m_OverrideSource = JobTransform.defaultNoSync;
            m_OverridePosition = Vector3.zero;
            m_OverrideRotation = Vector3.zero;
            m_Space = Space.Pivot;
            m_PositionWeight = 1f;
            m_RotationWeight = 1f;
        }

        JobTransform[] IRigReferenceSync.allReferences
        {
            get
            {
                if (m_OverrideSource.transform != null)
                    return new JobTransform[] { m_ConstrainedObject, m_OverrideSource };

                return new JobTransform[] { m_ConstrainedObject };
            }
        }
    }

    [AddComponentMenu("Animation Rigging/Override Transform")]
    public class OverrideTransform : RuntimeRigConstraint<
        OverrideTransformJob,
        OverrideTransformData,
        OverrideTransformJobBinder<OverrideTransformData>
        >
    {
    #if UNITY_EDITOR
    #pragma warning disable 0414
        [SerializeField, HideInInspector] bool m_SourceObjectsGUIToggle;
        [SerializeField, HideInInspector] bool m_SettingsGUIToggle;
    #endif
    }
}