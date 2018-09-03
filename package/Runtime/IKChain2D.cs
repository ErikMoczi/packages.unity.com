using System;

namespace UnityEngine.Experimental.U2D.IK
{
    [Serializable]
    public class IKChain2D
    {
        [SerializeField]
        private Transform m_Target;
        [SerializeField]
        private Transform m_Effector;
        [SerializeField]
        private int m_TransformCount;
        [SerializeField]
        private Transform[] m_Transforms;
        [SerializeField]
        private Quaternion[] m_DefaultLocalRotations;
        [SerializeField]
        private Quaternion[] m_StoredLocalRotations;

        protected float[] m_Lengths;

        public Transform target
        {
            get { return m_Target; }
            set { m_Target = value; }
        }

        public Transform effector
        {
            get { return m_Effector; }
            set { m_Effector = value; }
        }

        public Transform[] transforms
        {
            get { return m_Transforms; }
        }

        public Transform rootTransform
        {
            get
            {
                if (m_Transforms != null && transformCount > 0 && m_Transforms.Length == transformCount)
                    return m_Transforms[0];
                return null;
            }
        }

        private Transform lastTransform
        {
            get
            {
                if (m_Transforms != null && transformCount > 0 && m_Transforms.Length == transformCount)
                    return m_Transforms[transformCount - 1];
                return null;
            }
        }

        public int transformCount
        {
            get { return m_TransformCount; }
            set { m_TransformCount = Mathf.Max(0, value); }
        }

        public bool isValid
        {
            get { return Validate(); }
        }

        public float[] lengths
        {
            get
            {
                if(isValid)
                {
                    PrepareLengths();
                    return m_Lengths;
                }

                return null;
            }
        }

        private bool Validate()
        {
            if (target == null)
                return false;
            if (transformCount == 0)
                return false;
            if (m_Transforms == null || m_Transforms.Length != transformCount)
                return false;
            if (m_DefaultLocalRotations == null || m_DefaultLocalRotations.Length != transformCount)
                return false;
            if (m_StoredLocalRotations ==  null || m_StoredLocalRotations.Length != transformCount)
                return false;
            if (rootTransform == null)
                return false;
            if (lastTransform != target)
                return false;
            if (effector && IKUtility.IsDescendentOf(effector, rootTransform))
                return false;
            return true;
        }

        public void Initialize()
        {
            if (target == null || transformCount == 0 || IKUtility.GetAncestorCount(target) < transformCount - 1)
                return;

            m_Transforms = new Transform[transformCount];
            m_DefaultLocalRotations = new Quaternion[transformCount];
            m_StoredLocalRotations = new Quaternion[transformCount];

            var currentTransform = target;
            int index = transformCount - 1;

            while (currentTransform && index >= 0)
            {
                m_Transforms[index] = currentTransform;
                m_DefaultLocalRotations[index] = currentTransform.localRotation;

                currentTransform = currentTransform.parent;
                --index;
            }
        }

        private void PrepareLengths()
        {
            var currentTransform = target;
            int index = transformCount - 1;

            if (m_Lengths == null || m_Lengths.Length != transformCount - 1)
                m_Lengths = new float[transformCount - 1];

            while (currentTransform && index >= 0)
            {
                if (currentTransform.parent && index > 0)
                    m_Lengths[index - 1] = (currentTransform.position - currentTransform.parent.position).magnitude;

                currentTransform = currentTransform.parent;
                --index;
            }
        }

        public void RestoreDefaultPose(bool targetRotationIsConstrained)
        {
            var count = targetRotationIsConstrained ? transformCount : transformCount-1;
            for (int i = 0; i < count; ++i)
                m_Transforms[i].localRotation = m_DefaultLocalRotations[i];
        }

        public void StoreLocalRotations()
        {
            for (int i = 0; i < m_Transforms.Length; ++i)
                m_StoredLocalRotations[i] = m_Transforms[i].localRotation;
        }

        public void BlendFkToIk(float finalWeight, bool targetRotationIsConstrained)
        {
            var count = targetRotationIsConstrained ? transformCount : transformCount-1;
            for (int i = 0; i < count; ++i)
                m_Transforms[i].localRotation = Quaternion.Slerp(m_StoredLocalRotations[i], m_Transforms[i].localRotation, finalWeight);
        }
    }
}
