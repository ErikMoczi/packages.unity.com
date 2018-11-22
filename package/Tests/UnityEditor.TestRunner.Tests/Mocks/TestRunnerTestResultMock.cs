using System;
using System.Collections.Generic;
using UnityEditor.TestTools.TestRunner.Api;
using TNode = NUnit.Framework.Interfaces.TNode;

namespace FrameworkTests
{
    internal class TestRunnerTestResultMock : ITestResult
    {
        public TNode ToXml()
        {
            var node = new TNode("test-result");
            node.AddElement("InnerValueOfTestResult");
            return node;
        }

        public string ResultState { get; set; }
        public TestStatus TestStatus { get; set; }
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
