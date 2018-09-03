#if USE_ROSLYN_API && (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Properties.Editor.Serialization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Mono.Cecil;

namespace Unity.Properties.Tests.JSonSchema
{
    [TestFixture]
    internal class ReflectionJsonSchemaTests
    {
        [Test]
        public void WhenNullAssembly_ReflectionJsonSchemaGenerator_Throws()
        {
            AssemblyDefinition assembly = null;
            Assert.Throws<Exception>(() => ReflectionPropertyTree.Read(assembly));
        }

        [Test]
        public void WhenAssemblyDoesNotContainPropertyContainers_ReflectionJsonSchemaGenerator_ReturnsAnEmptyJson()
        {
            string assemblyFilePath = string.Empty;
            string errors = string.Empty;

            string code = @"
                using System.Collections.Generic;
                public partial class HelloWorld
                {
                    public class Foo
                    {
                        public int Data;
                        public List<float> Floats = new List<float>();
                    }
                    public Foo foo { get; } = new Foo();
                };
            ";

            Assert.IsTrue(CompileTestUtils.TryCompileToFile(code, out assemblyFilePath, out errors), errors);

            using (new FileDisposer(assemblyFilePath))
            {
                var result = JsonSchema.FromJson(
                    JsonSchema.ToJson(
                        new JsonSchema()
                        {
                            PropertyTypeNodes = ReflectionPropertyTree.Read(assemblyFilePath)
                        }
                    )
                );

                var containers = new List<PropertyTypeNode>();

                VisitContainer(result.PropertyTypeNodes, c => { containers.Add(c); });

                Assert.Zero(containers.Count);
            }
        }

        [Test]
        public void WhenAssemblyContainsPropertyContainer_ReflectionJsonSchemaGenerator_ReturnsAValidJson()
        {
            string assemblyFilePath = string.Empty;
            string errors = string.Empty;

            string code = @"
                using System.Collections.Generic;
                using Unity.Properties;
                
                namespace Unity.Properties.TestCases {

                public partial class HelloWorld : IPropertyContainer
                {
                    public static IPropertyBag bag { get; } = new PropertyBag(new List<IProperty> {}.ToArray());

                    public IVersionStorage VersionStorage { get; }
                    public IPropertyBag PropertyBag => bag;

                    public class Foo
                    {
                        public int Data;
                        public List<float> Floats = new List<float>();
                    }
                    public Foo foo { get; } = new Foo();
                };
                }
            ";

            Assert.IsTrue(CompileTestUtils.TryCompileToFile(code, out assemblyFilePath, out errors), errors);

            using (new FileDisposer(assemblyFilePath))
            {
                var result = JsonSchema.FromJson(
                    JsonSchema.ToJson(
                        new JsonSchema()
                        {
                            PropertyTypeNodes = ReflectionPropertyTree.Read(assemblyFilePath)
                        }
                    )
                );

                var containers = new List<PropertyTypeNode>();

                VisitContainer(result.PropertyTypeNodes, c => { containers.Add(c); });

                Assert.True(containers.Count == 1);
                Assert.True(containers[0].TypeName == "HelloWorld");
            }
        }

        [Test]
        public void WhenAssemblyPropertyContainer_ReflectionJsonSchemaGenerator_ReturnsAValidJson()
        {
            string assemblyFilePath = string.Empty;
            string errors = string.Empty;

            string code = @"
                using System.Collections.Generic;
                using Unity.Properties;
                
                namespace Unity.Properties.TestCases {

                public partial class HelloWorld : IPropertyContainer
                {
                    public static IPropertyBag bag { get; } = new PropertyBag(new List<IProperty> {}.ToArray());

                    public IVersionStorage VersionStorage { get; }
                    public IPropertyBag PropertyBag => bag;

                    private MyContainer m_MyContainer;
                    private static readonly ContainerProperty<HelloWorld, MyContainer> s_MyContainer =
                        new ContainerProperty<HelloWorld, MyContainer>(
                            ""MyContainer"",
                        c => c.m_MyContainer,
                        (c, v) => c.m_MyContainer = v);

                    public class MyContainer : IPropertyContainer
                    {
                        public static IPropertyBag bag { get; } = new PropertyBag(new List<IProperty> {}.ToArray());

                        public IVersionStorage VersionStorage { get; }
                        public IPropertyBag PropertyBag => bag;
                    }
                };
                
                }
            ";

            Assert.IsTrue(CompileTestUtils.TryCompileToFile(code, out assemblyFilePath, out errors), errors);

            using (new FileDisposer(assemblyFilePath))
            {
                var result = JsonSchema.FromJson(
                    JsonSchema.ToJson(
                        new JsonSchema()
                        {
                            PropertyTypeNodes = ReflectionPropertyTree.Read(assemblyFilePath)
                        }
                    )
                );

                var containers = new List<PropertyTypeNode>();

                VisitContainer(result.PropertyTypeNodes, c => { containers.Add(c); });

                Assert.True(containers.Count == 2);
                Assert.True(containers[0].TypeName == "HelloWorld");
                Assert.True(containers[1].TypeName == "MyContainer");
            }
        }


        [Test]
        public void WhenPropertyIsPublic_ReflectionJsonSchemaGenerator_SetsThePropertyAsPublicInTypeTree()
        {
            string assemblyFilePath = string.Empty;
            string errors = string.Empty;

            string code = @"
                using System.Collections.Generic;
                using Unity.Properties;
                
                namespace Unity.Properties.TestCases {

                public partial class HelloWorld : IPropertyContainer
                {
                    public static IPropertyBag bag { get; } = new PropertyBag(new List<IProperty> {}.ToArray());

                    public IVersionStorage VersionStorage { get; }
                    public IPropertyBag PropertyBag => bag;

                    private int m_MyContainer;
                    public static readonly Property<HelloWorld, int> s_MyField =
                        new Property<HelloWorld, int>(
                            ""MyField"",
                            c => c.m_MyContainer,
                            (c, v) => c.m_MyContainer = v);
                    };
                }
            ";

            Assert.IsTrue(CompileTestUtils.TryCompileToFile(code, out assemblyFilePath, out errors), errors);

            using (new FileDisposer(assemblyFilePath))
            {
                var result = ReflectionPropertyTree.Read(assemblyFilePath);
                Assert.True(result[0].Properties[0].IsPublicProperty);
            }
        }

        [Test]
        public void WhenAssemblyContainsNestedPropertyContainers_ReflectionJsonSchemaGenerator_ReturnsAValidJson()
        {
            string assemblyFilePath = string.Empty;
            string errors = string.Empty;

            string code = @"
                using System.Collections.Generic;
                using Unity.Properties;

                namespace Unity.Properties.TestCases {

                public partial class HelloWorld : IPropertyContainer
                {
                    public static IPropertyBag bag { get; } = new PropertyBag(new List<IProperty> {}.ToArray());

                    public IVersionStorage VersionStorage { get; }
                    public IPropertyBag PropertyBag => bag;

                    public class Foo : IPropertyContainer
                    {
                        public static IPropertyBag bag { get; } = new PropertyBag(new List<IProperty> {}.ToArray());

                        public IVersionStorage VersionStorage { get; }
                        public IPropertyBag PropertyBag => bag;

                        public class Bar : IPropertyContainer
                        {
                            public static IPropertyBag bag { get; } = new PropertyBag(new List<IProperty> {}.ToArray());

                            public IVersionStorage VersionStorage { get; }
                            public IPropertyBag PropertyBag => bag;
                        }
                    }
                };

                }
            ";

            Assert.IsTrue(CompileTestUtils.TryCompileToFile(code, out assemblyFilePath, out errors), errors);

            using (new FileDisposer(assemblyFilePath))
            {
                var result = JsonSchema.FromJson(
                    JsonSchema.ToJson(
                        new JsonSchema()
                        {
                            PropertyTypeNodes = ReflectionPropertyTree.Read(assemblyFilePath)
                        }
                    )
                );

                var containers = new List<PropertyTypeNode>();

                VisitContainer(result.PropertyTypeNodes, c => { containers.Add(c); });

                var containerNames = containers.Select(c => c.TypeName).ToList();

                Assert.AreEqual(
                    new System.Collections.Generic.List<string>
                    {
                        "HelloWorld", "Foo", "Bar"
                    },
                    containerNames
                );
            }
        }

        [TestCase("struct", ExpectedResult = "Foo")]
        [TestCase("class", ExpectedResult = "Foo")]
        public string TestContainsPropertyContainerNestedInsidePlainStructOrClass(string parentType)
        {
            string assemblyFilePath = string.Empty;
            string errors = string.Empty;

            string code = $@"
                using System.Collections.Generic;
                using Unity.Properties;
                public {parentType} HelloWorld
                {{
                    public class Foo : IPropertyContainer
                    {{
                        public static IPropertyBag bag {{ get; }} = new PropertyBag(new List<IProperty> {{}}.ToArray());

                        public IVersionStorage VersionStorage {{ get; }}
                        public IPropertyBag PropertyBag => bag;
                    }}
                    public Foo foo {{ get; }}
                }};
            ";

            Assert.IsTrue(CompileTestUtils.TryCompileToFile(code, out assemblyFilePath, out errors), errors);

            using (new FileDisposer(assemblyFilePath))
            {
                var result = JsonSchema.FromJson(
                    JsonSchema.ToJson(
                        new JsonSchema()
                        {
                            PropertyTypeNodes = ReflectionPropertyTree.Read(assemblyFilePath)
                        }
                    )
                );

                var containers = new List<PropertyTypeNode>();

                VisitContainer(result.PropertyTypeNodes, c => { containers.Add(c); });

                Assert.True(containers.Count == 1);

                return containers[0].TypeName;
            }
        }
        
        private class FileDisposer : IDisposable
        {
            private string _filename = string.Empty;

            public FileDisposer(string filename)
            {
                _filename = filename;
            }

            public void Dispose()
            {
                if (File.Exists(_filename))
                {
                    File.Delete(_filename);
                }
            }
        }

        private class PropertyLeafVisitor : PropertyVisitor
        {
            public List<string> Properties { get; private set; } = new List<string>();

            protected override void Visit<TValue>(TValue value)
            {
                Properties.Add(value.ToString());
            }
        }

        private static void TestFullCircle(string json, string rootPropertyContainerTypeName)
        {
            // json -> csharp

            var g = new CSharpGenerationBackend();
            g.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);

            // csharp -> assembly

            var assemblyFilePath = string.Empty;
            var errors = string.Empty;

            Assert.IsTrue(
                CompileTestUtils.TryCompileToFile(
                    g.Code.ToString(),
                    out assemblyFilePath,
                    out errors));

            // assembly -> property visitor

            var assembly = Assembly.LoadFile(assemblyFilePath);
            Assert.NotNull(assembly);

            var type = assembly.GetType(rootPropertyContainerTypeName);
            var container = (IPropertyContainer) Activator.CreateInstance(type);
            var visitor = new PropertyLeafVisitor();
            container.Visit(visitor);

            Assert.NotZero(visitor.Properties.Count);

            //  -> json

            var generatedJson = JsonSchema.ToJson(
                new JsonSchema()
                {
                    PropertyTypeNodes = ReflectionPropertyTree.Read(assemblyFilePath)
                }
            );

            Assert.NotNull(generatedJson.Length);
        }

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
    }
}

#endif // USE_ROSLYN_API && (NET_4_6 || NET_STANDARD_2_0)

