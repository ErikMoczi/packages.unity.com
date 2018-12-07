using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging
{
    using RuntimeConstraints;

    [System.Serializable]
    public class MultiParentConstraintData : IAnimationJobData, IMultiParentConstraintData, IRigReferenceSync
    {
        [SerializeField] JobTransform m_ConstrainedObject;
        [SerializeField] Vector3Bool m_ConstrainedPositionAxes;
        [SerializeField] Vector3Bool m_ConstrainedRotationAxes;
        [SerializeField] List<WeightedJobTransform> m_SourceObjects;
        [SerializeField] bool m_MaintainOffset;

        // Since source weights can be updated at runtime keep a local cache instead of
        // extracting these constantly
        private WeightCache m_SrcWeightCache;

        public MultiParentConstraintData()
        {
            m_MaintainOffset = true;
            m_ConstrainedPositionAxes = new Vector3Bool(true);
            m_ConstrainedRotationAxes = new Vector3Bool(true);
        }

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

        public bool constrainedPositionXAxis { get => m_ConstrainedPositionAxes.x; set => m_ConstrainedPositionAxes.x = value; }
        public bool constrainedPositionYAxis { get => m_ConstrainedPositionAxes.y; set => m_ConstrainedPositionAxes.y = value; }
        public bool constrainedPositionZAxis { get => m_ConstrainedPositionAxes.z; set => m_ConstrainedPositionAxes.z = value; }
        public bool constrainedRotationXAxis { get => m_ConstrainedRotationAxes.x; set => m_ConstrainedRotationAxes.x = value; }
        public bool constrainedRotationYAxis { get => m_ConstrainedRotationAxes.y; set => m_ConstrainedRotationAxes.y = value; }
        public bool constrainedRotationZAxis { get => m_ConstrainedRotationAxes.z; set => m_ConstrainedRotationAxes.z = value; }

        Transform IMultiParentConstraintData.constrainedObject => m_ConstrainedObject.transform;
        Transform[] IMultiParentConstraintData.sourceObjects => ConstraintDataUtils.GetTransforms(m_SourceObjects);
        float[] IMultiParentConstraintData.sourceWeights => m_SrcWeightCache.GetWeights(m_SourceObjects);

        bool IAnimationJobData.IsValid()
        {
            if (m_ConstrainedObject.transform == null || m_SourceObjects == null || m_SourceObjects.Count == 0)
                return false;

            foreach (var src in m_SourceObjects)
                if (src.transform == null)
                    return false;

            return true;
        }

        IAnimationJobBinder IAnimationJobData.binder { get; } = new MultiParentConstraintJobBinder<MultiParentConstraintData>();

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

    [AddComponentMenu("Runtime Rigging/Multi-Parent Constraint")]
    public class MultiParentConstraint : RuntimeRigConstraint<MultiParentConstraintData>
    {
    #if UNITY_EDITOR
    #pragma warning disable 0414
        [SerializeField, HideInInspector] bool m_SourceObjectsGUIToggle;
        [SerializeField, HideInInspector] bool m_SettingsGUIToggle;
    #endif

        void OnValidate()
        {
            if (m_Data != null)
                m_Data.MarkSourceWeightsDirty();
        }
    }
}