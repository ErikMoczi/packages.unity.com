using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging
{
    using RuntimeConstraints;

    [System.Serializable]
    public class MultiReferentialConstraintData : IAnimationJobData, IMultiReferentialConstraintData, IRigReferenceSync
    {
        [SerializeField] int m_Driver;
        [SerializeField] List<JobTransform> m_SourceObjects;

        public int driver
        {
            get => m_Driver;
            set => m_Driver = Mathf.Clamp(value, 0, m_SourceObjects.Count - 1);
        }

        public List<JobTransform> sourceObjects
        {
            get
            {
                if (m_SourceObjects == null)
                    m_SourceObjects = new List<JobTransform>();

                return m_SourceObjects;
            }

            set
            {
                m_SourceObjects = value;
                m_Driver = Mathf.Clamp(m_Driver, 0, m_SourceObjects.Count - 1);
            }
        }

        Transform[] IMultiReferentialConstraintData.sourceObjects => ConstraintDataUtils.GetTransforms(m_SourceObjects);

        bool IAnimationJobData.IsValid()
        {
            if (m_SourceObjects.Count < 2)
                return false;

            foreach (var src in m_SourceObjects)
                if (src.transform == null)
                    return false;

            return true;
        }

        IAnimationJobBinder IAnimationJobData.binder { get; } = new MultiReferentialConstraintJobBinder<MultiReferentialConstraintData>();

        JobTransform[] IRigReferenceSync.allReferences => m_SourceObjects.ToArray();

        public void OnValidate() =>
            m_Driver = Mathf.Clamp(m_Driver, 0, m_SourceObjects.Count - 1);
    }

    [AddComponentMenu("Runtime Rigging/Multi-Referential Constraint")]
    public class MultiReferentialConstraint : RuntimeRigConstraint<MultiReferentialConstraintData>
    {
    #if UNITY_EDITOR
    #pragma warning disable 0414
        [SerializeField, HideInInspector] bool m_SourceObjectsGUIToggle;
    #endif

        private void OnValidate()
        {
            if (m_Data != null)
                m_Data.OnValidate();
        }
    }
}