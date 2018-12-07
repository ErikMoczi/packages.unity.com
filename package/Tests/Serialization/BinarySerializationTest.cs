using System.IO;
using NUnit.Framework;
using Unity.Properties;
using UnityEngine;

namespace Unity.Tiny.Test
{
    [TestFixture]
    internal class BinarySerializationTest
    {
        private IRegistry m_Registry;
        
        [SetUp]
        public void Setup()
        {
            m_Registry = new TinyContext(ContextUsage.Tests).Registry;
        }

        [Test]
        public void SimpleBinaryRoundTrip()
        {
            var entity = m_Registry.CreateEntity(TinyId.New(), "Entity");
            var entities = new IPropertyContainer[] {entity};
                
            using (var memory = new MemoryStream())
            {
                Serialization.Binary.BinaryBackEnd.Persist(memory, entities);
                memory.Position = 0;
                
                using (var commands = new MemoryStream())
                {
                    Serialization.Binary.BinaryFrontEnd.Accept(memory, commands);
                    commands.Position = 0;
                    
                    var output = new TinyRegistry();
                    Serialization.CommandStream.CommandFrontEnd.Accept(commands, output);

                    var readEntity = output.FindById<TinyEntity>(entity.Id);
                    Assert.NotNull(readEntity);
                }
            }
        }
        
        /// <summary>
        /// Tests a round trip JSON serialization and deserialization
        /// </summary>
        [Test]
        public void BinaryEntityPerformance()
        {
            var vector3Type = m_Registry.CreateType(
                TinyId.New(),
                "Vector3",
                TinyTypeCode.Struct);

            vector3Type.CreateField("X", (TinyType.Reference) TinyType.Float32);
            vector3Type.CreateField("Y", (TinyType.Reference) TinyType.Float32);
            vector3Type.CreateField("Z", (TinyType.Reference) TinyType.Float32);
            
            var transformType = m_Registry.CreateType(
                TinyId.New(),
                "Transform",
                TinyTypeCode.Component);
            
            transformType.CreateField("Position", (TinyType.Reference) vector3Type);
            transformType.CreateField("Scale", (TinyType.Reference) vector3Type);
            
            const int kCount = 1000;
            var entities = new IPropertyContainer[kCount];
            var transformTypeReference = (TinyType.Reference) transformType;

            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                for (var i = 0; i < kCount; i++)
                {
                    entities[i] = m_Registry.CreateEntity(TinyId.New(), "Entity_" + i);
                    var transform = ((TinyEntity)entities[i]).AddComponent(transformTypeReference);
                
                    // if (i < kCount)
                    {
                        transform.Refresh(null, true);
                    
                        var position = transform["Position"] as TinyObject;
                        position["X"] = i * 2f;
                    }
                }
                
                watch.Stop();
                Debug.Log($"Create Objects Entities=[{kCount}] {watch.ElapsedMilliseconds}ms");
            }
            
            using (var binary = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write the data model to a stream as json
                // mem -> command

                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    
                    Serialization.Binary.BinaryBackEnd.Persist(binary, entities);
                    
                    watch.Stop();
                    Debug.Log($"Binary.BackEnd.Persist Entities=[{kCount}] {watch.ElapsedMilliseconds}ms Len=[{binary.Position}]");
                }
                
                binary.Position = 0;
                
                // Push the types to the command stream before the entities
                Serialization.CommandStream.CommandBackEnd.Persist(command, vector3Type, transformType);
                
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    
                    Serialization.Binary.BinaryFrontEnd.Accept(binary, command);
                    
                    watch.Stop();
                    Debug.Log($"Binary.FrontEnd.Accept Entities=[{kCount}] {watch.ElapsedMilliseconds}ms Len=[{command.Position}]");
                }
                
                command.Position = 0;
                
                // Create a registry to hold accepted objects
                var output = new TinyContext(ContextUsage.Tests).Registry;

                // Process the command 
                // commands -> mem
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    
                    Serialization.CommandStream.CommandFrontEnd.Accept(command, output);
                    
                    watch.Stop();
                    Debug.Log($"CommandStream.FrontEnd.Accept Entities=[{kCount}] {watch.ElapsedMilliseconds}ms");
                }
            }
        }
    }
}

