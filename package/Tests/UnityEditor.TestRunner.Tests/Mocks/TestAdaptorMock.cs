using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using UnityEditor.TestTools.TestRunner.Api;
using RunState = UnityEditor.TestTools.TestRunner.Api.RunState;

namespace FrameworkTests
{
    internal class TestAdaptorMock : ITestAdaptor
    {
        public TestAdaptorMock(int testCaseTimeout)
        {
            TestCaseTimeout = testCaseTimeout;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public int TestCaseCount { get; private set; }
        public bool HasChildren { get; private set; }
        public bool IsSuite { get; private set; }
        public IEnumerable<ITestAdaptor> Children { get; private set; }
        public int TestCaseTimeout { get; private set; }
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
}
