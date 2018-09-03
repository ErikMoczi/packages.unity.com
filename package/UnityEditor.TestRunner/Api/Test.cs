using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Interfaces;
using UnityEngine.TestRunner.NUnitExtensions.Runner;
using UnityEngine.TestRunner.TestLaunchers;

namespace UnityEditor.TestTools.TestRunner.Api
{
    internal class Test : ITest
    {
        internal Test(NUnit.Framework.Interfaces.ITest test)
        {
            Id = test.Id;
            Name = test.Name;
            FullName = test.FullName;
            TestCaseCount = test.TestCaseCount;
            HasChildren = test.HasChildren;
            IsSuite = test.IsSuite;
            Children = test.Tests.Select(t => new Test(t)).ToArray();
            TestCaseTimeout = UnityTestExecutionContext.CurrentContext.TestCaseTimeout;
        }

        internal Test(RemoteTestData test, RemoteTestData[] allTests)
        {
            Id = test.id;
            Name = test.name;
            FullName = test.fullName;
            TestCaseCount = test.testCaseCount;
            HasChildren = test.hasChildren;
            IsSuite = test.isSuite;
            Children = test.childrenIds.Select(id => new Test(allTests.First(t => t.id == id), allTests)).ToArray();
            TestCaseTimeout = test.testCaseTimeout;
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
