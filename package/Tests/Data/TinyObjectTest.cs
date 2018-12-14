

using NUnit.Framework;
using UnityEngine;

namespace Unity.Tiny.Test
{
    [TestFixture]
    internal class TinyObjectTest
    {
        private IRegistry m_Registry;
        private TinyType m_TestStruct;
        private TinyType m_TestStructWithList;
        private TinyType m_TestComponent;
        
        [SetUp]
        public void SetUp()
        {
            m_Registry = new TinyContext(ContextUsage.Tests).Registry;
            
            m_TestStruct = m_Registry.CreateType(TinyId.New(), "TestStruct", TinyTypeCode.Struct);
            m_TestStruct.CreateField("Foo", (TinyType.Reference) TinyType.String);
            m_TestStruct.CreateField("Bar", (TinyType.Reference) TinyType.Int32);
            
            m_TestStructWithList = m_Registry.CreateType(TinyId.New(), "TestStructWithList", TinyTypeCode.Struct);
            m_TestStructWithList.CreateField("Foo", (TinyType.Reference) TinyType.String, true);
            m_TestStructWithList.CreateField("Bar", (TinyType.Reference) TinyType.Int32, true);
            
            m_TestComponent = m_Registry.CreateType(TinyId.New(), "TestComponent", TinyTypeCode.Component);
            m_TestComponent.CreateField("TestStructField", (TinyType.Reference) m_TestStruct);
        }

        [Test]
        public void TinyObject_Dynamic_PrimitiveValue()
        {
            // Untyped dynamic object
            var @object = new TinyObject(m_Registry, TinyType.Reference.None)
            {
                ["Foo"] = "Test",
                ["Bar"] = 10
            };
            
            Assert.AreEqual("Test", @object["Foo"]);
            Assert.AreEqual(10, @object["Bar"]);
            
            Debug.Log(@object.ToString());
        }
        
        
        [Test]
        public void TinyObject_Dynamic_PrimitiveValuePromotion()
        {
            // Untyped dynamic object
            var @object = new TinyObject(m_Registry, TinyType.Reference.None)
            {
                ["Foo"] = "Test",
                ["Bar"] = 10
            };

            // Assign a type to it
            @object.Type = (TinyType.Reference) m_TestStruct;
            
            // Dynamic values should be promoted to field values
            @object.Refresh();
            
            Assert.AreEqual("Test", @object["Foo"]);
            Assert.AreEqual(10, @object["Bar"]);
            
            Debug.Log(@object.ToString());
        }
        
        [Test]
        public void TinyObject_FieldRename()
        {
            var @object = new TinyObject(m_Registry, (TinyType.Reference) m_TestStruct)
            {
                ["Foo"] = "Test",
                ["Bar"] = 10
            };

            m_TestStruct.Fields[0].Name = "Baz";
            
            // Dynamic values should be promoted to field values
            @object.Refresh();
            
            Assert.AreEqual("Test", @object["Baz"]);
            Assert.AreEqual(10, @object["Bar"]);
            
            Debug.Log(@object.ToString());
        }
        
        [Test]
        public void TinyObject_Dynamic_NestedObject()
        {
            // Untyped dynamic object
            var @object = new TinyObject(m_Registry, TinyType.Reference.None)
            {
                ["TestStructField"] = new TinyObject(m_Registry, TinyType.Reference.None)
                {
                    ["Foo"] = "Test",
                    ["Bar"] = 10
                },
                ["Baz"] = 1.3F
            };
            
            Assert.AreEqual("Test", (@object["TestStructField"] as TinyObject)?["Foo"]);
            Assert.AreEqual(10, (@object["TestStructField"] as TinyObject)?["Bar"]);
            Assert.AreEqual(1.3F, @object["Baz"]);
            
            Debug.Log(@object.ToString());
        }
        
        [Test]
        public void TinyObject_Dynamic_NestedObjectPromotion()
        {
            // Untyped dynamic object
            var @object = new TinyObject(m_Registry, TinyType.Reference.None)
            {
                ["TestStructField"] = new TinyObject(m_Registry, TinyType.Reference.None)
                {
                    ["Dynamic"] = "Value",
                    ["Foo"] = "Test",
                    // Bar should be auto generated in its default state
                },
                ["Baz"] = 1.3f
            };

            @object.Type = (TinyType.Reference) m_TestComponent;
            @object.Refresh();
            
            Assert.AreEqual("Test", (@object["TestStructField"] as TinyObject)?["Foo"]);
            Assert.AreEqual(0, (@object["TestStructField"] as TinyObject)?["Bar"]);
            
            Debug.Log(@object.ToString());
        }
        
        [Test]
        public void TinyObject_Dynamic_List()
        {
            // Untyped dynamic object
            var @object = new TinyObject(m_Registry, TinyType.Reference.None)
            {
                ["Foo"] = new TinyList(m_Registry, TinyType.Reference.None)
                {
                    "a", "b", "c"
                },
                ["Bar"] = new TinyList(m_Registry, TinyType.Reference.None)
                {
                    3, 6, 9
                }
            };
            
            Debug.Log(@object.ToString());
        }
        
        [Test]
        public void TinyObject_Dynamic_ListPromotion()
        {
            // Untyped dynamic object
            var @object = new TinyObject(m_Registry, TinyType.Reference.None)
            {
                ["Foo"] = new TinyList(m_Registry, TinyType.Reference.None)
                {
                    "a", "b", "c"
                },
                ["Bar"] = new TinyList(m_Registry, TinyType.Reference.None)
                {
                    3, 6, 9
                }
            };

            @object.Type = (TinyType.Reference) m_TestStructWithList;
            @object.Refresh();
            
            Debug.Log(@object.ToString());
        }
    }
}

