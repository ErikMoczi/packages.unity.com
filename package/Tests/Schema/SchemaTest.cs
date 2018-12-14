

using NUnit.Framework;
using UnityEngine;

namespace Unity.Tiny.Test
{
    [TestFixture]
    internal class SchemaTest
    {
        private IRegistry Registry;
        
        [SetUp]
        public void Setup()
        {
            Registry = new TinyContext(ContextUsage.Tests).Registry;
        }
        
        /// <summary>
        /// Simple struct type creation with a single int field
        /// </summary>
        [Test]
        public void StructType()
        {
            var type = Registry.CreateType(
                TinyId.New(),
                "TestStruct",
                TinyTypeCode.Struct
            );
            
            type.CreateField(
                "TestField",
                (TinyType.Reference) TinyType.Int32);
        
            Assert.AreEqual(type.Fields.Count, 1);
        }
        
        /// <summary>
        /// Create type based UnityEngine.Object (e.g. Texture2D, Mesh)
        /// </summary>
        [Test]
        public void FieldTest()
        {
            var @enum = Registry.CreateType(
                TinyId.New(),
                "TestEnum",
                TinyTypeCode.Enum
            );

            @enum.BaseType = (TinyType.Reference) TinyType.Int32;
            @enum.CreateField("A", (TinyType.Reference) TinyType.Int32);

            var type = Registry.CreateType(
                TinyId.New(),
                "TestStruct",
                TinyTypeCode.Struct
            );
            
            type.CreateField(
                "TextureReference",
                (TinyType.Reference) TinyType.Texture2DEntity);
            
            type.CreateField(
                "EntityReference",
                (TinyType.Reference) TinyType.EntityReference);
            
            type.CreateField(
                "EnumReference",
                (TinyType.Reference) @enum);
            
            type.Refresh();
            
            Debug.Log(type);
        }

        [Test]
        public void NameChangeTest()
        {
            var type = Registry.CreateType(
                TinyId.New(),
                "TestStruct",
                TinyTypeCode.Struct
            );

            var module = Registry.CreateModule(
                TinyId.New(),
                "TestModule"
            );

            module.AddStructReference((TinyType.Reference) type);
            module.Refresh();

            type.Name = "NewStruct";
            
            Debug.Log(module.ToString());
        }
    }
}

