using System;

using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation
{
    public interface IBone
    {
        IBone parent { get; }
        int parentId { get; }
        string name { get; }
        Vector3 position { get; }
        Quaternion rotation { get; }
        float length { get; }
        bool isRoot { get; }
        Vector3 tip { get; }
        float depth { get; set; }
    }

    [Serializable]
    internal class Bone : IBone
    {
        [SerializeField]
        private string m_UniqueId;
        public string uniqueId
        {
            get { return m_UniqueId; }
        }

        [SerializeField]
        private int m_ParentId;
        public int parentId
        {
            get { return m_ParentId; }
            set { m_ParentId = value; }
        }

        [SerializeField]
        private string m_Name;
        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        [SerializeField]
        private Vector3 m_LocalPosition;
        public Vector3 localPosition
        {
            get { return m_LocalPosition; }
            set { m_LocalPosition = value; }
        }

        [SerializeField]
        private Quaternion m_LocalRotation;
        public Quaternion localRotation
        {
            get { return m_LocalRotation; }
            set { m_LocalRotation = value; }
        }

        [SerializeField]
        private float m_Length;
        public float length
        {
            get { return m_Length; }
            set { m_Length = value; }
        }

        [SerializeField]
        private int m_DebugIndex;
        public int debugIndex
        {
            get { return m_DebugIndex; }
            set { m_DebugIndex = value; }
        }

        private Bone m_Parent;
        public IBone parent
        {
            get { return m_Parent; }
            set
            {
                m_Parent = (Bone)value;
                if (m_Parent != null)
                    parentId = m_Parent.debugIndex;
                else
                    parentId = -1;
            }
        }

        public bool isRoot { get { return parent == null; } }

        private Matrix4x4 m_Matrix = Matrix4x4.identity;
        public Matrix4x4 matrix
        {
            get { return m_Matrix; }
            set { m_Matrix = value; }
        }

        private Vector3 m_PreviousWorldPosition;
        private Vector3 m_PreviousTipWorldPosition;
        private bool m_MarkedRetainWorldPosition = false;
        public bool markedRetainWorldPosition
        {
            get { return m_MarkedRetainWorldPosition; }
            set
            {
                m_MarkedRetainWorldPosition = value;
                if (m_MarkedRetainWorldPosition)
                {
                    m_PreviousWorldPosition = position;
                    m_PreviousTipWorldPosition = tip;
                }
            }
        }

        public Vector3 position
        {
            get
            {
                if (isRoot)
                    return m_LocalPosition;
                else
                {
                    var parentM = m_Parent.matrix;
                    return parentM.MultiplyPoint(m_LocalPosition);
                }
            }
            set
            {
                if (isRoot)
                {
                    m_LocalPosition = value;
                }
                else
                {
                    var parentM = m_Parent.matrix;
                    var invParentM = parentM.inverse;
                    var worldToLocal = invParentM.MultiplyPoint(value);
                    m_LocalPosition = worldToLocal;
                }
            }
        }

        public Quaternion rotation
        {
            get
            {
                if (isRoot)
                    return m_LocalRotation;
                else
                {
                    var parentM = m_Parent.matrix;
                    return parentM.rotation * m_LocalRotation;
                }
            }
            set
            {
                if (isRoot)
                    m_LocalRotation = value;
                else
                {
                    var parentM = m_Parent.matrix;
                    m_LocalRotation = Quaternion.Inverse(parentM.rotation) * value;
                }
            }
        }

        public Vector3 tip
        {
            get
            {
                if (isRoot)
                {
                    Vector3 direction = localRotation * Vector3.right;
                    return position + direction * length;
                }
                else
                {
                    Vector3 direction = localRotation * Vector3.right;
                    var po = m_LocalPosition + direction * length;
                    var parentM = m_Parent.matrix;
                    return parentM.MultiplyPoint(po);
                }
            }
            set
            {
                if (isRoot)
                {
                    Vector3 direction = value - m_LocalPosition;
                    m_Length = direction.magnitude;
                    m_LocalRotation = Quaternion.Euler(0.0f, 0.0f, Vector2.SignedAngle(Vector2.right, direction));
                }
                else
                {
                    var parentM = m_Parent.matrix;
                    var invParentM = parentM.inverse;
                    var worldToLocal = invParentM.MultiplyPoint(value);

                    Vector3 direction = worldToLocal - m_LocalPosition;
                    m_Length = direction.magnitude;
                    m_LocalRotation = Quaternion.Euler(0.0f, 0.0f, Vector2.SignedAngle(Vector2.right, direction));
                }
            }
        }

        [SerializeField]
        float m_Depth;
        public float depth
        {
            get { return m_Depth; }
            set { m_Depth = value; }
        }

        public Bone()
        {
            parent = null;
            localRotation = Quaternion.identity;
            position = Vector3.zero;
            length = 0.0f;
            m_UniqueId = "";
        }

        public Bone(
            string name,
            Bone parent,
            Vector3 position,
            Quaternion localRotation,
            float length,
            int index,
            string uniqueId,
            float depth
            )
        {
            this.name = name;
            this.parent = parent;
            this.localPosition = position;
            this.localRotation = localRotation;
            this.length = length;
            this.debugIndex = index;
            m_UniqueId = uniqueId;
            this.depth = depth;

            RecalculateMatrix();
        }

        public void RecalculateMatrix()
        {
            if (isRoot)
            {
                m_Matrix.SetTRS(m_LocalPosition, localRotation, Vector3.one);
            }
            else
            {
                var parentM = m_Parent.matrix;
                m_Matrix = parentM * Matrix4x4.Translate(m_LocalPosition) * Matrix4x4.Rotate(m_LocalRotation);
            }
        }

        public void RestoreWorldPosition()
        {
            if (!isRoot)
            {
                position = m_PreviousWorldPosition;
                tip = m_PreviousTipWorldPosition;
                markedRetainWorldPosition = false;
            }
        }

        public void UpdateParentId()
        {
            if (m_Parent != null)
                m_ParentId = m_Parent.debugIndex;
            else
                m_ParentId = -1;
        }
    }
}
