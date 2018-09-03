#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

using Mono.Cecil;

using Unity.Properties.Serialization;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Unity.Properties.Editor.Serialization
{
    public class ReflectionPropertyTree
    {
        public static List<PropertyTypeNode> Read(AssemblyDefinition assembly)
        {
            if (assembly == null)
            {
                throw new Exception("Invalid assembly");
            }

            return new ReflectionPropertyTree().Parse(assembly);
        }

        public static List<PropertyTypeNode> Read(string assemblyFilePath)
        {
            var resolver = new DefaultAssemblyResolver();
            var basePath = Path.GetDirectoryName(assemblyFilePath);

            resolver.AddSearchDirectory(basePath);

            foreach (var assemblyLocation in ReferenceAssemblies.Locations)
            {
                resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyLocation));
            }

            var assembly = AssemblyDefinition.ReadAssembly(
                assemblyFilePath, new ReaderParameters { AssemblyResolver = resolver });

            return Read(assembly);
        }

        private List<PropertyTypeNode> Parse(Mono.Cecil.AssemblyDefinition assembly)
        {
            _typesBeingSerialized.Clear();
            _parsedContainerTypeNodesCache.Clear();

            // 1. Collect all the types to be looked at (all being Ipropertycontainers)

            foreach (var type in CollectPropertyContainers(assembly.Modules.SelectMany(m => m.GetTypes()).ToList()))
            {
                _typesBeingSerialized[type.FullName] = type;
            }

            // 2. Create the type tree (taking nested classes into account)

            return TypeDefinitionsToPropertyTypes(_typesBeingSerialized);
        }

        private List<PropertyTypeNode> TypeDefinitionsToPropertyTypes(Dictionary<string, TypeDefinition> definitions)
        {
            var roots = new List<PropertyTypeNode>();

            foreach (var kv in _typesBeingSerialized)
            {
                var typeName = kv.Key;

                PropertyTypeNode node = null;
                if (GetOrCreate(typeName, out node))
                {
                    Assert.IsNotNull(node);
                    roots.Add(node);
                }
            }

            return roots;
        }

        public static Type TypeFromTypeDefinition(TypeDefinition typeDefinition)
        {
            return Type.GetType(Assembly.CreateQualifiedName(typeDefinition.Module.Assembly.FullName,
                typeDefinition.FullName));
        }
        
        public static List<PropertyTypeNode>
            SerializePropertyFieldsForType(TypeDefinition type)
        {
            // Collect plain backing fields by name & type

            var candidateBackingFields = CollectBackingFieldCandidatesForType(type);

            var properties = new List<PropertyTypeNode>();

            foreach (var f in type.Fields)
            {
                try
                {
                    // Property field validations
                    TypeDefinition fieldType = null;
                    if (!TryResolveTypeReference(f.FieldType, out fieldType))
                        continue;

                    if (!f.IsStatic)
                        continue;

                    if (!InheritsFrom(fieldType, typeof(IProperty).FullName))
                        continue;

                    var genericProperty = new CecilPropertyDefinition(f.FieldType);
                    if (!genericProperty.IsValid)
                        continue;

                    // Parse

                    var resolvedFieldType = genericProperty.WrappedType.Resolve();

                    if (resolvedFieldType == null)
                    {
                        Debug.Log($"Could not resolve type '{genericProperty.WrappedType.FullName}' when parsing container field");
                        continue;
                    }

                    var property = new PropertyTypeNode()
                    {
                        PropertyName = PropertyNameFromFieldName(f.Name),
                        Tag = genericProperty.TypeTag,
                        TypeName = genericProperty.TypeID,
                        Of = genericProperty.ListOf,
                        IsReadonly = genericProperty.IsReadonly,
                        NativeType = TypeFromTypeDefinition(resolvedFieldType),
                        IsPublicProperty = f.IsPublic
                    };

                    const string propertyBackingFieldPrefix = "m_";
                    if (candidateBackingFields.ContainsKey(propertyBackingFieldPrefix + property.PropertyName))
                    {
                        // Find default value
                        var a = candidateBackingFields[propertyBackingFieldPrefix + property.PropertyName];
                    }
                    else
                    {
                        // oops ... @TODO find proper backing field
                        property.PropertyBackingAccessor = string.Empty;
                    }

                    if (!string.IsNullOrEmpty(property.PropertyName))
                    {
                        properties.Add(property);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.ToString());
                }
            }
            return properties;
        }

        private static bool IsPropertyContainer(TypeDefinition type)
        {
            return InheritsFrom(type, typeof(IPropertyContainer).FullName);
        }

        private static bool HasDirectInterfacesOf(TypeDefinition type, string interfaceName)
        {
            foreach (var i in type.Interfaces)
            {
                if (i.InterfaceType.FullName == interfaceName)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool InheritsFrom(TypeDefinition type, string baseTypename)
        {
            if (typeof(object).FullName == type.FullName)
            {
                // we went up there
                return false;
            }

            if (baseTypename == type.FullName)
            {
                return true;
            }

            if (HasDirectInterfacesOf(type, baseTypename))
            {
                return true;
            }

            TypeDefinition baseTypeDefinition = null;
            if ( ! TryResolveTypeReference(type.BaseType, out baseTypeDefinition))
            {
                return false;
            }

            return InheritsFrom(baseTypeDefinition, baseTypename);
        }

        private static IEnumerable<TypeDefinition> CollectPropertyContainers(IEnumerable<TypeDefinition> types)
        {
            List<TypeDefinition> r = new List<TypeDefinition>();
            foreach (var type in types)
            {
                if (IsPropertyContainer(type))
                {
                    r.Add(type);
                }
            }
            return r;
        }

        private static bool TryResolveTypeReference(
            TypeReference typeReference,
            out TypeDefinition typeDefinition)
        {
            try
            {
                typeDefinition = typeReference.Resolve();
                return true;
            }
            catch (Exception)
            {
                typeDefinition = null;
            }
            return false;
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

        private sealed class CecilPropertyDefinition : IntrospectedPropertyDefinition
        {
            public CecilPropertyDefinition(TypeReference typeReference)
            {
                TypeDefinition fieldtype = null;

                if (!TryResolveTypeReference(typeReference, out fieldtype))
                {
                    IsValid = false;
                    return;
                }
                Debug.Assert(fieldtype != null);

                TDefinition = fieldtype;

                if (!typeReference.IsGenericInstance
                    || !InheritsFrom(TDefinition, typeof(IProperty).FullName))
                {
                    IsValid = false;
                    return;
                }

                var genericInstance = typeReference as GenericInstanceType;
                if (genericInstance == null)
                {
                    // oops
                    IsValid = false;
                    return;
                }

                if (!SetUpTypesFrom(fieldtype, genericInstance))
                {
                    IsValid = false;
                    return;
                }

                IsReadonly = IsReadonlyPropertyType(WrapperType);
                IsValueType = IsValuePropertyType(WrapperType);


                IsValid = true;
            }
            private static string ExtractPlainGenericTypeName(string genericTypeName)
            {
                // @TODO hackish
                return genericTypeName.Substring(0, genericTypeName.IndexOf("`", StringComparison.Ordinal));
            }

            private bool SetUpTypesFrom(
                TypeDefinition fieldType,
                GenericInstanceType genericInstance)
            {
                var plainGenericType = ExtractPlainGenericTypeName(fieldType.Name);

                WrapperType = TypeEnumFromType(plainGenericType);

                GenericArguments = genericInstance.GenericArguments.ToList();

                if (GenericArguments.Count > 1)
                {
                    WrappedType = GenericArguments[1];
                }

                var innerTypeName = GenericArguments.Last().Name;

                SetUpTypesFrom(innerTypeName);

                return true;
            }

            public List<TypeReference> GenericArguments { get; internal set; } = new List<TypeReference>();

            public TypeDefinition TDefinition { get; internal set; } = null;

            public TypeReference WrappedType { get; internal set; } = null;
        }

        private static Dictionary<string, FieldDefinition>
            CollectBackingFieldCandidatesForType(TypeDefinition type)
        {
            // @TODO HACK: this is a bad heuristic to determine BY NAME if there is a backing
            // field for that property

            return type.Fields
                .Where(f => !f.IsStatic && f.IsPrivate)
                .Select(f => new KeyValuePair<string /* field name */, FieldDefinition>(f.Name, f))
                .ToDictionary(e => e.Key, e => e.Value);
        }

        private readonly Dictionary<string, TypeDefinition> _typesBeingSerialized = new Dictionary<string, TypeDefinition>();

        private readonly Dictionary<string, PropertyTypeNode> _parsedContainerTypeNodesCache = new Dictionary<string, PropertyTypeNode>();

        private static PropertyTypeNode.TypeTag TypeTagFromTypeDefinition(TypeDefinition type)
        {
            if (type.IsClass)
            {
                return PropertyTypeNode.TypeTag.Class;
            }
            if (type.IsEnum)
            {
                return PropertyTypeNode.TypeTag.Enum;
            }
            if (type.Attributes.HasFlag(TypeAttributes.SequentialLayout))
            {
                return PropertyTypeNode.TypeTag.Struct;
            }
            if (HasDirectInterfacesOf(type, "IEnumerable"))
            {
                return PropertyTypeNode.TypeTag.List;
            }
            return PropertyTypeNode.TypeTag.Primitive;
        }

        private bool GetOrCreate(string typeName, out PropertyTypeNode node)
        {
            if (_parsedContainerTypeNodesCache.ContainsKey(typeName))
            {
                // Already created
                node = _parsedContainerTypeNodesCache[typeName];
                return false;
            }

            if (!_typesBeingSerialized.ContainsKey(typeName))
            {
                // Ooops
                node = null;
                return false;
            }

            var type = _typesBeingSerialized[typeName];

            var n = _parsedContainerTypeNodesCache[typeName] = new PropertyTypeNode()
            {
                IsAbstractClass = type.IsAbstract,
                Tag = TypeTagFromTypeDefinition(type),
                UserHooks = type.Methods.FirstOrDefault(m => m.Name == UserHookFlags.OnPropertyBagConstructed.ToString()) != null ?
                    UserHookFlags.OnPropertyBagConstructed : UserHookFlags.None,
                OverrideDefaultBaseClass = HasDirectInterfacesOf(type, typeof(IPropertyContainer).FullName) ?
                    string.Empty : type.BaseType.Resolve().Name,
                TypeName = type.Name,
                Properties = SerializePropertyFieldsForType(type),
                NativeType = TypeFromTypeDefinition(type),
                TypePath = new ContainerTypeTreePath { Namespace = type.Namespace }
            };

            // @TODO beware of circular deps

            if (type.IsNested)
            {
                var path = ContainerTypeTreePath.CreateFromString(type.FullName);
                path.TypePath.Pop();

                n.TypePath = path;

                // if we are a nested class part of a non ipropertycontainer derived struct/class
                // we stop the traversal here

                if (! _typesBeingSerialized.ContainsKey(type.DeclaringType.FullName)
                        && (type.DeclaringType.IsClass || type.DeclaringType.IsValueType))
                {
                    node = n;

                    return true;
                }
                else
                {
                    // get the parent

                    PropertyTypeNode parent = null;

                    GetOrCreate(type.DeclaringType.FullName, out parent);

                    if (!_typesBeingSerialized.ContainsKey(parent.FullTypeName))
                    {
                        throw new Exception($"Invalid type definition tree when trying to recurse to {type.FullName}'s type parent");
                    }

                    var parentDefinition = _typesBeingSerialized[parent.FullTypeName];

                    if (parent.NestedContainers.FirstOrDefault(c => c.FullTypeName == type.FullName) != null)
                    {
                        // Should not happen unless there is an issue with the cache
                        throw new Exception(
                            $"Cannot add duplicate nested child node {type.FullName} to parent {parent.FullTypeName}");
                    }

                    parent.NestedContainers.Add(n);

                    node = null;
                    return false;
                }
            }

            node = n;

            return true;
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
