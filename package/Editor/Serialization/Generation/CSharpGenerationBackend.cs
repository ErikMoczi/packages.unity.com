#if NET_4_6
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Properties.Serialization;

namespace Unity.Properties.Editor.Serialization
{
    public class CSharpGenerationBackend : IPropertyContainerGenerationBackend
    {
        public StringBuffer Code { get; internal set;  } = new StringBuffer();

        public void Generate(List<PropertyTypeNode> root)
        {
            var gen = new StringBuffer();

            // expect only one for now
            foreach (var container in root)
            {
                ResetInternalGenerationStates();
                var c = GeneratePropertyContainer(container);
                gen.Append(c.ToString());
            }

            Code.Append(gen.ToString());
        }

        private void ResetInternalGenerationStates()
        {
            StaticConstructorInitializerFragments = new Dictionary<ConstructorStage, StaticConstructorStagePrePostFragments>();
            PropertyBagItemNames = new List<string>();
            ConstructorInitializerFragments = new List<Func<string>>();
        }

        private static class Style
        {
            public const int Space = 4;
        }

        private static string TypeStringFromEnumerableProperty(PropertyTypeNode propertyType)
        {
            return $"{propertyType.TypeName}<{propertyType.Of.TypeName}>";
        }

        private static string TypeDeclarationStringForProperty(PropertyTypeNode propertyType)
        {
            if (PropertyTypeNode.IsEnumerableType(propertyType.Tag))
            {
                if (propertyType.Tag == PropertyTypeNode.TypeTag.Array)
                {
                    return $"{propertyType.Of.TypeName}[]";
                }
                // TODO value type etc.
                return TypeStringFromEnumerableProperty(propertyType);
            }

            return propertyType.TypeName;
        }

        private static string GenerateInitializerFromProperty(PropertyTypeNode propertyType)
        {
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
        }

        private static List<string> ModifiersToStrings(AccessModifiers modifiers)
        {
            List<string> s = new List<string>();
            // TODO fairly bad, no semantics and not forced to handle all modifiers
            if (modifiers.HasFlag(AccessModifiers.Public))
            {
                s.Add("public");
            }
            if (modifiers.HasFlag(AccessModifiers.Private))
            {
                s.Add("private");
            }
            if (modifiers.HasFlag(AccessModifiers.Partial))
            {
                s.Add("partial");
            }
            if (modifiers.HasFlag(AccessModifiers.Static))
            {
                s.Add("static");
            }
            if (modifiers.HasFlag(AccessModifiers.Readonly))
            {
                s.Add("readonly");
            }
            if (modifiers.HasFlag(AccessModifiers.Abstract))
            {
                s.Add("abstract");
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
            // TODO warning we rely on type definitions here
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

                case PropertyTypeNode.TypeTag.Array:
                case PropertyTypeNode.TypeTag.List:
                    {
                        var t = TypeStringFromEnumerableProperty(propertyType);
                        return $"{propertyWrapperTypeStringPrefix}ListProperty<{containerName}, {t}, {propertyType.Of.TypeName}>";
                    }
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
            propertyAccessorName = $"m_{propertyName}";

            if (string.IsNullOrEmpty(propertyType.PropertyBackingAccessor))
            {
                var modifiers = AccessModifiers.Private
                     | (PropertyTypeNode.IsCompositeType(propertyType.Tag) ? AccessModifiers.Readonly : AccessModifiers.None);
                return GenerateBackingField(
                    TypeDeclarationStringForProperty(propertyType),
                    propertyName,
                    modifiers
                    );
            }

            // TODO check & get by id instead of by name
            var propertyAccessorDelegate = propertyType.PropertyBackingAccessor;
            propertyAccessorName = $"{propertyAccessorDelegate}.{propertyName}";

            return string.Empty;
        }

        private static string GeneratePropertyWrapperInitializer(
            string propertyName,
            string containerName,
            PropertyTypeNode.TypeTag containerTypeTag,
            string propertyWrapperTypeString,
            bool isReadonlyProperty,
            string propertyAccessorName,
            string propertyTypeString,
            PropertyTypeNode.TypeTag propertyTypeTag
            )
        {
            List<string> initializerParams = new List<string>();

            var containerAsAParemeterType =
                containerTypeTag.HasFlag(PropertyTypeNode.TypeTag.Struct) ? $"ref {containerName}" : $"{containerName}";

            initializerParams.Add($"nameof({propertyName})");

            initializerParams.Add($"/* GET */ ({containerAsAParemeterType} container) => container.{propertyAccessorName}");

            var propertySetter = "/* SET */ null";
            if (!PropertyTypeNode.IsCompositeType(propertyTypeTag) && ! isReadonlyProperty)
            {
                propertySetter = $"/* SET */ ({containerAsAParemeterType} container, {propertyTypeString} value) => container.{propertyAccessorName} = value";
            }
            initializerParams.Add(propertySetter);

            if (propertyTypeTag == PropertyTypeNode.TypeTag.Struct)
            {
                initializerParams.Add(
                    $"/* REF */ ({containerAsAParemeterType} container, {propertyWrapperTypeString}.RefVisitMethod a, IPropertyVisitor v) => a(ref container.m_{propertyName}, v)"
                    );
            }

            string parameters = string.Join(", ", initializerParams);

            return $@"new { propertyWrapperTypeString }( {parameters} )";
        }

        private List<Func<string>> ConstructorInitializerFragments { get; set; }
        
        private enum ConstructorStage
        {
            PropertyInitializationStage,
            PropertyFreezeStage,
        };

        private class StaticConstructorStagePrePostFragments
        {
            public StaticConstructorStagePrePostFragments(ConstructorStage stage)
            {
                Stage = stage;
            }

            public ConstructorStage Stage { get; internal set; }

            private static string NopFragment() { return string.Empty; }

            public List<Func<string>> InStageFragments { get; set; } = new List<Func<string>> ();

            public List<Func<string>> PostStageFragments { get; set; } = new List<Func<string>> ();
        };

        private Dictionary<ConstructorStage, StaticConstructorStagePrePostFragments>
            StaticConstructorInitializerFragments { get; set; }

        private void AddStaticConstructorInStageFragment(ConstructorStage s, Func<string> f)
        {
            if ( ! StaticConstructorInitializerFragments.ContainsKey(s))
            {
                StaticConstructorInitializerFragments[s] = new StaticConstructorStagePrePostFragments(s);
            }

            StaticConstructorInitializerFragments[s].InStageFragments.Add(f);
        }

        private void AddStaticConstructorPostStageFragment(ConstructorStage s, Func<string> f)
        {
            if (!StaticConstructorInitializerFragments.ContainsKey(s))
            {
                StaticConstructorInitializerFragments[s] = new StaticConstructorStagePrePostFragments(s);
            }

            StaticConstructorInitializerFragments[s].PostStageFragments.Add(f);
        }

        private List<string> PropertyBagItemNames { get; set; }
        
        private void GenerateProperty(
            string containerName,
            PropertyTypeNode.TypeTag containerTypeTag,
            PropertyTypeNode propertyType,
            StringBuffer gen)
        {
            var propertyWrapperVariableName = $"s_{propertyType.Name}Property";
            var propertyTypeString = TypeDeclarationStringForProperty(propertyType);

            if (!string.IsNullOrWhiteSpace(propertyType.IsInheritedFrom))
            {
                // Just add a propertybag member
                PropertyBagItemNames.Add(propertyWrapperVariableName);
                return;
            }

            var propertyWrapperTypeName = GetPropertyWrapperTypeFor(
                containerName,
                containerTypeTag,
                propertyType);

            bool isCompositeType = PropertyTypeNode.IsCompositeType(propertyType.Tag);

            // Backing field if any

            var propertyAccessorName = string.Empty;
            var backingField = GeneratePropertyBackingField(
                propertyType.Name,
                propertyType,
                out propertyAccessorName
                );

            if (!string.IsNullOrEmpty(backingField))
            {
                // If we have a class we delegate init to constructor otherwise we default construct here

                var initializer = GenerateInitializerFromProperty(propertyType);

                gen.Append(' ', Style.Space * 1);
                gen.Append(backingField);

                if (!string.IsNullOrEmpty(initializer))
                {
                    if (containerTypeTag == PropertyTypeNode.TypeTag.Struct)
                    {
                        gen.Append($"= {initializer}");
                    }
                    else
                    {
                        // Add an initializer for that
                        ConstructorInitializerFragments.Add(() => $"{propertyAccessorName} = {initializer};");
                    }
                }

                gen.Append($";{Environment.NewLine}");
            }

            // Public C# Property

            // @TODO extract as a method

            var containerAsAParamTokens = new List<String> { };
            if (containerTypeTag.HasFlag(PropertyTypeNode.TypeTag.Struct))
            {
                containerAsAParamTokens.Add("ref");
            }
            containerAsAParamTokens.Add("this");

            var getSetValueCallContainerParam = string.Join(" ", containerAsAParamTokens);

            gen.Append(' ', Style.Space * 1);
            var modifiers = string.Join(" ", ModifiersToStrings(AccessModifiers.Public));
            gen.Append($@"{modifiers} {propertyTypeString} {propertyType.Name}
        {{
            get {{ return {propertyWrapperVariableName}.GetValue({getSetValueCallContainerParam}); }}
            set {{ {
                    (isCompositeType ? string.Empty : $"{propertyWrapperVariableName}.SetValue({getSetValueCallContainerParam}, value);")
                } }}
        }}");

            // Public C# Property

            gen.Append(Environment.NewLine);
            gen.Append(' ', Style.Space * 1);

            modifiers = string.Join(" ", ModifiersToStrings(AccessModifiers.Private | AccessModifiers.Static | AccessModifiers.Readonly));
            gen.Append($@"{modifiers} {propertyWrapperTypeName} {propertyWrapperVariableName};");

            gen.Append(Environment.NewLine);

            //      -> Add constructor initializer fragments

            AddStaticConstructorInStageFragment(
                ConstructorStage.PropertyInitializationStage,
                () =>
                {
                    var initializer = GeneratePropertyWrapperInitializer(
                        propertyType.Name,
                        containerName,
                        containerTypeTag,
                        propertyWrapperTypeName,
                        propertyType.IsReadonly,
                        propertyAccessorName,
                        propertyTypeString,
                        propertyType.Tag);

                    return $"{propertyWrapperVariableName} = {initializer};";
                });

            PropertyBagItemNames.Add(propertyWrapperVariableName);
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

        private void GenerateUserHooksFor(PropertyTypeNode c, StringBuffer gen)
        {
            if (c.UserHooks.HasFlag(UserHookFlags.OnPropertyConstructed))
            {
                GenerateUserHook(
                    gen,
                    OnPropertyBagConstructedMethodName,
                    new List<string>() { "IPropertyBag bag" });

                // Add a post property construction stage for that hook call

                AddStaticConstructorPostStageFragment(
                    ConstructorStage.PropertyInitializationStage,
                    () =>
                    {
                        return $"{OnPropertyBagConstructedMethodName}({PropertyBagStaticVarName});";
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

            if (c.Tag == PropertyTypeNode.TypeTag.Class && c.Children.Count == 0)
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
                    gen.Append(' ', Style.Space * 2);
                    gen.Append(fragment());
                    gen.Append(Environment.NewLine);
                }
            }

            gen.Append(' ', Style.Space * 1);
            gen.Append("}"); gen.Append(Environment.NewLine);
        }

        private string PropertyBagStaticVarName { get; set; } = "sProperties";

        private static void GenerateFragments(List<Func<string>> fragments, StringBuffer gen)
        {
            foreach (var fragment in fragments)
            {
                gen.Append(' ', Style.Space * 2);
                gen.Append(fragment());
                gen.Append(Environment.NewLine);
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
            if (StaticConstructorInitializerFragments.TryGetValue(
                ConstructorStage.PropertyInitializationStage,
                out stageFragments))
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
            public List<string> BaseClasses { get; set; } = new List<string> ();

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
                    foreach(var bc in baseClasses)
                    {
                        WithBaseClass(bc);
                    }
                }

                GenerateHeader();
            }

            public PropertyContainerDataTypeDecorator WithBaseClass(string baseClassName)
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

        private void GeneratePropertyBag(PropertyTypeNode c, List<string> propertyNames, StringBuffer gen)
        {
            gen.Append(' ', Style.Space * 1);
            gen.Append($"public IPropertyBag PropertyBag => {PropertyBagStaticVarName};");
            gen.Append(Environment.NewLine); gen.Append(Environment.NewLine);

            var propertyBagInitializers = propertyNames != null
                ? string.Join(", ", propertyNames)
                : string.Empty;
            ;
            gen.Append(' ', Style.Space * 1);

            var modifiers = AccessModifiers.Private | AccessModifiers.Static | AccessModifiers.Readonly;
            gen.Append($"{string.Join(" ", ModifiersToStrings(modifiers))} PropertyBag {PropertyBagStaticVarName};");

            // TODO should be ordered (after property wrappers creation)

            AddStaticConstructorInStageFragment(
                ConstructorStage.PropertyInitializationStage,
                () =>
                {
                    var initializer = $@"new PropertyBag(new List<IProperty>{{
                        {propertyBagInitializers}
                    }}.ToArray())";
                    return $"{PropertyBagStaticVarName} = {initializer};";
                });

            gen.Append(Environment.NewLine); gen.Append(Environment.NewLine);
        }

        private void GeneratePropertyContainerFor(PropertyTypeNode c, StringBuffer gen)
        {
            var containerName = c.TypeName;
            var containerTypeTag = c.Tag;

            var baseClass = string.IsNullOrEmpty(c.OverrideDefaultBaseClass)
                ? "IPropertyContainer" : c.OverrideDefaultBaseClass;

            using (var d = new PropertyContainerDataTypeDecorator(c, gen, new List<string> { baseClass }))
            {
                if (c.Children.Count != 0)
                {
                    foreach (var propertyType in c.Children)
                    {
                        GenerateProperty(
                            containerName,
                            containerTypeTag,
                            propertyType,
                            gen
                            );
                    }
                }

                GenerateUserHooksFor(c, gen);

                GeneratePropertyBag(c, PropertyBagItemNames, gen);

                GenerateConstructorFor(c, gen);

                GenerateStaticConstructorFor(c, gen);

                gen.Append(' ', Style.Space * 1);
                gen.Append("public IVersionStorage VersionStorage => DefaultVersionStorage.Instance;"); gen.Append(Environment.NewLine);
            }
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
            private PropertyTypeNode _container;

            public CSharpSourceFileDecorator(PropertyTypeNode container, StringBuffer sb)
            {
                _sb = sb;
                _container = container;

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

                if (!string.IsNullOrEmpty(_container.Namespace))
                {
                    _sb.Append($"namespace {_container.Namespace} {Environment.NewLine} {{");
                    _sb.Append(Environment.NewLine);
                    _sb.Append(Environment.NewLine);
                }
            }

            private void GenerateFooter()
            {
                if (_container == null || _sb == null)
                    return;

                if (!string.IsNullOrEmpty(_container.Namespace))
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

        private StringBuffer GeneratePropertyContainer(PropertyTypeNode container)
        {
            StringBuffer gen = new StringBuffer();
            using (var d = new CSharpSourceFileDecorator(container, gen))
            {
                GeneratePropertyContainerFor(container, gen);
            }
            return gen;
        }
    }
}
#endif // NET_4_6
