using System.Collections.Generic;
using UnityEditor.TestTools.TestRunner.Api;

namespace FrameworkTests
{
    internal class TestRunnerTestMock : ITest
    {
        public TestRunnerTestMock(int testCaseTimeout)
        {
            TestCaseTimeout = testCaseTimeout;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public int TestCaseCount { get; private set; }
        public bool HasChildren { get; private set; }
        public bool IsSuite { get; private set; }
        public IEnumerable<ITest> Children { get; private set; }
        public int TestCaseTimeout { get; private set; }
    }
}
