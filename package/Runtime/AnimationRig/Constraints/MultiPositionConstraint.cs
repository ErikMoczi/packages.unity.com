using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging
{
    using RuntimeConstraints;

    [System.Serializable]
    public struct MultiPositionConstraintData : IAnimationJobData, IMultiPositionConstraintData
    {
        [SerializeField] Transform m_ConstrainedObject;

        [SyncSceneToStream, SerializeField] List<WeightedTransform> m_SourceObjects;
        [SyncSceneToStream, SerializeField] Vector3 m_Offset;

        [NotKeyable, SerializeField] Vector3Bool m_ConstrainedAxes;
        [NotKeyable, SerializeField] bool m_MaintainOffset;

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

        public bool constrainedXAxis { get => m_ConstrainedAxes.x; set => m_ConstrainedAxes.x = value; }
        public bool constrainedYAxis { get => m_ConstrainedAxes.y; set => m_ConstrainedAxes.y = value; }
        public bool constrainedZAxis { get => m_ConstrainedAxes.z; set => m_ConstrainedAxes.z = value; }

        Transform[] IMultiPositionConstraintData.sourceObjects => ConstraintDataUtils.GetTransforms(m_SourceObjects);
        float[] IMultiPositionConstraintData.sourceWeights => m_SrcWeightCache.GetWeights(m_SourceObjects);
        string IMultiPositionConstraintData.offsetVector3Property => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_Offset));

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
            m_ConstrainedAxes = new Vector3Bool(true);
            m_SourceObjects = new List<WeightedTransform>();
            m_MaintainOffset = true;
            m_Offset = Vector3.zero;
        }

        public void MarkSourceWeightsDirty() => m_SrcWeightCache.MarkDirty();
    }

    [DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Multi-Position Constraint")]
    public class MultiPositionConstraint : RigConstraint<
        MultiPositionConstraintJob,
        MultiPositionConstraintData,
        MultiPositionConstraintJobBinder<MultiPositionConstraintData>
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
