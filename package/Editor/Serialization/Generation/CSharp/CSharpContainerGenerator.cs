#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Properties.Serialization;

namespace Unity.Properties.Editor.Serialization
{
    public class CSharpContainerGenerator : GenerationBackend
    {
        public StringBuffer Code { get; internal set; } = new StringBuffer();

        public bool DoGenerateNamespace { get; set; } = true;

        public void GeneratePropertyContainer(
            PropertyTypeNode container,
            Func<string, CSharpGenerationCache.CodeInfo> dependancyLookupFunc = null)
        {
            ResetInternalGenerationStates();

            StringBuffer gen = new StringBuffer();

            using (new NamespaceDecorator(container, DoGenerateNamespace ? gen : null))
            {
                GeneratePropertyContainerFor(
                    container,
                    dependancyLookupFunc,
                    gen);
            }

            Code = gen;
        }

        public List<string> PropertyBagItemNames { get; set; } = new List<string>();

        private static class Style
        {
            public const int Space = 4;
        }

        public class Scope : IDisposable
        {
            public StringBuffer Code { get; set; } = new StringBuffer();

            public int IndentLevel { get; internal set; } = 0;

            public Scope(Scope parentScope = null)
            {
                if (parentScope != null)
                {
                    IndentLevel = parentScope.IndentLevel + 1;
                    Code = parentScope.Code;
                }
            }

            public virtual void AddLine(string line)
            {
                Code.Append(' ', Style.Space * IndentLevel);
                Code.Append(line);
                Code.Append(Environment.NewLine);
            }

            public void Dispose()
            {
            }
        }

        private class CSharpGenerationFragmentContext : FragmentContext
        {
            public Scope Scope { private get; set; }

            public string Fragment { get; set; }

            public override void AddStringFragment()
            {
                if (!string.IsNullOrEmpty(Fragment))
                {
                    Scope.AddLine(Fragment);
                }
            }
        }

        private static string GetPropertyWrapperVariableName(PropertyTypeNode propertyType)
        {
            string prefix = propertyType.IsPublicProperty ? string.Empty : "s_";
            return $"{prefix}{propertyType.PropertyName}Property";
        }

        private static void GenerateClassPropertiesForPropertyAccessor(
            PropertyTypeNode.TypeTag containerTypeTag,
            PropertyTypeNode propertyType,
            Scope code
            )
        {
            var containerAsAParamTokens = new List<string> { };
            if (containerTypeTag.HasFlag(PropertyTypeNode.TypeTag.Struct))
            {
                containerAsAParamTokens.Add("ref");
            }
            containerAsAParamTokens.Add("this");

            var getSetValueCallContainerParam = string.Join(" ", containerAsAParamTokens);

            var propertyWrapperVariableName = GetPropertyWrapperVariableName(propertyType);

            var propertyTypeString = TypeDeclarationStringForProperty(propertyType);

            var isCompositeType = PropertyTypeNode.IsCompositeType(propertyType.Tag);

            var modifiers = string.Join(" ", ModifiersToStrings(AccessModifiers.Public));

            code.AddLine($"{modifiers} {propertyTypeString} {propertyType.PropertyName}");

            {
                code.AddLine("{");
                var accessorScope = new Scope(code);
                accessorScope.AddLine($"get {{ return {propertyWrapperVariableName}.GetValue({getSetValueCallContainerParam}); }}");
                if ( ! isCompositeType)
                {
                    accessorScope.AddLine($"set {{ {propertyWrapperVariableName}.SetValue({getSetValueCallContainerParam}, value); }}");
                }
                code.AddLine("}");
            }
        }

        private static string GeneratePropertyWrapperBackingVariable(
            string propertyWrapperTypeName,
            PropertyTypeNode.TypeTag containerTypeTag,
            PropertyTypeNode propertyType)
        {
            var accessModifiers = AccessModifiers.Static | AccessModifiers.Readonly;
            if (containerTypeTag == PropertyTypeNode.TypeTag.Class)
            {
                accessModifiers |= (propertyType.IsPublicProperty ? AccessModifiers.Public : AccessModifiers.Protected);
            }
            else
            {
                accessModifiers |= AccessModifiers.Private;
            }
            var modifiers = string.Join(" ", ModifiersToStrings(accessModifiers));

            var propertyWrapperVariableName = GetPropertyWrapperVariableName(propertyType);

            return $@"{modifiers} {propertyWrapperTypeName} {propertyWrapperVariableName};";
        }

        public void GenerateProperty(
            string containerName,
            PropertyTypeNode.TypeTag containerTypeTag,
            PropertyTypeNode propertyType,
            Scope code)
        {
            // Public C# Property & backing field

            GenerateClassPropertiesForPropertyAccessor(containerTypeTag, propertyType, code);

            var propertyWrapperTypeName = GetPropertyWrapperTypeFor(
                containerName,
                containerTypeTag,
                propertyType);

            code.AddLine(GeneratePropertyWrapperBackingVariable(
                propertyWrapperTypeName, containerTypeTag, propertyType));

            // Backing field if any

            var propertyAccessorName = string.Empty;
            var backingField = GeneratePropertyBackingField(
                propertyType.PropertyName,
                propertyType,
                out propertyAccessorName
            );

            if (!string.IsNullOrEmpty(backingField))
            {
                // If we have a class we delegate init to constructor otherwise we default construct here

                var initializer = GenerateInitializerFromProperty(containerTypeTag, propertyType);

                var initializerFragment = backingField;

                if (!string.IsNullOrEmpty(initializer) && !propertyType.DontInitializeBackingField)
                {
                    initializerFragment += $" = {initializer}";
                }

                initializerFragment += ";";

                code.AddLine(initializerFragment);
            }

            //      -> Add constructor initializer fragments for later stage for that property

            var propertyTypeString = TypeDeclarationStringForProperty(propertyType);
            var propertyWrapperVariableName = GetPropertyWrapperVariableName(propertyType);

            {
                var initializer = GeneratePropertyWrapperInitializers(
                    propertyType,
                    containerName,
                    containerTypeTag,
                    propertyWrapperTypeName,
                    propertyAccessorName,
                    propertyTypeString,
                    code);

                AddStaticConstructorInStageFragment(
                    ConstructorStage.PropertyInitializationStage,
                    new CSharpGenerationFragmentContext()
                    {
                        Scope = code,
                        Fragment = $"{propertyWrapperVariableName} = {initializer};"
                    });
            }

            PropertyBagItemNames.Add(propertyWrapperVariableName);
        }

        private void ResetInternalGenerationStates()
        {
            StaticConstructorInitializerFragments = new Dictionary<ConstructorStage, StaticConstructorStagePrePostFragments>();
            PropertyBagItemNames = new List<string>();
            ConstructorInitializerFragments = new List<FragmentContext>();
        }

        private static string TypeStringFromEnumerableProperty(PropertyTypeNode propertyType)
        {
            return $"{propertyType.TypeName}<{propertyType.Of.TypeName}>";
        }

        private static string TypeDeclarationStringForProperty(PropertyTypeNode propertyType)
        {
            if (PropertyTypeNode.IsEnumerableType(propertyType.Tag))
            {
                return TypeStringFromEnumerableProperty(propertyType);
            }
            return propertyType.TypeName;
        }

        private static string GenerateInitializerFromProperty(
            PropertyTypeNode.TypeTag containerTypeTag,
            PropertyTypeNode propertyType)
        {
            // No initializer for structs
            if (containerTypeTag == PropertyTypeNode.TypeTag.Struct)
                return string.Empty;

            var type = TypeDeclarationStringForProperty(propertyType);
            if (PropertyTypeNode.IsEnumerableType(propertyType.Tag) || propertyType.Tag == PropertyTypeNode.TypeTag.Struct)
            {
                // TODO value type etc.
                return $"new {type} {{}}";
            }

            if (propertyType.Tag == PropertyTypeNode.TypeTag.Class)
            {
                return $"new {type} ()";
            }

            return !string.IsNullOrEmpty(propertyType.DefaultValue)
                ? propertyType.DefaultValue
                : string.Empty;
        }

        [Flags]
        private enum AccessModifiers
        {
            None = 1,
            Public = 2,
            Private = 4,
            Readonly = 8,
            Static = 16,
            Partial = 32,
            Abstract = 64,
            Protected = 128,
        }

        private static List<string> ModifiersToStrings(AccessModifiers modifiers)
        {
            var s = new List<string>();
            // TODO fairly bad, no semantics and not forced to handle all modifiers
            if (modifiers.HasFlag(AccessModifiers.Public))
            {
                s.Add("public");
            }
            if (modifiers.HasFlag(AccessModifiers.Private))
            {
                s.Add("private");
            }
            if (modifiers.HasFlag(AccessModifiers.Protected))
            {
                s.Add("protected");
            }
            if (modifiers.HasFlag(AccessModifiers.Static))
            {
                s.Add("static");
            }
            if (modifiers.HasFlag(AccessModifiers.Abstract))
            {
                s.Add("abstract");
            }
            if (modifiers.HasFlag(AccessModifiers.Partial))
            {
                s.Add("partial");
            }
            if (modifiers.HasFlag(AccessModifiers.Readonly))
            {
                s.Add("readonly");
            }
            return s;
        }

        private static string GenerateBackingField(string propType,
            string fieldName,
            AccessModifiers modifiers
            )
        {
            var accessModifiers = string.Join(" ", ModifiersToStrings(modifiers));
            return $"{accessModifiers} {propType} m_{fieldName}";
        }

        private static string GetPropertyWrapperTypeFor(
            string containerName,
            PropertyTypeNode.TypeTag containerTypeTag,
            PropertyTypeNode propertyType)
        {
            // @TODO warning we rely on type definitions here

            var propertyWrapperTypeStringPrefix = "";
            if (containerTypeTag.HasFlag(PropertyTypeNode.TypeTag.Struct))
            {
                propertyWrapperTypeStringPrefix = "Struct";
            }

            switch (propertyType.Tag)
            {
                case PropertyTypeNode.TypeTag.Struct:
                    return $"{propertyWrapperTypeStringPrefix}MutableContainerProperty<{containerName}, {propertyType.TypeName}>";

                case PropertyTypeNode.TypeTag.Class:
                    return $"{propertyWrapperTypeStringPrefix}ContainerProperty<{containerName}, {propertyType.TypeName}>";

                case PropertyTypeNode.TypeTag.Enum:
                    return $"{propertyWrapperTypeStringPrefix}EnumProperty<{containerName}, {propertyType.TypeName}>";

                case PropertyTypeNode.TypeTag.List:
                    {
                        var t = TypeStringFromEnumerableProperty(propertyType);

                        switch (propertyType.Of.Tag)
                        {
                            case PropertyTypeNode.TypeTag.Primitive:
                                return $"{propertyWrapperTypeStringPrefix}ListProperty<{containerName}, {t}, {propertyType.Of.TypeName}>";
                            case PropertyTypeNode.TypeTag.Struct:
                                return $"{propertyWrapperTypeStringPrefix}MutableContainerListProperty<{containerName}, {t}, {propertyType.Of.TypeName}>";
                            case PropertyTypeNode.TypeTag.Class:
                                return $"{propertyWrapperTypeStringPrefix}ContainerListProperty<{containerName}, {t}, {propertyType.Of.TypeName}>";
                            case PropertyTypeNode.TypeTag.Enum:
                                return $"{propertyWrapperTypeStringPrefix}EnumListProperty<{containerName}, {t}, {propertyType.Of.TypeName}>";
                            case PropertyTypeNode.TypeTag.Unknown:
                            case PropertyTypeNode.TypeTag.List:
                            default:
                                throw new Exception($"Invalid property tag of list property name {propertyType.PropertyName}");
                        }
                    }

                case PropertyTypeNode.TypeTag.Primitive:
                    return $"{propertyWrapperTypeStringPrefix}Property<{containerName}, {propertyType.TypeName}>";

                default:
                    break;
            }

            return $"{propertyWrapperTypeStringPrefix}Property<{containerName}, {propertyType.TypeName}>";
        }

        private static string GeneratePropertyBackingField(
            string propertyName,
            PropertyTypeNode propertyType,
            out string propertyAccessorName)
        {
            if (propertyType.IsCustomProperty)
            {
                propertyAccessorName = string.Empty;
                return string.Empty;
            }

            propertyAccessorName = $"m_{propertyName}";

            if (string.IsNullOrEmpty(propertyType.PropertyBackingAccessor))
            {
                var modifiers = AccessModifiers.Private;

                // @TODO
                //    | (PropertyTypeNode.IsCompositeType(propertyType.Tag) ? AccessModifiers.Readonly : AccessModifiers.None);

                return GenerateBackingField(
                    TypeDeclarationStringForProperty(propertyType),
                    propertyName,
                    modifiers
                    );
            }

            // TODO check & get by id instead of by name
            var propertyAccessorDelegate = propertyType.PropertyBackingAccessor;
            propertyAccessorName = $"{propertyAccessorDelegate}";

            return string.Empty;
        }

        private class PropertyWrapperAccessors
        {
            public string ValueGetter { get; set; } = string.Empty;
            public string ValueSetter { get; set; } = string.Empty;
            public string RefGetter { get; set; } = string.Empty;
        }

        private static string CleanupNameForMethod(string name)
        {
            return new string(name.Select(c => char.IsLetter(c) ? c : '_').ToArray());
        }

        private static PropertyWrapperAccessors GetPropertyWrapperAccessors(
            PropertyTypeNode propertyType,
            string containerName,
            PropertyTypeNode.TypeTag containerTypeTag,
            string propertyWrapperTypeString,
            string propertyTypeString,
            string propertyAccessorName)
        {
            if (propertyType.IsCustomProperty)
            {
                return new PropertyWrapperAccessors()
                {
                    ValueGetter = $"GetValue_{CleanupNameForMethod(propertyType.PropertyName)}",
                    ValueSetter = $"SetValue_{CleanupNameForMethod(propertyType.PropertyName)}",
                    RefGetter = propertyType.Tag == PropertyTypeNode.TypeTag.Struct
                        ? $"GetRef_{CleanupNameForMethod(propertyType.PropertyName)}"
                        : string.Empty
                };
            }

            var containerAsAParemeterType =
                containerTypeTag.HasFlag(PropertyTypeNode.TypeTag.Struct) ? $"ref {containerName}" : $"{containerName}";

            var propertySetter = "/* SET */ null";
            if (!PropertyTypeNode.IsCompositeType(propertyType.Tag) && !propertyType.IsReadonly)
            {
                propertySetter = $"/* SET */ ({containerAsAParemeterType} container, {propertyTypeString} value) => container.{propertyAccessorName} = value";
            }

            var propertyRefGetter = string.Empty;
            if (propertyType.Tag == PropertyTypeNode.TypeTag.Struct)
            {
                propertyRefGetter = $"/* REF */ ({containerAsAParemeterType} container, {propertyWrapperTypeString}.RefVisitMethod a, IPropertyVisitor v) => a(ref container.m_{propertyType.PropertyName}, v)";
            }

            return new PropertyWrapperAccessors()
            {
                ValueGetter = $"/* GET */ ({containerAsAParemeterType} container) => container.{propertyAccessorName}",
                ValueSetter = propertySetter,
                RefGetter = propertyRefGetter
            };
        }

        private static string GeneratePropertyWrapperInitializers(
            PropertyTypeNode propertyType,
            string containerName,
            PropertyTypeNode.TypeTag containerTypeTag,
            string propertyWrapperTypeString,
            string propertyAccessorName,
            string propertyTypeString,
            Scope code
            )
        {
            var initializerParams = new List<string>();

            var accessors = GetPropertyWrapperAccessors(
                propertyType,
                containerName,
                containerTypeTag,
                propertyWrapperTypeString,
                propertyTypeString,
                propertyAccessorName);

            // @TODO shouldn't be done here, and be located in a cleaner place

#if ENABLE_CUSTOM_PROPERTY_PARTIALS
            if (propertyType.IsCustomProperty)
            {
                var containerAsAParemeterType =
                    containerTypeTag.HasFlag(PropertyTypeNode.TypeTag.Struct) ? $"ref {containerName}" : $"{containerName}";
                
                code.AddLine("");
                
                if (!string.IsNullOrEmpty(accessors.RefGetter))
                {
                    code.AddLine(
                        $"partial void {accessors.RefGetter}({containerAsAParemeterType} value, IPropertyVisitor visitor);");
                }
                
                code.AddLine($"partial void {accessors.ValueSetter}({containerAsAParemeterType} container, {propertyTypeString} value);");
                
                code.AddLine(
                    $"partial {propertyTypeString} {accessors.ValueGetter}({containerAsAParemeterType} container);");
                
                code.AddLine("");
            }
#endif

            initializerParams.Add($"nameof({propertyType.PropertyName})");
            initializerParams.Add(accessors.ValueGetter);
            initializerParams.Add(accessors.ValueSetter);

            if (!string.IsNullOrEmpty(accessors.RefGetter))
            {
                initializerParams.Add(accessors.RefGetter);
            }
            
            return $@"new { propertyWrapperTypeString }( {string.Join(", ", initializerParams)} )";
        }
        
        private string OnPropertyBagConstructedMethodName { get; } = "OnPropertyBagConstructed";

        private static void GenerateUserHook(StringBuffer gen, string name, List<string> parameters = null)
        {
            if (gen == null || string.IsNullOrEmpty(name))
            {
                return;
            }

            if (parameters == null)
            {
                parameters = new List<string>();
            }

            gen.Append(Environment.NewLine);
            gen.Append(' ', Style.Space * 1);
            var modifiers = AccessModifiers.Static | AccessModifiers.Partial;
            gen.Append($"{string.Join(" ", ModifiersToStrings(modifiers))} void {name}({string.Join(",", parameters)});");
            gen.Append(Environment.NewLine);
            gen.Append(Environment.NewLine);
        }

        private void GenerateUserHooksFor(PropertyTypeNode c, Scope scope)
        {
            if (c.UserHooks.HasFlag(UserHookFlags.OnPropertyBagConstructed))
            {
                GenerateUserHook(
                    scope.Code,
                    OnPropertyBagConstructedMethodName,
                    new List<string>() { "IPropertyBag bag" });

                // Add a post property construction stage for that hook call

                AddStaticConstructorPostStageFragment(
                    ConstructorStage.PropertyInitializationStage,
                    new CSharpGenerationFragmentContext()
                    {
                        Scope = scope,
                        Fragment = $"{OnPropertyBagConstructedMethodName}({PropertyBagStaticVarName});"
                    });
            }
        }

        private void GenerateConstructorFor(
            PropertyTypeNode c,
            StringBuffer gen)
        {
            var constructor = c.Constructor;
            if (constructor == null)
            {
                return;
            }

            if (c.Tag == PropertyTypeNode.TypeTag.Struct && constructor.ParameterTypes.Count == 0)
            {
                // baild out for struct + no param constructor
                return;
            }

            if (c.Tag == PropertyTypeNode.TypeTag.Class && c.Properties.Count == 0)
            {
                // baild out for class + no properties
                return;
            }

            var containerTypeName = c.TypeName;

            int i = 0;
            var i1 = i;
            gen.Append(' ', Style.Space * 1);
            gen.Append(
                $"public {containerTypeName} ({string.Join(",", constructor.ParameterTypes.Select(p => $"{p.Key} p{i1}"))}){Environment.NewLine}"
            );

            gen.Append(' ', Style.Space * 1);
            gen.Append("{");

            // Generate constructor param -> field init

            i = 0;
            foreach (var paramType in constructor.ParameterTypes)
            {
                gen.Append(' ', Style.Space * 2);
                gen.Append($"{paramType.Value} = p{i};\n");
            }

            // Generate property initializers

            if (ConstructorInitializerFragments.Count != 0)
            {
                gen.Append(Environment.NewLine);
                gen.Append(' ', Style.Space * 2);
                gen.Append("// Property backing field initializers");
                gen.Append(Environment.NewLine);

                foreach (var fragment in ConstructorInitializerFragments)
                {
                    var codeFragment = fragment as CSharpGenerationFragmentContext;
                    if (codeFragment != null)
                    {
                        gen.Append(' ', Style.Space * 2);
                        gen.Append(codeFragment.Fragment);
                        gen.Append(Environment.NewLine);
                    }
                }
            }

            gen.Append(' ', Style.Space * 1);
            gen.Append("}"); gen.Append(Environment.NewLine);
        }

        private string PropertyBagStaticVarName { get; set; } = "sProperties";

        private static void GenerateFragments(List<FragmentContext> fragments, StringBuffer gen)
        {
            foreach (var fragment in fragments)
            {
                var codeFragment = fragment as CSharpGenerationFragmentContext;
                if (codeFragment != null)
                {
                    gen.Append(' ', Style.Space * 2);
                    gen.Append(codeFragment.Fragment);
                    gen.Append(Environment.NewLine);
                }
            }
        }

        private void GenerateStaticConstructorFor(
            PropertyTypeNode c,
            StringBuffer gen)
        {
            var containerTypeName = c.TypeName;

            gen.Append(' ', Style.Space * 1);
            gen.Append($"static {containerTypeName} (){Environment.NewLine}");
            gen.Append(' ', Style.Space * 1);
            gen.Append("{");
            gen.Append(Environment.NewLine);

            // Handle property initializers generation stage

            StaticConstructorStagePrePostFragments stageFragments;
            if (StaticConstructorInitializerFragments.TryGetValue(ConstructorStage.PropertyInitializationStage, out stageFragments))
            {
                var fragments = stageFragments.InStageFragments;

                if (fragments.Count != 0)
                {
                    gen.Append(' ', Style.Space * 2);
                    gen.Append("// Property wrappers initializers");
                    gen.Append(Environment.NewLine);

                    GenerateFragments(fragments, gen);
                }

                // Handle property initializers post generation stage (user hooks etc.)

                fragments = stageFragments.PostStageFragments;

                if (fragments.Count != 0)
                {
                    gen.Append(Environment.NewLine);
                    gen.Append(' ', Style.Space * 2);
                    gen.Append("// User Hooks");
                    gen.Append(Environment.NewLine);

                    GenerateFragments(fragments, gen);
                }
            }

            gen.Append(Environment.NewLine);
            gen.Append(' ', Style.Space * 2);
            gen.Append("// Freeze property bag items");

            foreach (var item in PropertyBagItemNames)
            {
                gen.Append(Environment.NewLine);
                gen.Append(' ', Style.Space * 2);
                gen.Append($"// {item}.Freeze();");
            }
            gen.Append(Environment.NewLine);

            gen.Append(' ', Style.Space * 1);
            gen.Append("}"); gen.Append(Environment.NewLine);
        }

        private class PropertyContainerDataTypeDecorator : IDisposable
        {
            public List<string> BaseClasses { get; set; } = new List<string>();

            private StringBuffer _sb;
            private PropertyTypeNode _containerNode;

            public PropertyContainerDataTypeDecorator(
                PropertyTypeNode containerNode,
                StringBuffer sb,
                List<string> baseClasses = null)
            {
                _sb = sb;
                _containerNode = containerNode;

                if (baseClasses != null)
                {
                    foreach (var bc in baseClasses)
                    {
                        WithBaseClass(bc);
                    }
                }

                GenerateHeader();
            }

            private PropertyContainerDataTypeDecorator WithBaseClass(string baseClassName)
            {
                if (!string.IsNullOrEmpty(baseClassName))
                {
                    BaseClasses.Add(baseClassName);
                }
                return this;
            }

            private void GenerateHeader()
            {
                bool isStructType = _containerNode.Tag == PropertyTypeNode.TypeTag.Struct;

                var dataType = (isStructType ? "struct" : "class");

                var inheritanceDeclaration = string.Empty;
                if (BaseClasses.Count != 0)
                {
                    inheritanceDeclaration = ": " + string.Join(",", BaseClasses);
                }

                var modifiers = AccessModifiers.Public | AccessModifiers.Partial;
                if (_containerNode.IsAbstractClass && !isStructType)
                {
                    modifiers |= AccessModifiers.Abstract;
                }

                _sb.Append(
                    $"{string.Join(" ", ModifiersToStrings(modifiers))} {dataType} {_containerNode.TypeName} {inheritanceDeclaration} {Environment.NewLine}{{{Environment.NewLine}");
            }

            private void GenerateFooter()
            {
                _sb.Append("}");
                _sb.Append(Environment.NewLine);
            }

            public void Dispose()
            {
                GenerateFooter();
            }
        }

        private void GeneratePropertyBag(PropertyTypeNode c, List<string> propertyNames, Scope scope)
        {
            var gen = scope.Code;

            gen.Append(' ', Style.Space * 1);
            gen.Append($"public IPropertyBag PropertyBag => {PropertyBagStaticVarName};");
            gen.Append(Environment.NewLine); gen.Append(Environment.NewLine);

            gen.Append(' ', Style.Space * 1);

            const AccessModifiers modifiers = AccessModifiers.Private | AccessModifiers.Static | AccessModifiers.Readonly;
            gen.Append($"{string.Join(" ", ModifiersToStrings(modifiers))} PropertyBag {PropertyBagStaticVarName};");

            // TODO should be ordered (after property wrappers creation)

            var propertyBagInitializers = propertyNames != null
                ? string.Join(", ", propertyNames)
                : string.Empty;

            var initializer = $@"new PropertyBag(new List<IProperty>{{
                        {propertyBagInitializers}
                    }}.ToArray())";

            AddStaticConstructorInStageFragment(
                ConstructorStage.PropertyInitializationStage,
                new CSharpGenerationFragmentContext()
                {
                    Scope = scope,
                    Fragment = $"{PropertyBagStaticVarName} = {initializer};"
                })
                ;

            gen.Append(Environment.NewLine); gen.Append(Environment.NewLine);
        }

        private void GeneratePropertyContainerFor(
            PropertyTypeNode c,
            Func<string, CSharpGenerationCache.CodeInfo> dependancyLookupFunc,
            StringBuffer gen)
        {
            var containerName = c.TypeName;

            var containerTypeTag = c.Tag;

            var baseClass = string.IsNullOrEmpty(c.OverrideDefaultBaseClass)
                ? "IPropertyContainer" : c.OverrideDefaultBaseClass;

            var rootScope = new Scope();

            var shouldGeneratePRopertyContainerImplementation = ! c.NoDefaultImplementation;

            using (var d = new PropertyContainerDataTypeDecorator(c, rootScope.Code, new List<string> { baseClass }))
            using (var scope = new Scope(rootScope))
            {
                if (shouldGeneratePRopertyContainerImplementation)
                {
                    if (c.Properties.Count != 0)
                    {
                        foreach (var propertyType in c.Properties)
                        {
                            GenerateProperty(
                                containerName,
                                containerTypeTag,
                                propertyType,
                                scope
                            );
                            scope.AddLine("");
                        }

                        scope.AddLine("");
                    }

                    GenerateUserHooksFor(c, scope);

                    // Add inherited properties if it applies

                    var propertyBagElementNames = PropertyBagItemNames;
                    if (!string.IsNullOrEmpty(c.OverrideDefaultBaseClass) && dependancyLookupFunc != null)
                    {
                        var cachedContainer = dependancyLookupFunc(c.OverrideDefaultBaseClass);
                        if (cachedContainer != null)
                        {
                            propertyBagElementNames = PropertyBagItemNames.Select(n => n).ToList();

                            propertyBagElementNames.AddRange(cachedContainer.GeneratedPropertyFieldNames);
                        }
                    }

                    GeneratePropertyBag(
                        c,
                        propertyBagElementNames,
                        scope);

                    GenerateConstructorFor(c, scope.Code);

                    GenerateStaticConstructorFor(c, scope.Code);

                    // @TODO Cleanup
                    // Recurse to collect nested container definitions

                    foreach (var nestedContainer in c.NestedContainers)
                    {
                        if (nestedContainer == null)
                            continue;

                        var g = new CSharpContainerGenerator()
                        {
                            DoGenerateNamespace = false
                        };
                        g.GeneratePropertyContainer(nestedContainer, dependancyLookupFunc);

                        if (!string.IsNullOrEmpty(g.Code.ToString()))
                        {
                            scope.AddLine(string.Empty);
                            scope.AddLine(string.Empty);

                            scope.AddLine(g.Code.ToString());

                            scope.AddLine(string.Empty);
                            scope.AddLine(string.Empty);
                        }
                    }

                    scope.AddLine("public IVersionStorage VersionStorage => DefaultVersionStorage.Instance;");
                    scope.AddLine(Environment.NewLine);
                }
            }

            gen.Append(rootScope.Code);
        }

        private class CSharpSourceFileDecorator : IDisposable
        {
            private readonly List<string> _usings = new List<string>()
            {
                "System",
                "System.Collections.Generic",
                "Unity.Properties"
            };

            private StringBuffer _sb;

            public CSharpSourceFileDecorator(StringBuffer sb)
            {
                _sb = sb;

                GenerateHeader();
            }

            private void GenerateHeader()
            {
                _usings.ForEach((currentUsing) =>
                {
                    _sb.Append($"using {currentUsing};");
                    _sb.Append(Environment.NewLine);
                });
                _sb.Append(Environment.NewLine);
            }

            public void Dispose()
            {
            }
        }

        private class NamespaceDecorator : IDisposable
        {
            private StringBuffer _sb;
            private PropertyTypeNode _container;

            public NamespaceDecorator(PropertyTypeNode container, StringBuffer sb)
            {
                _sb = sb;
                _container = container;

                GenerateHeader();
            }

            private void GenerateHeader()
            {
                if (_sb != null && !string.IsNullOrEmpty(_container.TypePath.Namespace))
                {
                    _sb.Append($"namespace {_container.TypePath.Namespace} {Environment.NewLine} {{");
                    _sb.Append(Environment.NewLine);
                    _sb.Append(Environment.NewLine);
                }
            }

            private void GenerateFooter()
            {
                if (_container == null || _sb == null)
                    return;

                if (!string.IsNullOrEmpty(_container.TypePath.Namespace))
                {
                    _sb.Append("}");
                    _sb.Append(Environment.NewLine);
                    _sb.Append(Environment.NewLine);
                }
            }

            public void Dispose()
            {
                GenerateFooter();
            }
        }

        public override void OnPropertyContainerGenerationStarted(PropertyTypeNode c)
        {
            ResetInternalGenerationStates();

        }

        public override void OnPropertyGenerationStarted(PropertyTypeNode container, PropertyTypeNode property)
        {
            throw new NotImplementedException();
        }

        public override void OnGenerateUserHooksForContainer(PropertyTypeNode container)
        {
            throw new NotImplementedException();
        }

        public override void OnGeneratePropertyBagForContainer(PropertyTypeNode container)
        {
            throw new NotImplementedException();
        }

        public override void OnGenerateConstructorForContainer(PropertyTypeNode container)
        {
            throw new NotImplementedException();
        }

        public override void OnGenerateStaticConstructorForContainer(PropertyTypeNode container)
        {
            throw new NotImplementedException();
        }

        public override void OnGenerateNestedContainer(PropertyTypeNode container, PropertyTypeNode nestedContainer)
        {
            throw new NotImplementedException();
        }

        public override void OnPropertyContainerGenerationCompleted(PropertyTypeNode c)
        {
            throw new NotImplementedException();
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
