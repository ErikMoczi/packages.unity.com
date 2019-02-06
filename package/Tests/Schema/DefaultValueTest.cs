using NUnit.Framework;

namespace Unity.Tiny.Test
{
    [TestFixture]
    internal class DefaultValueTest
    {
        private IRegistry m_Registry;

        [SetUp]
        public void Setup()
        {
            m_Registry = new TinyContext(ContextUsage.Tests).Registry;
        }
        
        
        /// <summary>
        /// Tests thats the defaultValue object is correctly initialized when creating new types
        /// </summary>
        [Test]
        public void TypeInitialDefaultValue()
        {
            var type = m_Registry.CreateType(
                TinyId.New(),
                "TestStruct",
                TinyTypeCode.Struct);

            type.CreateField(
                "TestIntField",
                (TinyType.Reference) TinyType.Int32);

            type.CreateField(
                "TestFloatField",
                (TinyType.Reference) TinyType.Float32);

            var defaultValue = type.DefaultValue as TinyObject;

            // Assert that we have some default value object that has been created for us
            Assert.IsNotNull(defaultValue);

            // Test the existance and value of the fields
            Assert.AreEqual(0, defaultValue["TestIntField"]);
            Assert.AreEqual(0f, defaultValue["TestFloatField"]);
        }

        /// <summary>
        /// Tests that compound types have the correct default values at creation
        /// </summary>
        [Test]
        public void NestedTypeInitialDefaultValue()
        {
            // Create a struct with a single int field
            var structType = m_Registry.CreateType(
                TinyId.New(),
                "TestStruct",
                TinyTypeCode.Struct);

            structType.CreateField(
                "TestIntField",
                (TinyType.Reference) TinyType.Int32);

            // Default the TestStruct.IntField to 7
            structType.DefaultValue = new TinyObject(m_Registry, (TinyType.Reference) structType)
            {
                ["TestIntField"] = 7
            };

            // Create a component with a single TestStruct field
            var componentType = m_Registry.CreateType(
                TinyId.New(),
                "TestComponent",
                TinyTypeCode.Component);

            componentType.CreateField(
                "TestStructField",
                (TinyType.Reference) structType);

            // Grab the default value for TestComponent
            var testComponentDefaultValue = componentType.DefaultValue as TinyObject;

            Assert.IsNotNull(testComponentDefaultValue);

            // Grab the TestComponent.TestStructField FIELD defaultValue
            // NOTE: This is NOT the same as the TestStruct TYPE defaultValue
            var testComponentTestStructFieldDefaultValue = testComponentDefaultValue["TestStructField"] as TinyObject;

            Assert.IsNotNull(testComponentTestStructFieldDefaultValue);

            Assert.AreNotEqual(testComponentDefaultValue, testComponentTestStructFieldDefaultValue);

            // This value should have been inherited from the type level but CAN be overriden
            Assert.AreEqual(7, testComponentTestStructFieldDefaultValue["TestIntField"]);
        }

        /// <summary>
        /// Tests that changing default values on compound types will correctly reflect to fields of that type
        /// </summary>
        [Test]
        public void NestedTypePropagateDefaultValueChange()
        {
            // Create a struct with 2 fields
            var testStructType = m_Registry.CreateType(
                TinyId.New(),
                "TestStructType",
                TinyTypeCode.Struct);

            testStructType.CreateField(
                "TestIntField",
                (TinyType.Reference) TinyType.Int32);

            testStructType.CreateField(
                "TestFloatField",
                (TinyType.Reference) TinyType.Float32);

            // Default the TestStruct.IntField to 7 and FloatField to 0.5f
            testStructType.DefaultValue = new TinyObject(m_Registry, (TinyType.Reference) testStructType)
            {
                ["TestIntField"] = 7,
                ["TestFloatField"] = 0.5f
            };

            // Create a component with a single TestStruct field
            var testComponentType = m_Registry.CreateType(
                TinyId.New(),
                "TestComponentType",
                TinyTypeCode.Component);

            testComponentType.CreateField(
                "TestStructField",
                (TinyType.Reference) testStructType);

            // Sanity check
            // NOTE: This is covered in other tests
            {
                var testComponentTypeDefaultValue = testComponentType.DefaultValue as TinyObject;
                Assert.IsNotNull(testComponentTypeDefaultValue);

                var testComponentTypeTestStructFieldDefaultValue = testComponentTypeDefaultValue["TestStructField"] as TinyObject;
                Assert.IsNotNull(testComponentTypeTestStructFieldDefaultValue);

                // This value should have been inherited from the type level but CAN be overridden
                Assert.AreEqual(7, testComponentTypeTestStructFieldDefaultValue["TestIntField"]);
                Assert.AreEqual(0.5, testComponentTypeTestStructFieldDefaultValue["TestFloatField"]);
            }

            {
                var testComponentTypeDefaultValue = (TinyObject) testComponentType.DefaultValue;
                Assert.IsNotNull(testComponentTypeDefaultValue);

                var testComponentTypeTestStructFieldDefaultValue = testComponentTypeDefaultValue["TestStructField"] as TinyObject;
                Assert.IsNotNull(testComponentTypeTestStructFieldDefaultValue);

                // Override the default value of the TestComponent.TestStructField.FloatField to 2.5f
                testComponentTypeTestStructFieldDefaultValue["TestFloatField"] = 2.5f;
            }

            {
                var testStructTypeDefaultValue = (TinyObject) testStructType.DefaultValue;
                Assert.IsNotNull(testStructTypeDefaultValue);

                // Update the default value of TestStruct.IntField to 10
                testStructTypeDefaultValue["TestIntField"] = 10;
            }

            {
                var testComponentTypeDefaultValue = (TinyObject) testComponentType.DefaultValue;
                Assert.IsNotNull(testComponentTypeDefaultValue);

                var testComponentTypeTestStructFieldDefaultValue = testComponentTypeDefaultValue["TestStructField"] as TinyObject;
                Assert.IsNotNull(testComponentTypeTestStructFieldDefaultValue);

                // The IntField should have been correctly updated while the float field should remain overridden
                Assert.AreEqual(10, testComponentTypeTestStructFieldDefaultValue["TestIntField"]);
                Assert.AreEqual(2.5f, testComponentTypeTestStructFieldDefaultValue["TestFloatField"]);
            }
        }

        /// <summary>
        /// Tests that an object can be reset to it's default values
        /// </summary>
        [Test]
        public void ResetObjectToDefaultValues()
        {
            // Create a type
            var type = m_Registry.CreateType(
                TinyId.New(),
                "TestStructType",
                TinyTypeCode.Struct);

            type.CreateField(
                "TestIntField",
                (TinyType.Reference) TinyType.Int32);

            type.CreateField(
                "TestFloatField",
                (TinyType.Reference) TinyType.Float32);

            // Default the TestStruct.IntField to 7 and FloatField to 0.5f
            type.DefaultValue = new TinyObject(m_Registry, (TinyType.Reference) type)
            {
                ["TestIntField"] = 7,
                ["TestFloatField"] = 0.5f
            };

            var @object = new TinyObject(m_Registry, (TinyType.Reference) type);

            Assert.AreEqual(7, @object["TestIntField"]);
            Assert.AreEqual(0.5f, @object["TestFloatField"]);

            @object["TestIntField"] = 1;
            @object["TestFloatField"] = 7.9f;

            Assert.AreEqual(1, @object["TestIntField"]);
            Assert.AreEqual(7.9f, @object["TestFloatField"]);

            @object.Reset();

            Assert.AreEqual(7, @object["TestIntField"]);
            Assert.AreEqual(0.5f, @object["TestFloatField"]);
        }

        [Test]
        public void EnumDefaultValue()
        {
            var enumType = m_Registry.CreateType(TinyId.New(), "TestEnum", TinyTypeCode.Enum);
            enumType.BaseType = (TinyType.Reference) TinyType.Int32;
            enumType.CreateField("A", (TinyType.Reference) TinyType.Int32);
            enumType.CreateField("B", (TinyType.Reference) TinyType.Int32);
            enumType.CreateField("C", (TinyType.Reference) TinyType.Int32);
            enumType.DefaultValue = new TinyObject(m_Registry, (TinyType.Reference) enumType)
            {
                ["A"] = 1,
                ["B"] = 2,
                ["C"] = 3,
            };

            var structType = m_Registry.CreateType(TinyId.New(), "TestStruct", TinyTypeCode.Struct);
            structType.CreateField("EnumField", (TinyType.Reference) enumType);
            structType.DefaultValue = new TinyObject(m_Registry, (TinyType.Reference) structType)
            {
                ["EnumField"] = new TinyEnum.Reference(enumType, "B")
            };
            
            var instance = new TinyObject(m_Registry, (TinyType.Reference) structType);
            Assert.AreEqual(2, ((TinyEnum.Reference) instance["EnumField"]).Value);
            Assert.AreEqual("B", ((TinyEnum.Reference) instance["EnumField"]).Name);
        }
    }
}

