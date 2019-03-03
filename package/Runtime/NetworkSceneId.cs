#if ENABLE_UNET
using System;

namespace UnityEngine.Networking
{
    [Serializable]
    [Obsolete("The high level API classes are deprecated and will be removed in the future.")]
    public struct NetworkSceneId : IEquatable<NetworkSceneId>
    {
        public NetworkSceneId(uint value)
        {
            m_Value = value;
        }

        [SerializeField]
        uint m_Value;

        public bool IsEmpty()
        {
            return m_Value == 0;
        }

        public override int GetHashCode()
        {
            return (int)m_Value;
        }

        public override bool Equals(object obj)
        {
            return obj is NetworkSceneId && Equals((NetworkSceneId)obj);
        }

        public bool Equals(NetworkSceneId other)
        {
            return this == other;
        }

        public static bool operator==(NetworkSceneId c1, NetworkSceneId c2)
        {
            return c1.m_Value == c2.m_Value;
        }

        public static bool operator!=(NetworkSceneId c1, NetworkSceneId c2)
        {
            return c1.m_Value != c2.m_Value;
        }

        public override string ToString()
        {
            return m_Value.ToString();
        }

        public uint Value { get { return m_Value; } }
    }
}
#endif
