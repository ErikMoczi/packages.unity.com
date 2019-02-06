

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Unity.Tiny.Serialization.CommandStream;

namespace Unity.Tiny.Test
{
    [TestFixture]
    internal class CommandStreamTest
    {
        private IRegistry m_Registry;
        
        [SetUp]
        public void Setup()
        {
            m_Registry = new TinyContext(ContextUsage.Tests).Registry;
        }

        /// <summary>
        /// Tests a round trip command stream
        /// </summary>
        [Test]
        public void StreamingRoundTrip()
        {
            var type = m_Registry.CreateType(
                TinyId.New(),
                "TestStruct",
                TinyTypeCode.Struct);

            type.CreateField("IntField", (TinyType.Reference) TinyType.Int32);
            type.CreateField("FloatField", (TinyType.Reference) TinyType.Int32);
            type.CreateField("StringField", (TinyType.Reference) TinyType.Int32);

            var module = m_Registry.CreateModule(
                TinyId.New(),
                "TestModule");
            
            module.AddStructReference((TinyType.Reference) type);
            
            using (var command = new MemoryStream())
            {
                // Write the data model to a stream as json
                // mem -> command
                CommandBackEnd.Persist(command,  
                    type, 
                    module);

                command.Position = 0;

                // Create a registry to hold accepted objects
                var output = new TinyContext(ContextUsage.Tests).Registry;

                // Process the command 
                // commands -> mem
                CommandFrontEnd.Accept(command, output);

                Assert.IsNotNull(output.FindById<TinyType>(type.Id));
                Assert.IsNotNull(output.FindById<TinyModule>(module.Id));
            }
        }
        
        /// <summary>
        /// Tests a round trip JSON serialization and deserialization
        /// </summary>
        [Test]
        public void CommandStreamEntityPerformance()
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
            var entities = new TinyEntity[kCount];
            var transformTypeReference = (TinyType.Reference) transformType;

            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                for (var i = 0; i < kCount; i++)
                {
                    entities[i] = m_Registry.CreateEntity(TinyId.New(), "Entity_" + i);
                    var transform = entities[i].AddComponent(transformTypeReference);
                
                    // if (i < kCount)
                    {
                        var position = transform["Position"] as TinyObject;
                        position["X"] = i * 2f;
                    }
                }
                
                watch.Stop();
                Debug.Log($"Create Objects Entities=[{kCount}] {watch.ElapsedMilliseconds}ms");
            }
            
            using (var command = new MemoryStream())
            {
                // Write the data model to a stream as json
                // mem -> command

                // Push the types to the command stream before the accept
                CommandBackEnd.Persist(command, vector3Type, transformType);
                
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    
                    CommandBackEnd.Persist(command, (IEnumerable<TinyEntity>) entities);
                    
                    watch.Stop();
                    Debug.Log($"CommandStream.BackEnd.Persist Entities=[{kCount}] {watch.ElapsedMilliseconds}ms Len=[{command.Position}]");
                }
                
                command.Position = 0;

                // Create a registry to hold accepted objects
                var output = new TinyContext(ContextUsage.Tests).Registry;

                // Process the command 
                // commands -> mem
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    
                    CommandFrontEnd.Accept(command, output);
                    
                    watch.Stop();
                    Debug.Log($"CommandStream.FrontEnd.Accept Entities=[{kCount}] {watch.ElapsedMilliseconds}ms");
                }
            }
        }
    }
}

