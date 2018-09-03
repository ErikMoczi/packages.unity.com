#if USE_ROSLYN_API && (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Formatting;
using Mono.Cecil;
using Unity.Properties.Serialization;

using UnityEngine.Assertions;

namespace Unity.Properties.Editor.Serialization.Experimental
{
    public class RoslynPropertyTypeNode : IPropertyTypeNodeDeserializer, IPropertyTypeNodeSerializer
    {
        public string Code { get; set; } = string.Empty;

        public void Serialize(List<PropertyTypeNode> nodes)
        {
            // @TODO for now use a more convoluted (but more tested route)
        }

        public List<PropertyTypeNode> Deserialize()
        {
            var definitions = new List<PropertyTypeNode>();
            if (string.IsNullOrEmpty(Code))
            {
                return definitions;
            }

            var ast = CSharpSyntaxTree.ParseText(Code);

            var root = (CompilationUnitSyntax) ast.GetRoot();

            var references = ReferenceAssemblies.Locations.Select(
                location => (MetadataReference) MetadataReference.CreateFromFile(location)).ToArray();

            var compilation = CSharpCompilation.Create(Path.GetRandomFileName())
                .AddReferences(references)
                .AddSyntaxTrees(ast);

            var model = compilation.GetSemanticModel(ast);

            var collector = new SemanticInformationCollector(model);

            collector.Visit(root);

            definitions = ExtractPropertyNodesFrom(model, collector);

            return definitions;
        }

        private class FieldCollector : CSharpSyntaxWalker
        {
            public class Property
            {
				public IFieldSymbol Symbol { get; set; }
                public FieldDeclarationSyntax SyntaxNode { get; set; }
            }

			public List<Property> Properties { get; set; } = new List<Property>();

            private readonly SemanticModel _model = null;

            public FieldCollector(SemanticModel model)
            {
                _model = model;
            }
			
            public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                if (IsPropertyDefinition(node))
                {
                    Properties.Add(new Property()
                    {
						Symbol = _model.GetDeclaredSymbol(node.Declaration.Variables.First()) as IFieldSymbol,
                        SyntaxNode = node
                    }
                    );
                }

				base.VisitFieldDeclaration(node);
            }

            private bool IsPropertyDefinition(FieldDeclarationSyntax node)
            {
                if ( ! node.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    return false;
                }

                return IsIProperty(node);
            }

            private bool IsIProperty(FieldDeclarationSyntax node)
            {
                var symbol = _model.GetDeclaredSymbol(node.Declaration.Variables.First()) as IFieldSymbol;

                if (symbol == null)
                {
                    return false;
                }

                if (symbol.Type.AllInterfaces.Any(i => i.Name == typeof(IProperty).Name))
                {
                    return true;
                }

                if (symbol.Type.BaseType != null && symbol.Type.BaseType.Name != typeof(object).Name)
                {
                    return IsDerivedFrom<IProperty>(symbol.Type.BaseType);
                }

                return false;
            }
        }

        private class SemanticInformationCollector : CSharpSyntaxWalker
        {
            public readonly List<string> Usings = new List<string>();
            public readonly List<TypeDeclarationSyntax> ContainerTypes = new List<TypeDeclarationSyntax>();

            private readonly SemanticModel _model = null;

            public SemanticInformationCollector(SemanticModel model)
            {
                _model = model;
            }

            public override void VisitUsingDirective(UsingDirectiveSyntax node)
            {
                Usings.Add(node.ToString());

                base.VisitUsingDirective(node);
            }

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
            {
                if (IsCandidateType(node))
                {
                    return;
                }

                bool isDirectChildOf = true;
                if ( ! InheritsFrom<IPropertyContainer>(_model, node, out isDirectChildOf))
                {
                    return;
                }

                ContainerTypes.Add(node);

                base.VisitStructDeclaration(node);
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if (IsCandidateType(node))
                {
                    return;
                }

                bool isDirectChildOf = true;
                if (!InheritsFrom<IPropertyContainer>(_model, node, out isDirectChildOf))
                {
                    return;
                }

                ContainerTypes.Add(node);

                base.VisitClassDeclaration(node);
            }

            private static bool IsCandidateType(BaseTypeDeclarationSyntax node)
            {
                return node.Modifiers.Contains(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            }
        }

        // Intermediate forms for the AST -> PropertyNode transformation

        internal class Node
        {
            public TypeDeclarationSyntax TypeNode { get; set; }

            public PropertyTypeNode PropertyNode { get; set; }

            public List<Node> Children { get; } = new List<Node>();
        }

        private static bool InheritsFrom<T>(
            SemanticModel model,
            TypeDeclarationSyntax node,
            out bool isDirectInheritance)
        {
            var symbol = model.GetDeclaredSymbol(node);

            if (symbol.AllInterfaces.Any(i => i.Name == typeof(T).Name))
            {
                isDirectInheritance = false;
                return true;
            }

            if (symbol.BaseType != null && symbol.BaseType.Name != typeof(object).Name)
            {
                isDirectInheritance = false;

                return IsDerivedFrom<T>(symbol.BaseType);
            }

            isDirectInheritance = true;

            return false;
        }

        private static bool IsDerivedFrom<T>(INamedTypeSymbol node)
        {
            if (node.AllInterfaces.Any(i => i.Name == typeof(T).Name))
            {
                return true;
            }

            if (node.BaseType != null && node.BaseType.Name != typeof(object).Name)
            {
                return IsDerivedFrom<T>(node.BaseType);
            }

            return false;
        }

        private sealed class RoslynProperty : IntrospectedPropertyDefinition
        {
            public RoslynProperty(IFieldSymbol symbol)
            {
                Symbol = symbol;

                if ( ! (symbol.Type is INamedTypeSymbol))
                {
                    IsValid = false;
                    return;
                }

                var namedType = (INamedTypeSymbol) symbol.Type;
                if ( ! namedType.IsGenericType)
                {
                    IsValid = false;
                    return;
                }

                if (!SetUpTypesFrom(symbol))
                {
                    IsValid = false;
                    return;
                }

                IsReadonly = IsReadonlyPropertyType(WrapperType);
                IsValueType = IsValuePropertyType(WrapperType);

                IsValid = true;
            }

            private bool SetUpTypesFrom(
                IFieldSymbol fieldType)
            {
                WrapperType = TypeEnumFromType(fieldType.Type.Name);

                if ( ! (fieldType.Type is INamedTypeSymbol))
                {
                    return false;
                }

                var namedFieldType = fieldType.Type as INamedTypeSymbol;
                GenericArguments = namedFieldType.TypeArguments.ToList();

                if (GenericArguments.Count > 1)
                {
                    WrappedType = GenericArguments[1];
                }

                var innerTypeName = GenericArguments.Last().Name;

                SetUpTypesFrom(innerTypeName);

                return true;
            }

            public List<ITypeSymbol> GenericArguments { get; internal set; } = new List<ITypeSymbol>();

            public IFieldSymbol Symbol { get; internal set; } = null;

            public ITypeSymbol WrappedType { get; internal set; } = null;
        }

        private static string PropertyNameFromFieldName(string fieldName)
        {
            if (fieldName.StartsWith("s_"))
            {
                fieldName = fieldName.Substring(2);
            }
            if (fieldName.EndsWith("Property"))
            {
                fieldName = fieldName.Substring(0, fieldName.Length - "Property".Length);
            }
            return fieldName;
        }

        private static string NamespaceForType(TypeDeclarationSyntax type)
        {
            SyntaxNode parent = type.Parent;

            while (parent != null)
            {
                if (parent is NamespaceDeclarationSyntax)
                {
                    return ((NamespaceDeclarationSyntax) parent).Name.ToString();
                }

                parent = parent.Parent;
            }

            return string.Empty;
        }

        private List<PropertyTypeNode> ExtractPropertyNodesFrom(
            SemanticModel model, SemanticInformationCollector collector)
        {
            if (model == null)
            {
                throw new Exception("Invalid semantic model for property node introspection (null).");
            }

			var nodes = new List<PropertyTypeNode>();

            var typePerFullTypename = new Dictionary<string, PropertyTypeNode>();

            // Expect a top -> bottom tree traversal, so it means that the
            // list contains types declared in order.

            foreach (var type in collector.ContainerTypes)
            {
                var tag = PropertyTypeNode.TypeTag.Class;
                if (type is StructDeclarationSyntax)
                {
                    tag = PropertyTypeNode.TypeTag.Struct;
                }

                var symbol = model.GetDeclaredSymbol(type);

                var typePath = new ContainerTypeTreePath
                {
                    Namespace = NamespaceForType(type)
                };

                var fullTypeNamePath = symbol.ToString().Replace(typePath.Namespace, "").Trim('.').Split('.');
                fullTypeNamePath = fullTypeNamePath.Take(fullTypeNamePath.Length - 1).ToArray();
                foreach (var pathPart in fullTypeNamePath)
                {
                    typePath.TypePath.Push(pathPart);
                }

                var node = new PropertyTypeNode
                {
					Tag = tag,
                    TypePath = typePath,
					TypeName = symbol.Name,
                    IsAbstractClass = symbol.IsAbstract,
                    OverrideDefaultBaseClass = symbol.AllInterfaces.Any(i => i.Name == typeof(IPropertyContainer).Name)
                        ? string.Empty : symbol.BaseType.Name
                };

                var fieldCollector = new FieldCollector(model);

                fieldCollector.Visit(type);

                foreach (var field in fieldCollector.Properties)
                {
                    var genericProperty = new RoslynProperty(field.Symbol);
                    if (!genericProperty.IsValid)
                        continue;

                    // Parse

                    var property = new PropertyTypeNode()
                    {
                        PropertyName = PropertyNameFromFieldName(field.Symbol.Name),
                        Tag = genericProperty.TypeTag,
                        TypeName = genericProperty.TypeID,
                        Of = genericProperty.ListOf,
                        IsReadonly = genericProperty.IsReadonly,
						IsPublicProperty = field.SyntaxNode.Modifiers.Any(SyntaxKind.PublicKeyword)
                    };

                    // Extract info about backing fields

                    var initializer = field.SyntaxNode.Declaration.Variables.First().Initializer;
                    if (initializer?.Value is ExpressionSyntax)
                    {
/*
                        // @TODO 
                        var initializerExpression = initializer.Value;

                        // @TODO 
                        var symbolNames = model.LookupSymbols(initializerExpression.SpanStart).Select(s => s.Name).ToList();
*/

						// property.DefaultValue = 
                        property.PropertyBackingAccessor = string.Empty;
                    }

                    if (!string.IsNullOrEmpty(property.PropertyName))
                    {
                        node.Properties.Add(property);
                    }
                }

                var containingSymbolFullName = symbol.ContainingSymbol.ToDisplayString();
                // symbol.ContainingSymbol.Name
                if (typePerFullTypename.ContainsKey(containingSymbolFullName))
                {
                    // This is a nested node

                    var parent = typePerFullTypename[containingSymbolFullName];

                    node.TypePath = new ContainerTypeTreePath(parent.TypePath);

                    node.TypePath.TypePath.Push(parent.TypeName);

                    parent.NestedContainers.Add(node);
                }
                else
                {
					// This is a new node

                    typePerFullTypename[node.FullTypeName] = node;
                    nodes.Add(node);
                }
            }

            return nodes;
        }

        private static IEnumerable<TypeDeclarationSyntax> GetTypeDeclarationNodes(
            CompilationUnitSyntax root,
            Func<TypeDeclarationSyntax, bool> predicate)
        {
            var candidateTypes =
                from t in root.ChildNodes().OfType<TypeDeclarationSyntax>().ToList()
                where predicate(t)
                select t;

            return candidateTypes;
        }

        private static IEnumerable<FieldDeclarationSyntax> GetTypeFieldNodes(
            TypeDeclarationSyntax root,
            Func<FieldDeclarationSyntax, bool> predicate)
        {
            var fields = from fieldDeclaration in root.DescendantNodes().OfType<FieldDeclarationSyntax>()
                where predicate(fieldDeclaration)
                select fieldDeclaration;
            return fields;
        }

        private static Dictionary<string, FieldDeclarationSyntax> GetFieldNameAndTypes(
            TypeDeclarationSyntax root,
            SyntaxTokenList modifiers)
        {
            var fields = from fieldDeclaration in GetTypeFieldNodes(root, (t) =>
                        {
                            if (t.Modifiers.Contains(SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                                || t.Modifiers.Contains(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                            {
                                return false;
                            }

                            return true;
                        })
                         from variableDeclarationSyntax in fieldDeclaration.Declaration.Variables
                         select new { variableDeclarationSyntax.Identifier.ValueText, fieldDeclaration };

            return fields.ToDictionary(e => e.ValueText, e => e.fieldDeclaration);
        }

        private static FieldDeclarationSyntax GetFieldSyntaxNodeByName(
            TypeDeclarationSyntax root,
            string fieldName,
            SyntaxTokenList modifiers)
        {
/*            var field = from fieldDeclaration in GetFieldSyntaxNodes(root, modifiers)
                        from variableDeclarationSyntax in fieldDeclaration.Declaration.Variables
                        where variableDeclarationSyntax.Identifier.ValueText == fieldName
                        select fieldDeclaration;

            return field.First();*/
            return null;
        }

        private static ExpressionSyntax GetVariableAssignmentExpression(
            BlockSyntax block,
            string variableName)
        {
            var field = from assignmentExpression in block.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                        where (assignmentExpression.Left as IdentifierNameSyntax) != null &&
                              ((IdentifierNameSyntax)assignmentExpression.Left).Identifier.ValueText == variableName
                        select assignmentExpression.Right;
            return field.First();
        }
    }
}

#endif