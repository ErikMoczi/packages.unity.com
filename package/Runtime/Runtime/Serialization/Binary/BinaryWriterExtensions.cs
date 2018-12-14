

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Unity.Tiny.Serialization.Binary
{
    internal static class BinaryWriterExtensions
    {
        [StructLayout(LayoutKind.Explicit)]
        private unsafe struct GuidBuffer
        {
            [FieldOffset(0)] private fixed long m_Buffer[2];
            [FieldOffset(0)] private readonly Guid m_Guid;

            public GuidBuffer(Guid guid) : this()
            {
                m_Guid = guid;
            }

            public void CopyTo(byte[] dest, int offset)
            {
                if (dest.Length - offset < 16)
                {
                    throw new ArgumentException("Destination buffer is too small");
                }

                fixed (byte* bDestRoot = dest)
                fixed (long* bSrc = m_Buffer)
                {
                    var bDestOffset = bDestRoot + offset;
                    var bDest = (long*) bDestOffset;
                    bDest[0] = bSrc[0];
                    bDest[1] = bSrc[1];
                }
            }
        }

        private static readonly byte[] s_GuidBytes = new byte[16];

        public static void Write(this BinaryWriter writer, Guid guid)
        {
            var buffer = new GuidBuffer(guid);
            buffer.CopyTo(s_GuidBytes, 0);
            writer.Write(s_GuidBytes, 0, 16);
        }
    }
}

