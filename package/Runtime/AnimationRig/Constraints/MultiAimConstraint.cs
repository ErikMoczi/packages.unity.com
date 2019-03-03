using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging
{
    using RuntimeConstraints;

    [System.Serializable]
    public struct MultiAimConstraintData : IAnimationJobData, IMultiAimConstraintData
    {
        public enum Axis { X, X_NEG, Y, Y_NEG, Z, Z_NEG }

        [SerializeField] Transform m_ConstrainedObject;

        [SyncSceneToStream, SerializeField] List<WeightedTransform> m_SourceObjects;
        [SyncSceneToStream, SerializeField] Vector3 m_Offset;
        [SyncSceneToStream, SerializeField, Range(-180f, 180f)] float m_MinLimit;
        [SyncSceneToStream, SerializeField, Range(-180f, 180f)] float m_MaxLimit;

        [NotKeyable, SerializeField] Axis m_AimAxis;
        [NotKeyable, SerializeField] bool m_MaintainOffset;
        [NotKeyable, SerializeField] Vector3Bool m_ConstrainedAxes;

        // Since source weights can be updated at runtime keep a local cache instead of
        // extracting these constantly
        private WeightCache m_SrcWeightCache;

        public Transform constrainedObject { get => m_ConstrainedObject; set => m_ConstrainedObject = value; }

        public List<WeightedTransform> sourceObjects
        {
            get
            {
                if (m_SourceObjects == null)
                    m_SourceObjects = new List<WeightedTransform>();

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

        Transform[] IMultiAimConstraintData.sourceObjects => ConstraintDataUtils.GetTransforms(m_SourceObjects);
        float[] IMultiAimConstraintData.sourceWeights => m_SrcWeightCache.GetWeights(m_SourceObjects);
        Vector3 IMultiAimConstraintData.aimAxis => Convert(m_AimAxis);
        string IMultiAimConstraintData.offsetVector3Property => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_Offset));
        string IMultiAimConstraintData.minLimitFloatProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_MinLimit));
        string IMultiAimConstraintData.maxLimitFloatProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_MaxLimit));

        bool IAnimationJobData.IsValid()
        {
            if (m_ConstrainedObject == null || m_SourceObjects == null || m_SourceObjects.Count == 0)
                return false;

            foreach (var src in m_SourceObjects)
                if (src.transform == null)
                    return false;

            return true;
        }

        void IAnimationJobData.SetDefaultValues()
        {
            m_ConstrainedObject = null;
            m_AimAxis = Axis.X;
            m_SourceObjects = new List<WeightedTransform>();
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

        public void MarkSourceWeightsDirty() => m_SrcWeightCache.MarkDirty();
    }

    [DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Multi-Aim Constraint")]
    public class MultiAimConstraint : RigConstraint<
        MultiAimConstraintJob,
        MultiAimConstraintData,
        MultiAimConstraintJobBinder<MultiAimConstraintData>
        >
    {
    #if UNITY_EDITOR
    #pragma warning disable 0414
        [NotKeyable, SerializeField, HideInInspector] bool m_SourceObjectsGUIToggle;
        [NotKeyable, SerializeField, HideInInspector] bool m_SettingsGUIToggle;
    #endif

        void OnValidate()
        {
            m_Data.MarkSourceWeightsDirty();
        }
    }
}
