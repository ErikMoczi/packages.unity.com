using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine.TestRunner.NUnitExtensions;
using UnityEngine.TestRunner.NUnitExtensions.Runner;
using UnityEngine.TestRunner.TestLaunchers;
using UnityEngine.TestTools.Utils;

namespace UnityEditor.TestTools.TestRunner.Api
{
    internal class TestAdaptor : ITestAdaptor
    {
        internal TestAdaptor(ITest test) : this(test, new ITest[0])
        {
        }

        internal TestAdaptor(ITest test, ITest[] additionalChildren)
        {
            Id = test.Id;
            Name = test.Name;
            FullName = test.FullName;
            TestCaseCount = test.TestCaseCount;
            HasChildren = test.HasChildren;
            IsSuite = test.IsSuite;
            Children = new[] {test.Tests, additionalChildren}.SelectMany(t => t).Select(t => new TestAdaptor(t)).ToArray();
            if (UnityTestExecutionContext.CurrentContext != null)
            {
                TestCaseTimeout = UnityTestExecutionContext.CurrentContext.TestCaseTimeout;
            }
            else
            {
                TestCaseCount = CoroutineRunner.k_DefaultTimeout;
            }

            TypeInfo = test.TypeInfo;
            Method = test.Method;
            FullPath = GetFullPath(test);
            Categories = test.GetAllCategoriesFromTest().Distinct().ToArray();
            IsTestAssembly = test is TestAssembly;
            RunState = (RunState)Enum.Parse(typeof(RunState), test.RunState.ToString());
            Description = (string)test.Properties.Get(PropertyNames.Description);
            SkipReason = test.GetSkipReason();
            ParentId = test.GetParentId();
            UniqueName = test.GetUniqueName();
            ParentUniqueName = test.GetParentUniqueName();
        }

        internal TestAdaptor(RemoteTestData test)
        {
            Id = test.id;
            Name = test.name;
            FullName = test.fullName;
            TestCaseCount = test.testCaseCount;
            HasChildren = test.hasChildren;
            IsSuite = test.isSuite;
            m_ChildrenIds = test.childrenIds;
            TestCaseTimeout = test.testCaseTimeout;
            Categories = test.Categories;
            IsTestAssembly = test.IsTestAssembly;
            RunState = (RunState)Enum.Parse(typeof(RunState), test.RunState.ToString());
            Description = test.Description;
            SkipReason = test.SkipReason;
            ParentId = test.ParentId;
            UniqueName = test.UniqueName;
            ParentUniqueName = test.ParentUniqueName;
        }

        internal void ApplyChildren(IEnumerable<TestAdaptor> allTests)
        {
            Children = m_ChildrenIds.Select(id => allTests.First(t => t.Id == id)).ToArray();
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public int TestCaseCount { get; private set; }
        public bool HasChildren { get; private set; }
        public bool IsSuite { get; private set; }
        public IEnumerable<ITestAdaptor> Children { get; private set; }
        public int TestCaseTimeout { get; private set; }
        public ITypeInfo TypeInfo { get; private set; }
        public IMethodInfo Method { get; private set; }
        public string FullPath { get; private set; }
        private string[] m_ChildrenIds;

        public string[] Categories { get; private set; }
        public bool IsTestAssembly { get; private set; }
        public RunState RunState { get; }
        public string Description { get; }
        public string SkipReason { get; }
        public string ParentId { get; }
        public string UniqueName { get; }
        public string ParentUniqueName { get; }

        private static string GetFullPath(ITest test)
        {
            if (test.Parent != null && test.Parent.Parent != null)
                return GetFullPath(test.Parent) + "/" + test.Name;
            return test.Name;
        }
    }
}
