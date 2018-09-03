using System;
using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine.TestRunner.NUnitExtensions;

namespace UnityEngine.TestTools.TestRunner.GUI
{
    [Serializable]
    internal class TestRunnerResult
    {
        public string id;
        public string name;
        public string fullName;
        public ResultStatus resultStatus = ResultStatus.NotRun;
        public float duration;
        public string messages;
        public string output;
        public string stacktrace;
        public bool notRunnable;
        public bool ignoredOrSkipped;
        public string description;
        public bool isSuite;
        public List<string> categories;
        public string parentID;

        //This field is suppose to mark results from before domain reload
        //Such result is outdated because the code might haev changed
        //This field will get reset every time a domain reload happens
        [NonSerialized]
        public bool notOutdated;

        protected Action<TestRunnerResult> m_OnResultUpdate;

        internal TestRunnerResult(ITest test)
        {
            id = GetId(test);

            fullName = test.FullName;
            name = test.Name;
            description = (string)test.Properties.Get(PropertyNames.Description);
            isSuite = test.IsSuite;

            ignoredOrSkipped = test.RunState == RunState.Ignored || test.RunState == RunState.Skipped;
            notRunnable = test.RunState == RunState.NotRunnable;

            if (ignoredOrSkipped)
            {
                if (test.Properties.ContainsKey(PropertyNames.SkipReason))
                    messages = (string)test.Properties.Get(PropertyNames.SkipReason);
            }
            if (notRunnable)
            {
                resultStatus = ResultStatus.Failed;
                if (test.Properties.ContainsKey(PropertyNames.SkipReason))
                    messages = (string)test.Properties.Get(PropertyNames.SkipReason);
            }
            categories = test.GetAllCategoriesFromTest();
            if (test.Parent != null)
                parentID = GetId(test.Parent);
        }

        internal TestRunnerResult(ITestResult testResult) : this(testResult.Test)
        {
            notOutdated = true;

            messages = testResult.Message;
            output = testResult.Output;
            stacktrace = testResult.StackTrace;
            duration = (float)testResult.Duration;
            if (testResult.Test.IsSuite && testResult.ResultState == ResultState.Ignored)
            {
                resultStatus = ResultStatus.Passed;
            }
            else
            {
                resultStatus = ParseNUnitResultStatus(testResult.ResultState.Status);
            }
        }

        public static string GetId(ITest test)
        {
            var id = GetFullName(test);
            if (test.HasChildIndex())
            {
                var index = test.GetChildIndex();
                if (index >= 0)
                    id += index;
            }
            if (test.IsSuite)
            {
                id += "[suite]";
            }
            return id;
        }

        private static string GetFullName(ITest test)
        {
            if (test.TypeInfo == null && (test.Parent == null || test.Parent.TypeInfo == null))
            {
                return "[" + test.FullName + "]";
            }
            var assemblyId = test.TypeInfo == null ? test.Parent.TypeInfo.Assembly.GetName().Name : test.TypeInfo.Assembly.GetName().Name;
            return string.Format("[{0}][{1}]", assemblyId, test.FullName);
        }

        public void Update(TestRunnerResult result)
        {
            if (ReferenceEquals(result, null))
                return;
            resultStatus = result.resultStatus;
            duration = result.duration;
            messages = result.messages;
            output = result.output;
            stacktrace = result.stacktrace;
            ignoredOrSkipped = result.ignoredOrSkipped;
            notRunnable = result.notRunnable;
            description = result.description;
            notOutdated = result.notOutdated;
            if (m_OnResultUpdate != null)
                m_OnResultUpdate(this);
        }

        public void SetResultChangedCallback(Action<TestRunnerResult> resultUpdated)
        {
            m_OnResultUpdate = resultUpdated;
        }

        [Serializable]
        internal enum ResultStatus
        {
            NotRun,
            Passed,
            Failed,
            Inconclusive,
            Skipped
        }

        private static ResultStatus ParseNUnitResultStatus(TestStatus status)
        {
            switch (status)
            {
                case TestStatus.Passed:
                    return ResultStatus.Passed;
                case TestStatus.Failed:
                    return ResultStatus.Failed;
                case TestStatus.Inconclusive:
                    return ResultStatus.Inconclusive;
                case TestStatus.Skipped:
                    return ResultStatus.Skipped;
                default:
                    return ResultStatus.NotRun;
            }
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", name, fullName);
        }

        public void Clear()
        {
            resultStatus = ResultStatus.NotRun;
            if (m_OnResultUpdate != null)
                m_OnResultUpdate(this);
        }
    }
}
