using System;
using UnityEngine;

namespace UnityEditor.TestTools.TestRunner.Api
{
    internal class TestRunnerApi : ScriptableObject, ITestRunnerApi
    {
        public void Execute(ExecutionSettings executionSettings = null)
        {
            if (executionSettings == null)
            {
                throw new ArgumentException("Filter for execution is undefined.");
            }

            Debug.Log("Executing tests with settings: " + ExecutionSettingsToString(executionSettings));

            var launcherFactory = new TestLauncherFactory();
            var data = TestRunData.instance;
            data.executionSettings = executionSettings;

            var testLauncher = launcherFactory.GetLauncher(executionSettings);
            testLauncher.Run();
        }

        public void RegisterCallbacks<T>(T testCallbacks) where T : ICallbacks
        {
            if (testCallbacks == null)
            {
                throw new ArgumentException("TestCallbacks for execution is undefined.");
            }

            CallbacksHolder.instance.Add(testCallbacks);
        }

        private static string ExecutionSettingsToString(ExecutionSettings executionSettings)
        {
            if (executionSettings == null)
            {
                return "none";
            }

            if (executionSettings.filter == null)
            {
                return "no filter";
            }

            return "test mode = " + executionSettings.filter.testMode;
        }
    }
}
