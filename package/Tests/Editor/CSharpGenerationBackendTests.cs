#if NET_4_6
using UnityEngine;
using NUnit.Framework;
using Unity.Properties.Serialization;
using Unity.Properties.Editor.Serialization;

namespace Unity.Properties.Tests.JSonSchema
{
    [TestFixture]
    internal class CSharpGenerationBackendTests
    {
        [Test]
        public void WhenEmptyStringForSchema_CSharpCodeGen_ReturnsAnEmptyContainerList()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(string.Empty);
            backend.Generate(result);
            var code = backend.Code;
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
            backend.Generate(result);
            var code = backend.Code;
            Assert.Zero(code.Length);
        }

        [Test]
        public void WhenClassContainerWithBaseTypes_CSharpCodeGen_ReturnsAValidContainerList()
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
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            Assert.IsTrue(code.ToString().Contains("Property<HelloWorld, int>"));
            Assert.IsTrue(code.ToString().Contains("ListProperty<HelloWorld, List<float>, float>"));
            Assert.IsTrue(code.ToString().Contains("MutableContainerProperty<HelloWorld, SomeData>"));
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
                        ""IsValueType"": ""false"",
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
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            Assert.IsFalse(code.ToString().Contains("m_Data"));
            Assert.IsFalse(code.ToString().Contains("m_Floats"));
            Assert.IsTrue(code.ToString().Contains(".backing"));
            Assert.IsTrue(code.ToString().Contains(".backing"));
        }

        [Test]
        public void WhenValueTypeNotSpecified_CSharpCodeGen_GeneratesAClassContainerByDefault()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""Properties"": { }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            Assert.IsTrue(code.ToString().Contains("public partial class HelloWorld"));
        }

        [Test]
        public void WhenBackingFieldGenerated_CSharpCodeGen_TheyAreGeneratedAsPrivate()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""Foo"",
                                ""IsValueType"": ""true"",
                            },
                            ""Foo"": {
                                ""TypeId"": ""int""
                            },
                        }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            Assert.IsTrue(code.ToString().Contains("private int m_Foo;"));
            Assert.IsTrue(code.ToString().Contains("private readonly Foo m_Data;"));
        }

        [Test]
        public void WhenValueTypeNotSpecified_CSharpCodeGen_GeneratesAStructContainer()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""true"",
                        ""Properties"": { }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            Assert.IsTrue(code.ToString().Contains("public partial struct HelloWorld"));
        }

        [Test]
        public void WhenIsStructContainerContainsStructProperty_CSharpCodeGen_GeneratesProperyContainerWrapper()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""true"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""Foo"",
                                ""IsValueType"": ""true"",
                            },
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            Assert.IsTrue(code.ToString().Contains("StructMutableContainerProperty<HelloWorld, Foo>"));
        }

        [Test]
        public void WhenIsClassContainerContainsStructProperty_CSharpCodeGen_GeneratesProperyContainerWrapper()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""false"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""Foo"",
                                ""IsValueType"": ""true"",
                            },
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            Assert.IsTrue(code.ToString().Contains("ContainerProperty<HelloWorld, Foo>"));
        }

        [Test]
        public void WhenContainsStructProperty_CSharpCodeGen_GeneratesGetSetValueWithRef()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""true"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""Foo"",
                                ""IsValueType"": ""true"",
                            },
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            Assert.IsTrue(code.ToString().Contains("GetValue(ref this"));
        }

        [Test]
        public void WhenIsClassPropertyContainer_CSharpCodeGen_GeneratesGetSetValueWithNoRef()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""false"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""Foo"",
                                ""IsValueType"": ""true"",
                            },
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            Assert.IsTrue(code.ToString().Contains("GetValue(this"));
        }

        [Test]
        public void WhenIsClassProperty_CSharpCodeGen_GeneratesGetSetValueWithNoRef()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""false"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""int"",
                            },
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            Assert.IsTrue(code.ToString().Contains("GetValue(this"));
            Assert.IsTrue(code.ToString().Contains("SetValue(this"));
        }

        [Test]
        public void WhenIsClassProperty_CSharpCodeGen_DelegatesDefaultValueBackingFieldConstruction()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""false"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""int"",
                                ""DefaultValue"": ""5"",
                            },
                            ""Ints"": {
                                ""TypeId"": ""List"",
                                ""ItemTypeId"": ""float"",
                            },
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            Assert.IsTrue(code.ToString().Contains("int m_Data;"));
            Assert.IsTrue(code.ToString().Contains("m_Data = 5;"));
            Assert.IsTrue(code.ToString().Contains("m_Ints = new List<float> {};"));
        }

        [Test]
        public void WhenIsStructProperty_CSharpCodeGen_DefaultValueBackingFieldConstructionIsDoneInPlace()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""true"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""int"",
                                ""DefaultValue"": ""5"",
                            },
                            ""Ints"": {
                                ""TypeId"": ""List"",
                                ""ItemTypeId"": ""float"",
                            },
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            // TODO tokenize
            Assert.IsTrue(code.ToString().Contains("private int m_Data= 5;"));
            Assert.IsTrue(code.ToString().Contains("private readonly List<float> m_Ints= new List<float> {};"));
        }

        [Test]
        public void WhenIsStructProperty_CSharpCodeGen_NoDefaultValue()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""true"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""int"",
                            },
                            ""Ints"": {
                                ""TypeId"": ""List"",
                                ""ItemTypeId"": ""float"",
                            },
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            // TODO tokenize
            Assert.IsTrue(code.ToString().Contains("private int m_Data;"));
            Assert.IsTrue(code.ToString().Contains("private readonly List<float> m_Ints= new List<float> {};"));
        }

        [Test]
        public void WhenIsStructContainer_CSharpCodeGen_DoesNotGenerateConstructor()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""true"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""int"",
                            },
                            ""Ints"": {
                                ""TypeId"": ""List"",
                                ""ItemTypeId"": ""float"",
                            },
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            // TODO tokenize
            Assert.IsFalse(code.ToString().Contains("public HelloWorld ()"));
        }

        [Test]
        public void WhenIsClassContainer_CSharpCodeGen_DoesNotGenerateConstructor()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""false"",
                        ""Properties"": {
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            // TODO tokenize
            Assert.IsFalse(code.ToString().Contains("public HelloWorld ()"));
        }

        [Test]
        public void WhenIsClassContainerAndNoProperties_CSharpCodeGen_DoesNotGenerateConstructor()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""false"",
                        ""Properties"": {
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            // TODO tokenize
            Assert.IsFalse(code.ToString().Contains("public HelloWorld ()"));
        }

        [Test]
        public void WhenUserHookSpecified_CSharpCodeGen_GeneratesIt()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""true"",
                        ""GeneratedUserHooks"": ""OnPropertyConstructed"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""int"",
                            },
                            ""Ints"": {
                                ""TypeId"": ""List"",
                                ""ItemTypeId"": ""float"",
                            },
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            // TODO tokenize
            Assert.IsTrue(code.ToString().Contains("partial static void OnPropertyBagConstructed(IPropertyBag bag);"));
            Assert.IsTrue(code.ToString().Contains("OnPropertyBagConstructed(sProperties);"));
        }

        [Test]
        public void WhenOverrideDefaultBaseClasseClass_CSharpCodeGen_GeneratesContainerWithOverridenBaseClass()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""OverrideDefaultBaseClass"": ""Foo"",
                        ""Properties"": { }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            // TODO tokenize, bad britle test
            Assert.IsTrue(code.ToString().Contains("public partial class HelloWorld : Foo"));
        }

        [Test]
        public void WhenPropertyContainerSetAsAbstract_CSharpCodeGen_GeneratesAbstractClass()

        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsAbstractClass"": ""true"",
                        ""Properties"": { }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            // TODO tokenize, bad britle test
            Assert.IsTrue(code.ToString().Contains("public partial abstract class HelloWorld : IPropertyContainer"));
        }

        [Test]
        public void WhenPropertyContainerSetAsAbstractForStructContainer_CSharpCodeGen_DoestNotGeneratesAbstractStruct()

        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsAbstractClass"": ""true"",
                        ""IsValueType"": ""true"",
                        ""Properties"": {
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            // TODO tokenize, bad britle test
            Assert.IsTrue(code.ToString().Contains("public partial struct HelloWorld : IPropertyContainer"));
        }

        [Test]
        public void WhenPropertyIsInherited_CSharpCodeGen_DoestNotGeneratesBackingField()

        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsAbstractClass"": ""true"",
                        ""IsValueType"": ""true"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""int"",
                                ""InheritedPropertyFrom"": ""BaseClass"",
                            },
                        }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            // TODO tokenize, bad britle test, should use proper test for backing field existance
            Assert.IsFalse(code.ToString().Contains("int m_Data;"));
            Assert.IsFalse(code.ToString().Contains("s_DataProperty ="));
            Assert.IsTrue(code.ToString().Contains("s_DataProperty"));
        }

        [Test]
        public void WhenPropertyDeclaredAsReadonly_CSharpCodeGen_GeneratesReadonlyPropertyAccessor()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonPropertyContainerSchemaReader.Read(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""IsValueType"": ""true"",
                        ""GeneratedUserHooks"": ""OnPropertyConstructed"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""int"",
                                ""IsReadonlyProperty"": ""true"",
                            },
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);
            // TODO tokenize, very BAD
            Assert.IsTrue(code.ToString().Contains("/* GET */"));
            Assert.IsTrue(code.ToString().Contains("/* SET */ null"));
        }
    }
}
#endif // NET_4_6
