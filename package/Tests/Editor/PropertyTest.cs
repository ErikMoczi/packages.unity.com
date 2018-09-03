using UnityEngine;
using NUnit.Framework;

namespace Unity.Properties.Tests
{
	[TestFixture]
	internal class PropertyTest
	{
		[Test]
		public void Simple_Visitor()
		{
			// test PropertyBag
			var container = new TestContainer { IntValue = 123 };

			// test Set/GetValue
			Assert.AreEqual(123, TestContainer.IntValueProperty.GetValue(container));

			// test visitor
			var leafVisitor = new LeafCountVisitor();
			container.PropertyBag.Visit(container, leafVisitor);
			Assert.AreEqual(TestContainer.LeafCount, leafVisitor.LeafCount);
		}

		private class LeafCountVisitor : PropertyVisitor
		{
			public int LeafCount { get; private set; }

			protected override void Visit<TValue>(TValue value)
			{
				++LeafCount;
			}
		}
	}
}