using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UnityEditor.TestTools.TestRunner.UnityTestProtocol
{
    internal class UnityTestProtocolListener : ScriptableObject, ICallbacks
    {
        private IUtpMessageReporter m_UtpMessageReporter;

        public UnityTestProtocolListener()
        {
            m_UtpMessageReporter = new UtpMessageReporter(new UtpDebugLogger());
        }

        public void RunStarted(ITest testsToRun)
        {
            m_UtpMessageReporter.ReportTestRunStarted(testsToRun);
        }

        public void RunFinished(ITestResult testResults)
        {
            // Apparently does nothing :)
        }

        public void TestStarted(ITest test)
        {
            m_UtpMessageReporter.ReportTestStarted(test);
        }

        public void TestFinished(ITestResult result)
        {
            m_UtpMessageReporter.ReportTestFinished(result);
        }
    }
}
