using System.Linq;
using UnityEngine;
using NUnit.Framework;
using Unity.Properties.Serialization;

namespace Unity.Properties.Tests.Schema
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
                        ""IsValueType"": false,
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
            Assert.AreEqual("HelloWorld", result[0].PropertyTypeNode.TypeName);
            Assert.AreEqual(4, result[0].PropertyTypeNode.children.Count);
            Assert.AreEqual(new System.Collections.Generic.List<string> {"Data", "Floats", "Ints", "Struct"},
                result[0].PropertyTypeNode.children.Select(c => c.Key)
            );
        }
    }
}