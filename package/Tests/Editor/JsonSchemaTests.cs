#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

using Unity.Properties.Editor.Serialization;

namespace Unity.Properties.Tests.JSonSchema
{
    [TestFixture]
    internal class JsonSchemaTests
    {
        [Test]
        public void WhenEmptyStringForSchema_SchemaReader_ReturnsAnEmptyContainerList()
        {
            var result = JsonSchema.FromJson(string.Empty);
            Assert.Zero(result.Count);
        }

        [Test]
        public void WhenNoTypesInSchema_SchemaReader_ReturnsAnEmptyContainerList()
        {
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""SchemaVersion"": 1,
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": []
                 }
            ]
        ");
            Assert.Zero(result.Count);
        }

        [Test]
        public void WhenClassType_SchemaReader_ReturnsTypeTagAsClass()
        {
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""SchemaVersion"": 1,
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""TypeId"": ""1"",
                        ""Name"": ""class HelloWorld"",
                        ""Properties"": { }
                      }
                    ]
                 }
            ]
        ");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("HelloWorld", result[0].TypeName);
            Assert.IsTrue(result[0].Tag == PropertyTypeNode.TypeTag.Class);
        }
        
        [Test]
        public void WhenValueType_SchemaReader_ReturnsTypeTagAsStruct()
        {
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""SchemaVersion"": 1,
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""TypeId"": ""1"",
                        ""Name"": ""struct HelloWorld"",
                        ""Properties"": { }
                      }
                    ]
                 }
            ]
        ");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("HelloWorld", result[0].TypeName);
            Assert.IsTrue(result[0].Tag == PropertyTypeNode.TypeTag.Struct);
        }

        [Test]
        public void WhenBasicTypesInSchema_SchemaReadser_ReturnsAValidContainerList()
        {
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""SchemaVersion"": 1,
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""TypeId"": ""1"",
                        ""Name"": ""struct HelloWorld"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""int"",
                                ""DefaultValue"": ""5""
                            },
                            ""Floats"": {
                                ""TypeId"": ""list"",
                                ""ItemTypeId"": ""float""
                            },
                            ""MyStruct"": {
                                ""TypeId"": ""struct SomeData""
                            }
                        }
                        }
                     ]
                 }
            ]
        ");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("HelloWorld", result[0].TypeName);
            Assert.IsTrue(result[0].Tag == PropertyTypeNode.TypeTag.Struct);
            Assert.AreEqual(3, result[0].Children.Count);
            Assert.AreEqual(new System.Collections.Generic.List<string> {"Data", "Floats", "MyStruct"},
                result[0].Children.Select(c => c.Name)
            );
            Assert.AreEqual(new System.Collections.Generic.List<string> { "int", "List", "SomeData" },
                result[0].Children.Select(c => c.TypeName)
            );
            Assert.AreEqual(new System.Collections.Generic.List<string> { "5", "", "" },
                result[0].Children.Select(c => c.DefaultValue)
            );
            Assert.AreEqual(new System.Collections.Generic.List<string> { "", "float", "" },
                result[0].Children.Select(c => (c.Of != null ? c.Of.TypeName : string.Empty))
            );
        }

        private static string GetJsonPropertyDefinitionWithName(
            string propertyQualifiedTypeName, List<string> nestedContainers)
        {
            return $@"
            {{
              ""TypeId"": ""1"",
              ""Name"": ""{propertyQualifiedTypeName}"",
              ""NestedPropertyContainers"": [{string.Join(",", nestedContainers)}],
              ""Properties"": {{
                ""Data"": {{
                  ""TypeId"": ""int"",
                  ""DefaultValue"": ""5""
                }},
                ""Floats"": {{
                  ""TypeId"": ""list"",
                  ""ItemTypeId"": ""float""
                }},
              }}
            }}";
        }

        // pre order
        private static void VisitContainer(
            List<PropertyTypeNode> containerNodes,
            Action<PropertyTypeNode> nodeFunc)
        {
            foreach (var node in containerNodes)
            {
                nodeFunc(node);
                VisitContainer(node.ChildContainers, nodeFunc);
            }
        }

        [Test]
        public void WhenNestedPropertyContainers_SchemaReadser_GeneratesNestedTypeTree()
        {
            var result = JsonSchema.FromJson($@"
            [
                {{
                    ""SchemaVersion"": 1,
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"":
                    [
                    {
                    GetJsonPropertyDefinitionWithName(
                        "class Root", new List<string>()
                        {
                            GetJsonPropertyDefinitionWithName("class Root>Foo", new List<string>()),
                            GetJsonPropertyDefinitionWithName("class Root>Bar", new List<string>()),
                            GetJsonPropertyDefinitionWithName("class Root>FooBar", new List<string>()
                            {
                                GetJsonPropertyDefinitionWithName("class Root>FooBar>BarFoo", new List<string>()),

                            }),
                        })
                    }
                    ]
                 }}
            ]
            ");

            Assert.AreEqual(1, result.Count);

            var classNames = new List<string>();
            VisitContainer(result, (PropertyTypeNode n) => { classNames.Add(n.Name); });

            Assert.AreEqual(
                new System.Collections.Generic.List<string>
                {
                    "Root", "Root>Foo", "Root>Bar", "Root>FooBar", "Root>FooBar>BarFoo"
                },
                classNames
            );
        }
    }
}
#endif // NET_4_6
