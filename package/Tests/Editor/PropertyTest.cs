using UnityEngine;
using NUnit.Framework;
using Unity.Properties;

[TestFixture]
public class PropertyTest
{
	[Test]
	public void SimpleContainer()
	{
		// test PropertyBag
		var container = new TestContainer();
		Assert.AreEqual(1, container.PropertyBag.PropertyCount);

		// test Set/GetValue
		container.FloatValue = 123f;
		Assert.AreEqual(123f, TestContainer.FloatValueProperty.GetValue(container));
		
		// test visitor
		var leafVisitor = new LeafCountVisitor();
		container.PropertyBag.Visit(container, leafVisitor);
		Assert.AreEqual(1, leafVisitor.LeafCount);
	}

	private class TestContainer : IPropertyContainer
	{
		private float m_FloatValue;

		public float FloatValue
		{
			get { return FloatValueProperty.GetValue(this); }
			set { FloatValueProperty.SetValue(this, value); }
		}

		public static readonly Property<TestContainer, float> FloatValueProperty = new Property<TestContainer, float>(
			nameof(m_FloatValue),
			c => c.m_FloatValue,
			(c, v) => c.m_FloatValue = v);
		
		public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;
		
		private static readonly PropertyBag s_PropertyBag = new PropertyBag(FloatValueProperty);

		public IPropertyBag PropertyBag => s_PropertyBag;
	}

	private class LeafCountVisitor : IPropertyVisitor
	{
		public int LeafCount { get; private set; }

		public void Visit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) where TContainer : IPropertyContainer
		{
			++LeafCount;
		}

		public void VisitEnum<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) where TContainer : IPropertyContainer where TValue : struct
		{
			++LeafCount;
		}

		public bool BeginContainer<TContainer, TValue>(ref TContainer container, SubtreeContext<TValue> context) where TContainer : IPropertyContainer
		{
			return true;
		}

		public void EndContainer<TContainer, TValue>(ref TContainer container, SubtreeContext<TValue> context) where TContainer : IPropertyContainer
		{
		}

		public bool BeginList<TContainer, TValue>(ref TContainer container, ListContext<TValue> context) where TContainer : IPropertyContainer
		{
			return true;
		}

		public void EndList<TContainer, TValue>(ref TContainer container, ListContext<TValue> context) where TContainer : IPropertyContainer
		{
		}
	}
}
