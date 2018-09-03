using System;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UnityEditor.TestTools.TestRunner.CommandLineTest
{
    [Serializable]
    internal class ExitCallbacks : ScriptableObject, ICallbacks
    {
        private bool m_AnyTestsExecuted;
        private bool m_RunFailed;


        public void RunFinished(ITestResult testResults)
        {
            if (!m_AnyTestsExecuted)
            {
                Debug.LogWarning("No tests were executed");
            }
            EditorApplication.Exit(m_RunFailed ? (int)Executer.ReturnCodes.Failed : (int)Executer.ReturnCodes.Ok);
        }

        public void TestStarted(ITest test)
        {
            if (!test.IsSuite)
            {
                m_AnyTestsExecuted = true;
            }
        }

        public void TestFinished(ITestResult result)
        {
            if (!result.Test.IsSuite && (result.TestStatus == TestStatus.Failed))
            {
                m_RunFailed = true;
            }
        }

        public void RunStarted(ITest testsToRun)
        {
        }
    }
}
