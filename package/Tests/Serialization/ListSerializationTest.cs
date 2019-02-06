using System.IO;
using NUnit.Framework;

namespace Unity.Tiny.Test
{
    /// <summary>
    /// These tests are to ensure that UnityEngine.Object value are preserved when passing through the serializaton pipeline
    /// </summary>
    [TestFixture]
    internal class ListSerializationTest
    {
        private IRegistry m_Registry;
        private TinyType m_IntArrayComponentType;
        private TinyType m_StructArrayComponentType;
        private TinyType m_StructType;
        private TinyEntity m_IntArrayEntity;
        private TinyEntity m_StructArrayEntity;

        [SetUp]
        public void SetUp()
        {
            m_Registry = new TinyContext(ContextUsage.Tests).Registry;
            
            // Create a component with an int array field
            m_IntArrayComponentType = m_Registry.CreateType(
                TinyId.New(),
                "TestComponent",
                TinyTypeCode.Component);

            m_IntArrayComponentType.CreateField(
                "TestIntArrayField",
                (TinyType.Reference) TinyType.Int32,
                true);

            m_IntArrayEntity = m_Registry.CreateEntity(TinyId.New(), "TestEntity");
            var component = m_IntArrayEntity.AddComponent((TinyType.Reference) m_IntArrayComponentType);
            
            component["TestIntArrayField"] = new TinyList(m_Registry, (TinyType.Reference) TinyType.Int32)
            {
                3, 6, 9
            };
            
            m_StructType = m_Registry.CreateType(
                TinyId.New(),
                "TestStruct",
                TinyTypeCode.Struct);
            
            m_StructType.CreateField(
                "TestIntField",
                (TinyType.Reference) TinyType.Int32);
            
            m_StructArrayComponentType = m_Registry.CreateType(
                TinyId.New(),
                "TestComponent2",
                TinyTypeCode.Component);

            m_StructArrayComponentType.CreateField(
                "TestStructArrayField",
                (TinyType.Reference) m_StructType,
                true);
            
            m_StructArrayEntity = m_Registry.CreateEntity(TinyId.New(), "TestEntity2");
            var component2 = m_StructArrayEntity.AddComponent((TinyType.Reference) m_StructArrayComponentType);
            component2["TestStructArrayField"] = new TinyList(m_Registry, (TinyType.Reference) m_StructType)
            {
                new TinyObject(m_Registry,  (TinyType.Reference) m_StructType)
                {
                    ["TestIntField"] = 3
                },
                new TinyObject(m_Registry,  (TinyType.Reference) m_StructType)
                {
                    ["TestIntField"] = 6
                },
                new TinyObject(m_Registry,  (TinyType.Reference) m_StructType)
                {
                    ["TestIntField"] = 9
                }
            };
        }

        /// <summary>
        /// Ensures UnityEngine.Object values can be serialized correctly
        /// </summary>
        [Test]
        public void BinaryListValue()
        {
            // Output registry
            var registry = new TinyRegistry();
            
            using (var binary = new MemoryStream())
            using (var command = new MemoryStream())
            {
                Serialization.Binary.BinaryBackEnd.Persist(binary, m_IntArrayComponentType, m_IntArrayEntity);

                binary.Position = 0;

                Serialization.Binary.BinaryFrontEnd.Accept(binary, command);

                command.Position = 0;

                Serialization.CommandStream.CommandFrontEnd.Accept(command, registry);
            }
            
            AssertListValue(registry);
        }
        
        /// <summary>
        /// Ensures UnityEngine.Object values can be serialized correctly
        /// </summary>
        [Test]
        public void JsonListValue()
        {
            // Output registry
            var registry = new TinyRegistry();
            
            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                Serialization.Json.JsonBackEnd.Persist(json, m_IntArrayComponentType, m_IntArrayEntity);

                json.Position = 0;

                Serialization.Json.JsonFrontEnd.Accept(json, command);

                command.Position = 0;

                Serialization.CommandStream.CommandFrontEnd.Accept(command, registry);
            }
            
            AssertListValue(registry);
        }
        
        /// <summary>
        /// Ensures UnityEngine.Object values can be serialized correctly
        /// </summary>
        [Test]
        public void JsonStructListValue()
        {
            // Output registry
            var registry = new TinyRegistry();
            
            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                Serialization.Json.JsonBackEnd.Persist(json, m_StructType, m_StructArrayComponentType, m_StructArrayEntity);

                json.Position = 0;
                
                var reader = new StreamReader(json);
                {
                    UnityEngine.Debug.Log(reader.ReadToEnd());
                }
                
                json.Position = 0;

                Serialization.Json.JsonFrontEnd.Accept(json, command);

                command.Position = 0;

                Serialization.CommandStream.CommandFrontEnd.Accept(command, registry);
            }
        }

        private void AssertListValue(IRegistry registry)
        {
            var componentType = registry.FindById<TinyType>(m_IntArrayComponentType.Id);
            var entity = registry.FindById<TinyEntity>(m_IntArrayEntity.Id);
            var component = entity.GetComponent((TinyType.Reference) componentType);
            var list = component["TestIntArrayField"] as TinyList;
            
            Assert.IsNotNull(list);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(3, list[0]);
            Assert.AreEqual(6, list[1]);
            Assert.AreEqual(9, list[2]);
        }
    }
}

