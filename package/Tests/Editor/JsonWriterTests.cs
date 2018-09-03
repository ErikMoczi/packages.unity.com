using System.ComponentModel.Design;
using UnityEngine;
using NUnit.Framework;
using Unity.Properties;
using Unity.Properties.Serialization;

[TestFixture]
public class JsonWriterTests
{
    [Test]
    public void WhenEmptyPropertyContainer_JsonSerialization_ReturnsAnEmptyResult()
    {
        var result = JsonSerializer.SerializeStruct(new NullStructContainer());
        Assert.NotZero(result.Length);
    }

    [Test]
    public void WhenStructPropertyContainer_JsonSerialization_ReturnsAValidResult()
    {
        var result = JsonSerializer.SerializeStruct(new TestStructContainer());
        Debug.Log(result);
        Assert.IsTrue(result.Contains("FloatValue"));
    }

    private struct NullStructContainer : IPropertyContainer
    {
        public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;
        private static readonly PropertyBag s_PropertyBag = new PropertyBag();
        public IPropertyBag PropertyBag => s_PropertyBag;
    }

    private struct TestStructContainer : IPropertyContainer
    {
        private float m_FloatValue;

        public float FloatValue
        {
            get { return FloatValueProperty.GetValue(ref this); }
            set { FloatValueProperty.SetValue(ref this, value); }
        }

        public static readonly StructProperty<TestStructContainer, float> FloatValueProperty = new StructProperty<TestStructContainer, float>(
            nameof(FloatValue),
            (ref TestStructContainer c) =>  c.m_FloatValue,
            (ref TestStructContainer c, float v) => c.m_FloatValue = v);

        public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;

        private static readonly PropertyBag s_PropertyBag = new PropertyBag(FloatValueProperty);

        public IPropertyBag PropertyBag => s_PropertyBag;
    }
}
