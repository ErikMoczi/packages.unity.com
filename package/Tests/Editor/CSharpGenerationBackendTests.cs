#if USE_ROSLYN_API && (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using NUnit.Framework;
using Unity.Properties.Serialization;
using Unity.Properties.Editor.Serialization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Formatting;

using Mono.Cecil;

namespace Unity.Properties.Tests.JSonSchema
{
    [TestFixture]
    internal class CSharpGenerationBackendTests
    {
#region CSharpGenerationBackendTests Tools

        private static string WithLineNumbers(string code)
        {
            int i = 0;
            var lines = code.Split('\n').Select(line =>
            {
                var formattedLine = i.ToString() + ": " + line;
                ++i;
                return formattedLine;
            });
            return string.Join("\n", lines);
        }

        private static TypeDeclarationSyntax GetTypeDeclarationNodeFromName(CompilationUnitSyntax root, string className)
        {
            var candidateTypes =
                from t in root.DescendantNodes().OfType<TypeDeclarationSyntax>()
                where t.Identifier.ValueText == className
                select t;

            var classDeclarationSyntaxs = candidateTypes as TypeDeclarationSyntax[] ?? candidateTypes.ToArray();
            Assert.NotZero(classDeclarationSyntaxs.Length);

            var foundClass = classDeclarationSyntaxs.First();
            Assert.IsNotNull(foundClass);

            return foundClass;
        }

        private static Assembly CheckIfCodeIsCompilable(string code)
        {
            string compilationErrors;
            Assembly assembly = null;
            var compiled = CompileTestUtils.TryCompile(code, out assembly, out compilationErrors);
            Assert.IsTrue(compiled, compilationErrors + "\n\n" + WithLineNumbers(code));
            return assembly;
        }

        private static IEnumerable<FieldDeclarationSyntax> GetFieldSyntaxNodes(
            TypeDeclarationSyntax root,
            SyntaxTokenList modifiers)
        {
            var fields = from fieldDeclaration in root.DescendantNodes().OfType<FieldDeclarationSyntax>()
                where fieldDeclaration.WithModifiers(modifiers) != null
                select fieldDeclaration;
            return fields;
        }

        private static Dictionary<string, FieldDeclarationSyntax> GetFieldNameAndTypes(
            TypeDeclarationSyntax root,
            SyntaxTokenList modifiers)
        {
            var fields = from fieldDeclaration in GetFieldSyntaxNodes(root, modifiers)
                from variableDeclarationSyntax in fieldDeclaration.Declaration.Variables
                select new { variableDeclarationSyntax.Identifier.ValueText, fieldDeclaration};

            return fields.ToDictionary(e => e.ValueText, e => e.fieldDeclaration);
        }

        private static FieldDeclarationSyntax GetFieldSyntaxNodeByName(
            TypeDeclarationSyntax root,
            string fieldName,
            SyntaxTokenList modifiers)
        {
            var field = from fieldDeclaration in GetFieldSyntaxNodes(root, modifiers)
                from variableDeclarationSyntax in fieldDeclaration.Declaration.Variables
                where variableDeclarationSyntax.Identifier.ValueText == fieldName
                select fieldDeclaration;

            var fieldDeclarationSyntaxs = field.ToList();
            Assert.NotZero(fieldDeclarationSyntaxs.Count());

            return fieldDeclarationSyntaxs.First();
        }

        private static void CheckIfPropertyContainerIsSerializable(Assembly assembly, string typename)
        {
            Type type = assembly.GetType(typename);
            Assert.IsNotNull(type);

            var container = (IPropertyContainer)Activator.CreateInstance(type);
            Assert.IsNotNull(container);

            var serializedContainer = JsonSerializer.Serialize(container);
            Assert.NotZero(serializedContainer.Length);
        }

        private static ExpressionSyntax GetVariableAssignmentExpression(
            BlockSyntax block,
            string variableName)
        {
            var field = from assignmentExpression in block.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                where (assignmentExpression.Left as IdentifierNameSyntax) != null &&
                      ((IdentifierNameSyntax) assignmentExpression.Left).Identifier.ValueText == variableName
                select assignmentExpression.Right;
            return field.First();
        }

#endregion

        [Test]
        public void WhenEmptyStringForSchema_CSharpCodeGen_ReturnsAnEmptyContainerList()
        {
            var backend = new CSharpGenerationBackend();
            var result = PropertyTypeNodeJsonSerializer.FromJson(
                string.Empty, new TypeResolver());
            backend.Generate(result);
            var code = backend.Code;
            Assert.Zero(code.Length);
        }

        [Test]
        public void WhenNoTypesInSchema_CSharpCodeGen_ReturnsAnEmptyContainerList()
        {
            var backend = new CSharpGenerationBackend();

            var text = new JsonSchemaBuilder()
                .WithVersion(JsonSchema.CurrentVersion)
                .WithNamespace("Unity.Properties.Samples.Tests")
                .ToJson();

            var result = JsonSchema.FromJson(text);

            backend.Generate(result.PropertyTypeNodes);
            var code = backend.Code;
            Assert.Zero(code.Length);
        }

        [TestCase("class", "int", ExpectedResult = "Property<HelloWorld, int>")]
        [TestCase("struct", "int", ExpectedResult = "StructProperty<HelloWorld, int>")]
        [TestCase("class", "class Foo", ExpectedResult = "ContainerProperty<HelloWorld, Foo>")]
        [TestCase("class", "struct Foo", ExpectedResult = "MutableContainerProperty<HelloWorld, Foo>")]
        [TestCase("struct", "class Foo", ExpectedResult = "StructContainerProperty<HelloWorld, Foo>")]
        [TestCase("struct", "struct Foo", ExpectedResult = "StructMutableContainerProperty<HelloWorld, Foo>")]
        public string TestPropertyFieldTypeGeneration(
            string containerType, string qualifiedPropertyType)
        {
            var isCompound = qualifiedPropertyType.Split(' ').Length > 1;

            var propertyTypeName = qualifiedPropertyType;
            var propertyTypeKind = string.Empty;

            if (isCompound)
            {
                var names = qualifiedPropertyType.Split(' ');
                propertyTypeKind = names[0];
                propertyTypeName = names[1];
            }

            var backend = new CSharpGenerationBackend();

            var text = new JsonSchemaBuilder()
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld", containerType == "struct")
                        .WithProperty("Data", propertyTypeName)
                )
                .ToJson();

            Dictionary<string, PropertyTypeNode.TypeTag> injectedBuiltinTypes = null;
            if (isCompound)
            {
                var tag = propertyTypeKind == "class"
                    ? PropertyTypeNode.TypeTag.Class
                    : PropertyTypeNode.TypeTag.Struct;
                injectedBuiltinTypes = new Dictionary<string, PropertyTypeNode.TypeTag> { { propertyTypeName, tag } };
            }

            var result = JsonSchema.FromJson(
                text, injectedBuiltinTypes
            );

            backend.Generate(result.PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);

            // Inject value/class types

            if (isCompound)
            {
                code += $@"
                    public {propertyTypeKind} {propertyTypeName} : IPropertyContainer
                    {{
                        public IVersionStorage VersionStorage {{ get; }}
                        public IPropertyBag PropertyBag {{ get; }}
                    }};
                ";
            }

            CheckIfCodeIsCompilable(code);

            var root = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot();
            var classNode = GetTypeDeclarationNodeFromName(root, "HelloWorld");

            var modifiers = SyntaxFactory.TokenList(new[]
            {
                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                SyntaxFactory.Token(containerType == "class" ? SyntaxKind.ProtectedKeyword : SyntaxKind.PrivateKeyword)
            });
            var property = GetFieldSyntaxNodeByName(classNode, "s_DataProperty", modifiers);

            return property.Declaration.Type.ToString();
        }

        [TestCase("class", "int", ExpectedResult = "ListProperty<HelloWorld, List<int>, int>")]
        [TestCase("struct", "int", ExpectedResult = "StructListProperty<HelloWorld, List<int>, int>")]
        [TestCase("class", "class Foo", ExpectedResult = "ContainerListProperty<HelloWorld, List<Foo>, Foo>")]
        [TestCase("struct", "struct Foo", ExpectedResult = "StructMutableContainerListProperty<HelloWorld, List<Foo>, Foo>")]
        [TestCase("class", "class Foo", ExpectedResult = "ContainerListProperty<HelloWorld, List<Foo>, Foo>")]
        [TestCase("struct", "struct Foo", ExpectedResult = "StructMutableContainerListProperty<HelloWorld, List<Foo>, Foo>")]
        public string TestListPropertyFieldTypeGeneration(
            string containerType, string qualifiedPropertyType)
        {
            var isCompound = qualifiedPropertyType.Split(' ').Length > 1;

            var propertyTypeName = qualifiedPropertyType;
            var propertyTypeKind = string.Empty;

            if (isCompound)
            {
                var names = qualifiedPropertyType.Split(' ');
                propertyTypeKind = names[0];
                propertyTypeName = names[1];
            }

            var backend = new CSharpGenerationBackend();

            var text = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld", containerType == "struct")
                        .WithProperty("Data", "list", "", propertyTypeName)
                )
                .ToJson();

            Dictionary<string, PropertyTypeNode.TypeTag> injectedBuiltinTypes = null;
            if (isCompound)
            {
                var tag = propertyTypeKind == "class"
                    ? PropertyTypeNode.TypeTag.Class
                    : PropertyTypeNode.TypeTag.Struct;
                injectedBuiltinTypes = new Dictionary<string, PropertyTypeNode.TypeTag> {{ propertyTypeName, tag } };
            }

            var result = JsonSchema.FromJson(
                text, injectedBuiltinTypes
                );

            backend.Generate(result.PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);

            // Inject value/class types
            if (isCompound)
            {
                code += $@"
                    public {propertyTypeKind} {propertyTypeName} : IPropertyContainer
                    {{
                        public IVersionStorage VersionStorage {{ get; }}
                        public IPropertyBag PropertyBag {{ get; }}
                    }};
                ";
            }

            CheckIfCodeIsCompilable(code);

            var root = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot();
            var modifiers = SyntaxFactory.TokenList(new[]
            {
                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword),
                SyntaxFactory.Token(containerType == "class" ? SyntaxKind.ProtectedKeyword : SyntaxKind.PrivateKeyword)
            });
            var classNode = GetTypeDeclarationNodeFromName(root, "HelloWorld");
            var property = GetFieldSyntaxNodeByName(classNode, "s_DataProperty", modifiers);

            return property.Declaration.Type.ToString();
        }

        [TestCase("class", false, ExpectedResult = "EnumProperty<HelloWorld, Foo>")]
        [TestCase("struct", false, ExpectedResult = "StructEnumProperty<HelloWorld, Foo>")]
        [TestCase("class", true, ExpectedResult = "EnumListProperty<HelloWorld, List<Foo>, Foo>")]
        [TestCase("struct", true, ExpectedResult = "StructEnumListProperty<HelloWorld, List<Foo>, Foo>")]
        public string TestEnumPropertyFieldTypeGeneration(
            string containerType, bool isList)
        {
            var backend = new CSharpGenerationBackend();

            var text = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld", containerType == "struct")
                        .WithProperty("Data", isList ? "list" : "Foo", "", "Foo")
                )
                .ToJson();

            Dictionary<string, PropertyTypeNode.TypeTag> injectedBuiltinTypes =
                new Dictionary<string, PropertyTypeNode.TypeTag>
                {
                    { "Foo", PropertyTypeNode.TypeTag.Enum }
                };

            var result = JsonSchema.FromJson(text, injectedBuiltinTypes);

            backend.Generate(result.PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);

            // Inject value/class types
            code += $@"
                public enum Foo
                {{
                    ONE
                }};
            ";

            CheckIfCodeIsCompilable(code);

            var root = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot();
            var modifiers = SyntaxFactory.TokenList(new[]
            {
                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                SyntaxFactory.Token(containerType == "class" ? SyntaxKind.ProtectedKeyword : SyntaxKind.PrivateKeyword)
            });
            var classNode = GetTypeDeclarationNodeFromName(root, "HelloWorld");
            var property = GetFieldSyntaxNodeByName(classNode, "s_DataProperty", modifiers);

            return property.Declaration.Type.ToString();
        }

        [Test]
        public void WhenClassContainerWithBaseTypes_CSharpCodeGen_ReturnsAValidContainerList()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithProperty("Data", "int", "5")
                        .WithProperty("Floats", "List", "", "float")
                        .WithProperty("MyStruct", "SomeData")
                )
                .ToJson();

            Dictionary<string, PropertyTypeNode.TypeTag> injectedBuiltinTypes =
                new Dictionary<string, PropertyTypeNode.TypeTag>
                {
                    { "SomeData", PropertyTypeNode.TypeTag.Struct }
                };

            var result = JsonSchema.FromJson(json, injectedBuiltinTypes);

            backend.Generate(result.PropertyTypeNodes);
            var code = backend.Code;
            Assert.NotZero(code.Length);

            Assert.IsTrue(code.Contains("Property<HelloWorld, int>"));
            Assert.IsTrue(code.Contains("ListProperty<HelloWorld, List<float>, float>"));
            Assert.IsTrue(code.Contains("MutableContainerProperty<HelloWorld, SomeData>"));

            // Inject value/class types
            code += @"
                public struct SomeData : IPropertyContainer
                {
                    public static IPropertyBag bag { get; } = new PropertyBag(new List<IProperty> {}.ToArray());

                    public IVersionStorage VersionStorage { get; }
                    public IPropertyBag PropertyBag => bag;
                };
            ";

            Assembly assembly = CheckIfCodeIsCompilable(code);

            // @TODO
            CheckIfPropertyContainerIsSerializable(assembly, "Unity.Properties.Samples.Tests.HelloWorld");
        }
        
        [Test]
        public void WhenClassHasInheritedPropertiesFromBaseClass_CSharpCodeGen_InheritedPropertiesAreAddedToPropertyBag()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithProperty("Data", "int", "5")
                        .WithProperty("Floats", "List", "", "float")
                        .WithBaseClassOverriden("Foo")
                )
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("Foo")
                        .WithProperty("FooData", "int", "5")
                        .WithProperty("FooFloats", "List", "", "float")
                )
                .ToJson();

            var schema = JsonSchema.FromJson(json);

            backend.Generate(schema.PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);

            var assembly = CheckIfCodeIsCompilable(code);

            var backingFieldModifiers = SyntaxFactory.TokenList(new[]
            {
                SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)
            });
            var root = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot();

            // @ TODO simplify
            {
                var classNode = GetTypeDeclarationNodeFromName(root, "HelloWorld");

                var fieldTypesPerName =
                    GetFieldNameAndTypes(classNode, backingFieldModifiers).Select(
                            f => new KeyValuePair<string, string>(
                                f.Key,
                                f.Value.Declaration.Type.ToString()))
                        .ToDictionary(e => e.Key, e => e.Value);

                // @ TODO improve by being more precise on the class/type
                Assert.IsTrue(fieldTypesPerName.ContainsKey("s_DataProperty"));
                Assert.IsTrue(fieldTypesPerName["s_DataProperty"] == "Property<HelloWorld, int>");

                Assert.IsTrue(fieldTypesPerName.ContainsKey("s_FloatsProperty"));
                Assert.IsTrue(fieldTypesPerName["s_FloatsProperty"] == "ListProperty<HelloWorld, List<float>, float>");
            }
            {
                var classNode = GetTypeDeclarationNodeFromName(root, "Foo");

                var fieldTypesPerName =
                    GetFieldNameAndTypes(classNode, backingFieldModifiers).Select(
                            f => new KeyValuePair<string, string>(
                                f.Key,
                                f.Value.Declaration.Type.ToString()))
                        .ToDictionary(e => e.Key, e => e.Value);

                // @ TODO improve by being more precise on the class/type
                Assert.IsTrue(fieldTypesPerName.ContainsKey("s_FooDataProperty"));
                Assert.IsTrue(fieldTypesPerName["s_FooDataProperty"] == "Property<Foo, int>");

                Assert.IsTrue(fieldTypesPerName.ContainsKey("s_FooFloatsProperty"));
                Assert.IsTrue(fieldTypesPerName["s_FooFloatsProperty"] == "ListProperty<Foo, List<float>, float>");
            }
            
            // @TODO
            CheckIfPropertyContainerIsSerializable(assembly, "Unity.Properties.Samples.Tests.HelloWorld");
        }



        [Test]
        public void WhenClassContainsNestedClass_CSharpCodeGen_NestedClassesAreProperlyGenerated()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithProperty("Data", "int", "5")
                        .WithProperty("Floats", "List", "", "float")
                        .WithNestedContainer(
                            new JsonSchemaBuilder.ContainerBuilder("Foo")
                                .WithProperty("Data", "int", "5")
                                .WithProperty("Floats", "List", "", "float")
                                .WithNestedContainer(
                                    new JsonSchemaBuilder.ContainerBuilder("Bar")
                                        .WithProperty("Data", "int", "5")
                                        .WithProperty("Floats", "List", "", "float")
                                )
                        )
                )
                .ToJson();

            var schema = JsonSchema.FromJson(json);

            backend.Generate(schema.PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);

            var assembly = CheckIfCodeIsCompilable(code);

            var root = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot();

            // @ TODO simplify
            var classNode = GetTypeDeclarationNodeFromName(root, "HelloWorld");
            Assert.IsNotNull(classNode);

            var fooClass = from classDeclaration in classNode.DescendantNodes().OfType<ClassDeclarationSyntax>()
                           where classDeclaration.Identifier.ValueText == "Foo"
                           select classDeclaration;
            Assert.NotZero(fooClass.Count());

            var barClass = from classDeclaration in fooClass.First().ChildNodes().OfType<ClassDeclarationSyntax>()
                           where classDeclaration.Identifier.ValueText == "Bar"
                           select classDeclaration;
            Assert.NotZero(barClass.Count());

            CheckIfPropertyContainerIsSerializable(assembly, "Unity.Properties.Samples.Tests.HelloWorld");
        }


        [Test]
        public void WhencontainerNestedClassHaveNamespace_CSharpCodeGen_NestedClassesAreProperlyGeneratedWithoutNamespace()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithNamespace("Unity.Properties.Samples")
                        .WithProperty("Data", "int", "5")
                        .WithNestedContainer(
                            new JsonSchemaBuilder.ContainerBuilder("Foo")
                                .WithNamespace("Unity.Properties.Samples")
                                .WithProperty("Data", "int", "5")
                                .WithNestedContainer(
                                    new JsonSchemaBuilder.ContainerBuilder("Bar")
                                        .WithNamespace("Unity.Properties.Samples")
                                        .WithProperty("Data", "int", "5")
                                )
                        )
                )
                .ToJson();

            var schema = JsonSchema.FromJson(json);

            backend.Generate(schema.PropertyTypeNodes);

            var code = backend.Code;
            var assembly = CheckIfCodeIsCompilable(code);
            
            CheckIfPropertyContainerIsSerializable(assembly, "Unity.Properties.Samples.HelloWorld");
            CheckIfPropertyContainerIsSerializable(assembly, "Unity.Properties.Samples.HelloWorld/Foo");
            CheckIfPropertyContainerIsSerializable(assembly, "Unity.Properties.Samples.HelloWorld/Foo/Bar");
        }

        [Test]
        public void WhenTypeWithBackingFieldInSchema_CSharpCodeGen_DoesNotGeneratePrivateDataMembers()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithProperty("Data", "int", "5", "", "m_BackingData")
                        .WithProperty("Floats", "List", "", "float", "m_BackingFloats")
                )
                .ToJson();

            var result = JsonSchema.FromJson(json);

            backend.Generate(result.PropertyTypeNodes);
            var code = backend.Code;
            Assert.NotZero(code.Length);

            Assert.IsFalse(code.Contains("m_Data"));
            Assert.IsFalse(code.Contains("m_Floats"));

            Assert.IsTrue(code.Contains(".m_BackingData"));
            Assert.IsTrue(code.Contains(".m_BackingFloats"));

            // Inject value/class types
            code += @"
                namespace Unity.Properties.Samples.Tests
                {
                public partial class HelloWorld
                {
                    public int m_BackingData;
                    public List<float> m_BackingFloats = new List<float>();
                };
                }
            ";

            Assembly assembly = CheckIfCodeIsCompilable(code);

            // @TODO
            CheckIfPropertyContainerIsSerializable(assembly, "Unity.Properties.Samples.Tests.HelloWorld");
        }


        [Test]
        public void WhenGlobalDefaultNamespaceIsSpecified_CSharpCodeGen_ShouldFixUpContainersThatHaveEmptyNamespaces()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("A")
                        .WithNamespace("MyNamespace")
                )
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("B")
                        .WithNamespace("MyNamespace")
                )
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("C")
                )
                .ToJson();

            var result = JsonSchema.FromJson(json);

            backend.Generate(result.PropertyTypeNodes);
            var code = backend.Code;
            Assert.NotZero(code.Length);

            Assembly assembly = CheckIfCodeIsCompilable(code);

            // @TODO
            CheckIfPropertyContainerIsSerializable(assembly, "MyNamespace.A");
            CheckIfPropertyContainerIsSerializable(assembly, "MyNamespace.B");
            CheckIfPropertyContainerIsSerializable(assembly, "Unity.Properties.Samples.Tests.C");
        }

        [Test]
        public void WhenValueTypeNotSpecified_CSharpCodeGen_GeneratesAClassContainerByDefault()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithEmptyPropertiesList()
                )
                .ToJson();

            var result = JsonSchema.FromJson(json);

            backend.Generate(result.PropertyTypeNodes);
            var code = backend.Code;
            Assert.NotZero(code.Length);

            Assembly assembly = CheckIfCodeIsCompilable(code);

            var root = (CompilationUnitSyntax) CSharpSyntaxTree.ParseText(code).GetRoot();

            var helloWorldClass = GetTypeDeclarationNodeFromName(root, "HelloWorld");

            Assert.IsTrue(helloWorldClass.Modifiers.Any(SyntaxKind.PartialKeyword));
            Assert.IsTrue(helloWorldClass.Modifiers.Any(SyntaxKind.PublicKeyword));

            // @TODO
            CheckIfPropertyContainerIsSerializable(assembly, "Unity.Properties.Samples.Tests.HelloWorld");
        }

        [Test]
        public void WhenBackingFieldGenerated_CSharpCodeGen_TheyAreGeneratedAsPrivate()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithProperty("Data", "Foo")
                        .WithProperty("Foo", "int")
                )
                .ToJson();

            Dictionary<string, PropertyTypeNode.TypeTag> injectedBuiltinTypes =
                new Dictionary<string, PropertyTypeNode.TypeTag>
                {
                    { "Foo", PropertyTypeNode.TypeTag.Struct }
                };

            var result = JsonSchema.FromJson(json, injectedBuiltinTypes);

            backend.Generate(result.PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);

            // Inject value/class types
            code += @"
                namespace Unity.Properties.Samples.Tests {
                public struct Foo : IPropertyContainer
                {
                    public static IPropertyBag bag { get; } = new PropertyBag(new List<IProperty> {}.ToArray());

                    public IVersionStorage VersionStorage { get; }
                    public IPropertyBag PropertyBag => bag;
                };
                }
            ";

            var assembly = CheckIfCodeIsCompilable(code);

            var root = (CompilationUnitSyntax) CSharpSyntaxTree.ParseText(code).GetRoot();

            var modifiers = SyntaxFactory.TokenList(new[]
            {
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword)
            });

            var classNode = GetTypeDeclarationNodeFromName(root, "HelloWorld");

            var fooField = GetFieldSyntaxNodeByName(classNode, "m_Foo", modifiers);
            var dataField = GetFieldSyntaxNodeByName(classNode, "m_Data", modifiers);

            Assert.NotNull(fooField);
            Assert.NotNull(dataField);

            Assert.IsTrue(fooField.Declaration.Type.ToString() == "int");
            Assert.IsTrue(dataField.Declaration.Type.ToString() == "Foo");

            // @TODO
            CheckIfPropertyContainerIsSerializable(assembly, "Unity.Properties.Samples.Tests.HelloWorld");
        }

        [Test]
        public void WhenValueTypeNotSpecified_CSharpCodeGen_GeneratesAStructContainer()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld", true)
                        .WithEmptyPropertiesList()
                )
                .ToJson();

            var result = JsonSchema.FromJson(json);
            backend.Generate(result.PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            Assert.IsTrue(code.Contains("public partial struct HelloWorld"));
        }

        [Test]
        public void WhenIsStructContainerContainsStructProperty_CSharpCodeGen_GeneratesProperyContainerWrapper()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld", true)
                        .WithProperty("Data", "Foo")
                )
                .ToJson();

            Dictionary<string, PropertyTypeNode.TypeTag> injectedBuiltinTypes =
                new Dictionary<string, PropertyTypeNode.TypeTag>
                {
                    { "Foo", PropertyTypeNode.TypeTag.Struct }
                };

            backend.Generate(JsonSchema.FromJson(json, injectedBuiltinTypes).PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);
            Assert.IsTrue(code.Contains("StructMutableContainerProperty<HelloWorld, Foo>"));
        }

        [Test]
        public void WhenIsClassContainerContainsStructProperty_CSharpCodeGen_GeneratesProperyContainerWrapper()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithProperty("Data", "Foo")
                )
                .ToJson();

            var injectedBuiltinTypes =
                new Dictionary<string, PropertyTypeNode.TypeTag>
                {
                    { "Foo", PropertyTypeNode.TypeTag.Struct }
                };

            backend.Generate(JsonSchema.FromJson(json, injectedBuiltinTypes).PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);
            Assert.IsTrue(code.Contains("ContainerProperty<HelloWorld, Foo>"));
        }

        [Test]
        public void WhenContainsStructProperty_CSharpCodeGen_GeneratesGetSetValueWithRef()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld", true)
                        .WithProperty("Data", "Foo")
                )
                .ToJson();

            var injectedBuiltinTypes =
                new Dictionary<string, PropertyTypeNode.TypeTag>
                {
                    { "Foo", PropertyTypeNode.TypeTag.Struct }
                };
            backend.Generate(JsonSchema.FromJson(json, injectedBuiltinTypes).PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);
            Assert.IsTrue(code.Contains("GetValue(ref this"));
        }

        [Test]
        public void WhenIsClassPropertyContainer_CSharpCodeGen_GeneratesGetSetValueWithNoRef()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithProperty("Data", "Foo")
                )
                .ToJson();

            var injectedBuiltinTypes =
                new Dictionary<string, PropertyTypeNode.TypeTag>
                {
                    { "Foo", PropertyTypeNode.TypeTag.Struct }
                };
            backend.Generate(JsonSchema.FromJson(json, injectedBuiltinTypes).PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);
            Assert.IsTrue(code.Contains("GetValue(this"));
        }

        [Test]
        public void WhenIsClassProperty_CSharpCodeGen_GeneratesGetSetValueWithNoRef()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithProperty("Data", "int")
                )
                .ToJson();

            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);
            Assert.IsTrue(code.Contains("GetValue(this"));
            Assert.IsTrue(code.Contains("SetValue(this"));
        }

        public void WhenIsClassProperty_CSharpCodeGen_DelegatesDefaultValueBackingFieldConstruction()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithProperty("Data", "int", "5")
                        .WithProperty("Ints", "List", "", "float")
                )
                .ToJson();

            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);

            CheckIfCodeIsCompilable(code);

            Assert.IsTrue(code.Contains("private int m_Data = 5;"));
            Assert.IsTrue(code.Contains("private List<float> m_Ints = new List<float> {};"));
        }

        public void WhenIsStructProperty_CSharpCodeGen_DelegatesDefaultValueBackingFieldConstruction()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld", true)
                        .WithProperty("Data", "int", "5")
                        .WithProperty("Ints", "List", "", "float")
                )
                .ToJson();

            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);

            CheckIfCodeIsCompilable(code);

            Assert.IsTrue(code.Contains("private int m_Data = 5;"));
            Assert.IsTrue(code.Contains("private List<float> m_Ints = new List<float> {};"));
        }

        public void WhenDontInitializeBackingFieldIsSet_CSharpCodeGen_DoesNotInitializeBackingFields()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld", true)
                        .WithProperty("Data", "int", "5")
                        .WithProperty("Ints", "List", "", "float", "", false, false, false, true)
                )
                .ToJson();

            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);
            Debug.Log(code);

            CheckIfCodeIsCompilable(code);

            Assert.IsTrue(code.Contains("private int m_Data = 5;"));
            Assert.IsTrue(code.Contains("private List<float> m_Ints;"));
        }

        [Test]
        public void WhenIsStructProperty_CSharpCodeGen_NoDefaultValue()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld", true)
                        .WithProperty("Data", "int")
                        .WithProperty("Floats", "List", "", "float")
                )
                .ToJson();

            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);
            
            var code = backend.Code;
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            var root = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot();

            var modifiers = SyntaxFactory.TokenList(new[]
            {
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword)
            });

            var classNode = GetTypeDeclarationNodeFromName(root, "HelloWorld");

            var floatsField = GetFieldSyntaxNodeByName(classNode, "m_Floats", modifiers);
            var dataField = GetFieldSyntaxNodeByName(classNode, "m_Data", modifiers);

            Assert.NotNull(dataField);
            Assert.NotNull(floatsField);

            Assert.IsTrue(dataField.Declaration.Type.ToString() == "int");
            Assert.IsTrue(floatsField.Declaration.Type.ToString() == "List<float>");

            Assert.IsNull(dataField.Declaration.Variables.First().Initializer);
            Assert.IsNull(floatsField.Declaration.Variables.First().Initializer);

            // @TODO
            // CheckIfPropertyContainerIsSerializable(assembly, "HelloWorld");
        }

        [Test]
        public void WhenIsStructContainer_CSharpCodeGen_DoesNotGenerateConstructor()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld", true)
                        .WithProperty("Data", "int")
                        .WithProperty("Floats", "List", "", "float")
                )
                .ToJson();

            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);
            
            var code = backend.Code;
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            var constructors = ((CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot())
                .DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .Where(c => !c.Modifiers.Any(SyntaxKind.StaticKeyword));

            Assert.Zero(constructors.Count());

            // @TODO
            // CheckIfPropertyContainerIsSerializable(assembly, "HelloWorld");
        }

        [Test]
        public void WhenIsClassContainer_CSharpCodeGen_DoesNotGenerateConstructor()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithEmptyPropertiesList()
                )
                .ToJson();

            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);
            
            var code = backend.Code;
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            var constructors = ((CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot())
                .DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .Where(c => !c.Modifiers.Any(SyntaxKind.StaticKeyword));

            Assert.Zero(constructors.Count());
        }

        [Test]
        public void WhenIsClassContainerAndNoProperties_CSharpCodeGen_DoesNotGenerateConstructor()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithEmptyPropertiesList()
                )
                .ToJson();

            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);
            
            var code = backend.Code;
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            var constructors = ((CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot())
                .DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .Where(c => !c.Modifiers.Any(SyntaxKind.StaticKeyword));

            Assert.Zero(constructors.Count());
        }

        [Test]
        public void WhenContainerDoesHaveDefaultImplementation_CSharpCodeGen_DoesNotGenerateImplementation()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithNoDefaultImplementation()
                        .WithEmptyPropertiesList()
                )
                .ToJson();

            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);

            var properties = ((CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot())
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Where(c => c.Modifiers.Any(SyntaxKind.StaticKeyword))
                .Select(node => node.Identifier.ValueText)
                .ToList();
            
            Assert.Zero(properties.Count());
        }

        public void WhenPropertyIsCustom_CSharpCodeGen_DoesGenerateCustomBits()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithNoDefaultImplementation()
                        .WithProperty("Data", "int", "5", "", "", false, false, true)
                )
                .ToJson();

            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);
            var code = backend.Code;

            CheckIfCodeIsCompilable(code);

            var properties = ((CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot())
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Where(c => c.Modifiers.Any(SyntaxKind.StaticKeyword))
                .Select(node => node.Identifier.ValueText)
                .ToList();

            foreach (var p in properties)
            {
                Assert.IsFalse(p.Contains("Data"));
            }
        }

        [Test]
        public void WhenUserHookSpecified_CSharpCodeGen_GeneratesIt()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithProperty("Data", "int")
                        .WithProperty("Floats", "List", "", "float")
                        .WithUserHooks(new List<string> { "OnPropertyBagConstructed" })
                )
                .ToJson();

            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);
            
            var code = backend.Code;
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            var userHookMethod = ((CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot())
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.ValueText == "OnPropertyBagConstructed");

            Assert.NotZero(userHookMethod.Count());

            // TODO tokenize
            Assert.IsTrue(code.Contains("static partial void OnPropertyBagConstructed(IPropertyBag bag);"));
            Assert.IsTrue(code.Contains("OnPropertyBagConstructed(sProperties);"));
        }

        [Test]
        public void WhenOverrideDefaultBaseClasseClass_CSharpCodeGen_GeneratesContainerWithOverridenBaseClass()
        {
            var backend = new CSharpGenerationBackend();
            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithEmptyPropertiesList()
                        .WithBaseClassOverriden("Foo")
                )
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("Foo")
                        .WithEmptyPropertiesList()
                )
                .ToJson();

            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);
            
            var code = backend.Code;
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            // TODO tokenize, bad britle test
            Assert.IsTrue(code.Contains("public partial class HelloWorld : Foo"));
        }

        [Test]
        public void WhenPropertyContainerSetAsAbstract_CSharpCodeGen_GeneratesAbstractClass()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithEmptyPropertiesList()
                        .WithIsAbstract(true)
                )
                .ToJson();
            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);
            
            CheckIfCodeIsCompilable(code);

            // TODO tokenize, bad britle test
            Assert.IsTrue(code.Contains("public abstract partial class HelloWorld : IPropertyContainer"));
        }

        [Test]
        public void WhenPropertyContainerSetAsAbstractForStructContainer_CSharpCodeGen_DoestNotGeneratesAbstractStruct()
        {
            var backend = new CSharpGenerationBackend();

            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld", true)
                        .WithEmptyPropertiesList()
                        .WithIsAbstract(true)
                )
                .ToJson();
            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            // TODO tokenize, bad britle test
            Assert.IsTrue(code.Contains("public partial struct HelloWorld : IPropertyContainer"));
        }

        [Test]
        public void WhenPropertyIsInherited_CSharpCodeGen_DoesNotGeneratesBackingField()
        {
            var backend = new CSharpGenerationBackend();
            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithBaseClassOverriden("BaseClass")
                )
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("BaseClass")
                        .WithProperty("Data", "int")
                )
                .ToJson();

            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            var root = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot();

            var classNode = GetTypeDeclarationNodeFromName(root, "HelloWorld");

            var fields = from fieldDeclaration in classNode.ChildNodes().OfType<FieldDeclarationSyntax>()
                         where fieldDeclaration.Modifiers.Contains(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                            && !fieldDeclaration.Modifiers.Contains(SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                         select fieldDeclaration;
            Assert.Zero(fields.Count());
        }

        [Test]
        public void WhenPropertyDeclaredAsReadonly_CSharpCodeGen_GeneratesReadonlyPropertyAccessor()
        {
            var backend = new CSharpGenerationBackend();
            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld", true)
                        .WithUserHooks(new List<string> { "OnPropertyBagConstructed" })
                        .WithProperty("Data", "int", "", "", "", true)
                )
                .ToJson();
            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            // TODO tokenize, very BAD
            Assert.IsTrue(code.Contains("/* GET */"));
            Assert.IsTrue(code.Contains("/* SET */ null"));
        }

        [Test]
        public void WhenDefaultUsingsAreOverriden_CSharpCodeGen_GeneratesUsingsWithProperOverridenAssemblies()
        {
            var backend = new CSharpGenerationBackend();
            var json = new JsonSchemaBuilder()
                .WithNamespace("Unity.Properties.Samples.Tests")
                .WithUsing(new List<string>() { "System", "Unity.Properties", "Unity.Properties.Editor" })
                .WithContainer(new JsonSchemaBuilder.ContainerBuilder("HelloWorld"))
                .ToJson();

            var schema = JsonSchema.FromJson(json);

            backend.Generate(schema.PropertyTypeNodes, schema.UsingAssemblies);

            var code = backend.Code;
            Assert.NotZero(code.Length);

            var root = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot();
            var usings = (from usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>() select usingDirective.Name.ToString()).ToList() ;

            Assert.IsTrue(usings.Count == 3);
            Assert.IsTrue(usings[0] == "System");
            Assert.IsTrue(usings[1] == "Unity.Properties");
            Assert.IsTrue(usings[2] == "Unity.Properties.Editor");
        }

        [Test]
        public void WhenPropertyIsPublic_CSharpCodeGen_GeneratedPublicProperty()
        {
            var backend = new CSharpGenerationBackend();
            var json = new JsonSchemaBuilder()
                .WithContainer(
                    new JsonSchemaBuilder.ContainerBuilder("HelloWorld")
                        .WithProperty("Data", "int", "", "", "", false, true)
                )
                .ToJson();

            backend.Generate(JsonSchema.FromJson(json).PropertyTypeNodes);

            var code = backend.Code;
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            var root = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot();

            var classNode = GetTypeDeclarationNodeFromName(root, "HelloWorld");

            var fields = from fieldDeclaration in classNode.ChildNodes().OfType<FieldDeclarationSyntax>()
                where fieldDeclaration.Declaration.Variables.First().Identifier.ValueText == "DataProperty"
                         select fieldDeclaration;

            Assert.NotZero(fields.Count());
            var p = fields.First();
            var modifiers = string.Join(",", p.Modifiers.Select(m => m.ValueText));
            Assert.IsTrue(modifiers == "public,static,readonly");
        }
    }
}

#endif // USE_ROSLYN_API && (NET_4_6 || NET_STANDARD_2_0)
