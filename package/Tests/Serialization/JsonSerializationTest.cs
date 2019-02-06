using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Tiny.Test
{
    [TestFixture]
    internal class JsonSerializationTest
    {
        private IRegistry m_Registry;
        
        [SetUp]
        public void Setup()
        {
            m_Registry = new TinyContext(ContextUsage.Tests).Registry;
        }
        
        [Test]
        public void JsonProjectWrite()
        {
            var project = m_Registry.CreateProject(
                TinyId.New(),
                "TestProject");

            var json = Serialization.Json.JsonBackEnd.Persist(project);
            Debug.Log(json);
        }
        
        [Test]
        public void JsonProjectRoundTrip()
        {
            var project = m_Registry.CreateProject(
                TinyId.New(),
                "TestProject");
            
            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write the data model to a stream as json
                // mem -> json
                Serialization.Json.JsonBackEnd.Persist(json, project);

                json.Position = 0;

                var reader = new StreamReader(json);
                {
                    Debug.Log(reader.ReadToEnd());
                }
                
                json.Position = 0;

                // Read the data model
                // json -> commands
                Serialization.Json.JsonFrontEnd.Accept(json, command);

                command.Position = 0;

                // Create a registry to hold accepted objects
                var output = new TinyContext(ContextUsage.Tests).Registry;

                // Process the command 
                // commands -> mem
                Serialization.CommandStream.CommandFrontEnd.Accept(command, output);
            }
        }
        
        [Test]
        public void JsonTypeWrite()
        {
            var type = m_Registry.CreateType(
                TinyId.New(),
                "TestStruct",
                TinyTypeCode.Struct);

            type.CreateField("IntField", (TinyType.Reference) TinyType.Int32);
            type.CreateField("FloatField", (TinyType.Reference) TinyType.Int32);
            type.CreateField("StringField", (TinyType.Reference) TinyType.Int32);

            var json = Serialization.Json.JsonBackEnd.Persist(type);
            Debug.Log(json);
        }
        
        [Test]
        public void JsonSceneWrite()
        {
            var entityGroup = m_Registry.CreateEntityGroup(
                TinyId.New(),
                "TestEntityGroup");

            var entity = m_Registry.CreateEntity(
                TinyId.New(),
                "TestEntity");
            
            entityGroup.AddEntityReference((TinyEntity.Reference) entity);

            var json = Serialization.Json.JsonBackEnd.Persist(entityGroup, entity);
            Debug.Log(json);
        }


        [Test]
        public void JsonEntityRoundTrip()
        {
            var type = m_Registry.CreateType(
                TinyId.New(),
                "TestType",
                TinyTypeCode.Component
            );

            type.CreateField("TestIntField", (TinyType.Reference) TinyType.Int32);
            type.CreateField("TestStringField", (TinyType.Reference) TinyType.String);

            var entity = m_Registry.CreateEntity(
                TinyId.New(),
                "TestEntity");

            var component = entity.AddComponent((TinyType.Reference) type);

            component["TestIntField"] = 10;
            component["TestStringField"] = "Test";
            
            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write the data model to a stream as json
                // mem -> json
                Serialization.Json.JsonBackEnd.Persist(json, 
                    type,
                    entity);

                json.Position = 0;

                var reader = new StreamReader(json);
                {
                    Debug.Log(reader.ReadToEnd());
                }
                
                json.Position = 0;

                // Read the data model
                // json -> commands
                Serialization.Json.JsonFrontEnd.Accept(json, command);

                command.Position = 0;

                // Create a registry to hold accepted objects
                var output = new TinyContext(ContextUsage.Tests).Registry;

                // Process the command 
                // commands -> mem
                Serialization.CommandStream.CommandFrontEnd.Accept(command, output);
            }
        }

        /// <summary>
        /// Tests a round trip JSON serialization and deserialization
        /// </summary>
        [Test]
        public void JsonRoundTrip()
        {
            var structType = m_Registry.CreateType(
                TinyId.New(),
                "TestStruct",
                TinyTypeCode.Struct);

            structType.CreateField("IntField", (TinyType.Reference) TinyType.Int32);
            structType.CreateField("FloatField", (TinyType.Reference) TinyType.Int32);
            structType.CreateField("StringField", (TinyType.Reference) TinyType.Int32);

            var module = m_Registry.CreateModule(
                TinyId.New(),
                "TestModule");
            
            module.AddStructReference((TinyType.Reference) structType);

            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write the data model to a stream as json
                // mem -> json
                Serialization.Json.JsonBackEnd.Persist(json, 
                    structType, 
                    module);

                json.Position = 0;

                var reader = new StreamReader(json);
                {
                    Debug.Log(reader.ReadToEnd());
                }
                
                json.Position = 0;

                // Read the data model
                // json -> commands
                Serialization.Json.JsonFrontEnd.Accept(json, command);

                command.Position = 0;

                // Create a registry to hold accepted objects
                var output = new TinyContext(ContextUsage.Tests).Registry;

                // Process the command 
                // commands -> mem
                Serialization.CommandStream.CommandFrontEnd.Accept(command, output);

                Assert.IsNotNull(output.FindById<TinyType>(structType.Id));
                Assert.IsNotNull(output.FindById<TinyModule>(module.Id));
            }
        }

        /// <summary>
        /// Tests a round trip JSON serialization and deserialization
        /// </summary>
        [Test]
        public void JsonEntityPerformance()
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
            
            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write the data model to a stream as json
                // mem -> json

                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    
                    Serialization.Json.JsonBackEnd.Persist(json, entities);
                    
                    watch.Stop();
                    Debug.Log($"Json.BackEnd.Persist Entities=[{kCount}] {watch.ElapsedMilliseconds}ms Len=[{json.Position}]");
                }
                
                json.Position = 0;
                
                // Push the types to the command stream before the accept
                Serialization.CommandStream.CommandBackEnd.Persist(command, vector3Type, transformType);

                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    
                    // Read the data model
                    // json -> commands
                    Serialization.Json.JsonFrontEnd.Accept(json, command);
                    
                    watch.Stop();
                    Debug.Log($"Json.FrontEnd.Accept Entities=[{kCount}] {watch.ElapsedMilliseconds}ms Len=[{command.Position}]");
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

