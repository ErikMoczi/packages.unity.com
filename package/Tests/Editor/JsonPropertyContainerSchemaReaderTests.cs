using System.ComponentModel.Design;
using UnityEngine;
using NUnit.Framework;
using Unity.Properties;
using Unity.Properties.Serialization;


[TestFixture]
class JsonPropertyContainerSchemaReaderTests
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
    }
}
