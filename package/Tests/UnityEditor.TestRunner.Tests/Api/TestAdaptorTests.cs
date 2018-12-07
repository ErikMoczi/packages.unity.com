using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FrameworkTests;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEditor.TestTools.TestRunner.Api;

public class TestAdaptorTests
{
    [Test]
    public void TestRunnerTestWrapsITest()
    {
        var categories = new[] { "catA", "catB", "catA" };
        var parentMock = new TestMock()
        {
            Id = "parentId",
            FullName = "ParentName"
        };

        var testMock = new TestMock(categories)
        {
            Id = "testId",
            FullName = "FullName",
            HasChildren = true,
            IsSuite = true,
            Name = "TestSuite",
            TestCaseCount = 4,
            TypeInfo = new TypeInfoMock(Assembly.GetExecutingAssembly()),
            Method = new MethodInfoMock(),
        };
        var childTestMock = new TestMock()
        {
            Id = "child",
        };
        testMock.Tests = new List<ITest>() { childTestMock };
        var description = "description";
        testMock.Properties.Set(PropertyNames.Description, description);
        var skipReason = "skipReason";
        testMock.Properties.Set(PropertyNames.SkipReason, skipReason);
        testMock.Parent = parentMock;

        var testAdaptorUnderTest = new TestAdaptor(testMock);

        Assert.AreEqual(testMock.Id, testAdaptorUnderTest.Id);
        Assert.AreEqual(testMock.FullName, testAdaptorUnderTest.FullName);
        Assert.AreEqual(testMock.HasChildren, testAdaptorUnderTest.HasChildren);
        Assert.AreEqual("child", testAdaptorUnderTest.Children.Single().Id);
        Assert.AreEqual(testMock.IsSuite, testAdaptorUnderTest.IsSuite);
        Assert.AreEqual(testMock.Name, testAdaptorUnderTest.Name);
        Assert.AreEqual(testMock.TestCaseCount, testAdaptorUnderTest.TestCaseCount);
        Assert.AreEqual(0, testAdaptorUnderTest.TestCaseTimeout);
        Assert.AreEqual(testMock.TypeInfo, testAdaptorUnderTest.TypeInfo);
        Assert.AreEqual(testMock.Method, testAdaptorUnderTest.Method);
        Assert.AreEqual(testMock.Name, testAdaptorUnderTest.FullPath);
        CollectionAssert.AreEqual(categories.Distinct(), testAdaptorUnderTest.Categories);
        Assert.IsFalse(testAdaptorUnderTest.IsTestAssembly);
        Assert.AreEqual(testMock.RunState.ToString(), testAdaptorUnderTest.RunState.ToString());
        Assert.AreEqual(description, testAdaptorUnderTest.Description);
        Assert.AreEqual(skipReason, testAdaptorUnderTest.SkipReason);
        Assert.AreEqual(parentMock.Id, testAdaptorUnderTest.ParentId);
        Assert.AreEqual("[UnityEditor.TestRunner.Tests][FullName][suite]", testAdaptorUnderTest.UniqueName);
        Assert.AreEqual("[ParentName]", testAdaptorUnderTest.ParentUniqueName);
    }

    [Test]
    [Timeout(100)]
    public void TestRunnerTestIncludesCustomTestCaseTimeout()
    {
        var testMock = new TestMock();

        var testAdaptorUnderTest = new TestAdaptor(testMock);

        Assert.AreEqual(100, testAdaptorUnderTest.TestCaseTimeout);
    }

    internal class TestMock : ITest
    {
        public TestMock() : this(new string[0])
        {
        }

        public TestMock(string[] categories)
        {
            Tests = new List<ITest>();
            Properties = new PropertyBag();
            Properties[PropertyNames.Category] = categories;
        }

        public TNode ToXml(bool recursive)
        {
            throw new System.NotImplementedException();
        }

        public TNode AddToXml(TNode parentNode, bool recursive)
        {
            throw new System.NotImplementedException();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public ITypeInfo TypeInfo { get; set; }
        public IMethodInfo Method { get; set; }
        public NUnit.Framework.Interfaces.RunState RunState { get; set; }
        public int TestCaseCount { get; set; }
        public IPropertyBag Properties { get; set; }
        public ITest Parent { get; set; }
        public bool IsSuite { get; set; }
        public bool HasChildren { get; set; }
        public IList<ITest> Tests { get; set; }
        public object Fixture { get; set; }
    }
}
