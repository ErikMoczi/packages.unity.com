

using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Tiny.Test
{
    /// <summary>
    /// These tests are to ensure that enum value are preserved when passing through the serializaton pipeline
    /// </summary>
    [TestFixture]
    internal class EnumSerializationTest
    {
        private IRegistry m_Registry;
        private TinyType m_EnumType;
        private TinyType m_ComponentType;
        private TinyEntity m_Entity;

        [SetUp]
        public void SetUp()
        {
            m_Registry = new TinyContext(ContextUsage.Tests).Registry;

            m_EnumType = m_Registry.CreateType(
                TinyId.New(),
                "TestEnum",
                TinyTypeCode.Enum);

            m_EnumType.BaseType = (TinyType.Reference) TinyType.Int32;

            m_EnumType.CreateField("A", (TinyType.Reference) TinyType.Int32);
            m_EnumType.CreateField("B", (TinyType.Reference) TinyType.Int32);
            m_EnumType.CreateField("C", (TinyType.Reference) TinyType.Int32);

            m_EnumType.DefaultValue = new TinyObject(m_Registry, (TinyType.Reference) m_EnumType)
            {
                // @NOTE We are intentionally starting at 1 to detect 0 case as errors
                ["A"] = 1,
                ["B"] = 2,
                ["C"] = 3
            };

            // Create a component with a single int field
            m_ComponentType = m_Registry.CreateType(
                TinyId.New(),
                "TestComponent",
                TinyTypeCode.Component);

            m_ComponentType.CreateField(
                "TestEnumField",
                (TinyType.Reference) m_EnumType);

            m_Entity = m_Registry.CreateEntity(TinyId.New(), "TestEntity");
            var component = m_Entity.AddComponent((TinyType.Reference) m_ComponentType);
            component.Refresh();
        }

        /// <summary>
        /// Ensure our default values pass basic tests
        /// @NOTE This is covered more thoroughly in "DefaultValueTest.cs"
        /// </summary>
        [Test]
        public void DefaultValueSanityCheck()
        {
            var component = m_Entity.GetComponent((TinyType.Reference) m_ComponentType);
            var enumReference = (TinyEnum.Reference) component["TestEnumField"];

            // The default value has not been explicitly defined.
            // It should be set the the first field
            Assert.AreEqual(m_EnumType.Fields[0].Id, enumReference.Id);
            Assert.AreEqual(1, enumReference.Value);
        }

        /// <summary>
        /// Ensures enum values can be serialized correctly
        /// </summary>
        [Test]
        public void BinaryDefaultValue()
        {
            // Output registry
            var registry = new TinyContext(ContextUsage.Tests).Registry;

            using (var binary = new MemoryStream())
            using (var command = new MemoryStream())
            {
                Debug.Log(Serialization.Json.JsonBackEnd.Persist(m_EnumType, m_ComponentType, m_Entity));
                
                // Write our data to binary
                Serialization.Binary.BinaryBackEnd.Persist(binary, m_EnumType, m_ComponentType, m_Entity);

                binary.Position = 0;

                // Read from binary to command stream
                Serialization.Binary.BinaryFrontEnd.Accept(binary, command);

                command.Position = 0;

                // Read from command stream to registry
                Serialization.CommandStream.CommandFrontEnd.Accept(command, registry);
            }

            AssertDefaultValue(registry);
        }
        
        /// <summary>
        /// Ensures enum values can be serialized correctly
        /// </summary>
        [Test]
        public void JsonDefaultValue()
        {
            // Output registry
            var registry = new TinyContext(ContextUsage.Tests).Registry;

            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write our data to json
                Serialization.Json.JsonBackEnd.Persist(json, m_EnumType, m_ComponentType, m_Entity);

                json.Position = 0;

                // Read from json to command stream
                Serialization.Json.JsonFrontEnd.Accept(json, command);

                command.Position = 0;

                // Read from command stream to registry
                Serialization.CommandStream.CommandFrontEnd.Accept(command, registry);
            }

            AssertDefaultValue(registry);
        }
        
        
        /// <summary>
        /// Ensures enum values can be serialized correctly
        /// </summary>
        [Test]
        public void CommandStreamDefaultValue()
        {
            // Output registry
            var registry = new TinyContext(ContextUsage.Tests).Registry;

            using (var command = new MemoryStream())
            {
                // Write our data to command stream
                Serialization.CommandStream.CommandBackEnd.Persist(command, m_EnumType, m_ComponentType, m_Entity);

                command.Position = 0;

                // Read from command stream to registry
                Serialization.CommandStream.CommandFrontEnd.Accept(command, registry);
            }

            AssertDefaultValue(registry);
        }

        private void AssertDefaultValue(IRegistry registry)
        {
            var componentType = registry.FindById<TinyType>(m_ComponentType.Id);

            var entity = registry.FindById<TinyEntity>(m_Entity.Id);
            var component = entity.GetComponent((TinyType.Reference) componentType);

            var enumReference = (TinyEnum.Reference) component["TestEnumField"];

            // The default value has not been explicitly defined.
            // It should be set the the first field
            Assert.AreEqual(m_EnumType.Fields[0].Id, enumReference.Id);
            Assert.AreEqual(1, enumReference.Value);
        }
        
        /// <summary>
        /// Ensures enum values can be serialized correctly
        /// </summary>
        [Test]
        public void BinarySerializationOverrideValue()
        {
            // Output registry
            var registry = new TinyContext(ContextUsage.Tests).Registry;

            var component = m_Entity.GetComponent((TinyType.Reference) m_ComponentType);
            component["TestEnumField"] = new TinyEnum.Reference(m_EnumType, 3);

            using (var binary = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write our data to binary
                Serialization.Binary.BinaryBackEnd.Persist(binary, m_EnumType, m_ComponentType, m_Entity);

                binary.Position = 0;

                // Read from binary to command stream
                Serialization.Binary.BinaryFrontEnd.Accept(binary, command);

                command.Position = 0;

                // Read from command stream to registry
                Serialization.CommandStream.CommandFrontEnd.Accept(command, registry);
            }

            AssertOverrideValue(registry);
        }
        
        /// <summary>
        /// Ensures enum values can be serialized correctly
        /// </summary>
        [Test]
        public void JsonOverrideValue()
        {
            // Output registry
            var registry = new TinyContext(ContextUsage.Tests).Registry;

            var component = m_Entity.GetComponent((TinyType.Reference) m_ComponentType);
            component["TestEnumField"] = new TinyEnum.Reference(m_EnumType, 3);

            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write our data to json
                Serialization.Json.JsonBackEnd.Persist(json, m_EnumType, m_ComponentType, m_Entity);

                json.Position = 0;

                // Read from json to command stream
                Serialization.Json.JsonFrontEnd.Accept(json, command);

                command.Position = 0;

                // Read from command stream to registry
                Serialization.CommandStream.CommandFrontEnd.Accept(command, registry);
            }

            AssertOverrideValue(registry);
        }
        
        
        /// <summary>
        /// Ensures enum values can be serialized correctly
        /// </summary>
        [Test]
        public void CommandStreamOverrideValue()
        {
            // Output registry
            var registry = new TinyContext(ContextUsage.Tests).Registry;

            var component = m_Entity.GetComponent((TinyType.Reference) m_ComponentType);
            component["TestEnumField"] = new TinyEnum.Reference(m_EnumType, 3);

            using (var command = new MemoryStream())
            {
                // Write our data to command stream
                Serialization.CommandStream.CommandBackEnd.Persist(command, m_EnumType, m_ComponentType, m_Entity);

                command.Position = 0;

                // Read from command stream to registry
                Serialization.CommandStream.CommandFrontEnd.Accept(command, registry);
            }

            AssertOverrideValue(registry);
        }
        
        private void AssertOverrideValue(IRegistry registry)
        {
            var componentType = registry.FindById<TinyType>(m_ComponentType.Id);

            var entity = registry.FindById<TinyEntity>(m_Entity.Id);
            var component = entity.GetComponent((TinyType.Reference) componentType);
            component.Refresh();

            var enumReference = (TinyEnum.Reference) component["TestEnumField"];

            // The default value has not been overriden
            // It should be set the the last field
            Assert.AreEqual(m_EnumType.Fields[2].Id, enumReference.Id);
            Assert.AreEqual(3, enumReference.Value);
        }
    }
}

