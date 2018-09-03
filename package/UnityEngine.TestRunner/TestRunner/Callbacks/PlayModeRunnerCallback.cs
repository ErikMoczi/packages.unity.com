using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestTools.TestRunner.Callbacks
{
    internal class PlayModeRunnerCallback : MonoBehaviour, ITestRunnerListener
    {
        public void RunFinished(ITestResult testResults)
        {
            Application.logMessageReceivedThreaded -= LogRecieved;
        }

        public void TestFinished(ITestResult result)
        {
        }

        public void RunStarted(ITest testsToRun)
        {
            Application.logMessageReceivedThreaded += LogRecieved;
        }

        public void TestStarted(ITest test)
        {
        }

        private void LogRecieved(string message, string stacktrace, LogType type)
        {
            if (TestContext.Out != null)
                TestContext.Out.WriteLine(message);
        }
    }
}
