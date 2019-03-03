using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging
{
    using RuntimeConstraints;

    [System.Serializable]
    public class TwistNode : ITransformProvider, IWeightProvider
    {
        public Transform transform;

        [Range(-1f, 1f)]
        public float weight;

        public TwistNode(Transform transform, float weight = 0f)
        {
            this.transform = transform;
            this.weight = Mathf.Clamp(weight, -1f, 1f);
        }

        Transform ITransformProvider.transform { get => transform; set => transform = value; }
        float IWeightProvider.weight { get => weight; set => weight = value; }
    }

    [System.Serializable]
    public struct TwistCorrectionData : IAnimationJobData, ITwistCorrectionData
    {
        public enum Axis { X, Y ,Z }

        [SyncSceneToStream, SerializeField] Transform m_Source;

        [NotKeyable, SerializeField] Axis m_TwistAxis;
        [SerializeField] List<TwistNode> m_TwistNodes;

        // Since twist node weights can be updated at runtime keep a local cache instead of
        // extracting these constantly
        private WeightCache m_TwistNodeWeightCache;

        public Transform sourceObject { get => m_Source; set => m_Source = value; }

        public List<TwistNode> twistNodes
        {
            get
            {
                if (m_TwistNodes == null)
                    m_TwistNodes = new List<TwistNode>();

                return m_TwistNodes;
            }

            set
            {
                m_TwistNodes = value;
                m_TwistNodeWeightCache.MarkDirty();
            }
        }

        public Axis twistAxis { get => m_TwistAxis; set => m_TwistAxis = value; }

        Transform ITwistCorrectionData.source => m_Source.transform;
        Transform[] ITwistCorrectionData.twistNodes => ConstraintDataUtils.GetTransforms(m_TwistNodes);
        float[] ITwistCorrectionData.twistNodeWeights => m_TwistNodeWeightCache.GetWeights(m_TwistNodes);
        Vector3 ITwistCorrectionData.twistAxis => Convert(m_TwistAxis);

        static Vector3 Convert(Axis axis)
        {
            if (axis == Axis.X)
                return Vector3.right;

            if (axis == Axis.Y)
                return Vector3.up;

            return Vector3.forward;
        }

        bool IAnimationJobData.IsValid()
        {
            if (m_Source.transform == null)
                return false;

            for (int i = 0; i < m_TwistNodes.Count; ++i)
                if (m_TwistNodes[i].transform == null)
                    return false;

            return true;
        }

        void IAnimationJobData.SetDefaultValues()
        {
            m_Source = null;
            m_TwistAxis = Axis.X;
            m_TwistNodes = new List<TwistNode>();
        }

        public void MarkTwistNodeWeightsDirty() => m_TwistNodeWeightCache.MarkDirty();
    }

    [DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Twist Correction")]
    public class TwistCorrection : RigConstraint<
        TwistCorrectionJob,
        TwistCorrectionData,
        TwistCorrectionJobBinder<TwistCorrectionData>
        >
    {
    #if UNITY_EDITOR
    #pragma warning disable 0414
        [NotKeyable, SerializeField, HideInInspector] bool m_TwistNodesGUIToggle;
    #endif

        void OnValidate()
        {
            m_Data.MarkTwistNodeWeightsDirty();
        }
    }
}
