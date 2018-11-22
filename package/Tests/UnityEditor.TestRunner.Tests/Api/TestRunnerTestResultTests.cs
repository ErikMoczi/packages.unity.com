using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.TestTools.TestRunner.Api;
using ITest = NUnit.Framework.Interfaces.ITest;
using ITestResult = NUnit.Framework.Interfaces.ITestResult;
using TNode = NUnit.Framework.Interfaces.TNode;
using ResultState = NUnit.Framework.Interfaces.ResultState;

public class NewTestScript
{
    [Test]
    public void TestRunnerTestResultWrapsITestResult()
    {
        var expectedXmlNode = new TNode("x");
        var testResultMock = new TestResultMock()
        {
            Children = new ITestResult[]
            {
                new TestResultMock()
                {
                    Name = "Child",
                    Children = new ITestResult[0],
                    ResultState = ResultState.Success,
                    Test = new TestRunnerTestTests.TestMock()
                    {
                        Tests = new ITest[0]
                    },
                    ToXmlResult = expectedXmlNode
                }
            },
            HasChildren = true,
            Test = new TestRunnerTestTests.TestMock()
            {
                Tests = new ITest[0]
            },
            Name = "ResultName",
            FullName = "FullResultName",
            ResultState = ResultState.Error,
            StartTime = new DateTime(2018, 1, 1, 1, 0, 0),
            EndTime = new DateTime(2018, 1, 1, 1, 2, 3),
            Duration = 1.234,
            AssertCount = 1,
            FailCount = 2,
            PassCount = 3,
            SkipCount = 4,
            InconclusiveCount = 5,
            Output = "ResultOutput",
            ToXmlResult = expectedXmlNode
        };

        var testResultUnderTest = new TestResult(testResultMock);

        Assert.AreEqual(testResultMock.Name, testResultUnderTest.Name);
        Assert.AreEqual(testResultMock.FullName, testResultUnderTest.FullName);
        Assert.AreEqual("Failed:Error", testResultUnderTest.ResultState);
        Assert.AreEqual(TestStatus.Failed, testResultUnderTest.TestStatus);
        Assert.AreEqual("Child", testResultUnderTest.Children.Single().Name);
        Assert.AreEqual(testResultMock.HasChildren, testResultUnderTest.HasChildren);
        Assert.AreEqual(testResultMock.Duration, testResultUnderTest.Duration);
        Assert.AreEqual(testResultMock.StartTime, testResultUnderTest.StartTime);
        Assert.AreEqual(testResultMock.EndTime, testResultUnderTest.EndTime);
        Assert.AreEqual(testResultMock.AssertCount, testResultUnderTest.AssertCount);
        Assert.AreEqual(testResultMock.FailCount, testResultUnderTest.FailCount);
        Assert.AreEqual(testResultMock.PassCount, testResultUnderTest.PassCount);
        Assert.AreEqual(testResultMock.SkipCount, testResultUnderTest.SkipCount);
        Assert.AreEqual(testResultMock.InconclusiveCount, testResultUnderTest.InconclusiveCount);
        Assert.AreEqual(testResultMock.Output, testResultUnderTest.Output);
    }

    [Test]
    public void TestRunnerTestResultForwardsToXmlCalls()
    {
        var expectedXmlNode = new TNode("result");
        var testResultMock = new TestResultMock()
        {
            ResultState = ResultState.Success,
            Children = new ITestResult[0],
            Test = new TestRunnerTestTests.TestMock()
            {
                Tests = new ITest[0]
            },
            ToXmlResult = expectedXmlNode
        };
        var testResultUnderTest = new TestResult(testResultMock);

        var xmlNode = testResultUnderTest.ToXml();

        Assert.AreEqual("<result />", xmlNode.OuterXml);
        Assert.AreEqual(true, testResultMock.ToXmlRecursive);
    }

    [Test]
    public void TestRunnerTestResultWrapsITestResultForInconclusiveResult()
    {
        var expectedXmlNode = new TNode("result");
        var testResultMock = new TestResultMock()
        {
            ResultState = ResultState.Inconclusive,
            Children = new ITestResult[0],
            Test = new TestRunnerTestTests.TestMock()
            {
                Tests = new ITest[0]
            },
            ToXmlResult = expectedXmlNode,
        };

        var testResultUnderTest = new TestResult(testResultMock);

        Assert.AreEqual(TestStatus.Failed, testResultUnderTest.TestStatus);
    }

    private class TestResultMock : ITestResult
    {
        public TNode ToXmlResult;
        public bool ToXmlRecursive;
        public TNode ToXml(bool recursive)
        {
            ToXmlRecursive = recursive;
            return ToXmlResult;
        }

        public TNode AddToXml(TNode parentNode, bool recursive)
        {
            throw new NotImplementedException();
        }

        public ResultState ResultState { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public double Duration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public int AssertCount { get; set; }
        public int FailCount { get; set; }
        public int PassCount { get; set; }
        public int SkipCount { get; set; }
        public int InconclusiveCount { get; set; }
        public bool HasChildren { get; set; }
        public IEnumerable<ITestResult> Children { get; set; }
        public ITest Test { get; set; }
        public string Output { get; set; }
    }
}
