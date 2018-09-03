#if NET_4_6
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
        public void WhenNullAssembly_ReflectionJsonSchemaGenerator_ReturnsAnEmptyJson()
        {
            AssemblyDefinition assembly = null;
            var result = PropertyTypeNode.ToJson(ReflectionJsonSchemaGenerator.Read(assembly));
            Assert.IsTrue(result == "[]");
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

            Assert.IsTrue(TryCompile(code, out assemblyFilePath, out errors), errors);

            using (new FileDisposer(assemblyFilePath))
            {
                var result = PropertyTypeNode.FromJson(
                    PropertyTypeNode.ToJson(
                        ReflectionJsonSchemaGenerator.Read(
                            assemblyFilePath)));

                var containers = new List<PropertyTypeNode>();

                VisitContainer(result, c => { containers.Add(c); });

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
            ";

            Assert.IsTrue(TryCompile(code, out assemblyFilePath, out errors), errors);

            using (new FileDisposer(assemblyFilePath))
            {
                var result = PropertyTypeNode.FromJson(
                    PropertyTypeNode.ToJson(
                        ReflectionJsonSchemaGenerator.Read(
                            assemblyFilePath)));

                var containers = new List<PropertyTypeNode>();

                VisitContainer(result, c => { containers.Add(c); });

                Assert.True(containers.Count == 1);
                Assert.True(containers[0].Name == "HelloWorld");
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
            ";

            Assert.IsTrue(TryCompile(code, out assemblyFilePath, out errors), errors);

            using (new FileDisposer(assemblyFilePath))
            {
                var result = PropertyTypeNode.FromJson(
                    PropertyTypeNode.ToJson(
                        ReflectionJsonSchemaGenerator.Read(
                            assemblyFilePath)));

                var containers = new List<PropertyTypeNode>();

                VisitContainer(result, c => { containers.Add(c); });

                var containerNames = containers.Select(c => c.Name).ToList();

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

            Assert.IsTrue(TryCompile(code, out assemblyFilePath, out errors), errors);

            using (new FileDisposer(assemblyFilePath))
            {
                var result = PropertyTypeNode.FromJson(
                    PropertyTypeNode.ToJson(
                        ReflectionJsonSchemaGenerator.Read(
                            assemblyFilePath)));

                var containers = new List<PropertyTypeNode>();

                VisitContainer(result, c => { containers.Add(c); });

                Assert.True(containers.Count == 1);

                return containers[0].Name;
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
            g.Generate(JsonSchema.FromJson(json));

            // csharp -> assembly

            var assemblyFilePath = string.Empty;
            var errors = string.Empty;

            Assert.IsTrue(
                TryCompile(
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

            var generatedJson = PropertyTypeNode.ToJson(ReflectionJsonSchemaGenerator.Read(assemblyFilePath));
            Assert.NotNull(generatedJson.Length);
        }

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

        private static bool TryCompile(string code, out string assemblyFilePath, out string errorMessage)
        {
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);

                assemblyFilePath = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), "dll");

                var references = new MetadataReference[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IPropertyContainer).Assembly.Location),
                };

                var compilation = CSharpCompilation.Create(
                    Path.GetRandomFileName(),
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                var result = compilation.Emit(assemblyFilePath);

                if (!result.Success)
                {
                    var messages = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error)
                        .Select(
                            diagnostic => $"{diagnostic.Id} {diagnostic.GetMessage()} {diagnostic.Location.GetLineSpan().Span.ToString()}"
                            );

                    errorMessage = string.Join("\n", messages);

                    return false;
                }
            }
            catch (Exception e)
            {
                assemblyFilePath = string.Empty;
                errorMessage = e.ToString();

                return false;
            }

            errorMessage = string.Empty;

            return true;
        }
    }
}

#endif // NET_4_6
