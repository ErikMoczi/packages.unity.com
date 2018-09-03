#if NET_4_6

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
        private static bool TryCompile(string code, out Assembly assembly, out string errorMessage)
        {
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);

                var assemblyName = Path.GetRandomFileName();

                var references = new MetadataReference[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IPropertyContainer).Assembly.Location),
                };

                var compilation = CSharpCompilation.Create(
                    assemblyName,
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                // Make sure that we build all
                using (var ms = new MemoryStream())
                {
                    var result = compilation.Emit(ms);

                    if ( ! result.Success)
                    {
                        var messages = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error)
                            .Select(
                                diagnostic => $"{diagnostic.Id} {diagnostic.GetMessage()} {diagnostic.Location.GetLineSpan().Span.ToString()}"
                                );

                        errorMessage = string.Join("\n", messages);

                        assembly = null;

                        return false;
                    }
                    else
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        assembly = Assembly.Load(ms.ToArray());
                    }
                }
            }
            catch (Exception e)
            {
                assembly = null;
                errorMessage = e.ToString();

                return false;
            }

            errorMessage = string.Empty;

            return true;
        }

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
            string compilationErrors = string.Empty;
            Assembly assembly = null;
            var compiled = TryCompile(code, out assembly, out compilationErrors);
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

            Assert.NotZero(field.Count());

            return field.First();
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
            var result = PropertyTypeNode.FromJson(string.Empty);
            backend.Generate(result);
            var code = backend.Code;
            Assert.Zero(code.Length);
        }

        [Test]
        public void WhenNoTypesInSchema_CSharpCodeGen_ReturnsAnEmptyContainerList()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
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

        [TestCase("class", "int", ExpectedResult = "Property<HelloWorld, int>")]
        [TestCase("struct", "int", ExpectedResult = "StructProperty<HelloWorld, int>")]
        [TestCase("class", "class Foo", ExpectedResult = "ContainerProperty<HelloWorld, Foo>")]
        [TestCase("class", "struct Foo", ExpectedResult = "MutableContainerProperty<HelloWorld, Foo>")]
        [TestCase("struct", "class Foo", ExpectedResult = "StructContainerProperty<HelloWorld, Foo>")]
        [TestCase("struct", "struct Foo", ExpectedResult = "StructMutableContainerProperty<HelloWorld, Foo>")]
        public string TestPropertyFieldTypeGeneration(
            string containerType, string qualifiedPropertyType)
        {
            var backend = new CSharpGenerationBackend();

            var text = $@"
            [
                {{
                    ""Types"": [
                        {{
                        ""Name"": ""{containerType} HelloWorld"",
                        ""Properties"": {{
                            ""Data"": {{
                                ""TypeId"": ""{qualifiedPropertyType}"",
                            }},
                        }}
                        }}
                    ]
                }}
            ]
            ";

            var result = JsonSchema.FromJson(text);
            backend.Generate(result);
            var code = backend.Code.ToString();
            Assert.NotZero(code.Length);

            // Inject value/class types

            bool isCompound = qualifiedPropertyType.Split(' ').Length > 1;
            if (isCompound)
            {
                code += $@"
                    public {qualifiedPropertyType.Split(' ')[0]} {qualifiedPropertyType.Split(' ')[1]} : IPropertyContainer
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
        [TestCase("struct", "class Foo", ExpectedResult = "StructContainerListProperty<HelloWorld, List<Foo>, Foo>")]
        [TestCase("class", "struct Foo", ExpectedResult = "MutableContainerListProperty<HelloWorld, List<Foo>, Foo>")]
        [TestCase("struct", "struct Foo", ExpectedResult = "StructMutableContainerListProperty<HelloWorld, List<Foo>, Foo>")]
        public string TestListPropertyFieldTypeGeneration(
            string containerType, string qualifiedPropertyType)
        {
            var backend = new CSharpGenerationBackend();

            var text = $@"
            [
                {{
                    ""Types"": [
                        {{
                        ""Name"": ""{containerType} HelloWorld"",
                        ""Properties"": {{
                            ""Data"": {{
                                ""TypeId"": ""list"",
                                ""ItemTypeId"": ""{qualifiedPropertyType}""
                            }},
                        }}
                        }}
                    ]
                }}
            ]
            ";

            var result = JsonSchema.FromJson(text);
            backend.Generate(result);
            var code = backend.Code.ToString();
            Assert.NotZero(code.Length);

            // Inject value/class types
            bool isCompound = qualifiedPropertyType.Split(' ').Length > 1;
            if (isCompound)
            {
                code += $@"
                    public {qualifiedPropertyType.Split(' ')[0]} {qualifiedPropertyType.Split(' ')[1]} : IPropertyContainer
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
        public string TestListPropertyFieldTypeGeneration(
            string containerType, bool isList)
        {
            var backend = new CSharpGenerationBackend();

            var dataDefinitionFragment = string.Empty;
            if (isList)
            {
                dataDefinitionFragment = @"
                    ""TypeId"": ""list"",
                    ""ItemTypeId"": ""enum Foo"",
                ";
            }
            else
            {
                dataDefinitionFragment = @"
                    ""TypeId"": ""enum Foo"",
                ";
            }

            var text = $@"
            [
                {{
                    ""Types"": [
                        {{
                        ""Name"": ""{containerType} HelloWorld"",
                        ""Properties"": {{
                            ""Data"": {{
                                {dataDefinitionFragment}
                            }},
                        }}
                        }}
                    ]
                }}
            ]
            ";

            var result = JsonSchema.FromJson(text);
            backend.Generate(result);
            var code = backend.Code.ToString();
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
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""SchemaVersion"": 1,
                    ""Types"": [
                      {
                        ""TypeId"": ""1"",
                        ""Name"": ""class HelloWorld"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""int"",
                                ""DefaultValue"": ""5""
                            },
                            ""Floats"": {
                                ""TypeId"": ""List"",
                                ""ItemTypeId"": ""float""
                            },
                            ""MyStruct"": {
                                ""TypeId"": ""struct SomeData"",
                            }
                        }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code.ToString();
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
            CheckIfPropertyContainerIsSerializable(assembly, "HelloWorld");
        }
        
        [Test]
        public void WhenClassHasInheritedPropertiesFromBaseClass_CSharpCodeGen_InheritedPropertiesAreAddedToPropertyBag()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""SchemaVersion"": 1,
                    ""Types"":
                    [
                      {
                        ""TypeId"": ""1"",
                        ""Name"": ""class HelloWorld"",
                        ""OverrideDefaultBaseClass"": ""Foo"",
                        ""Properties"": {
                          ""Data"": {
                            ""TypeId"": ""int"",
                            ""DefaultValue"": ""5""
                          },
                          ""Floats"": {
                            ""TypeId"": ""List"",
                            ""ItemTypeId"": ""float""
                          },
                        }
                      },
                      {
                        ""TypeId"": ""2"",
                        ""Name"": ""class Foo"",
                        ""Properties"": {
                          ""FooData"": {
                            ""TypeId"": ""int"",
                            ""DefaultValue"": ""5""
                          },
                          ""FooFloats"": {
                            ""TypeId"": ""List"",
                            ""ItemTypeId"": ""float""
                          },
                        }
                      },
                    ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code.ToString();
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
            CheckIfPropertyContainerIsSerializable(assembly, "HelloWorld");
        }


        [Test]
        public void WhenClassContainsNestedClass_CSharpCodeGen_NestedClassesAreProperlyGenerated()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""SchemaVersion"": 1,
                    ""Types"":
                    [
                      {
                        ""TypeId"": ""1"",
                        ""Name"": ""class HelloWorld"",
                        ""NestedPropertyContainers"":
                        [
                          {
                            ""TypeId"": ""2"",
                            ""Name"": ""class Foo"",
                            ""NestedPropertyContainers"":
                            [
                              {
                                ""TypeId"": ""2"",
                                ""Name"": ""class Bar"",
                                ""NestedPropertyContainers"": [],
                                ""Properties"": {
                                  ""FooData"": {
                                    ""TypeId"": ""int"",
                                    ""DefaultValue"": ""5""
                                  },
                                  ""FooFloats"": {
                                    ""TypeId"": ""List"",
                                    ""ItemTypeId"": ""float""
                                  },
                                }
                              },
                            ],
                            ""Properties"": {
                              ""FooData"": {
                                ""TypeId"": ""int"",
                                ""DefaultValue"": ""5""
                              },
                              ""FooFloats"": {
                                ""TypeId"": ""List"",
                                ""ItemTypeId"": ""float""
                              },
                            }
                          },
                        ],
                        ""Properties"": {
                          ""Data"": {
                            ""TypeId"": ""int"",
                            ""DefaultValue"": ""5""
                          },
                          ""Floats"": {
                            ""TypeId"": ""List"",
                            ""ItemTypeId"": ""float""
                          },
                        }
                      },
                    ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code.ToString();
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

            CheckIfPropertyContainerIsSerializable(assembly, "HelloWorld");
        }

        [Test]
        public void WhenTypeWithBackingFieldInSchema_CSharpCodeGen_DoesNotGeneratePrivateDataMembers()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""SchemaVersion"": 1,
                    ""Types"": [
                      {
                        ""TypeId"": ""1"",
                        ""Name"": ""class HelloWorld"",
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
            var code = backend.Code.ToString();
            Assert.NotZero(code.Length);

            Assert.IsFalse(code.Contains("m_Data"));
            Assert.IsFalse(code.Contains("m_Floats"));
            Assert.IsTrue(code.Contains(".backing"));
            Assert.IsTrue(code.Contains(".backing"));

            // Inject value/class types
            code += @"
                public partial class HelloWorld
                {
                    public class Backing
                    {
                        public int Data;
                        public List<float> Floats = new List<float>();
                    }
                    public Backing backing { get; } = new Backing();
                };
            ";

            Assembly assembly = CheckIfCodeIsCompilable(code);

            // @TODO
            CheckIfPropertyContainerIsSerializable(assembly, "HelloWorld");
        }

        [Test]
        public void WhenValueTypeNotSpecified_CSharpCodeGen_GeneratesAClassContainerByDefault()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
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
            var code = backend.Code.ToString();
            Assert.NotZero(code.Length);

            Assembly assembly = CheckIfCodeIsCompilable(code);

            var root = (CompilationUnitSyntax) CSharpSyntaxTree.ParseText(code).GetRoot();

            var helloWorldClass = GetTypeDeclarationNodeFromName(root, "HelloWorld");

            Assert.IsTrue(helloWorldClass.Modifiers.Any(SyntaxKind.PartialKeyword));
            Assert.IsTrue(helloWorldClass.Modifiers.Any(SyntaxKind.PublicKeyword));

            // @TODO
            CheckIfPropertyContainerIsSerializable(assembly, "HelloWorld");
        }

        [Test]
        public void WhenBackingFieldGenerated_CSharpCodeGen_TheyAreGeneratedAsPrivate()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Types"": [
                      {
                        ""Name"": ""HelloWorld"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""struct Foo"",
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
            var code = backend.Code.ToString();
            Assert.NotZero(code.Length);

            // Inject value/class types
            code += @"
                public struct Foo : IPropertyContainer
                {
                    public static IPropertyBag bag { get; } = new PropertyBag(new List<IProperty> {}.ToArray());

                    public IVersionStorage VersionStorage { get; }
                    public IPropertyBag PropertyBag => bag;
                };
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
            CheckIfPropertyContainerIsSerializable(assembly, "HelloWorld");
        }

        [Test]
        public void WhenValueTypeNotSpecified_CSharpCodeGen_GeneratesAStructContainer()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""struct HelloWorld"",
                        ""Properties"": { }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code.ToString();
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            Assert.IsTrue(code.ToString().Contains("public partial struct HelloWorld"));
        }

        [Test]
        public void WhenIsStructContainerContainsStructProperty_CSharpCodeGen_GeneratesProperyContainerWrapper()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""struct HelloWorld"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""struct Foo"",
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
            Assert.IsTrue(code.ToString().Contains("StructMutableContainerProperty<HelloWorld, Foo>"));
        }

        [Test]
        public void WhenIsClassContainerContainsStructProperty_CSharpCodeGen_GeneratesProperyContainerWrapper()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""class HelloWorld"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""struct Foo"",
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
            Assert.IsTrue(code.ToString().Contains("ContainerProperty<HelloWorld, Foo>"));
        }

        [Test]
        public void WhenContainsStructProperty_CSharpCodeGen_GeneratesGetSetValueWithRef()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""struct HelloWorld"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""struct Foo"",
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
            Assert.IsTrue(code.ToString().Contains("GetValue(ref this"));
        }

        [Test]
        public void WhenIsClassPropertyContainer_CSharpCodeGen_GeneratesGetSetValueWithNoRef()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""class HelloWorld"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""struct Foo"",
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
            Assert.IsTrue(code.ToString().Contains("GetValue(this"));
        }

        [Test]
        public void WhenIsClassProperty_CSharpCodeGen_GeneratesGetSetValueWithNoRef()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""class HelloWorld"",
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
            Assert.IsTrue(code.ToString().Contains("GetValue(this"));
            Assert.IsTrue(code.ToString().Contains("SetValue(this"));
        }

        [Test]
        public void WhenIsClassProperty_CSharpCodeGen_DelegatesDefaultValueBackingFieldConstruction()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""class HelloWorld"",
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
            Assert.IsTrue(code.ToString().Contains("int m_Data;"));
            Assert.IsTrue(code.ToString().Contains("m_Data = 5;"));
            Assert.IsTrue(code.ToString().Contains("m_Ints = new List<float> {};"));
        }

        [Test]
        public void WhenIsStructProperty_CSharpCodeGen_DefaultValueBackingFieldConstructionIsDoneInPlace()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""class HelloWorld"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""int"",
                                ""DefaultValue"": ""5"",
                            },
                            ""Floats"": {
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
            var code = backend.Code.ToString();
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

            Assert.IsTrue(code.Contains("m_Data = 5;"));
            Assert.IsTrue(code.Contains("m_Floats = new List<float> {};"));

            // @TODO Inspect syntax tree
/*
            Assert.IsTrue(dataField.Declaration.Type.ToString() == "int");
            Assert.IsTrue(floatsField.Declaration.Type.ToString() == "List<float>");

            Assert.IsTrue(dataField.Declaration.Variables.First().Initializer.Value.ToString() == "5");
            Assert.IsTrue(floatsField.Declaration.Variables.First().Initializer.Value.ToString() == "new List<float>");
*/

            // @TODO
            // CheckIfPropertyContainerIsSerializable(assembly, "HelloWorld");
        }

        [Test]
        public void WhenIsStructProperty_CSharpCodeGen_NoDefaultValue()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""struct HelloWorld"",
                        ""Properties"": {
                            ""Data"": {
                                ""TypeId"": ""int"",
                            },
                            ""Floats"": {
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
            var code = backend.Code.ToString();
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
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""struct HelloWorld"",
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
            var code = backend.Code.ToString();
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            var modifiers = SyntaxFactory.TokenList(new[]
            {
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)
            });

            var constructors = ((CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot())
                .DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .Where(c => c.WithModifiers(modifiers) == null);

            Assert.Zero(constructors.Count());

            // @TODO
            // CheckIfPropertyContainerIsSerializable(assembly, "HelloWorld");
        }

        [Test]
        public void WhenIsClassContainer_CSharpCodeGen_DoesNotGenerateConstructor()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""class HelloWorld"",
                        ""Properties"": {
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code.ToString();
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            var modifiers = SyntaxFactory.TokenList(new[]
            {
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)
            });

            var constructors = ((CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot())
                .DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .Where(c => c.WithModifiers(modifiers) == null);

            Assert.Zero(constructors.Count());
        }

        [Test]
        public void WhenIsClassContainerAndNoProperties_CSharpCodeGen_DoesNotGenerateConstructor()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""class HelloWorld"",
                        ""Properties"": {
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code.ToString();
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            var modifiers = SyntaxFactory.TokenList(new[]
            {
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)
            });

            var constructors = ((CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot())
                .DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .Where(c => c.WithModifiers(modifiers) == null);

            Assert.Zero(constructors.Count());
        }

        [Test]
        public void WhenUserHookSpecified_CSharpCodeGen_GeneratesIt()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""struct HelloWorld"",
                        ""GeneratedUserHooks"": ""OnPropertyBagConstructed"",
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
            var code = backend.Code.ToString();
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            var userHookMethod = ((CompilationUnitSyntax)CSharpSyntaxTree.ParseText(code).GetRoot())
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.ValueText == "OnPropertyBagConstructed");

            Assert.NotZero(userHookMethod.Count());

            // TODO tokenize
            Assert.IsTrue(code.ToString().Contains("static partial void OnPropertyBagConstructed(IPropertyBag bag);"));
            Assert.IsTrue(code.ToString().Contains("OnPropertyBagConstructed(sProperties);"));
        }

        [Test]
        public void WhenOverrideDefaultBaseClasseClass_CSharpCodeGen_GeneratesContainerWithOverridenBaseClass()
        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""class HelloWorld"",
                        ""OverrideDefaultBaseClass"": ""Foo"",
                        ""Properties"": { }
                      },
                      {
                        ""Name"": ""class Foo"",
                        ""Properties"": { }
                      }
                    ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code.ToString();
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            // TODO tokenize, bad britle test
            Assert.IsTrue(code.Contains("public partial class HelloWorld : Foo"));
        }

        [Test]
        public void WhenPropertyContainerSetAsAbstract_CSharpCodeGen_GeneratesAbstractClass()

        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Types"": [
                      {
                        ""Name"": ""class HelloWorld"",
                        ""IsAbstractClass"": ""true"",
                        ""Properties"": { }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code.ToString();
            Assert.NotZero(code.Length);
            
            CheckIfCodeIsCompilable(code);

            // TODO tokenize, bad britle test
            Assert.IsTrue(code.Contains("public abstract partial class HelloWorld : IPropertyContainer"));
        }

        [Test]
        public void WhenPropertyContainerSetAsAbstractForStructContainer_CSharpCodeGen_DoestNotGeneratesAbstractStruct()

        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Types"": [
                      {
                        ""Name"": ""struct HelloWorld"",
                        ""IsAbstractClass"": ""true"",
                        ""Properties"": {
                          }
                        }
                     ]
                 }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code.ToString();
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            // TODO tokenize, bad britle test
            Assert.IsTrue(code.Contains("public partial struct HelloWorld : IPropertyContainer"));
        }

        [Test]
        public void WhenPropertyIsInherited_CSharpCodeGen_DoesNotGeneratesBackingField()

        {
            var backend = new CSharpGenerationBackend();
            var result = JsonSchema.FromJson(@"
            [
                {
                  ""Types"":
                  [
                    {
                      ""Name"": ""class HelloWorld"",
                      ""OverrideDefaultBaseClass"": ""BaseClass"",
                    },
                    {
                      ""Name"": ""class BaseClass"",
                      ""Properties"": {
                        ""Data"": {
                          ""TypeId"": ""int""
                        },
                      }
                    }
                 ]
               }
            ]
        ");
            backend.Generate(result);
            var code = backend.Code.ToString();
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
            var result = JsonSchema.FromJson(@"
            [
                {
                    ""Namespace"": ""Unity.Properties.Samples.Schema"",
                    ""Types"": [
                      {
                        ""Name"": ""struct HelloWorld"",
                        ""GeneratedUserHooks"": ""OnPropertyBagConstructed"",
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
            var code = backend.Code.ToString();
            Assert.NotZero(code.Length);

            CheckIfCodeIsCompilable(code);

            // TODO tokenize, very BAD
            Assert.IsTrue(code.Contains("/* GET */"));
            Assert.IsTrue(code.Contains("/* SET */ null"));
        }
    }
}

#endif // NET_4_6
