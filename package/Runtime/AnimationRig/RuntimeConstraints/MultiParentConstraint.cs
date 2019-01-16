using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging
{
    using RuntimeConstraints;

    [System.Serializable]
    public struct MultiParentConstraintData : IAnimationJobData, IMultiParentConstraintData, IRigReferenceSync
    {
        [SerializeField] Transform m_ConstrainedObject;
        [SerializeField] Vector3Bool m_ConstrainedPositionAxes;
        [SerializeField] Vector3Bool m_ConstrainedRotationAxes;
        [SerializeField] List<WeightedJobTransform> m_SourceObjects;

        [SerializeField] bool m_MaintainPositionOffset;
        [SerializeField] bool m_MaintainRotationOffset;

        // Since source weights can be updated at runtime keep a local cache instead of
        // extracting these constantly
        private WeightCache m_SrcWeightCache;

        public Transform constrainedObject { get => m_ConstrainedObject; set => m_ConstrainedObject = value; }

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

        public bool maintainPositionOffset { get => m_MaintainPositionOffset; set => m_MaintainPositionOffset = value; }
        public bool maintainRotationOffset { get => m_MaintainRotationOffset; set => m_MaintainRotationOffset = value; }

        public bool constrainedPositionXAxis { get => m_ConstrainedPositionAxes.x; set => m_ConstrainedPositionAxes.x = value; }
        public bool constrainedPositionYAxis { get => m_ConstrainedPositionAxes.y; set => m_ConstrainedPositionAxes.y = value; }
        public bool constrainedPositionZAxis { get => m_ConstrainedPositionAxes.z; set => m_ConstrainedPositionAxes.z = value; }
        public bool constrainedRotationXAxis { get => m_ConstrainedRotationAxes.x; set => m_ConstrainedRotationAxes.x = value; }
        public bool constrainedRotationYAxis { get => m_ConstrainedRotationAxes.y; set => m_ConstrainedRotationAxes.y = value; }
        public bool constrainedRotationZAxis { get => m_ConstrainedRotationAxes.z; set => m_ConstrainedRotationAxes.z = value; }

        Transform[] IMultiParentConstraintData.sourceObjects => ConstraintDataUtils.GetTransforms(m_SourceObjects);
        float[] IMultiParentConstraintData.sourceWeights => m_SrcWeightCache.GetWeights(m_SourceObjects);

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
            m_ConstrainedPositionAxes = new Vector3Bool(true);
            m_ConstrainedRotationAxes = new Vector3Bool(true);
            m_SourceObjects = new List<WeightedJobTransform>();
            m_MaintainPositionOffset = true;
            m_MaintainRotationOffset = true;
        }

        JobTransform[] IRigReferenceSync.allReferences => m_SourceObjects.ToArray();

        public void MarkSourceWeightsDirty() => m_SrcWeightCache.MarkDirty();
    }

    [AddComponentMenu("Animation Rigging/Multi-Parent Constraint")]
    public class MultiParentConstraint : RuntimeRigConstraint<
        MultiParentConstraintJob,
        MultiParentConstraintData,
        MultiParentConstraintJobBinder<MultiParentConstraintData>
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