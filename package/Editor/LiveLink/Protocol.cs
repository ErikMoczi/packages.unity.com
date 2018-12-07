
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Unity.Tiny
{
    // A message is structured as follow:
    //   -- 1 message name --
    //   4 bytes for name length (int32)
    //   N bytes for name data (bytes)
    //   -- N message data --
    //   4 bytes for data length (int32)
    //   N bytes for data (bytes)
    //   ...
    //   -- last message data --
    //   4 bytes for data length (int32, must be equal to 0)
    //   0 bytes for data
    //
    // The message name is an ASCII string (8-bits per character, no support for
    // UTF-8 necessary)
    //
    // The message data is raw bytes, can store anything.
    //
    // So a message must always have a name, this is used as an identifier for
    // commands and tagging what the data contains. However, it is perfectly fine
    // for a message to have no data attached, which means the data length bytes
    // should be present, but set to zero. It is also perfectly fine to have more
    // than one data block attached.
    internal class Protocol
    {
        public class Buffer
        {
            public Buffer(int size)
            {
                Bytes = new byte[size];
            }

            unsafe public void Add(byte* bytes, int size)
            {
                fixed (byte* ptr = Bytes)
                {
                    System.Buffer.MemoryCopy(bytes, ptr + Offset, FreeSpace, size);
                }
                Offset += size;
            }

            private int Offset { get; set; }
            public byte[] Bytes { get; private set; }
            public int FreeSpace => Bytes.Length - Offset;
            public bool IsFull => Offset == Bytes.Length;
            public override string ToString() => Encoding.ASCII.GetString(Bytes);
            public static int MaxLength => 16 * 1024 * 1024;
        }

        public static byte[] Combine(params byte[][] buffers)
        {
            var result = new byte[buffers.Sum(a => a.Length)];
            var offset = 0;
            foreach (var buffer in buffers)
            {
                System.Buffer.BlockCopy(buffer, 0, result, offset, buffer.Length);
                offset += buffer.Length;
            }
            return result;
        }

        public static byte[] Encode(byte value)
        {
            return value.AsEnumerable().ToArray();
        }

        public static byte[] Encode(int value)
        {
            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
        }

        private static byte[] EncodeSize(byte[] buffer)
        {
            return Combine(Encode(buffer.Length), buffer);
        }

        public static byte[] EncodeSize(params byte[][] buffers)
        {
            return Combine(buffers.Select(b => EncodeSize(b)).ToArray());
        }

        public static byte[] EncodeSize(params string[] strings)
        {
            return EncodeSize(strings.Select(s => Encoding.ASCII.GetBytes(s)).ToArray());
        }

        public static byte[] EncodeMessage(params byte[][] buffers)
        {
            return EncodeSize(Combine(EncodeSize(buffers), Encode(0)));
        }

        public static byte[] EncodeMessage(params string[] strings)
        {
            return EncodeMessage(strings.Select(s => Encoding.ASCII.GetBytes(s)).ToArray());
        }

        unsafe public static void Decode(List<Buffer> buffers, byte* bytes, int size, Action<IReadOnlyList<Buffer>> callback)
        {
            // Decode received bytes
            var read = 0;
            while (read < size)
            {
                // Check if we have an incomplete buffer
                var buffer = buffers.LastOrDefault();
                if (buffer == null || buffer.IsFull)
                {
                    // Check if we have enough bytes to read data size
                    if (size - read < sizeof(int))
                    {
                        throw new Exception($"Cannot read data size header.");
                    }

                    // Get buffer size
                    var bufferSize = IPAddress.NetworkToHostOrder(*(int*)(bytes + read));
                    if (bufferSize > Buffer.MaxLength)
                    {
                        throw new Exception($"Buffer size ({bufferSize}) is too large (max {Buffer.MaxLength} bytes).");
                    }
                    if (bufferSize < 0)
                    {
                        throw new Exception($"Buffer size ({bufferSize}) is negative.");
                    }
                    read += sizeof(int);

                    // Check if its a new buffer or the end of a message
                    if (bufferSize > 0)
                    {
                        // We have data, add a new buffer
                        buffer = new Buffer(bufferSize);
                        buffers.Add(buffer);
                    }
                    else
                    {
                        // Data length of zero means end of message
                        if (buffers.Count > 0)
                        {
                            callback(buffers);
                            buffers.Clear();
                        }
                        continue;
                    }
                }

                // Add bytes to existing buffer
                var length = Math.Min(size - read, buffer.FreeSpace);
                buffer.Add(bytes + read, length);
                read += length;
            }
        }
    }
}
