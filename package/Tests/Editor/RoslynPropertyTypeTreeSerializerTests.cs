#if USE_ROSLYN_API && (NET_4_6 || NET_STANDARD_2_0)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

using Unity.Properties.Editor.Serialization;
using Unity.Properties.Editor.Serialization.Experimental;

namespace Unity.Properties.Tests.JSonSchema
{
    [TestFixture]
    internal class RoslynPropertyTypeTreeSerializerTests
    {
        [Test]
        public void WhenCodeDoesNotContainPropertyContainers_RoslynTreeSerializer_ReturnsAnEmptyJson()
        {
            RoslynPropertyTypeNode serializer = new RoslynPropertyTypeNode()
            {
                Code = @"
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
            "
            };

            var result = serializer.Deserialize();
            Assert.Zero(result.Count);
        }

        [Test]
        public void WhenCodeWithContainers_RoslynTreeSerializer_ReturnsAValidJson()
        {
            RoslynPropertyTypeNode serializer = new RoslynPropertyTypeNode()
            {
                Code = @"
using System.Collections.Generic;
using Unity.Properties;
                
namespace Unity.Properties.TestCases {

public partial class HelloWorld : IPropertyContainer
{
    public static IPropertyBag bag { get; } = new PropertyBag(new List<IProperty> {}.ToArray());

    public IVersionStorage VersionStorage { get; }
    public IPropertyBag PropertyBag => bag;

    private int m_MyInt;
    public static readonly Property<HelloWorld, int> s_MyInt =
        new Property<HelloWorld, int>(
            ""MyInt"",
            c => c.m_MyInt,
            (c, v) => c.m_MyInt = v);

    private MyContainer m_MyContainerProperty;
    private static readonly ContainerProperty<HelloWorld, NestedContainer> s_MyContainerProperty =
        new ContainerProperty<HelloWorld, NestedContainer>(
            ""MyContainerProperty"",
        c => c.m_MyContainerProperty,
        (c, v) => c.m_MyContainerProperty = v);
    
    private List<float> _floatList = new List<float>();
    public static readonly ListProperty<TestContainer, List<float>, float> FloatListProperty =
        new ListProperty<TestContainer, List<float>, float>(nameof(FloatList),
            c => c._floatList,
            null,
            null);
    public List<float> FloatList
    {
        get { return FloatListProperty.GetValue(this); }
    }

    public enum TestEnum
    {
        This,
        Test
    }
    public static readonly EnumProperty<TestContainer, TestEnum> EnumValueProperty = new EnumProperty<TestContainer, TestEnum>(
        nameof(EnumValue),
        c => c._enumValue,
        (c, v) => c._enumValue = v);
    public TestEnum EnumValue
    {
        get { return EnumValueProperty.GetValue(this); }
        set { EnumValueProperty.SetValue(this, value); }
    }

    public class NestedContainer : IPropertyContainer
    {
        public static IPropertyBag bag { get; } = new PropertyBag(new List<IProperty> {}.ToArray());

        public IVersionStorage VersionStorage { get; }
        public IPropertyBag PropertyBag => bag;
    }
}"
            };
            var result = serializer.Deserialize();
            Assert.IsTrue(result.Count == 1);

            Assert.AreEqual("Unity.Properties.TestCases.HelloWorld", result[0].FullTypeName);
            Assert.AreEqual("Unity.Properties.TestCases.HelloWorld/NestedContainer", result[0].NestedContainers[0].FullTypeName);

            Assert.AreEqual(
                new List<string>
                {
                    "MyInt",
                    "MyContainer",
                    "FloatList",
                    "EnumValue"
               },
                result[0].Properties.Select(p => p.PropertyName)
            );
            Assert.AreEqual(
                new List<PropertyTypeNode.TypeTag>
                {
                    PropertyTypeNode.TypeTag.Primitive,
                    PropertyTypeNode.TypeTag.Class,
                    PropertyTypeNode.TypeTag.List,
                    PropertyTypeNode.TypeTag.Enum
                },
                result[0].Properties.Select(p => p.Tag)
            );
            Assert.AreEqual(
                new List<string>
                {
                    "Int32",
                    "NestedContainer",
                    "List",
                    "TestEnum"
                },
                result[0].Properties.Select(p => p.TypeName)
            );
            Assert.AreEqual(
                new List<bool>
                {
                    true,
                    false,
                    true,
                    true
                },
                result[0].Properties.Select(p => p.IsPublicProperty)
            );

            // Lists

            Assert.AreEqual(result[0].Properties[2].Of.TypeName, "Single");
        }
    }
}

#endif // USE_ROSLYN_API && (NET_4_6 || NET_STANDARD_2_0)
