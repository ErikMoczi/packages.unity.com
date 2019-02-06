

using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Tiny.Test
{
    /// <summary>
    /// These tests are to ensure that default values and overrides are preserved when passing through the serializaton pipeline
    /// </summary>
    [TestFixture]
    internal class DefaultValueSerializationTest
    {
        private IRegistry m_Registry;
        private TinyType m_ComponentType;
        private TinyEntity m_DefaultEntity;
        private TinyEntity m_OverridenEntity;

        private const int KTestFieldDefaultValue = 7;
        private const int KTestFieldOverrideValue = 7;
        
        [SetUp]
        public void SetUp()
        {
            m_Registry = new TinyContext(ContextUsage.Tests).Registry;

            // Create a component with a single int field
            m_ComponentType = m_Registry.CreateType(
                TinyId.New(),
                "TestComponent",
                TinyTypeCode.Component);

            m_ComponentType.CreateField(
                "TestIntField",
                (TinyType.Reference) TinyType.Int32);

            // Default the TestStruct.IntField to 7
            m_ComponentType.DefaultValue = new TinyObject(m_Registry, (TinyType.Reference) m_ComponentType)
            {
                ["TestIntField"] = KTestFieldDefaultValue
            };

            // Create an entity with our test component
            m_DefaultEntity = m_Registry.CreateEntity(TinyId.New(), "DefaultEntity");

            {
                var c = m_DefaultEntity.AddComponent((TinyType.Reference) m_ComponentType);
            }
            
            // Create another entity with our test component
            m_OverridenEntity = m_Registry.CreateEntity(TinyId.New(), "OverridenEntity");
            {
                var c = m_OverridenEntity.AddComponent((TinyType.Reference) m_ComponentType);
                c["TestIntField"] = KTestFieldOverrideValue;
            }
        }

        /// <summary>
        /// Ensure our default values pass basic tests
        /// @NOTE This is covered more thoroughly in "DefaultValueTest.cs"
        /// </summary>
        [Test]
        public void DefaultValueSanityCheck()
        {
            {
                var c = m_DefaultEntity.GetComponent((TinyType.Reference) m_ComponentType);
                
                // TestIntField should be defaulted to 7 and marked flagged as unchanged
                Assert.IsFalse(c.IsOverridden);
                Assert.AreEqual(KTestFieldDefaultValue, c["TestIntField"]);
            }
            
            {
                var c = m_OverridenEntity.GetComponent((TinyType.Reference) m_ComponentType);
                
                // TestIntField should be overriden to 10 and marked flagged as changed
                Assert.IsTrue(c.IsOverridden);
                Assert.AreEqual(KTestFieldOverrideValue, c["TestIntField"]);
            }
        }

        /// <summary>
        /// Write to Binary and read back
        /// </summary>
        [Test]
        public void BinaryDefaultValues()
        {
            // Output registry
            var registry = new TinyRegistry();

            using (var binary = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write our data to binary
                Serialization.Binary.BinaryBackEnd.Persist(binary, m_ComponentType, m_DefaultEntity, m_OverridenEntity);

                binary.Position = 0;

                // Read from binary to command stream
                Serialization.Binary.BinaryFrontEnd.Accept(binary, command);

                command.Position = 0;

                // Read from command stream to registry
                Serialization.CommandStream.CommandFrontEnd.Accept(command, registry);
            }

            AssertSerializationDefaultValues(registry);
        }
        
        /// <summary>
        /// Write to Json and read back
        /// </summary>
        [Test]
        public void JsonDefaultValues()
        {
            // Output registry
            var registry = new TinyRegistry();
            
            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                Debug.Log(Serialization.Json.JsonBackEnd.Persist(m_ComponentType, m_DefaultEntity, m_OverridenEntity));
                
                // Write our data to binary
                Serialization.Json.JsonBackEnd.Persist(json, m_ComponentType, m_DefaultEntity, m_OverridenEntity);

                json.Position = 0;
                
                // Read from binary to command stream
                Serialization.Json.JsonFrontEnd.Accept(json, command);
                
                command.Position = 0;
                
                // Read from command stream to registry
                Serialization.CommandStream.CommandFrontEnd.Accept(command, registry);
            }
            
            AssertSerializationDefaultValues(registry);
        }
        
        /// <summary>
        /// Write to CommandsStream and read back
        /// </summary>
        [Test]
        public void CommandStreamDefaultValues()
        {
            // Output registry
            var registry = new TinyRegistry();
            
            using (var command = new MemoryStream())
            {
                // Write from memory directly to the command stream
                Serialization.CommandStream.CommandBackEnd.Persist(command, m_ComponentType, m_DefaultEntity, m_OverridenEntity);
                
                command.Position = 0;
                
                // Read from command stream to registry
                Serialization.CommandStream.CommandFrontEnd.Accept(command, registry);
            }
            
            AssertSerializationDefaultValues(registry);
        }
        
        /// <summary>
        /// Helper method to make assertions based on the {FORMAT}SerializationDefaultValues tests
        /// </summary>
        /// <param name="registry"></param>
        private void AssertSerializationDefaultValues(IRegistry registry)
        {
            // Make sure the component type is transfered with its default values
            var componentType = registry.FindById<TinyType>(m_ComponentType.Id);
            {
                // Ensure we are dealing with the new type
                Assert.AreNotEqual(m_ComponentType, componentType);

                var defaultValue = componentType.DefaultValue as TinyObject;
                
                Assert.IsNotNull(defaultValue);
                Assert.AreEqual(KTestFieldDefaultValue, defaultValue["TestIntField"]);
            }

            var defaultEntity = registry.FindById<TinyEntity>(m_DefaultEntity.Id);
            {
                // Ensure we are dealing with the transfered entity
                Assert.AreNotEqual(m_DefaultEntity, defaultEntity);

                var c = defaultEntity.GetComponent((TinyType.Reference) componentType);
                
                // TestIntField should be defaulted to 7 and marked flagged as unchanged
                Assert.IsFalse(c.IsOverridden);
                Assert.AreEqual(KTestFieldDefaultValue, c["TestIntField"]);
            }
            
            var overrideEntity = registry.FindById<TinyEntity>(m_OverridenEntity.Id);
            {
                // Ensure we are dealing with the transfered entity
                Assert.AreNotEqual(m_OverridenEntity, overrideEntity);

                var c = overrideEntity.GetComponent((TinyType.Reference) componentType);
                
                // TestIntField should be overriden to 10 and marked flagged as changed
                Assert.IsTrue(c.IsOverridden);
                Assert.AreEqual(KTestFieldOverrideValue, c["TestIntField"]);
            }
        }
    }
}

