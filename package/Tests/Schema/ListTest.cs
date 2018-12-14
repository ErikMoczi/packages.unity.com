
using NUnit.Framework;
using UnityEngine;

namespace Unity.Tiny.Test
{
    [TestFixture]
    internal class ListTest
    {
        private IRegistry m_Registry;

        [SetUp]
        public void Setup()
        {
            m_Registry = new TinyContext(ContextUsage.Tests).Registry;
        }
        
        [Test]
        public void PrimitiveList()
        {
            var list = new TinyList(m_Registry, (TinyType.Reference) TinyType.Int32)
            {
                1, 2, 3
            };
            
            Assert.AreEqual(3, list.Count);
            
            Debug.Log(list);
        }
        
        [Test]
        public void ObjectListVersionChange()
        {
            var type = m_Registry.CreateType(TinyId.New(), "TestType", TinyTypeCode.Struct);
            type.CreateField("TestField", (TinyType.Reference) TinyType.Int32);

            var list = new TinyList(m_Registry, (TinyType.Reference) type)
            {
                new TinyObject(m_Registry, (TinyType.Reference) type)
                {
                    ["TestField"] = 1
                },
                new TinyObject(m_Registry, (TinyType.Reference) type)
                {
                    ["TestField"] = 2
                },
                new TinyObject(m_Registry, (TinyType.Reference) type)
                {
                    ["TestField"] = 3
                }
            };

            var version = list.Version;
            
            (list[0] as TinyObject)["TestField"] = 7;
            
            Assert.AreNotEqual(version, list.Version);
            
            Debug.Log(list);
        }
        
        [Test]
        public void ListField()
        {
            var type = m_Registry.CreateType(TinyId.New(), "TestType", TinyTypeCode.Struct);
            type.CreateField("TestField", (TinyType.Reference) TinyType.Int32, true);

            var instance = new TinyObject(m_Registry, (TinyType.Reference) type)
            {
                ["TestField"] = new TinyList(m_Registry, (TinyType.Reference) TinyType.Int32)
                {
                    1, 2, 3
                }
            };

            Debug.Log(instance);
        }
        
        [Test]
        public void ListFieldPrimitiveAssignment()
        {
            var type = m_Registry.CreateType(TinyId.New(), "TestType", TinyTypeCode.Struct);
            type.CreateField("TestField", (TinyType.Reference) TinyType.Int32, true);

            var instance = new TinyObject(m_Registry, (TinyType.Reference) type)
            {
                ["TestField"] = new TinyList(m_Registry, (TinyType.Reference) TinyType.Int32)
                {
                    1,
                    2,
                    3
                }
            };

            instance["TestField"] = new TinyList(m_Registry, (TinyType.Reference) TinyType.Int32)
            {
                3,
                6,
                7
            };

            Debug.Log(instance);
        }
    }
}

