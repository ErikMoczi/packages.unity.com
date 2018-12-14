

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Assertions;

namespace Unity.Properties.Serialization
{
    /// <summary>
    /// Reads a tokenized binary stream and returns generic .NET objects (IDictionary'String, Object and IList'Object)
    /// </summary>
    internal class BinaryObjectReader
    {
        internal interface IObjectFactory
        {
            IDictionary<string, object> GetDictionary();
            IList<object> GetList();
        }

        private class DefaultObjectFactory : IObjectFactory
        {
            public IDictionary<string, object> GetDictionary()
            {
                return new Dictionary<string, object>();
            }

            public IList<object> GetList()
            {
                return new List<object>();
            }
        }
        
        internal class PooledObjectFactory : IObjectFactory
        {
            private readonly Queue<IDictionary<string, object>> m_DictionaryPool = new Queue<IDictionary<string, object>>();
            private readonly Queue<IList<object>> m_ListPool = new Queue<IList<object>>();
            
            public IDictionary<string, object> GetDictionary()
            {
                return m_DictionaryPool.Count > 0 ? m_DictionaryPool.Dequeue() : new Dictionary<string, object>();
            }

            public IList<object> GetList()
            {
                return m_ListPool.Count > 0 ? m_ListPool.Dequeue() : new List<object>();
            }

            public void Release(IDictionary<string, object> dictionary)
            {
                m_DictionaryPool.Enqueue(dictionary);
                
                foreach (var element in dictionary.Values)
                {
                    if (element is IDictionary)
                    {
                        Release(element as IDictionary<string, object>);
                    }
                    else if (element is IList)
                    {
                        Release(element as IList<object>);
                    }
                }
                
                dictionary.Clear();
            }

            public void Release(IList<object> list)
            {
                m_ListPool.Enqueue(list);
                
                foreach (var element in list)
                {
                    if (element is IDictionary)
                    {
                        Release(element as IDictionary<string, object>);
                    }
                    else if (element is IList)
                    {
                        Release(element as IList<object>);
                    }
                }
                
                list.Clear();
            }
        }

        internal delegate object UserDefinedValueEventHandler(BinaryReader reader);

        private readonly IObjectFactory m_ObjectFactory;

        public UserDefinedValueEventHandler UserDefinedValueDelegate { private get; set; }
            
        public BinaryObjectReader(IObjectFactory objectFactory)
        {
            m_ObjectFactory = objectFactory;
        }
            
        public IDictionary<string, object> ReadObject(BinaryReader reader)
        {
            // Read token and sanity check
            var token = reader.ReadByte();
            
            // the Binary serialization always wraps objects in an Array at the top level
            // if we expect an object, but find an array, we assume its size is 1, and validate this later
            var isWrappedInArray = (token == BinaryToken.BeginArray);
            if (isWrappedInArray)
            {
                // assumption: array of size 1
                reader.ReadUInt32(); // skip array size, unused
                token = reader.ReadByte(); // first object
            }
            Assert.AreEqual(BinaryToken.BeginObject, token);

            // Read object size
            reader.ReadUInt32();

            var dictionary = m_ObjectFactory.GetDictionary();

            while ((token = reader.ReadByte()) != BinaryToken.EndObject)
            {
                Assert.AreEqual(BinaryToken.Property, token);
                var key = reader.ReadString();
                var value = ReadPropertyValue(reader);
                dictionary[key] = value;
            }

            if (isWrappedInArray)
            {
                // validate assumption
                token = reader.ReadByte();
                Assert.AreEqual(BinaryToken.EndArray, token);
            }

            return dictionary;
        }

        protected IList<object> ReadArray(BinaryReader reader)
        {
            // Read token and sanity check
            var token = reader.ReadByte();
            Assert.AreEqual(BinaryToken.BeginArray, token);

            // Read object size
            // NOTE: This is NOT the length, this is the bytesize from the BeginArray token to the EndArray token
            reader.ReadUInt32();

            var list = m_ObjectFactory.GetList();
                
            while ((token = reader.ReadByte()) != BinaryToken.EndArray)
            {
                reader.BaseStream.Position -= 1;
                list.Add(ReadPropertyValue(reader));
            }

            return list;
        }

        protected string ReadPropertyName(BinaryReader reader)
        {
            var token = reader.ReadByte();
            Assert.AreEqual(BinaryToken.Property, token);
            return reader.ReadString();
        }

        protected object ReadPropertyValue(BinaryReader reader)
        {
            var token = reader.ReadByte();

            switch (token)
            {
                case BinaryToken.BeginObject:
                {
                    reader.BaseStream.Position -= 1;
                    return ReadObject(reader); 
                }
                    
                case BinaryToken.BeginArray:
                {
                    reader.BaseStream.Position -= 1;
                    return ReadArray(reader); 
                }

                case BinaryToken.Value:
                {
                    return ReadValue(reader);
                }
            }

            return null;
        }

        protected object ReadValue(BinaryReader reader)
        {
            var typeCode = (TypeCode) reader.ReadByte();
                
            switch (typeCode)
            {
                case TypeCode.String:
                    return reader.ReadString();
                case TypeCode.Boolean:
                    return reader.ReadBoolean();
                case TypeCode.Char:
                    return reader.ReadChar();
                case TypeCode.SByte:
                    return reader.ReadSByte();
                case TypeCode.Byte:
                    return reader.ReadByte();
                case TypeCode.Int16:
                    return reader.ReadInt16();
                case TypeCode.UInt16:
                    return reader.ReadUInt16();
                case TypeCode.Int32:
                    return reader.ReadInt32();
                case TypeCode.UInt32:
                    return reader.ReadUInt32();
                case TypeCode.Int64:
                    return reader.ReadInt64();
                case TypeCode.UInt64:
                    return reader.ReadUInt64();
                case TypeCode.Single:
                    return reader.ReadSingle();
                case TypeCode.Double:
                    return reader.ReadDouble();
                case TypeCode.Object:
                    return ReadUserDefinedValue(reader);
                case TypeCode.Empty:
                case TypeCode.DBNull:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                    throw new NotSupportedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected object ReadUserDefinedValue(BinaryReader reader)
        {
            return UserDefinedValueDelegate?.Invoke(reader);
        }
    }
}

