namespace UnityEditor.TestTools.TestRunner.Api
{
    internal interface ICallbacks
    {
        void RunStarted(ITest testsToRun);
        void RunFinished(ITestResult result);
        void TestStarted(ITest test);
        void TestFinished(ITestResult result);
    }
}
