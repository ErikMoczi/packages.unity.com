#if NET_4_6
using System.Linq;
using NUnit.Framework;
using Unity.Properties.Editor.Serialization;

namespace Unity.Properties.Tests.JSonSchema
{
    [TestFixture]
    internal class JsonPropertyContainerSchemaReaderTests
    {
        [Test]
        public void WhenEmptyStringForSchema_SchemaReader_ReturnsAnEmptyContainerList()
        {
            var result = JsonPropertyContainerSchemaReader.Read(string.Empty);
            Assert.Zero(result.Count);
        }

        [Test]
        public void WhenNoTypesInSchema_SchemaReadser_ReturnsAnEmptyContainerList()
        {
            var result = JsonPropertyContainerSchemaReader.Read(@"
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
        public void WhenValueType_SchemaReadser_ReturnsTypeTagAsStruct()
        {
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""SchemaVersion"": 1,
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""TypeId"": ""1"",
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""false"",
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
        public void WhenClassType_SchemaReadser_ReturnsTypeTagAsStruct()
        {
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""SchemaVersion"": 1,
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""TypeId"": ""1"",
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""true"",
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
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""SchemaVersion"": 1,
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""TypeId"": ""1"",
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""false"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""int"",
                                ""DefaultValue"": ""5""
                            },
                            ""Floats"": {
                                ""TypeId"": ""List"",
                                ""ItemTypeId"": ""float""
                            },
                            ""Ints"": {
                                ""TypeId"": ""Array"",
                                ""ItemTypeId"": ""int""
                            },
                            ""Struct"": {
                                ""TypeId"": ""SomeData""
                            }
                        }
                        }
                     ]
                 }
            ]
        ");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("HelloWorld", result[0].TypeName);
            Assert.IsTrue(result[0].Tag == PropertyTypeNode.TypeTag.Class);
            Assert.AreEqual(4, result[0].Children.Count);
            Assert.AreEqual(new System.Collections.Generic.List<string> {"Data", "Floats", "Ints", "Struct"},
                result[0].Children.Select(c => c.Name)
            );
            Assert.AreEqual(new System.Collections.Generic.List<string> { "int", "List", "Array", "SomeData" },
                result[0].Children.Select(c => c.TypeName)
            );
            Assert.AreEqual(new System.Collections.Generic.List<string> { "5", "", "", "" },
                result[0].Children.Select(c => c.DefaultValue)
            );
            Assert.AreEqual(new System.Collections.Generic.List<string> { "", "float", "int", "" },
                result[0].Children.Select(c => (c.Of != null ? c.Of.TypeName : string.Empty))
            );
        }
    }
}
#endif // NET_4_6
