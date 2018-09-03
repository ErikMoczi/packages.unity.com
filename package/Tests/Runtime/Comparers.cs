using System.Collections.Generic;
using NUnit.Framework;

namespace UnityEngine.Experimental.U2D.IK.Tests
{
    public class Vector2Compare : IEqualityComparer<Vector2>
    {
        public Vector2Compare(float epsilon = 0.01f)
        {
            m_Epsilon = epsilon;
        }

        public bool Equals(Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b) <= m_Epsilon;
        }

        public int GetHashCode(Vector2 v)
        {
            return Mathf.RoundToInt(v.x) ^ Mathf.RoundToInt(v.y);
        }

        private float m_Epsilon;
    }

    public class Vector3Compare : IEqualityComparer<Vector3>
    {
        public Vector3Compare(float epsilon = 0.01f)
        {
            m_Epsilon = epsilon;
        }

        public bool Equals(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b) <= m_Epsilon;
        }

        public int GetHashCode(Vector3 v)
        {
            return Mathf.RoundToInt(v.x) ^ Mathf.RoundToInt(v.y);
        }

        private float m_Epsilon;
    }

    public class FloatCompare : IEqualityComparer<float>
    {
        public FloatCompare(float epsilon = 0.01f)
        {
            m_Epsilon = epsilon;
        }

        public bool Equals(float a, float b)
        {
            return Mathf.Abs(a - b) <= m_Epsilon;
        }

        public int GetHashCode(float v)
        {
            return v.GetHashCode();
        }

        private float m_Epsilon;
    }
}
