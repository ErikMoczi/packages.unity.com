using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging
{
    using RuntimeConstraints;

    [System.Serializable]
    public struct MultiPositionConstraintData : IAnimationJobData, IMultiPositionConstraintData, IRigReferenceSync
    {
        [SerializeField] JobTransform m_ConstrainedObject;
        [SerializeField] Vector3Bool m_ConstrainedAxes;
        [SerializeField] List<WeightedJobTransform> m_SourceObjects;
        [SerializeField] bool m_MaintainOffset;
        [SerializeField] Vector3 m_Offset;

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

        public bool constrainedXAxis { get => m_ConstrainedAxes.x; set => m_ConstrainedAxes.x = value; }
        public bool constrainedYAxis { get => m_ConstrainedAxes.y; set => m_ConstrainedAxes.y = value; }
        public bool constrainedZAxis { get => m_ConstrainedAxes.z; set => m_ConstrainedAxes.z = value; }

        Transform IMultiPositionConstraintData.constrainedObject => m_ConstrainedObject.transform;
        Transform[] IMultiPositionConstraintData.sourceObjects => ConstraintDataUtils.GetTransforms(m_SourceObjects);
        float[] IMultiPositionConstraintData.sourceWeights => m_SrcWeightCache.GetWeights(m_SourceObjects);

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
            m_ConstrainedAxes = new Vector3Bool(true);
            m_SourceObjects = new List<WeightedJobTransform>();
            m_MaintainOffset = true;
            m_Offset = Vector3.zero;
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

    [AddComponentMenu("Animation Rigging/Multi-Position Constraint")]
    public class MultiPositionConstraint : RuntimeRigConstraint<
        MultiPositionConstraintJob,
        MultiPositionConstraintData,
        MultiPositionConstraintJobBinder<MultiPositionConstraintData>
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