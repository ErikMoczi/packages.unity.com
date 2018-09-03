using System;
using System.Linq;
using NUnit.Framework.Interfaces;
using UnityEngine.TestRunner.NUnitExtensions.Runner;

namespace UnityEngine.TestRunner.TestLaunchers
{
    [Serializable]
    internal class RemoteTestResultDataWithTestData
    {
        public RemoteTestResultData[] results;
        public RemoteTestData[] tests;

        private RemoteTestResultDataWithTestData()
        {
        }

        internal static RemoteTestResultDataWithTestData FromTestResult(ITestResult result)
        {
            var tests = RemoteTestData.GetTestDataList(result.Test);
            tests.First().testCaseTimeout = UnityTestExecutionContext.CurrentContext.TestCaseTimeout;
            return new RemoteTestResultDataWithTestData()
            {
                results = RemoteTestResultData.GetTestResultDataList(result),
                tests = tests
            };
        }

        internal static RemoteTestResultDataWithTestData FromTest(ITest test)
        {
            var tests = RemoteTestData.GetTestDataList(test);
            tests.First().testCaseTimeout = UnityTestExecutionContext.CurrentContext.TestCaseTimeout;
            return new RemoteTestResultDataWithTestData()
            {
                tests = tests
            };
        }
    }
}
