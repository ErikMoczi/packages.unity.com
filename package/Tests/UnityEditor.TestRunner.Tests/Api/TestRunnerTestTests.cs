using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Interfaces;
using UnityEditor.TestTools.TestRunner.Api;
using ITest = NUnit.Framework.Interfaces.ITest;

public class TestRunnerTestTests
{
    [Test]
    public void TestRunnerTestWrapsITest()
    {
        var testMock = new TestMock()
        {
            Id = "testId",
            FullName = "FullName",
            HasChildren = true,
            IsSuite = true,
            Name = "TestSuite",
            TestCaseCount = 4,
        };
        var childTestMock = new TestMock()
        {
            Id = "child",
        };
        testMock.Tests = new List<ITest>() { childTestMock };

        var testRunnerTestUnderTest = new Test(testMock);

        Assert.AreEqual(testMock.Id, testRunnerTestUnderTest.Id);
        Assert.AreEqual(testMock.FullName, testRunnerTestUnderTest.FullName);
        Assert.AreEqual(testMock.HasChildren, testRunnerTestUnderTest.HasChildren);
        Assert.AreEqual("child", testRunnerTestUnderTest.Children.Single().Id);
        Assert.AreEqual(testMock.IsSuite, testRunnerTestUnderTest.IsSuite);
        Assert.AreEqual(testMock.Name, testRunnerTestUnderTest.Name);
        Assert.AreEqual(testMock.TestCaseCount, testRunnerTestUnderTest.TestCaseCount);
        Assert.AreEqual(0, testRunnerTestUnderTest.TestCaseTimeout);
    }

    [Test]
    [Timeout(100)]
    public void TestRunnerTestIncludesCustomTestCaseTimeout()
    {
        var testMock = new TestMock();

        var testRunnerTestUnderTest = new Test(testMock);

        Assert.AreEqual(100, testRunnerTestUnderTest.TestCaseTimeout);
    }

    internal class TestMock : ITest
    {
        public TestMock()
        {
            Tests = new List<ITest>();
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
        public RunState RunState { get; set; }
        public int TestCaseCount { get; set; }
        public IPropertyBag Properties { get; set; }
        public ITest Parent { get; set; }
        public bool IsSuite { get; set; }
        public bool HasChildren { get; set; }
        public IList<ITest> Tests { get; set; }
        public object Fixture { get; set; }
    }
}
