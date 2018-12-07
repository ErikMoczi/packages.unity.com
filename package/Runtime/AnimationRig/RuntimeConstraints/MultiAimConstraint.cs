using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging
{
    using RuntimeConstraints;

    [System.Serializable]
    public struct MultiAimConstraintData : IAnimationJobData, IMultiAimConstraintData, IRigReferenceSync
    {
        public enum Axis { X, X_NEG, Y, Y_NEG, Z, Z_NEG }

        [SerializeField] JobTransform m_ConstrainedObject;
        [SerializeField] Axis m_AimAxis;
        [SerializeField] List<WeightedJobTransform> m_SourceObjects;
        [SerializeField] bool m_MaintainOffset;
        [SerializeField] Vector3 m_Offset;
        [SerializeField] Vector3Bool m_ConstrainedAxes;
        [SerializeField, Range(-180f, 180f)] float m_MinLimit;
        [SerializeField, Range(-180f, 180f)] float m_MaxLimit;
    
        // Since source weights can be updated at runtime keep a local cache instead of
        // extracting these constantly
        private WeightCache m_SrcWeightCache;

        public JobTransform constrainedObject { get => m_ConstrainedObject; set => m_ConstrainedObject = value; }

        public List<WeightedJobTransform> sourceObjects
        {
            get
            {
                if (m_SourceObjects == null)
                    m_SourceObjects = new List<WeightedJobTransform>();

                return m_SourceObjects;
            }

            set
            {
                m_SourceObjects = value;
                m_SrcWeightCache.MarkDirty();
            }
        }

        public bool maintainOffset { get => m_MaintainOffset; set => m_MaintainOffset = value; }
        public Vector3 offset { get => m_Offset; set => m_Offset = value; }

        public Vector2 limits
        {
            get => new Vector2(m_MinLimit, m_MaxLimit);

            set
            {
                m_MinLimit = Mathf.Clamp(value.x, -180f, 180f);
                m_MaxLimit = Mathf.Clamp(value.y, -180f, 180f);
            }
        }

        public Axis aimAxis { get => m_AimAxis; set => m_AimAxis = value; }

        public bool constrainedXAxis { get => m_ConstrainedAxes.x; set => m_ConstrainedAxes.x = value; }
        public bool constrainedYAxis { get => m_ConstrainedAxes.y; set => m_ConstrainedAxes.y = value; }
        public bool constrainedZAxis { get => m_ConstrainedAxes.z; set => m_ConstrainedAxes.z = value; }

        Transform IMultiAimConstraintData.constrainedObject => m_ConstrainedObject.transform;
        Transform[] IMultiAimConstraintData.sourceObjects => ConstraintDataUtils.GetTransforms(m_SourceObjects);
        float[] IMultiAimConstraintData.sourceWeights => m_SrcWeightCache.GetWeights(m_SourceObjects);
        Vector3 IMultiAimConstraintData.aimAxis => Convert(m_AimAxis);

        bool IAnimationJobData.IsValid()
        {
            if (m_ConstrainedObject.transform == null || m_SourceObjects == null || m_SourceObjects.Count == 0)
                return false;

            foreach (var src in m_SourceObjects)
                if (src.transform == null)
                    return false;

            return true;
        }

        void IAnimationJobData.SetDefaultValues()
        {
            m_ConstrainedObject = JobTransform.defaultNoSync;
            m_AimAxis = Axis.X;
            m_SourceObjects = new List<WeightedJobTransform>();
            m_MaintainOffset = true;
            m_Offset = Vector3.zero;
            m_ConstrainedAxes = new Vector3Bool(true);
            m_MinLimit = -180f;
            m_MaxLimit = 180f;
        }

        static Vector3 Convert(Axis axis)
        {
            switch (axis)
            {
                case Axis.X:
                    return Vector3.right;
                case Axis.X_NEG:
                    return Vector3.left;
                case Axis.Y:
                    return Vector3.up;
                case Axis.Y_NEG:
                    return Vector3.down;
                case Axis.Z:
                    return Vector3.forward;
                case Axis.Z_NEG:
                    return Vector3.back;
                default:
                    return Vector3.up;
            }
        }

        JobTransform[] IRigReferenceSync.allReferences
        {
            get
            {
                JobTransform[] jobTx = new JobTransform[m_SourceObjects.Count + 1];
                jobTx[0] = m_ConstrainedObject;
                for (int i = 0; i < m_SourceObjects.Count; ++i)
                    jobTx[i + 1] = m_SourceObjects[i];

                return jobTx;
            }
        }

        public void MarkSourceWeightsDirty() => m_SrcWeightCache.MarkDirty();
    }

    [AddComponentMenu("Animation Rigging/Multi-Aim Constraint")]
    public class MultiAimConstraint : RuntimeRigConstraint<
        MultiAimConstraintJob,
        MultiAimConstraintData,
        MultiAimConstraintJobBinder<MultiAimConstraintData>
        >
    {
    #if UNITY_EDITOR
    #pragma warning disable 0414
        [SerializeField, HideInInspector] bool m_SourceObjectsGUIToggle;
        [SerializeField, HideInInspector] bool m_SettingsGUIToggle;
    #endif

        void OnValidate()
        {
            m_Data.MarkSourceWeightsDirty();
        }
    }
}