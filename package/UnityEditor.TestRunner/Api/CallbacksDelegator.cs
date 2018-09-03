using System;
using System.Linq;
using System.Text;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine;
using UnityEngine.TestRunner.TestLaunchers;

namespace UnityEditor.TestTools.TestRunner.Api
{
    internal class CallbacksDelegator : ScriptableSingleton<CallbacksDelegator>
    {
        public void RunStarted(NUnit.Framework.Interfaces.ITest testsToRun)
        {
            var testRunnerTestsToRun = new Test(testsToRun);
            TryInvokeAllCallbacks(callbacks => callbacks.RunStarted(testRunnerTestsToRun));
        }

        public void RunFailed(string failureMessage)
        {
            var nunitTestResult = new TestSuiteResult(new TestSuite("test"));
            nunitTestResult.SetResult(ResultState.Error, failureMessage);
            var testResult = new TestResult(nunitTestResult);
            TryInvokeAllCallbacks(callbacks => callbacks.RunFinished(testResult));
        }

        public void RunStartedRemotely(byte[] testsToRunData)
        {
            var testData = Deserialize<RemoteTestResultDataWithTestData>(testsToRunData);
            var testRunnerTestsToRun = new Test(testData.tests.First(), testData.tests);
            TryInvokeAllCallbacks(callbacks => callbacks.RunStarted(testRunnerTestsToRun));
        }

        public void RunFinished(NUnit.Framework.Interfaces.ITestResult testResults)
        {
            var testResult = new TestResult(testResults);
            TryInvokeAllCallbacks(callbacks => callbacks.RunFinished(testResult));
        }

        public void RunFinishedRemotely(byte[] testResultsData)
        {
            var remoteTestResult = Deserialize<RemoteTestResultDataWithTestData>(testResultsData);
            var testResult = new TestResult(remoteTestResult.results.First(), remoteTestResult);
            TryInvokeAllCallbacks(callbacks => callbacks.RunFinished(testResult));
        }

        public void TestStarted(NUnit.Framework.Interfaces.ITest test)
        {
            var testRunnerTest = new Test(test);
            TryInvokeAllCallbacks(callbacks => callbacks.TestStarted(testRunnerTest));
        }

        public void TestStartedRemotely(byte[] testStartedData)
        {
            var testData = Deserialize<RemoteTestResultDataWithTestData>(testStartedData);
            var testRunnerTest = new Test(testData.tests.First(), testData.tests);
            TryInvokeAllCallbacks(callbacks => callbacks.TestStarted(testRunnerTest));
        }

        public void TestFinished(NUnit.Framework.Interfaces.ITestResult result)
        {
            var testResult = new TestResult(result);
            TryInvokeAllCallbacks(callbacks => callbacks.TestFinished(testResult));
        }

        public void TestFinishedRemotely(byte[] testResultsData)
        {
            var remoteTestResult = Deserialize<RemoteTestResultDataWithTestData>(testResultsData);
            var testResult = new TestResult(remoteTestResult.results.First(), remoteTestResult);
            TryInvokeAllCallbacks(callbacks => callbacks.TestFinished(testResult));
        }

        private static void TryInvokeAllCallbacks(Action<ICallbacks> callbackAction)
        {
            foreach (var testRunnerApiCallback in CallbacksHolder.instance.GetAll())
            {
                try
                {
                    callbackAction(testRunnerApiCallback);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private static T Deserialize<T>(byte[] data)
        {
            return JsonUtility.FromJson<T>(Encoding.UTF8.GetString(data));
        }
    }
}
