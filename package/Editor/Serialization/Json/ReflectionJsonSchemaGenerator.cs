#if NET_4_6

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using Mono.Cecil;
using Unity.Properties.Serialization;

namespace Unity.Properties.Editor.Serialization
{
    public class ReflectionJsonSchemaGenerator
    {
        public static List<PropertyTypeNode> Read(AssemblyDefinition assembly)
        {
            if (assembly == null)
            {
                return new List<PropertyTypeNode>();
            }

            return new ReflectionJsonSchemaGenerator().Parse(assembly);
        }

        public static List<PropertyTypeNode> Read(string assemblyFilePath)
        {
            var resolver = new DefaultAssemblyResolver();
            var basePath = Path.GetDirectoryName(assemblyFilePath);
            resolver.AddSearchDirectory(basePath);

            var assembly = AssemblyDefinition.ReadAssembly(
                assemblyFilePath, new ReaderParameters { AssemblyResolver = resolver });

            return Read(assembly);
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

        private sealed class GenericProperty
        {
            public enum PropertyWrapperType
            {
                Unknown,
                Property,
                StructProperty,
                ContainerProperty,
                MutableContainerProperty,
                StructContainerProperty,
                StructMutableContainerProperty,
                ListProperty,
                StructListProperty,
                ContainerListProperty,
                StructContainerListProperty,
                MutableContainerListProperty,
                StructMutableContainerListProperty,
                EnumProperty,
                StructEnumProperty,
                EnumListProperty,
                StructEnumListProperty,
            }

            public bool IsValid { get; internal set; } = false;

            public PropertyWrapperType WrapperWrapperType { get; internal set; } = PropertyWrapperType.Unknown;

            public List<TypeReference> GenericArguments { get; internal set; } = new List<TypeReference>();

            public string TypeID { get; internal set; } = string.Empty;

            public PropertyTypeNode ListOf { get; internal set; } = null;

            public TypeDefinition TDefinition { get; internal set; } = null;

            public bool IsReadonly { get; internal set; } = false;

            public bool IsValueType { get; internal set; } = false;

            private static string ListTypeId { get; } = "List";

            public GenericProperty(TypeReference typeReference)
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
                    || ! InheritsFrom(TDefinition, typeof(IProperty).FullName))
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

                IsReadonly = IsReadonlyPropertyType(WrapperWrapperType);
                IsValueType = IsValuePropertyType(WrapperWrapperType);

                IsValid = true;
            }

            private bool SetUpTypesFrom(
                TypeDefinition fieldType,
                GenericInstanceType genericInstance)
            {
                WrapperWrapperType = TypeEnumFromType(fieldType);

                GenericArguments = genericInstance.GenericArguments.ToList();

                var innerTypeName = GenericArguments.Last().Name;

                switch (WrapperWrapperType)
                {
                    // --- Plain wrappers
                    case PropertyWrapperType.StructProperty:
                    case PropertyWrapperType.Property:
                        TypeID = innerTypeName;
                        break;
                    case PropertyWrapperType.ContainerProperty:
                    case PropertyWrapperType.StructContainerProperty:
                        TypeID = "class " + innerTypeName;
                        break;
                    case PropertyWrapperType.MutableContainerProperty:
                    case PropertyWrapperType.StructMutableContainerProperty:
                        TypeID = "struct " + innerTypeName;
                        break;

                    // --- Lists
                    case PropertyWrapperType.ListProperty:
                    case PropertyWrapperType.StructListProperty:
                        TypeID = ListTypeId;
                        ListOf = new PropertyTypeNode()
                        {
                            TypeName = innerTypeName
                        };
                        break;
                    case PropertyWrapperType.StructContainerListProperty:
                    case PropertyWrapperType.ContainerListProperty:
                        TypeID = ListTypeId;
                        ListOf = new PropertyTypeNode()
                        {
                            TypeName = "class " + innerTypeName
                        };
                        break;
                    case PropertyWrapperType.EnumListProperty:
                    case PropertyWrapperType.StructEnumListProperty:
                        TypeID = ListTypeId;
                        ListOf = new PropertyTypeNode()
                        {
                            TypeName = "enum " + innerTypeName
                        };
                        break;
                    case PropertyWrapperType.MutableContainerListProperty:
                    case PropertyWrapperType.StructMutableContainerListProperty:
                        TypeID = ListTypeId;
                        ListOf = new PropertyTypeNode()
                        {
                            TypeName = "struct " + innerTypeName
                        };
                        break;

                    // --- Enums
                    case PropertyWrapperType.EnumProperty:
                        TypeID = "enum " + innerTypeName;
                        break;
                    case PropertyWrapperType.StructEnumProperty:
                        TypeID = "enum " + innerTypeName;
                        break;
                    default:
                        return false;
                }

                return true;
            }

            private static bool IsReadonlyPropertyType(PropertyWrapperType t)
            {
                bool isReadonly = false;
                switch (t)
                {
                    case PropertyWrapperType.StructProperty:
                    case PropertyWrapperType.ContainerProperty:
                    case PropertyWrapperType.StructContainerProperty:
                    case PropertyWrapperType.ListProperty:
                    case PropertyWrapperType.StructListProperty:
                    case PropertyWrapperType.ContainerListProperty:
                        isReadonly = true;
                        break;
                }
                return isReadonly;
            }

            private static bool IsValuePropertyType(PropertyWrapperType t)
            {
                bool isValueType = false;
                switch (t)
                {
                    case PropertyWrapperType.MutableContainerProperty:
                    case PropertyWrapperType.StructMutableContainerProperty:
                    case PropertyWrapperType.StructEnumListProperty:
                        isValueType = true;
                        break;
                }
                return isValueType;
            }

            private static string ExtractPlainGenericTypeName(string genericTypeName)
            {
                // @TODO hackish
                return genericTypeName.Substring(0, genericTypeName.IndexOf("`", StringComparison.Ordinal));
            }

            private static PropertyWrapperType TypeEnumFromType(TypeDefinition typeDefinition)
            {
                var plainGenericType = ExtractPlainGenericTypeName(typeDefinition.Name);

                if (!Enum.GetNames(typeof(PropertyWrapperType)).Contains(plainGenericType))
                {
                    return PropertyWrapperType.Unknown;
                }

                PropertyWrapperType t = PropertyWrapperType.Unknown;
                if (!Enum.TryParse(plainGenericType, out t))
                {
                    return PropertyWrapperType.Unknown;
                }
                return t;
            }
        }

        private static Dictionary<string, FieldDefinition>
            CollectBackingFieldCandidatesForType(TypeDefinition type)
        {
            // @TODO HACK: this is a bad heuristic to determine BY NAME if there is a backing
            // field for that property

            return type.Fields
                .Where(f => !f.IsStatic && f.IsPrivate)
                .Select(f => new KeyValuePair<string /* field name */, FieldDefinition>(f.Name, f)).ToDictionary(e => e.Key, e => e.Value);
        }

        private static List<PropertyTypeNode>
            SerializePropertyFieldsForType(TypeDefinition type)
        {
            // Collect plain backing fields by name & type

            var candidateBackingFields = CollectBackingFieldCandidatesForType(type);

            var properties = new List<PropertyTypeNode>();

            foreach (var f in type.Fields)
            {
                // Property field validations

                if (!f.IsStatic)
                    continue;

                TypeDefinition fieldType = null;
                if (!TryResolveTypeReference(f.FieldType, out fieldType))
                    continue;

                if ( ! InheritsFrom(fieldType, typeof(IProperty).FullName))
                    continue;

                var genericProperty = new GenericProperty(f.FieldType);
                if (!genericProperty.IsValid)
                    continue;

                // Parse

                var property = new PropertyTypeNode()
                {
                    Name = PropertyNameFromFieldName(f.Name),
                    TypeName = genericProperty.TypeID,
                    Of = genericProperty.ListOf,
                    IsReadonly = genericProperty.IsReadonly,
                };

                var propertyBackingFieldPrefix = "m_";
                if (candidateBackingFields.ContainsKey(propertyBackingFieldPrefix + property.Name))
                {
                    // Find default value
                    var a = candidateBackingFields[propertyBackingFieldPrefix + property.Name];
                }
                else
                {
                    // oops ... @TODO find proper backing field
                    property.PropertyBackingAccessor = string.Empty;
                }

                if (!string.IsNullOrEmpty(property.Name))
                {
                    properties.Add(property);
                }
            }
            return properties;
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
                Name = string.IsNullOrEmpty(type.Namespace) ? type.FullName : type.FullName.Remove(0, type.Namespace.Length),
                Namespace = type.Namespace,
                Children = SerializePropertyFieldsForType(type)
            };

            // @TODO beware of circular deps

            if (type.IsNested)
            {
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

                    if (!_typesBeingSerialized.ContainsKey(parent.FullName))
                    {
                        throw new Exception($"Invalid type definition tree when trying to recurse to {type.FullName}'s type parent");
                    }

                    var parentDefinition = _typesBeingSerialized[parent.FullName];

                    if (parent.ChildContainers.FirstOrDefault(c => c.FullName == type.FullName) != null)
                    {
                        // Should not happen unless there is an issue with the cache
                        throw new Exception(
                            $"Cannot add duplicate nested child node {type.FullName} to parent {parent.FullName}");
                    }

                    parent.ChildContainers.Add(n);

                    node = null;
                    return false;
                }
            }

            node = n;
            return true;
        }

        private List<PropertyTypeNode> Parse(Mono.Cecil.AssemblyDefinition assembly)
        {
            try
            {
                _typesBeingSerialized.Clear();
                _parsedContainerTypeNodesCache.Clear();

                // 1. Collect all the types to be looked at (all being Ipropertycontainers)

                foreach (var type in CollectPropertyContainers(assembly.Modules.SelectMany(m => m.GetTypes()).ToList()))
                {
                    _typesBeingSerialized[type.FullName] = type;
                }

                // 2. Create the type tree (taking nested classes into account)

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
            catch (Exception) { }

            return new List<PropertyTypeNode> ();
        }
    }
}

#endif // NET_4_6
