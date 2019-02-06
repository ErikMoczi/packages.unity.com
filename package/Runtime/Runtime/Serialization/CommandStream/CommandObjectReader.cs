

using System;
using System.IO;
using Unity.Properties.Serialization;
using UnityEngine.Assertions;
using Unity.Tiny.Serialization.Binary;

namespace Unity.Tiny.Serialization.CommandStream
{
    internal class CommandObjectReader : BinaryObjectReader
    {
        private static class UserDefinedObjectReader
        {
            private static readonly byte[] s_GuidBuffer = new byte[16];

            public static object Read(BinaryReader reader)
            {
                var token = (TinyBinaryToken) reader.ReadByte();

                switch (token)
                {
                    case TinyBinaryToken.Id:
                        return ReadId(reader);
                    case TinyBinaryToken.ModuleReference:
                        return new TinyModule.Reference(ReadId(reader), reader.ReadString());
                    case TinyBinaryToken.TypeReference:
                        return new TinyType.Reference(ReadId(reader), reader.ReadString());
                    case TinyBinaryToken.SceneReference:
                        return new TinyEntityGroup.Reference(ReadId(reader), reader.ReadString());
                    case TinyBinaryToken.EntityReference:
                        return new TinyEntity.Reference(ReadId(reader), reader.ReadString());
                    case TinyBinaryToken.PrefabInstanceReference:
                        return new TinyPrefabInstance.Reference(ReadId(reader), reader.ReadString());
                    case TinyBinaryToken.UnityObject:
                        var guid = reader.ReadString();
                        var fileId = reader.ReadInt64();
                        var type = reader.ReadInt32();
                        return UnityObjectSerializer.FromObjectHandle(new UnityObjectHandle {Guid = guid, FileId = fileId, Type = type});
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private static TinyId ReadId(BinaryReader reader)
            {
                reader.Read(s_GuidBuffer, 0, 16);
                return new TinyId(new Guid(s_GuidBuffer));
            }
        }
        
        public CommandObjectReader(IObjectFactory objectFactory) : base(objectFactory)
        {
            UserDefinedValueDelegate = UserDefinedObjectReader.Read;
        }

        /// <summary>
        /// WIP attempting toe make a 'fast' path for entitiy deserialization... its currently the same speed or slow than the generic
        /// Makes assumptions about the structure of the data
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="registry"></param>
        public void ReadEntity(BinaryReader reader, IRegistry registry)
        {
            var token = reader.ReadByte();
            Assert.AreEqual(token, BinaryToken.BeginObject);
            reader.ReadUInt32();

            // Read TypeId
            ReadPropertyName(reader);
            ReadPropertyValue(reader);

            // Read Id
            ReadPropertyName(reader);
            var id = ParseId(ReadPropertyValue(reader));

            // Read Name
            ReadPropertyName(reader);
            var name = ReadPropertyValue(reader) as string;

            var entity = registry.CreateEntity(id, name);

            // Read Components
            ReadPropertyName(reader);
            token = reader.ReadByte();
            Assert.AreEqual(BinaryToken.BeginArray, token);

            // Read object size
            // NOTE: This is NOT the length, this is the bytesize from the BeginArray token to the EndArray token
            reader.ReadUInt32();

            while ((token = reader.ReadByte()) != BinaryToken.EndArray)
            {
                Assert.AreEqual(BinaryToken.BeginObject, token);
                reader.ReadUInt32(); // Size
                ReadPropertyName(reader); // "Type"
                var type = ReadTypeReferenceValue(reader);

                var component = entity.AddComponent(type);

                // Read Name
                ReadPropertyName(reader);
                ReadPropertyValue(reader);
                
                ReadPropertyName(reader); // "Properties"
                
                token = reader.ReadByte();
                Assert.AreEqual(BinaryToken.BeginObject, token);
                reader.ReadUInt32(); // Size
                while ((token = reader.ReadByte()) != BinaryToken.EndObject)
                {
                    reader.BaseStream.Position -= 1;
                    ReadTinyObjectProperty(reader, component);
                }
                token = reader.ReadByte(); // EndObject
                Assert.AreEqual(BinaryToken.EndObject, token);
            }
        }

        private void ReadTinyObjectProperty(BinaryReader reader, TinyObject @object)
        {
            var key = ReadPropertyName(reader); // Dynamic property name

            var token = reader.ReadByte();

            switch (token)
            {
                case BinaryToken.Value:
                {
                    @object[key] = ReadValue(reader);
                    return;
                }
                case BinaryToken.BeginObject:
                {
                    reader.ReadUInt32(); // Size
                    ReadPropertyName(reader); // "Type"
                    ReadTypeReferenceValue(reader);

                    // Read Name
                    ReadPropertyName(reader);
                    ReadPropertyValue(reader);
            
                    ReadPropertyName(reader); // "Properties"
            
                    token = reader.ReadByte();
                    Assert.AreEqual(BinaryToken.BeginObject, token);
                    reader.ReadUInt32(); // Size
                    var inner = @object[key] as TinyObject;
                    while ((token = reader.ReadByte()) != BinaryToken.EndObject)
                    {
                        reader.BaseStream.Position -= 1;
                        ReadTinyObjectProperty(reader, inner);
                    }
                    token = reader.ReadByte();
                    Assert.AreEqual(BinaryToken.EndObject, token);
                    return;
                }
                case BinaryToken.BeginArray:
                    throw new NotSupportedException();
            }
        }

        private TinyType.Reference ReadTypeReferenceValue(BinaryReader reader)
        {
            var token = reader.ReadByte();

            switch (token)
            {
                case BinaryToken.Value:
                {
                    return (TinyType.Reference) ReadValue(reader);
                }

                case BinaryToken.BeginObject:
                {
                    reader.ReadUInt32(); // Size
                    ReadPropertyName(reader); // "Id"
                    var id = ParseId(ReadPropertyValue(reader));
                    ReadPropertyName(reader); // "Name"
                    var name = ReadPropertyValue(reader) as string;
                    reader.ReadByte(); // EndObject
                    return new TinyType.Reference(id, name);
                }
                case BinaryToken.BeginArray:
                    throw new NotSupportedException();
            }

            return new TinyType.Reference();
        }

        private static TinyId ParseId(object obj)
        {
            if (null == obj)
            {
                return TinyId.Empty;
            }

            if (obj is TinyId)
            {
                return (TinyId) obj;
            }

            var s = obj as string;
            return s != null ? new TinyId(s) : TinyId.Empty;
        }
    }
}

