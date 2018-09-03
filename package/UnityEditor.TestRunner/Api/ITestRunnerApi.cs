namespace UnityEditor.TestTools.TestRunner.Api
{
    interface ITestRunnerApi
    {
        void Execute(ExecutionSettings executionSettings = null);
        void RegisterCallbacks<T>(T testCallbacks) where T : ICallbacks;
    }
}
