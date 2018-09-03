#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.Properties.Serialization;
using UnityEngine;

namespace Unity.Properties.Editor.Serialization.Experimental
{
    public class CecilGenerationBackend : GenerationBackend
    {
        public string AssemblyFilePath { get; set;  } = string.Empty;

        public TypeDefinition GeneratedContainerTypeDefinition { get; set; }

        public PropertyTypeNode GeneratePropertyTypeNodeFromType(Type referenceType)
        {
            return new PropertyTypeNode()
            {
                TypeName = referenceType.Name,
            };
        }

        private class CecilGenerationFragmentContext : FragmentContext
        {
            public ModuleDefinition Module { get; set; }

            public TypeDefinition Type { get; set; }

            public Action<ILProcessor> Fragment { private get; set; }

            public override void AddIlFragment(ILProcessor ilProcessor)
            {
                Fragment?.Invoke(ilProcessor);
            }
        }

        public List<FieldDefinition> PropertyBagItemTypes { get; set; } = new List<FieldDefinition>();

        private static AssemblyDefinition GetAssembly(string location)
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(IPropertyContainer).Assembly.Location));

            return AssemblyDefinition.ReadAssembly(
                location,
                new ReaderParameters()
                {
                    AssemblyResolver = resolver
                });
        }

        private static TypeReference _r = null;
        private static TypeReference GetPropertyContainerTypeReference()
        {
            if (_r == null)
            {
                var assembly = GetAssembly(typeof(IPropertyContainer).Assembly.Location);
                _r = assembly.MainModule.Types.FirstOrDefault(t => t.Name == typeof(IPropertyContainer).Name);
            }
            return _r;
        }

        private static readonly Dictionary<string, Type> PropertyWrapperTypeFromName =
            new Dictionary<string, Type>()
            {
                { "StructMutableContainerProperty", null },
                { "MutableContainerProperty", null },

                { "ContainerProperty", null },
                { "StructContainerProperty", null },

                { "EnumProperty", null },
                { "StructEnumProperty", null },

                { "ListProperty", null },
                { "StructListProperty", null },

                { "MutableContainerListProperty", null },
                { "StructMutableContainerListProperty", null },

                { "ContainerListProperty", null },
                { "StructContainerListProperty", null },

                { "EnumListProperty", null },
                { "StructEnumListProperty", null },

                { "Property", null },
                { "StructProperty", null },
            };

        private static string GetPropertyWrapperTypeString(
            PropertyTypeNode containerType,
            PropertyTypeNode propertyType)
        {
            var propertyWrapperTypeStringPrefix = string.Empty;
            if (containerType.Tag.HasFlag(PropertyTypeNode.TypeTag.Struct))
            {
                propertyWrapperTypeStringPrefix = "Struct";
            }

            switch (propertyType.Tag)
            {
                case PropertyTypeNode.TypeTag.Struct:
                    return $"{propertyWrapperTypeStringPrefix}MutableContainerProperty";

                case PropertyTypeNode.TypeTag.Class:
                    return $"{propertyWrapperTypeStringPrefix}ContainerProperty";

                case PropertyTypeNode.TypeTag.Enum:
                    return $"{propertyWrapperTypeStringPrefix}EnumProperty";

                case PropertyTypeNode.TypeTag.List:
                    {
                        switch (propertyType.Of.Tag)
                        {
                            case PropertyTypeNode.TypeTag.Primitive:
                                return $"{propertyWrapperTypeStringPrefix}ListProperty";
                            case PropertyTypeNode.TypeTag.Struct:
                                return $"{propertyWrapperTypeStringPrefix}MutableContainerListProperty";
                            case PropertyTypeNode.TypeTag.Class:
                                return $"{propertyWrapperTypeStringPrefix}ContainerListProperty";
                            case PropertyTypeNode.TypeTag.Enum:
                                return $"{propertyWrapperTypeStringPrefix}EnumListProperty";
                            case PropertyTypeNode.TypeTag.Unknown:
                            case PropertyTypeNode.TypeTag.List:
                            default:
                                throw new Exception($"Invalid property tag for list property name {propertyType.PropertyName}");
                        }
                    }

                case PropertyTypeNode.TypeTag.Primitive:
                    return $"{propertyWrapperTypeStringPrefix}Property";

                default:
                    break;
            }
            return $"{propertyWrapperTypeStringPrefix}Property";
        }

        private static Type GetPropertyWrapperTypeFor(
            PropertyTypeNode containerType,
            PropertyTypeNode propertyType)
        {
            var propertyWrapperTypeString = GetPropertyWrapperTypeString(containerType, propertyType);

            Type wrapperType;
            if ( ! PropertyWrapperTypeFromName.TryGetValue(propertyWrapperTypeString, out wrapperType))
            {
                return null;
            }
            if (wrapperType == null)
            {
                return null;
            }

            if (PropertyTypeNode.IsEnumerableType(propertyType.Tag))
            {
                return wrapperType.MakeGenericType(
                    containerType.NativeType,
                    typeof(List<>).MakeGenericType(propertyType.NativeType),
                    propertyType.NativeType);
            }

            return wrapperType.MakeGenericType(containerType.NativeType, propertyType.NativeType);
        }

        public void GenerateBackingField(
            ModuleDefinition module,
            TypeDefinition type,
            string fieldName,
            Type fieldType)
        {
            type.Fields.Add(new FieldDefinition(fieldName, FieldAttributes.Private, module.ImportReference(fieldType)));
        }

        public void GeneratePropertyWrapperBackingVariable(
            ModuleDefinition module,
            TypeDefinition type,
            string fieldName,
            Type fieldType)
        {
            /*
            if (containerTypeTag == PropertyTypeNode.TypeTag.Class)
            {
                accessModifiers |= AccessModifiers.Protected;
            }
            else
            {
                accessModifiers |= AccessModifiers.Private;
            }
            */

            type.Fields.Add(
                new FieldDefinition(
                    fieldName,
                    FieldAttributes.Static | FieldAttributes.Private, module.ImportReference(fieldType)
                    )
                );
        }

        private FieldDefinition GeneratePropertyBag(
            ModuleDefinition module,
            TypeDefinition type,
            List<FieldDefinition> propertyWrapperFields)
        {
            // Backing field

            var propertyBagBackingField = new FieldDefinition(
                "s_PropertyBag",
                FieldAttributes.Static | FieldAttributes.Private,
                module.ImportReference(typeof(PropertyBag))
                );

            type.Fields.Add(propertyBagBackingField);

            // Accessor method

            var getPropertyBagMethod =
                new MethodDefinition(
                    "get_PropertyBag",
                    MethodAttributes.Private |
                        MethodAttributes.HideBySig |
                        MethodAttributes.SpecialName |
                        MethodAttributes.RTSpecialName,
                    module.ImportReference(typeof(PropertyBag))
                    );

            getPropertyBagMethod.Body.Variables.Add(
                new VariableDefinition(module.ImportReference(typeof(PropertyBag))));

            var il = getPropertyBagMethod.Body.GetILProcessor();
            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Ldfld, propertyBagBackingField));
            il.Append(il.Create(OpCodes.Ret));

            type.Methods.Add(getPropertyBagMethod);

            // Actual property

            var propertyBagProperty = new PropertyDefinition(
                "PropertyBag",
                PropertyAttributes.None,
                module.ImportReference(typeof(PropertyBag))
                )
            {
                HasThis = true,
                GetMethod = getPropertyBagMethod,
            };

            type.Properties.Add(propertyBagProperty);

            AddStaticConstructorInStageFragment(
                    ConstructorStage.PropertyInitializationStage,
                    new CecilGenerationFragmentContext()
                    {
                        Module = module,
                        Type = type,
                        Fragment = ilProcessor =>
                        {
                            foreach (var propertyBagItem in PropertyBagItemTypes)
                            {
                                ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0));
                                ilProcessor.Append(ilProcessor.Create(OpCodes.Stfld, propertyBagItem));
                            }

                            ilProcessor.Append(ilProcessor.Create(OpCodes.Newobj,
                                type.Module.ImportReference(typeof(List<IProperty>))));

                            ilProcessor.Append(ilProcessor.Create(OpCodes.Newobj,
                                type.Module.ImportReference(typeof(PropertyBag))));

                            ilProcessor.Append(ilProcessor.Create(OpCodes.Stfld,
                                propertyBagBackingField));
                        }
                    })
                ;

            return propertyBagBackingField;
        }

        private void GenerateVersionStorage(
            ModuleDefinition module,
            TypeDefinition type)
        {
            // Accessor method

            var getVersionStorageMethod =
                new MethodDefinition(
                    "get_VersionStorage",
                    MethodAttributes.Private |
                        MethodAttributes.HideBySig |
                        MethodAttributes.SpecialName |
                        MethodAttributes.RTSpecialName,
                    module.ImportReference(typeof(IVersionStorage))
                    );

            getVersionStorageMethod.Body.Variables.Add(
                new VariableDefinition(module.ImportReference(typeof(IVersionStorage))));

            var il = getVersionStorageMethod.Body.GetILProcessor();
            il.Append(il.Create(OpCodes.Ldarg_0));

            var propertyName = "Instance";
            var instanceBackingFieldName = "<" + propertyName + ">k__BackingField";
            var passthroughReference = module.ImportReference((Type) null);

            try
            {
                var rp = passthroughReference.Resolve();

                var passthroughInstanceRef = rp.Fields.Single(field => field.Name == instanceBackingFieldName);

                il.Append(il.Create(OpCodes.Ldfld, module.ImportReference(passthroughInstanceRef)));
                il.Append(il.Create(OpCodes.Ret));

                type.Methods.Add(getVersionStorageMethod);

                // Actual property

                var versionStorageProperty = new PropertyDefinition(
                    "VersionStorage",
                    PropertyAttributes.None,
                    module.ImportReference(typeof(PropertyBag))
                )
                {
                    HasThis = true,
                    GetMethod = getVersionStorageMethod,
                };

                type.Properties.Add(versionStorageProperty);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private readonly Dictionary<PropertyTypeNode, TypeDefinition> _typeDefinitionPerPropertyNodeMap =
            new Dictionary<PropertyTypeNode, TypeDefinition>();

        private static string GetPropertyContainerClassNameForType(
            ModuleDefinition moduleDefinition, Type containerType, string containerName)
        {
            var baseName = $"{containerName}PropertyContainer";
            while (moduleDefinition.GetType($"{containerType.Namespace}.{baseName}") != null)
            {
                baseName += "1";
            }

            return baseName;
        }

        public override void OnPropertyContainerGenerationStarted(PropertyTypeNode container)
        {
            var assembly = GetAssembly(container.NativeType.Assembly.Location);

            var module = assembly.Modules.FirstOrDefault(
                m => m.FileName == container.NativeType.Module.FullyQualifiedName);
            if (module == null)
            {
                // It doesn't seem to exist
                // TODO fallback
                return;
            }

            var attributes = TypeAttributes.Public | TypeAttributes.Class;

            if (container.IsAbstractClass)
            {
                attributes |= TypeAttributes.Abstract;
            }

            if (container.Tag == PropertyTypeNode.TypeTag.Struct)
            {
                attributes |= TypeAttributes.SequentialLayout;
            }

            var containerType = new TypeDefinition(
                container.TypePath.Namespace,
                GetPropertyContainerClassNameForType(module, container.NativeType, container.TypeName),
                attributes,
                GetPropertyContainerTypeReference()
            );

            GenerateVersionStorage(module, containerType);

            module.Types.Add(containerType);

            GeneratedContainerTypeDefinition = containerType;

            _typeDefinitionPerPropertyNodeMap[container] = containerType;
        }

        private static string BackingFieldFromPropertyName(string name)
        {
            return $"_{name}";
        }

        private static string PropertyWrapperFieldName(string name)
        {
            return $"{name}Property";
        }

        public override void OnPropertyGenerationStarted(PropertyTypeNode container, PropertyTypeNode property)
        {
            TypeDefinition type;
            if (!_typeDefinitionPerPropertyNodeMap.TryGetValue(container, out type))
            {
                // @TODO error
                return;
            }
            
            // Backing field (should be optional)

            var backingField = new FieldDefinition(
                BackingFieldFromPropertyName(property.PropertyName),
                FieldAttributes.Private,
                type.Module.ImportReference(property.NativeType)
            );
            type.Fields.Add(backingField);

            ConstructorInitializerFragments.Add(
                new CecilGenerationFragmentContext()
                {
                    Module = type.Module,
                    Type = type,
                    Fragment = ilProcessor =>
                    {
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Newobj, type.Module.ImportReference(property.NativeType)));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Stfld, backingField));
                    }
                });

            // Property wrapper static field 
            var propertyWrapperType = type.Module.ImportReference(GetPropertyWrapperTypeFor(container, property));

            var propertyWrapperField = new FieldDefinition(
                PropertyWrapperFieldName(property.PropertyName),
                FieldAttributes.Private | FieldAttributes.Static,
                propertyWrapperType
            );
            type.Fields.Add(propertyWrapperField);

            PropertyBagItemTypes.Add(propertyWrapperField);

            // TODO add a post processing field initialization

            ConstructorInitializerFragments.Add(
                new CecilGenerationFragmentContext()
                {
                    Module = type.Module,
                    Type = type,
                    Fragment = ilProcessor =>
                    {
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0));
                        
                        // #arg 0
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, property.PropertyName));
                        
                        // #arg 1 (getter)
                        // #arg 2 (setter)
                        // #arg 2 (reference)
                    }
                });
            
            /*
            FloatListProperty =
                new ListProperty<TestContainer, List<float>, float>(nameof(FloatList),
                    c => c._floatList,
                    null,
                    null);
             */

            // Accessor method

            var getPropertyMethod =
                new MethodDefinition(
                    "get_" + property.PropertyName,
                    MethodAttributes.Public |
                    MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName |
                    MethodAttributes.RTSpecialName,
                    type.Module.ImportReference(property.NativeType)
                );

            getPropertyMethod.Body.Variables.Add(
                new VariableDefinition(type.Module.ImportReference(typeof(PropertyBag))));

            //      get { return FloatListProperty.GetValue(this); }
            var il = getPropertyMethod.Body.GetILProcessor();
            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Ldfld, propertyWrapperField));
            il.Append(il.Create(OpCodes.Ldarg_0));

            var getValueDelegateRef = propertyWrapperType.Resolve().Methods.FirstOrDefault(m => m.Name == "GetValue");
            il.Append(il.Create(OpCodes.Callvirt, type.Module.ImportReference(getValueDelegateRef)));
            il.Append(il.Create(OpCodes.Ret));

            type.Methods.Add(getPropertyMethod);

            // Actual property

            //      public List<float> FloatList
            var propertyAccessorForProperty = new PropertyDefinition(
                property.PropertyName,
                PropertyAttributes.None,
                type.Module.ImportReference(property.NativeType)
            )
            {
                HasThis = true,
                GetMethod = getPropertyMethod,
            };

            type.Properties.Add(propertyAccessorForProperty);
        }

        public override void OnGenerateUserHooksForContainer(PropertyTypeNode container)
        {
            if (container.UserHooks.HasFlag(UserHookFlags.OnPropertyBagConstructed))
            {
                TypeDefinition type = null;
                if (!_typeDefinitionPerPropertyNodeMap.TryGetValue(container, out type))
                {
                    // @TODO error
                    return;
                }
            }
        }

        public override void OnGeneratePropertyBagForContainer(PropertyTypeNode container)
        {
            TypeDefinition type = null;
            if (!_typeDefinitionPerPropertyNodeMap.TryGetValue(container, out type))
            {
                // @TODO error
                return;
            }

            GeneratePropertyBag(type.Module, type, PropertyBagItemTypes);
        }


        public override void OnGenerateConstructorForContainer(PropertyTypeNode container)
        {
            TypeDefinition type;
            if (!_typeDefinitionPerPropertyNodeMap.TryGetValue(container, out type))
            {
                // @TODO error
                return;
            }

            try
            {
                MethodDefinition constructor = null;

                // Check if there is one already
                var constructors = type.GetConstructors();
                if (constructors != null && constructors.Count() == 0)
                {
                    constructor = constructors.FirstOrDefault(c => !c.HasParameters);
                }

                bool constructorCreated = constructor == null;
                if (constructor == null)
                {
                    const MethodAttributes attributes = MethodAttributes.Public |
                                                        MethodAttributes.HideBySig |
                                                        MethodAttributes.SpecialName |
                                                        MethodAttributes.RTSpecialName;

                    constructor = new MethodDefinition(
                        ".ctor", attributes, type.Module.TypeSystem.Void);
                }

                constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));

                var baseConstructor = type.BaseType.Resolve().GetConstructors().FirstOrDefault(c => !c.HasParameters);
                if (baseConstructor != null)
                {
                    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, baseConstructor));
                }

                // TODO add static property initialisers
                // TODO add user hooks

                GenerateFragments(ConstructorInitializerFragments, constructor.Body.GetILProcessor());

                if (constructorCreated)
                {
                    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

                    type.Methods.Add(constructor);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                throw;
            }
        }

        private static void GenerateFragments(IEnumerable<FragmentContext> fragments, ILProcessor ilProcessor)
        {
            foreach (var fragment in fragments)
            {
                var ilFragment = fragment as CecilGenerationFragmentContext;
                ilFragment?.AddIlFragment(ilProcessor);
            }
        }

        public override void OnGenerateStaticConstructorForContainer(PropertyTypeNode container)
        {
            TypeDefinition type = null;
            if (!_typeDefinitionPerPropertyNodeMap.TryGetValue(container, out type))
            {
                // @TODO error
                return;
            }

            try
            {
                // Check if there is one already
                MethodDefinition staticConstructor = type.GetMethods().FirstOrDefault(c => c.Name == ".cctor");

                bool constructorCreated = staticConstructor == null;
                if (staticConstructor == null)
                {
                    const MethodAttributes attributes = MethodAttributes.Static |
                                                        MethodAttributes.HideBySig |
                                                        MethodAttributes.SpecialName |
                                                        MethodAttributes.RTSpecialName;

                    staticConstructor = new MethodDefinition(".cctor", attributes, type.Module.TypeSystem.Void);
                }

                var ilProcessor = staticConstructor.Body.GetILProcessor();

                staticConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));

                StaticConstructorStagePrePostFragments stageFragments;

                if (StaticConstructorInitializerFragments.TryGetValue(
                        ConstructorStage.PropertyInitializationStage,
                        out stageFragments))
                {
                    GenerateFragments(stageFragments.InStageFragments, ilProcessor);
                    GenerateFragments(stageFragments.PostStageFragments, ilProcessor);
                }

                if ( ! constructorCreated)
                {
                    return;
                }

                staticConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

                type.Methods.Add(staticConstructor);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                throw;
            }
        }

        public override void OnGenerateNestedContainer(PropertyTypeNode container, PropertyTypeNode nestedContainer)
        {
            TypeDefinition type = null;
            if (!_typeDefinitionPerPropertyNodeMap.TryGetValue(container, out type))
            {
                // @TODO error
                return;
            }

            var cecilBackend = new CecilGenerationBackend();

            cecilBackend.GenerateContainer(nestedContainer);

            type.NestedTypes.Add(cecilBackend.GeneratedContainerTypeDefinition);
        }

        public override void OnPropertyContainerGenerationCompleted(PropertyTypeNode container)
        {
            TypeDefinition type = null;
            if (!_typeDefinitionPerPropertyNodeMap.TryGetValue(container, out type))
            {
                // @TODO error
                return;
            }

            if (GeneratedContainerTypeDefinition?.Module == null)
            {
                return;
            }

            try
            {
                GeneratedContainerTypeDefinition.Module.Write();
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                throw;
            }
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
