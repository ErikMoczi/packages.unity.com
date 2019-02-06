

using System;
using System.IO;
using System.Text;
using Unity.Properties.Serialization;
using UnityEngine.Assertions;
using Unity.Tiny.Serialization.CommandStream;

namespace Unity.Tiny.Serialization.Binary
{
    internal static class BinaryFrontEnd
    {
        private struct BinaryObjectInfo
        {
            public TinyTypeId TypeId;
            public int Size;
        }

        private static readonly byte[] s_CopyBuffer = new byte[32768];

        /// <summary>
        /// Reads the given binary data in to a command stream
        /// </summary>
        /// <param name="path">Binary file containing modules, types etc.</param>
        /// <param name="output">Command stream to write to</param>
        public static void Accept(string path, Stream output)
        {
            using (var input = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Accept(input, output);
            }
        }

        public static void Accept(Stream input, Stream output)
        {
            using (var reader = new BinaryReader(input, new UTF8Encoding(), true))
            {
                var commandStreamWriter = new BinaryWriter(output);
                var token = reader.ReadByte();
                if (BinaryToken.BeginArray != token)
                {
                    throw new NotSupportedException("Binary frontend only supports arrays as top-level objects");
                }
                
                // Read size
                reader.ReadUInt32();

                while ((token = reader.ReadByte()) != BinaryToken.EndArray)
                {
                    reader.BaseStream.Position -= 1;
                    var info = GetObjectInfo(reader);

                    commandStreamWriter.Write(CommandType.GetCreateCommandType(info.TypeId));
                    commandStreamWriter.Write((uint) info.Size);
                    
                    // Directly pass the binary object to the output stream no conversion necessary
                    CopyStream(input, output, info.Size);
                }
            }
        }

        private static void CopyStream(Stream input, Stream output, int count)
        {
            int read;
            while (count > 0 && (read = input.Read(s_CopyBuffer, 0, Math.Min(s_CopyBuffer.Length, count))) > 0)
            {
                output.Write(s_CopyBuffer, 0, read);
                count -= read;
            }
        }

        /// <summary>
        /// Fast path, read until we find the $TypeId and size then exit out
        /// </summary>
        private static BinaryObjectInfo GetObjectInfo(BinaryReader reader)
        {
            var start = reader.BaseStream.Position;
            
            var token = reader.ReadByte();
            Assert.AreEqual(BinaryToken.BeginObject, token);

            // Get the object size
            var size = reader.ReadUInt32();
            Assert.IsTrue(size > 0);

            while (true)
            {
                switch (reader.ReadByte())
                {
                    case BinaryToken.Property:
                    {
                        var propertyName = reader.ReadString();

                        if (string.Equals("$TypeId", propertyName))
                        {
                            // Read the property type token
                            // It should be `Value` in this case

                            var propertyToken = reader.ReadByte();
                            Assert.AreEqual(BinaryToken.Value, propertyToken);

                            // Read the type code
                            reader.ReadByte();
                            var typeId = reader.ReadInt32();

                            // Return the the start of this object
                            reader.BaseStream.Position = start;
                            return new BinaryObjectInfo {TypeId = (TinyTypeId) typeId, Size = (int)size + sizeof(byte) + sizeof(uint)};
                        }
                        
                        // Consume the property and continue
                        IgnorePropertyValue(reader);
                    }
                        break;

                    default:
                        // Return the the start of this object
                        reader.BaseStream.Position = start;
                        return new BinaryObjectInfo {TypeId = TinyTypeId.Unknown, Size = 0};
                }
            }
        }

        private static void IgnorePropertyValue(BinaryReader reader)
        {
            var token = reader.ReadByte();
            switch (token)
            {
                case BinaryToken.BeginObject:
                case BinaryToken.BeginArray:
                    var size = reader.ReadUInt32();
                    reader.BaseStream.Seek(size, SeekOrigin.Current);
                    break;
                case BinaryToken.Value:
                    IgnoreValue(reader);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void IgnoreValue(BinaryReader reader)
        {
            var typeCode = (TypeCode) reader.ReadByte();
            
            switch (typeCode)
            {
                case TypeCode.String:
                    reader.ReadString();
                    break;
                case TypeCode.Boolean:
                    reader.ReadBoolean();
                    break;
                case TypeCode.Char:
                    reader.ReadChar();
                    break;
                case TypeCode.SByte:
                    reader.ReadSByte();
                    break;
                case TypeCode.Byte:
                    reader.ReadByte();
                    break;
                case TypeCode.Int16:
                    reader.ReadInt16();
                    break;
                case TypeCode.UInt16:
                    reader.ReadUInt16();
                    break;
                case TypeCode.Int32:
                    reader.ReadInt32();
                    break;
                case TypeCode.UInt32:
                    reader.ReadUInt32();
                    break;
                case TypeCode.Int64:
                    reader.ReadInt64();
                    break;
                case TypeCode.UInt64:
                    reader.ReadUInt64();
                    break;
                case TypeCode.Single:
                    reader.ReadSingle();
                    break;
                case TypeCode.Double:
                    reader.ReadDouble();
                    break;
                case TypeCode.Empty:
                case TypeCode.Object:
                case TypeCode.DBNull:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                    throw new NotSupportedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

