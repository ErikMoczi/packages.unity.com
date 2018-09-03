using System.Collections.Generic;

namespace Unity.Properties.Tests
{
    internal class TestContainer : IPropertyContainer
    {
        public const int LeafCount = 1;
        
        private int _intValue;

        public static readonly IProperty<TestContainer, int> IntValueProperty = new Property<TestContainer, int>(
            nameof(IntValue),
            c => c._intValue,
            (c, v) => c._intValue = v);

        public int IntValue
        {
            get { return IntValueProperty.GetValue(this); }
            set { IntValueProperty.SetValue(this, value); }
        }

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

        private List<TestChildContainer> _childList = new List<TestChildContainer>();

        public static readonly ContainerListProperty<TestContainer, List<TestChildContainer>, TestChildContainer>
            ChildListProperty =
                new ContainerListProperty<TestContainer, List<TestChildContainer>, TestChildContainer>(
                    nameof(ChildList),
                    c => c._childList,
                    null,
                    null);


        public List<TestChildContainer> ChildList
        {
            get { return ChildListProperty.GetValue(this); }
        }

        public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;

        private static PropertyBag sBag = new PropertyBag(IntValueProperty, FloatListProperty, ChildListProperty);
        public IPropertyBag PropertyBag => sBag;
    }

    public class TestChildContainer : IPropertyContainer
    {
        private int _intValue;

        public static readonly IProperty<TestChildContainer, int> IntValueProperty =
            new Property<TestChildContainer, int>(nameof(IntValue),
                c => c._intValue,
                (c, v) => c._intValue = v);

        public int IntValue
        {
            get { return IntValueProperty.GetValue(this); }
            set { IntValueProperty.SetValue(this, value); }
        }

        public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;

        private static PropertyBag sBag = new PropertyBag(IntValueProperty);
        public IPropertyBag PropertyBag => sBag;
    }
}