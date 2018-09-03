using UnityEngine;
using NUnit.Framework;
using Unity.Properties.Serialization;

namespace Unity.Properties.Tests.Schema
{
    [TestFixture]
    internal class CSharpGenerationBackendTests
    {
        [Test]
        public void WhenEmptyStringForSchema_CSharpCodeGen_ReturnsAnEmptyContainerList()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(string.Empty);
            var code = backend.Generate(result);
            Assert.Zero(code.Length);
        }

        [Test]
        public void WhenNoTypesInSchema_CSharpCodeGen_ReturnsAnEmptyContainerList()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""SchemaVersion"": 1,
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": []
                 }
            ]
        ");
            var code = backend.Generate(result);
            Assert.Zero(code.Length);
        }

        [Test]
        public void WhenBasicTypesInSchema_CSharpCodeGen_ReturnsAValidContainerList()
        {
            var backend = new CSharpGenerationBackend();
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
                                ""TypeId"": ""SomeData"",
                                ""IsValueType"": ""true""
                            }
                        }
                        }
                     ]
                 }
            ]
        ");
            var code = backend.Generate(result);
            Assert.NotZero(code.Length);
            Assert.IsTrue(code.ToString().Contains("Property<HelloWorld, int>"));
            Assert.IsTrue(code.ToString().Contains("Property<HelloWorld, List, float>"));
            Assert.IsTrue(code.ToString().Contains("Property<HelloWorld, Array, int>"));
            Assert.IsTrue(code.ToString().Contains("MutableStructProperty<HelloWorld, SomeData>"));
        }

        [Test]
        public void WhenTypeWithBackingFieldInSchema_CSharpCodeGen_DoesNotGeneratePrivateDataMembers()
        {
            var backend = new CSharpGenerationBackend();
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
                                ""DefaultValue"": ""5"",
                                ""BackingField"": ""backing""
                            },
                            ""Floats"": {
                                ""TypeId"": ""List"",
                                ""ItemTypeId"": ""float"",
                                ""BackingField"": ""backing""
                            },
                          }
                        }
                     ]
                 }
            ]
        ");
            var code = backend.Generate(result);
            Assert.NotZero(code.Length);
            Assert.IsFalse(code.ToString().Contains("m_Data"));
            Assert.IsFalse(code.ToString().Contains("m_Floats"));
            Assert.IsTrue(code.ToString().Contains(".backing"));
            Assert.IsTrue(code.ToString().Contains(".backing"));
        }
    }
}