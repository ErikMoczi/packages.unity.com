using System.Collections.Generic;

namespace UnityEditor.TestTools.TestRunner.Api
{
    internal interface ITest
    {
        string Id { get; }
        string Name { get; }
        string FullName { get; }
        int TestCaseCount { get; }
        bool HasChildren { get; }
        bool IsSuite { get; }
        IEnumerable<ITest> Children { get; }
        int TestCaseTimeout { get; }
    }
}
