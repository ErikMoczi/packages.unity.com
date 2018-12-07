

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Tiny.Test
{
    [TestFixture]
    internal class CaretakerTest
    {
        TinyContext context;
        TinyRegistry registry;
        TinyCaretaker caretaker;

        [SetUp]
        public void Setup()
        {
            context = new TinyContext(ContextUsage.Tests);
            registry = context.Registry;
            caretaker = (TinyCaretaker)context.Caretaker;
        }

        [TearDown]
        public void Teardown()
        {
            context = null;
            registry = null;
            caretaker = null;
        }

        [Test]
        public void DetectTypeChanges()
        {
            // Create two new types
            var testStructType = registry.CreateType(TinyId.New(), "TestStructType", TinyTypeCode.Struct);
            registry.CreateType(TinyId.New(), "TestComponentType", TinyTypeCode.Struct);

            // Update to get the initial state; flush changes
            caretaker.Update();

            {
                // Make some changes to the data model
                // NOTE: We can make as many changes as we want with no callbacks being invoked. It is simply a version increment
                testStructType.CreateField("TestIntField", (TinyType.Reference) TinyType.Int32);
                testStructType.CreateField("TestStringField", (TinyType.Reference) TinyType.String);
                testStructType.Name = "OtherTestStructType";

                var count = 0;
                
                // Register for changed events
                caretaker.OnGenerateMemento += (originator, memento) =>
                {
                    count++;
                    Assert.AreEqual(testStructType, originator);
                };
                
                // Invoke update
                // This will detect any changes that were made between now and the last Update.
                caretaker.Update();

                // We should be notified that one object was changed
                Assert.AreEqual(1, count);
            }
        }
        
        [Test]
        public void RestoreTest_Simple()
        {
            // Create a type
            var testStructType = registry.CreateType(TinyId.New(), "TestStructType", TinyTypeCode.Struct);
            var testRef = (TinyType.Reference) testStructType;

            IMemento initialState = null;
            
            // Register for changed events
            caretaker.OnGenerateMemento += (originator, memento) =>
            {
                initialState = memento;
            };
            
            // Update to get the initial state; flush changes
            caretaker.Update();
            
            Assert.NotNull(initialState);

            {
                // Make some changes to the created type
                testStructType.Documentation.Summary = "Documentation for a struct";
                Assert.AreEqual(testStructType.Documentation.Summary, "Documentation for a struct");

                // revert them
                testStructType.Restore(initialState);

                testStructType = testRef.Dereference(context.Registry);
                Assert.NotNull(testStructType);
                
                Assert.AreEqual(testStructType.Documentation.Summary, string.Empty);
            }
        }

        /// <summary>
        /// Test:
        ///     1- Create a component with a single field of type int.
        ///     2- Change the field type to be of EntityReference.
        ///     3- Restore it back to its initial field type (Undo).
        ///     4- Restore the field type change (Redo).
        /// </summary>
        [Test]
        public void RestoreTest_FieldTypeChanged()
        {
            //////////////////////////////////////////////////////////////
            // Setup for this specific test.
            //////////////////////////////////////////////////////////////
            var initialFieldType = registry.FindByName<TinyType>("Int32");
            var expectedInitialType = typeof(int);
            var changedFieldType = registry.FindByName<TinyType>("EntityReference");
            var expectedChangedType = typeof(TinyEntity.Reference);

            IMemento state = null;

            // Register for changed events
            caretaker.OnGenerateMemento += (originator, memento) =>
            {
                state = memento;
            };

            //////////////////////////////////////////////////////////////
            // 1. Create a component with a single field of type int.
            //////////////////////////////////////////////////////////////
            var componentType = registry.CreateType(TinyId.New(), "Component", TinyTypeCode.Component);
            var field = componentType.CreateField(TinyId.New(), "Field", (TinyType.Reference)initialFieldType);
            componentType.Refresh();

            // Check default value
            {
                var defaultValue = componentType.DefaultValue as TinyObject;
                Assert.IsNotNull(defaultValue);
                Assert.IsTrue(defaultValue.IsDefaultValue);
                Assert.IsTrue(field.FieldType.Equals((TinyType.Reference)initialFieldType));
                Assert.AreEqual(expectedInitialType, defaultValue["Field"].GetType());
                Assert.AreEqual(defaultValue["Field"], 0);
            }

            Debug.Log($"Initial State: {componentType.Id}: {(componentType.DefaultValue as TinyObject)["Field"]}");

            // Update to get the initial state; flush changes
            caretaker.Update();
            IMemento initialState = state;
            Assert.NotNull(initialState);

            //////////////////////////////////////////////////////////////
            // 2- Change the field type to be of EntityReference.
            //////////////////////////////////////////////////////////////
            field.FieldType = (TinyType.Reference)changedFieldType;
            componentType.Refresh();

            // Check default value
            {
                var defaultValue = componentType.DefaultValue as TinyObject;
                Assert.IsNotNull(defaultValue);
                Assert.IsTrue(defaultValue.IsDefaultValue);
                Assert.IsTrue(field.FieldType.Equals((TinyType.Reference)changedFieldType));
                Assert.AreEqual(expectedChangedType, defaultValue["Field"].GetType());
                Assert.AreEqual(defaultValue["Field"], TinyEntity.Reference.None);
            }

            Debug.Log($"Changed State: {componentType.Id}: {(componentType.DefaultValue as TinyObject)["Field"]}");

            // Update to get the changed state; flush changes
            caretaker.Update();
            IMemento changedState = state;
            Assert.NotNull(changedState);
            Assert.AreNotEqual(initialState, changedState);
            Assert.IsTrue(initialState.Version < changedState.Version);

            //////////////////////////////////////////////////////////////
            // 3 - Restore it back to its initial field type (Undo).
            //////////////////////////////////////////////////////////////
            Debug.Log("Undo");
            Debug.Log($"Before: {componentType.Id}: {(componentType.DefaultValue as TinyObject)["Field"]}");
            componentType.Restore(initialState);
            // Note: Restoring is not in-place, so we need to re-set the references
            componentType = registry.FindById<TinyType>(componentType.Id);
            componentType.Refresh();
            field = componentType.Fields[0];
            Debug.Log($"After: {componentType.Id}: {(componentType.DefaultValue as TinyObject)["Field"]}");
            // Check default value
            {
                var defaultValue = componentType.DefaultValue as TinyObject;
                Assert.IsNotNull(defaultValue);
                Assert.IsTrue(defaultValue.IsDefaultValue);
                Assert.IsTrue(field.FieldType.Equals((TinyType.Reference)initialFieldType));
                Assert.AreEqual(expectedInitialType, defaultValue["Field"].GetType());
                Assert.AreEqual(defaultValue["Field"], 0);
            }

            // Update to get the changed state; flush changes
            caretaker.Update();

            //////////////////////////////////////////////////////////////
            // 4- Restore the field type change (Redo).
            //////////////////////////////////////////////////////////////
            Debug.Log("Redo");
            Debug.Log($"Before: {componentType.Id}: {(componentType.DefaultValue as TinyObject)["Field"]}");
            componentType.Restore(changedState);
            // Note: Restoring is not in-place, so we need to re-set the references
            componentType = registry.FindById<TinyType>(componentType.Id);
            componentType.Refresh();
            field = componentType.Fields[0];
            Debug.Log($"After: {componentType.Id}: {(componentType.DefaultValue as TinyObject)["Field"]}");
            // Check default value
            {
                var defaultValue = componentType.DefaultValue as TinyObject;
                Assert.IsNotNull(defaultValue);
                Assert.IsTrue(defaultValue.IsDefaultValue);
                Assert.IsTrue(field.FieldType.Equals((TinyType.Reference)changedFieldType));
                Assert.AreEqual(expectedChangedType, defaultValue["Field"].GetType());
                Assert.AreEqual(defaultValue["Field"], TinyEntity.Reference.None);
            }
        }

        [Test]
        public void RestoreTest_TinyEntity()
        {
            var compType = registry.CreateType(TinyId.New(), "TestComponent", TinyTypeCode.Component);
            var compTypeRef = (TinyType.Reference) compType;
            
            var testStructType = registry.CreateType(TinyId.New(), "TestStruct", TinyTypeCode.Struct);
            testStructType.CreateField(TinyId.New(), "IntField", (TinyType.Reference) TinyType.Int32);
            
            compType.CreateField(TinyId.New(), "TestStructField", (TinyType.Reference) testStructType);
            
            var undo = new Dictionary<TinyId, IMemento>();
            caretaker.OnGenerateMemento += (originator, memento) =>
            {
                undo[originator.Id] = memento;
            };

            var entity = registry.CreateEntity(TinyId.New(), "TestEntity");
            var entityRef = (TinyEntity.Reference) entity;
            
            var testCompInstance = entity.AddComponent(compTypeRef);
            testCompInstance.Refresh();

            var obj = new TinyObject(registry, (TinyType.Reference) testStructType)
            {
                ["IntField"] = 0
            };
            testCompInstance["TestStructField"] = obj;
            var item = (TinyObject)testCompInstance["TestStructField"];
            
            // Update to get the initial state; flush changes
            caretaker.Update();
            
            item["IntField"] = 123;
            Assert.AreEqual(123, item["IntField"]);

            // UNDO
            entity.Restore(undo[entity.Id]);

            entity = entityRef.Dereference(context.Registry);
            Assert.NotNull(entity);
            testCompInstance = entity.GetComponent(compTypeRef);
            Assert.NotNull(testCompInstance);
            item = (TinyObject) testCompInstance["TestStructField"];
            Assert.NotNull(item);
            
            // make sure IntField was restored
            Assert.AreEqual(0, item["IntField"]);
        }

        [Test]
        public void RestoreTest_Lists_Containers()
        {
            // Create two new types
            var testStructType = registry.CreateType(TinyId.New(), "TestStructType", TinyTypeCode.Struct);
            var testStructTypeRef = (TinyType.Reference) testStructType;
            
            var listField = testStructType.CreateField(TinyId.New(), "TestListField", (TinyType.Reference)TinyType.Float32, true);

            var undo = new Dictionary<TinyId, IMemento>();
            
            // Register for changed events
            caretaker.OnGenerateMemento += (originator, memento) =>
            {
                undo[originator.Id] = memento;
            };
            
            // Update to get the initial state; flush changes
            caretaker.Update();
            // note: TinyField proxies version storage onto TinyType, so there should be only 1 memento
            Assert.AreEqual(1, undo.Count);
            
            {
                // Make some changes to the created type
                testStructType.Documentation.Summary = "Some documentation";
                Assert.AreEqual(testStructType.Documentation.Summary, "Some documentation");
                listField.Name = "RevertMe";
                Assert.AreEqual(listField.Name, "RevertMe");

                // revert changes
                var kvp = undo.First();
                var obj = registry.FindById<TinyType>(kvp.Key);
                
                Assert.NotNull(obj);
                Assert.IsTrue(ReferenceEquals(obj, testStructType));
                
                obj.Restore(kvp.Value);

                obj = testStructTypeRef.Dereference(context.Registry);
                Assert.NotNull(obj);
                
                // the field was detached from the list and re-created
                Assert.AreEqual(1, obj.Fields.Count);
                var newListField = obj.Fields[0];
                
                Assert.AreEqual(string.Empty, obj.Documentation.Summary);
                Assert.AreEqual("TestListField", newListField.Name);
                Assert.AreEqual(listField.Id, newListField.Id);
                Assert.AreEqual(listField.DeclaringType.Id, newListField.DeclaringType.Id);
            }
        }

        [Test]
        public void DetectEntityChanges()
        {
            // Create a type and an entity
            var componentType = registry.CreateType(TinyId.New(), "TestStructType", TinyTypeCode.Component);
            var entity = registry.CreateEntity(TinyId.New(), "TestEntity");

            // Update to get the initial state; flush changes
            caretaker.Update();

            {
                // Snapshot the initial version
                var entityVersion = entity.Version;

                // Make some changes to the data model
                // NOTE: We can make as many changes as we want with no callbacks being invoked. It is simply a version increment
                entity.AddComponent((TinyType.Reference) componentType);
                entity.Name = "NewEntityName";

                var count = 0;
                
                // Register for changed events
                caretaker.OnGenerateMemento += (originator, memento) =>
                {
                    count++;
                    Assert.AreEqual(originator, entity);
                };
                
                // Invoke update
                // This will detect any changes that were made between now and the last Update.
                caretaker.Update();

                Assert.AreNotEqual(entityVersion, entity.Version);

                // We should be notified that one object was changed
                Assert.AreEqual(1, count);
            }
        }
        
        [Test]
        public void DetectComponentChanges()
        {
            // Create a type and an entity
            var componentType = registry.CreateType(TinyId.New(), "TestComponentType", TinyTypeCode.Component);
            var testField = componentType.CreateField("TestField", (TinyType.Reference) TinyType.Int32);
            var entity = registry.CreateEntity(TinyId.New(), "TestEntity");
            var component = entity.AddComponent((TinyType.Reference) componentType);
            component.Refresh();
            
            // Update to get the initial state; flush changes
            caretaker.Update();
                
            // Register for changed events
            caretaker.OnGenerateMemento += (originator, memento) =>
            {
                Debug.Log(memento);
            };

            {
                (componentType.DefaultValue as TinyObject)["TestField"] = 5;
                componentType.Refresh();
                
                // Invoke update
                // This will detect any changes that were made between now and the last Update.
                Debug.Log("-------------------- UPDATE --------------------");
                caretaker.Update();

                component["TestField"] = 10;
                
                // Invoke update
                // This will detect any changes that were made between now and the last Update.
                Debug.Log("-------------------- UPDATE --------------------");
                caretaker.Update();

                testField.FieldType = (TinyType.Reference) TinyType.String;
                component.Refresh();
                
                // Invoke update
                // This will detect any changes that were made between now and the last Update.
                Debug.Log("-------------------- UPDATE --------------------");
                caretaker.Update();
            }
        }

        [Test]
        public void DetectSceneChanges()
        {

        }

        [Test]
        public void PerformanceTest()
        {
            var vector3Type = registry.CreateType(
                TinyId.New(),
                "Vector3",
                TinyTypeCode.Struct);

            vector3Type.CreateField("X", (TinyType.Reference) TinyType.Float32);
            vector3Type.CreateField("Y", (TinyType.Reference) TinyType.Float32);
            vector3Type.CreateField("Z", (TinyType.Reference) TinyType.Float32);
            
            var transformType = registry.CreateType(
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
                    entities[i] = registry.CreateEntity(TinyId.New(), "Entity_" + i);
                    var transform = entities[i].AddComponent(transformTypeReference);
                
                    // if (i < kCount)
                    {
                        transform.Refresh(null, true);
                    
                        var position = transform["Position"] as TinyObject;
                        position["X"] = i * 2f;
                    }
                }
                
                watch.Stop();
                Debug.Log($"Create Entities=[{kCount}] {watch.ElapsedMilliseconds}ms");
            }
            
            caretaker.OnGenerateMemento += (originiator, memento) =>
            {
                // Force the callback
            };
            
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                caretaker.Update();
                
                watch.Stop();
                Debug.Log($"Caretaker.Update Entities=[{kCount}] {watch.ElapsedMilliseconds}ms");
            }
            
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                caretaker.Update();
                
                watch.Stop();
                Debug.Log($"Caretaker.Update Entities=[{kCount}] {watch.ElapsedMilliseconds}ms");
            }
        }
    }
}

