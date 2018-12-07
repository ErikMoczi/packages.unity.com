using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor.TestTools.TestRunner.UnityTestProtocol;
using RunState = UnityEditor.TestTools.TestRunner.Api.RunState;
using TestStatus = UnityEditor.TestTools.TestRunner.Api.TestStatus;

namespace Assets.UtilTests
{
    public class UtpMessageReporterTests
    {
        IUtpMessageReporter m_UtpMessageReporter;
        UtpLoggerSpy m_Spy;

        [SetUp]
        public void BeforeEach()
        {
            m_Spy = new UtpLoggerSpy();
            m_UtpMessageReporter = new UtpMessageReporter(m_Spy);
        }

        [Test]
        public void TestStarted_ReportsCorrectUtpMessage()
        {
            var test = new TestAdaptorMock { FullName = "testName" };

            var msg = Call(x => x.ReportTestStarted(test));
            Assert.That(msg, Is.TypeOf(typeof(TestStartedMessage)));

            var testStartedMessage = (TestStartedMessage)msg;
            Assert.That(testStartedMessage.phase, Is.EqualTo("Begin"));
            Assert.That(testStartedMessage.name, Is.EqualTo(test.FullName));
            Assert.That(testStartedMessage.type, Is.EqualTo("TestStatus"));
            Assert.That(testStartedMessage.state, Is.EqualTo(TestState.Inconclusive));
        }

        [Test]
        public void TestFinished_ReportsCorrectUtpMessage_WhenTestPassed()
        {
            var result = new TestResultAdaptorMock("testName")
            {
                Duration = 1,
                Message = "testMsg",
                TestStatus = TestStatus.Passed
            };

            var message = Call(x => x.ReportTestFinished(result));
            Assert.That(message, Is.TypeOf(typeof(TestFinishedMessage)));

            var testFinishedMessage = (TestFinishedMessage)message;
            Assert.That(testFinishedMessage.state, Is.EqualTo(TestState.Success));

            Assert.That(testFinishedMessage.type, Is.EqualTo("TestStatus"));
            Assert.That(testFinishedMessage.phase, Is.EqualTo("End"));
            Assert.That(testFinishedMessage.name, Is.EqualTo("testName"));
            Assert.That(testFinishedMessage.message, Is.EqualTo("testMsg"));
            Assert.That(testFinishedMessage.duration, Is.EqualTo(1000));
            Assert.That(testFinishedMessage.durationMicroseconds, Is.EqualTo(1000000));
        }

        [Test]
        public void TestFinished_ReportsTestStateAsFailure_WhenTestStatusEqualsFailed()
        {
            var testResult = new TestResultAdaptorMock { TestStatus = TestStatus.Failed };

            var testFinishedMessage = (TestFinishedMessage)Call(x => x.ReportTestFinished(testResult));

            Assert.That(testFinishedMessage.state, Is.EqualTo(TestState.Failure));
        }

        [Test]
        public void TestFinished_ReportsNothing_WhenResultIsSuite()
        {
            var testResult = new TestResultAdaptorMock(isSuite: true);

            m_UtpMessageReporter.ReportTestFinished(testResult);

            Assert.That(m_Spy.messages, Is.Empty);
        }

        [Test]
        public void RunStarted_ReportsCorrectTestPlanMessage_ForOneTest()
        {
            var test = new TestAdaptorMock { FullName = "testName", IsSuite = false };

            var message = Call(x => x.ReportTestRunStarted(test));

            Assert.That(message, Is.TypeOf(typeof(TestPlanMessage)));
            var testPlanMessage = (TestPlanMessage)message;

            Assert.That(testPlanMessage.type, Is.EqualTo("TestPlan"));
            Assert.That(testPlanMessage.tests.Count, Is.EqualTo(1));
            Assert.That(testPlanMessage.tests, Has.All.EqualTo("testName"));
        }

        [Test]
        public void RunStarted_ReportsCorrectTestPlanMessage_ForSuiteWithChildTests()
        {
            var test = new TestAdaptorMock
            {
                FullName = "suite0",
                IsSuite = true,
                Children = new List<ITestAdaptor>
                {
                    new TestAdaptorMock {FullName = "test0"},
                    new TestAdaptorMock
                    {
                        FullName = "suite1", IsSuite = true,
                        Children = new List<ITestAdaptor> {new TestAdaptorMock {FullName = "test1"}, new TestAdaptorMock {FullName = "test2"}}},
                    new TestAdaptorMock
                    {
                        FullName = "test3", IsSuite = false,
                        Children = new List<ITestAdaptor> {new TestAdaptorMock {FullName = "test4"}}}
                }
            };

            var message = Call(x => x.ReportTestRunStarted(test));

            Assert.That(message, Is.TypeOf(typeof(TestPlanMessage)));
            var testPlanMessage = (TestPlanMessage)message;

            Assert.That(testPlanMessage.tests.Count, Is.EqualTo(5));
            Assert.That(testPlanMessage.tests, Has.All.Contain("test"));
        }

        [Test]
        public void RunStarted_ReportsCorrectTestPlanMessage_ForSuiteWithZeroTests()
        {
            var emptySuite = new TestAdaptorMock { IsSuite = true, Children = new List<ITestAdaptor>() };

            var message = Call(x => x.ReportTestRunStarted(emptySuite));

            Assert.That(message, Is.TypeOf(typeof(TestPlanMessage)));
            var testPlanMessage = (TestPlanMessage)message;

            Assert.That(testPlanMessage.tests, Is.Empty);
        }

        [Test]
        public void RunStarted_ReportsCorrectTestPlanMessage_ForSuiteWithNullTests()
        {
            var emptySuite = new TestAdaptorMock { IsSuite = true };

            var message = Call(x => x.ReportTestRunStarted(emptySuite));

            Assert.That(message, Is.TypeOf(typeof(TestPlanMessage)));
            var testPlanMessage = (TestPlanMessage)message;

            Assert.That(testPlanMessage.tests, Is.Empty);
        }

        [Test]
        public void TestFinished_ReportsTestStateAsInclonclusive_WhenResultStateEqualsInconclusive()
        {
            var testResult = new TestResultAdaptorMock { TestStatus = TestStatus.Failed, ResultState = "Inconclusive" };

            var testFinishedMessage = (TestFinishedMessage)Call(x => x.ReportTestFinished(testResult));

            Assert.That(testFinishedMessage.state, Is.EqualTo(TestState.Inconclusive));
        }

        [Test]
        public void TestFinished_ReportsTestStateAsError_WhenResultStateEndsWithCancelled()
        {
            var testResult = new TestResultAdaptorMock { TestStatus = TestStatus.Failed, ResultState = "Failed:Cancelled" };

            var testFinishedMessage = (TestFinishedMessage)Call(x => x.ReportTestFinished(testResult));

            Assert.That(testFinishedMessage.state, Is.EqualTo(TestState.Error));
        }

        [Test]
        public void TestFinished_ReportsTestStateAsError_WhenResultStateEndsWithError()
        {
            var testResult = new TestResultAdaptorMock { TestStatus = TestStatus.Failed, ResultState = "Failed:Error" };

            var testFinishedMessage = (TestFinishedMessage)Call(x => x.ReportTestFinished(testResult));

            Assert.That(testFinishedMessage.state, Is.EqualTo(TestState.Error));
        }

        [Test]
        public void TestFinished_ReportsTestStateAsSkipped_WhenTestStatusEqualsPassedAndNunitXmlRunstateEqualsExplicit()
        {
            var testResult = new TestResultAdaptorMock(isExplicit: true) { TestStatus = TestStatus.Passed };

            var testFinishedMessage = (TestFinishedMessage)Call(x => x.ReportTestFinished(testResult));

            Assert.That(testFinishedMessage.state, Is.EqualTo(TestState.Skipped));
        }

        [Test]
        public void TestFinished_ReportsTestStateAsSkipped_WhenTestStatusEqualsSkipped()
        {
            var testResult = new TestResultAdaptorMock { TestStatus = TestStatus.Skipped };

            var testFinishedMessage = (TestFinishedMessage)Call(x => x.ReportTestFinished(testResult));

            Assert.That(testFinishedMessage.state, Is.EqualTo(TestState.Skipped));
        }

        [Test]
        public void TestFinished_ReportsTestStateAsIgnored_WhenTestStatusEqualsSkippedAndResultStateEndsWithIgnored()
        {
            var testResult = new TestResultAdaptorMock { TestStatus = TestStatus.Skipped, ResultState = "Skipped:Ignored" };

            var testFinishedMessage = (TestFinishedMessage)Call(x => x.ReportTestFinished(testResult));

            Assert.That(testFinishedMessage.state, Is.EqualTo(TestState.Ignored));
        }

        private Message Call(Action<IUtpMessageReporter> testRunEvent)
        {
            testRunEvent(m_UtpMessageReporter);
            Assert.That(m_Spy.messages.Count, Is.EqualTo(1));

            return m_Spy.messages.First();
        }

        private class TestAdaptorMock : ITestAdaptor
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string FullName { get; set; }
            public int TestCaseCount { get; set; }
            public bool HasChildren { get; set; }
            public bool IsSuite { get; set; }
            public IEnumerable<ITestAdaptor> Children { get; set; }
            public int TestCaseTimeout { get; set; }
            public ITypeInfo TypeInfo { get; }
            public IMethodInfo Method { get; }
            public string[] Categories { get; }
            public bool IsTestAssembly { get; }
            public RunState RunState { get; }
            public string Description { get; }
            public string SkipReason { get; }
            public string ParentId { get; }
            public string UniqueName { get; }
            public string ParentUniqueName { get; }
        }

        private class TestResultAdaptorMock : ITestResultAdaptor
        {
            readonly bool m_IsExplicit;

            public TestResultAdaptorMock(string testName = "test", bool isSuite = false, bool isExplicit = false)
            {
                m_IsExplicit = isExplicit;
                Test = new TestAdaptorMock { FullName = testName, IsSuite = isSuite };
                ResultState = string.Empty;
            }

            public TNode ToXml()
            {
                var node = new TNode("testcase");
                node.AddAttribute("runstate", m_IsExplicit ? "explicit" : "passed");
                return node;
            }

            public ITestAdaptor Test { get; set; }
            public string Name { get; set; }
            public string FullName { get; set; }
            public string ResultState { get; set; }
            public TestStatus TestStatus { get; set; }
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
            public IEnumerable<ITestResultAdaptor> Children { get; set; }
            public string Output { get; set; }
        }

        class UtpLoggerSpy : IUtpLogger
        {
            List<Message> m_Messages = new List<Message>();

            public List<Message> messages
            {
                get { return m_Messages; }
            }

            public void Log(Message msg)
            {
                messages.Add(msg);
            }
        }
    }
}
