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
            Assert.Throws<Exception>(() => JsonSchema.FromJson(string.Empty));
        }

        [Test]
        public void WhenNoTypesInSchema_SchemaReader_ReturnsAnEmptyContainerList()
        {
            var result = JsonSchema.FromJson(
                new JsonSchemaBuilder()
                    .WithNamespace("Unity.Properties.Samples.Schema")
                    .WithEmptyContainerList()
                    .ToJson()
                );

            Assert.Zero(result.PropertyTypeNodes.Count);
        }

        [Test]
        public void WhenEmptyUsings_SchemaReader_DoesNotGenerateDefaults()
        {
            var result = JsonSchema.FromJson(
                new JsonSchemaBuilder()
                    .WithNamespace("Unity.Properties.Samples.Schema")
                    .WithUsing(new List<string> {})
                    .WithEmptyContainerList()
                    .ToJson()
            );

            Assert.Zero(result.UsingAssemblies.Count);
        }


        [Test]
        public void WhenUsingsSpecified_SchemaReader_DoesOverrideDefaults()
        {
            var result = JsonSchema.FromJson(
                new JsonSchemaBuilder()
                    .WithNamespace("Unity.Properties.Samples.Schema")
                    .WithUsing(new List<string> { "Unity.Properties.Editor" })
                    .WithEmptyContainerList()
                    .ToJson()
            );

            Assert.IsTrue(result.UsingAssemblies.Count == 1);
        }

        [Test]
        public void WhenClassWithNamespace_SchemaReader_ReturnsProperNamespaceForClass()
        {
            var result = JsonSchema.FromJson(
                new JsonSchemaBuilder()
                    .WithNamespace("Unity.Properties.Samples.Schema")
                    .WithContainer(
                        new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                            .WithEmptyPropertiesList()
                    )
                    .ToJson()
            );
            Assert.AreEqual(1, result.PropertyTypeNodes.Count);
            Assert.AreEqual("Unity.Properties.Samples.Schema", result.PropertyTypeNodes[0].Namespace);
        }

        [Test]
        public void WhenPropertyIsCustom_SchemaReader_ReturnsCustomProperty()
        {
            var result = JsonSchema.FromJson(
                new JsonSchemaBuilder()
                    .WithNamespace("Unity.Properties.Samples.Schema")
                    .WithContainer(
                        new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                            .WithProperty("Data", "int", "5", "", "", false, false, true)
                            .WithProperty("Int", "int")
                    )
                    .ToJson()
            );
            Assert.AreEqual(1, result.PropertyTypeNodes.Count);
            Assert.IsTrue(result.PropertyTypeNodes[0].Properties[0].IsCustomProperty);
            Assert.IsFalse(result.PropertyTypeNodes[0].Properties[1].IsCustomProperty);
        }

        [Test]
        public void WhenContainersetAsNoDefaultImplementation_SchemaReader_ReturnsNoDefaultImplementationFlagSet()
        {
            var result = JsonSchema.FromJson(
                new JsonSchemaBuilder()
                    .WithNamespace("Unity.Properties.Samples.Schema")
                    .WithContainer(
                        new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                            .WithNoDefaultImplementation()
                            .WithEmptyPropertiesList()
                    )
                    .ToJson()
            );
            Assert.AreEqual(1, result.PropertyTypeNodes.Count);
            Assert.IsTrue(result.PropertyTypeNodes[0].NoDefaultImplementation);
        }

        [TestCase("struct", ExpectedResult = PropertyTypeNode.TypeTag.Struct)]
        [TestCase("class", ExpectedResult = PropertyTypeNode.TypeTag.Class)]
        public PropertyTypeNode.TypeTag WhenClassType_SchemaReader_ReturnsTypeTagAsClass(string containerType)
        {
            var result = JsonSchema.FromJson(
                new JsonSchemaBuilder()
                    .WithNamespace("Unity.Properties.Samples.Schema")
                    .WithContainer(
                        new JsonSchemaBuilder.ContainerBuilder("HelloWorld", containerType)
                            .WithEmptyPropertiesList()
                        )
                    .ToJson()
                );
            Assert.AreEqual(1, result.PropertyTypeNodes.Count);
            Assert.AreEqual("HelloWorld", result.PropertyTypeNodes[0].TypeName);
            return result.PropertyTypeNodes[0].Tag;
        }

        [Test]
        public void WhenBasicTypesInSchema_SchemaReadser_ReturnsAValidContainerList()
        {
            var result = JsonSchema.FromJson(
                new JsonSchemaBuilder()
                    .WithNamespace("Unity.Properties.Samples.Schema")
                    .WithContainer(
                        new JsonSchemaBuilder.ContainerBuilder("HelloWorld", "struct")
                            .WithProperty("Data", "int", "5")
                            .WithProperty("Floats", "list", "", "float")
                            .WithProperty("MyStruct", "struct SomeData")
                    )
                    .ToJson()
                );

            Assert.AreEqual(1, result.PropertyTypeNodes.Count);
            Assert.AreEqual("HelloWorld", result.PropertyTypeNodes[0].TypeName);
            Assert.IsTrue(result.PropertyTypeNodes[0].Tag == PropertyTypeNode.TypeTag.Struct);
            Assert.AreEqual(3, result.PropertyTypeNodes[0].Properties.Count);
            Assert.AreEqual(new System.Collections.Generic.List<string> {"Data", "Floats", "MyStruct"},
                result.PropertyTypeNodes[0].Properties.Select(c => c.Name)
            );
            Assert.AreEqual(new System.Collections.Generic.List<string> { "int", "List", "SomeData" },
                result.PropertyTypeNodes[0].Properties.Select(c => c.TypeName)
            );
            Assert.AreEqual(new System.Collections.Generic.List<string> { "5", "", "" },
                result.PropertyTypeNodes[0].Properties.Select(c => c.DefaultValue)
            );
            Assert.AreEqual(new System.Collections.Generic.List<string> { "", "float", "" },
                result.PropertyTypeNodes[0].Properties.Select(c => (c.Of != null ? c.Of.TypeName : string.Empty))
            );
        }

        [Test]
        public void WhenPropertyIsNotPublic_SchemaReadser_Json()
        {
            // Do a roundtrip
            var result = JsonSchema.FromJson(
                JsonSchema.ToJson(
                    JsonSchema.FromJson(
                        new JsonSchemaBuilder()
                            .WithContainer(
                                new JsonSchemaBuilder.ContainerBuilder("HelloWorld", "struct")
                                    .WithProperty("Data", "int", "5", "", "", false, true)
                                    .WithProperty("Floats", "list", "", "float")
                            )
                            .ToJson()
                    )
                )
            );

            Assert.AreEqual(new List<bool> { true, false, },
                result.PropertyTypeNodes[0].Properties.Select(c => c.IsPublicProperty)
            );
        }

        private static string GetJsonContainerDefinitionWithName(
            string propertyQualifiedTypeName, List<string> nestedContainers)
        {
            return $@"
            {{
              ""Name"": ""{propertyQualifiedTypeName}"",
              ""NestedTypes"": [{string.Join(",", nestedContainers)}],
              ""Properties"":
                [
                {{
                  ""Name"": ""Data"",
                  ""Type"": ""int"",
                  ""DefaultValue"": ""5""
                }},
                {{
                  ""Name"": ""Floats"",
                  ""Type"": ""list"",
                  ""ItemType"": ""float""
                }},
              ]
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
                VisitContainer(node.NestedContainers, nodeFunc);
            }
        }

        [Test]
        public void WhenNestedPropertyContainers_SchemaReadser_GeneratesNestedTypeTree()
        {
            var result = JsonSchema.FromJson($@"
            [
                {{
                    ""Version"": ""{JsonSchema.CurrentVersion}"",
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"":
                    [
                    {
                    GetJsonContainerDefinitionWithName(
                        "Root", new List<string>()
                        {
                            GetJsonContainerDefinitionWithName("Root>Foo", new List<string>()),
                            GetJsonContainerDefinitionWithName("Root>Bar", new List<string>()),
                            GetJsonContainerDefinitionWithName("Root>FooBar", new List<string>()
                            {
                                GetJsonContainerDefinitionWithName("Root>FooBar>BarFoo", new List<string>()),

                            }),
                        })
                    }
                    ]
                 }}
            ]
            ");

            Assert.AreEqual(1, result.PropertyTypeNodes.Count);

            var classNames = new List<string>();
            VisitContainer(result.PropertyTypeNodes, (PropertyTypeNode n) => { classNames.Add(n.Name); });

            Assert.AreEqual(
                new List<string>
                {
                    "Root", "Root>Foo", "Root>Bar", "Root>FooBar", "Root>FooBar>BarFoo"
                },
                classNames
            );
        }
    }
}
#endif // NET_4_6
