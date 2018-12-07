using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    public struct BitArray : System.IDisposable
    {
        private static readonly float k_BitsPerByte = 8f;
        private static readonly int k_BitMask = 0x7;

        private NativeArray<byte> m_Data;

        public BitArray(int bitCount)
        {
            m_Data = new NativeArray<byte>(Mathf.CeilToInt(bitCount / k_BitsPerByte), Allocator.Persistent, NativeArrayOptions.ClearMemory);
        }

        public BitArray(bool[] bits)
        {
            m_Data = new NativeArray<byte>(Mathf.CeilToInt(bits.Length / k_BitsPerByte), Allocator.Persistent, NativeArrayOptions.ClearMemory);
            for (int i = 0; i < bits.Length; ++i)
                this[i] = bits[i];
        }

        public void Dispose()
        {
            m_Data.Dispose();
        }

        public bool this[int index]
        {
            get
            {
                var idx = index >> 3;
                if (idx < m_Data.Length)
                    return (m_Data[idx] & (byte)(1 << (index & k_BitMask))) != 0;

                return false;
            }

            set
            {
                var idx = index >> 3;
                if (idx < m_Data.Length)
                {
                    var mask = (1 << (index & k_BitMask));
                    if (value)
                        m_Data[idx] |= (byte)mask;
                    else
                        m_Data[idx] &= (byte)~mask;
                }
            }
        }
    }
}
