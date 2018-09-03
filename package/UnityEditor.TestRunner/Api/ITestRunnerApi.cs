namespace UnityEditor.TestTools.TestRunner.Api
{
    interface ITestRunnerApi
    {
        void Execute(ExecutionSettings executionSettings = null);
        void RegisterCallbacks<T>(T testCallbacks, int priority = 0) where T : ICallbacks;
    }
}
