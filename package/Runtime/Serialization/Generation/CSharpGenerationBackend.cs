using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unity.Properties.Serialization
{
    class CSharpGenerationBackend : IGenerationBackend
    {
        public StringBuffer Generate(List<PropertyContainerType> root)
        {
            StringBuffer gen = new StringBuffer();

            // expect only one for now
            foreach (var container in root)
            {
                var c = GeneratePropertyContainer(container);
                gen.Append(c.ToString());
            }
            return gen;
        }
        
        private static class Style
        {
            public const int Space = 4;
        }

        private static string TypeFromProperty(PropertyType property_type)
        {
            if (PropertyType.IsCompositeType(property_type.Tag))
            {
                // TODO remove that crap
                if (property_type.Tag == PropertyType.TypeTag.Array)
                {
                    return string.Format(
                        "{0}[]",
                        property_type.Of.Name
                        );
                }
                // TODO value type etc.
                return string.Format(
                    "{0}<{1}>",
                    property_type.Name,
                    property_type.Of.Name
                    );
            }
            return property_type.Name;
        }

        private static string InitializerFromProperty(PropertyType property_type)
        {
            if (PropertyType.IsCompositeType(property_type.Tag))
            {
                // TODO value type etc.
                return string.Format("new {0} {{}}", property_type.Name);
            }
            else if (property_type.Tag == PropertyType.TypeTag.Class)
            {
                return string.Format("new {0} ()", property_type.Name);
            }
            return !string.IsNullOrEmpty(property_type.DefaultValue)
                ? property_type.DefaultValue
                : string.Empty;
        }

        private static string GenerateDataBackend(string prop_type,
            string field_name,
            bool is_composite_type,
            string initial_value)
        {
            var composite_decorator = is_composite_type ? "readonly" : "";
            var initializer = "";
            if (!string.IsNullOrEmpty(initial_value))
            {
                initializer = string.Format("= {0}", initial_value);
            }
            return string.Format("private {0} {1} m_{2} {3};{4}",
                composite_decorator, prop_type, field_name, initializer, Environment.NewLine);
        }

        private static string GetPropertyWrapperFromProperty(
            string container_name,
            PropertyType property)
        {
            if (PropertyType.IsCompositeType(property.Tag))
            {
                // TODO value type etc.
                return string.Format(
                    "ListProperty<{0}, {1}, {2}>",
                    container_name,
                    property.Name,
                    property.Of.Name
                    );
            }
            else if (property.Tag == PropertyType.TypeTag.Struct)
            {
                // TODO value type etc.
                return string.Format(
                    "MutableStructProperty<{0}, {1}>",
                    container_name,
                    property.Name
                    );

            }
            else if (property.Tag == PropertyType.TypeTag.Class)
            {
                // TODO value type etc.
                return string.Format(
                    "ContainerProperty<{0}, {1}>",
                    container_name,
                    property.Name
                    );
            }
            return string.Format("Property<{0}, {1}>", container_name, property.Name);
        }

        private static string GenerateProperty(
            string container_name,
            string property_name,
            PropertyType property_type)
        {
            StringBuffer gen = new StringBuffer();

            string prop_type = property_type.Name;

            var property_wrapper = GetPropertyWrapperFromProperty(
                container_name,
                property_type);

            bool is_compositye_type = PropertyType.IsCompositeType(property_type.Tag);

            if (!string.IsNullOrEmpty(property_type.PropertyBackingAccessor))
            {
                gen.Append(' ', Style.Space * 1);
                gen.Append(
                    GenerateDataBackend(
                        prop_type,
                        property_name,
                        is_compositye_type,
                        // TOO factor out
                        InitializerFromProperty(property_type)
                        ));
            }

            gen.Append(' ', Style.Space * 1);
            gen.Append(string.Format(@"public {0} {1}
        {{
            get {{ return {2}Property.GetValue(this); }}
            set {{ {3} }}
        }}", prop_type,
             property_name,
             property_name,

             // TODO bundle the 2 (composite & struct)
             (is_compositye_type || property_type.Tag == PropertyType.TypeTag.Struct || property_type.Tag == PropertyType.TypeTag.Class)
                ? string.Empty
                : string.Format("{0}Property.SetValue(this, value);", property_name)
             ));

            var property_accessor_name = string.Format("m_{0}", property_name);
            if (!string.IsNullOrEmpty(property_type.PropertyBackingAccessor))
            {
                // TODO check & get by id instead of by name
                var property_accessor_delegate = property_type.PropertyBackingAccessor;
                property_accessor_name = string.Format(
                    "{0}.{1}",
                    property_name,
                    property_accessor_delegate);
            }

            var property_setter = "null";
            // TODO Fix this MESS !!
            if (!is_compositye_type && property_type.Tag != PropertyType.TypeTag.Struct && property_type.Tag != PropertyType.TypeTag.Class)
            {
                property_setter = string.Format(
                    "(ref {0} container, {1} value) => container.{2} = value",
                    container_name,
                    prop_type,
                    property_accessor_name);
            }

            gen.Append(Environment.NewLine);
            gen.Append(' ', Style.Space * 1);
            gen.Append(string.Format(@"public static readonly {0} {1}Property = new {2}(
        nameof({3}),
        /* GET */ (ref {4} container) => container.{5},
        /* SET */ {6}
        /* REF */ {7}
        );
        ",
                property_wrapper, property_name, property_wrapper,
                property_name,
                container_name, property_accessor_name,
                property_setter,
                property_type.Tag == PropertyType.TypeTag.Struct ?
                    string.Format(@", (ref {0} container,
                        {1}.RefVisitMethod a,
                        IPropertyVisitor v) => a(ref container.m_{2}, v) //<-- this is awesome",
                            container_name, property_wrapper, property_name)
                        : string.Empty
                ));

            gen.Append(Environment.NewLine);
            return gen.ToString();
        }

        private static string GeneratePropertiesFor(PropertyTypeNode c)
        {
            StringBuffer gen = new StringBuffer();

            var container_name = c.TypeName;

            gen.Append(
                string.Format(
                    "public partial {0} {1} : IPropertyContainer{2}{{{3}"
                    , c.tag == PropertyType.TypeTag.Struct ? "struct" : "class" // We default to class, probably something to tweak
                    , container_name
                    , Environment.NewLine, Environment.NewLine));

            if (c.constructor != null)
            {
                gen.Append(' ', Style.Space * 1);

                int i = 0;
                gen.Append(
                    string.Format(
                        "public {0} ({1}){2}",
                        container_name,
                        string.Join(
                            ","
                            , c.constructor.ParameterTypes.Select(p => {
                                return string.Format("{0} p{1}", p.Key, i);
                            })
                        ),
                        Environment.NewLine
                    )
                );

                gen.Append(' ', Style.Space * 1);
                gen.Append("{");

                i = 0;
                foreach (var param_type in c.constructor.ParameterTypes)
                {
                    gen.Append(' ', Style.Space * 2);
                    gen.Append(string.Format("{0} = p{1};\n", param_type.Value, i));
                }

                gen.Append(' ', Style.Space * 1);
                gen.Append("}"); gen.Append(Environment.NewLine);
            }

            gen.Append(' ', Style.Space * 1);
            gen.Append("public IPropertyBag PropertyBag => sProperties;");
            gen.Append(Environment.NewLine); gen.Append(Environment.NewLine);

            if (c.children.Count != 0)
            {
                // Empty props if not
                foreach (var property_name in c.children.Keys)
                {
                    gen.Append(GenerateProperty(container_name, property_name, c.children[property_name]));
                }
            }

            gen.Append(' ', Style.Space * 1);
            gen.Append(string.Format(@"private static readonly PropertyBag sProperties = new PropertyBag(new List<IProperty>{{ {0} }}.ToArray());"
                        , string.Join(",", c.children.Keys.Select(a => {
                            return string.Format("{0}Property", a);
                        }))
                    )
                );
            gen.Append(Environment.NewLine); gen.Append(Environment.NewLine);

            gen.Append(' ', Style.Space * 1);
            gen.Append("public IVersionStorage VersionStorage => DefaultVersionStorage.Instance;"); gen.Append(Environment.NewLine);
            gen.Append("}");
            gen.Append(Environment.NewLine);

            return gen.ToString();
        }

        readonly private List<string> usings = new List<string>()
        {
            "System",
            "System.Collections.Generic",
            "Unity.Properties"
        };

        private StringBuffer GeneratePropertyContainer(PropertyContainerType container)
        {
            StringBuffer gen = new StringBuffer();

            usings.ForEach((current_using) =>
            {
                gen.Append(string.Format("using {0};", current_using)); gen.Append(Environment.NewLine);
            });
            gen.Append(Environment.NewLine);

            if (!string.IsNullOrEmpty(container.Namespace))
            {
                gen.Append(string.Format("namespace {0} {1} {{", container.Namespace, Environment.NewLine));
                gen.Append(Environment.NewLine); gen.Append(Environment.NewLine);
            }

            gen.Append(GeneratePropertiesFor(container.PropertyTypeNode));

            if (!string.IsNullOrEmpty(container.Namespace))
            {
                gen.Append("}"); gen.Append(Environment.NewLine); gen.Append(Environment.NewLine);
            }

            return gen;
        }
    }
}
