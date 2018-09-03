using System;
using System.Linq;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UnityEditor.TestTools.TestRunner.CommandLineTest
{
    internal class Executer
    {
        private ITestRunnerApi m_TestRunnerApi;
        private ISettingsBuilder m_SettingsBuilder;
        private Action<string, object[]> m_LogErrorFormat;
        private Action<Exception> m_LogException;
        private Action<int> m_ExitEditorApplication;
        private Func<bool> m_ScriptCompilationFailedCheck;

        public Executer(ITestRunnerApi testRunnerApi, ISettingsBuilder settingsBuilder, Action<string, object[]> logErrorFormat, Action<Exception> logException, Action<int> exitEditorApplication, Func<bool> scriptCompilationFailedCheck)
        {
            m_TestRunnerApi = testRunnerApi;
            m_SettingsBuilder = settingsBuilder;
            m_LogErrorFormat = logErrorFormat;
            m_LogException = logException;
            m_ExitEditorApplication = exitEditorApplication;
            m_ScriptCompilationFailedCheck = scriptCompilationFailedCheck;
        }

        internal void InitializeAndExecuteRun(string[] commandLineArgs)
        {
            Api.ExecutionSettings executionSettings;
            try
            {
                executionSettings = m_SettingsBuilder.BuildApiExecutionSettings(commandLineArgs);
            }
            catch (SetupException exception)
            {
                HandleSetupException(exception);
                return;
            }

            try
            {
                m_TestRunnerApi.Execute(executionSettings);
            }
            catch (Exception exception)
            {
                m_LogException(exception);
                m_ExitEditorApplication((int)ReturnCodes.RunError);
            }
        }

        internal ExecutionSettings BuildExecutionSettings(string[] commandLineArgs)
        {
            return m_SettingsBuilder.BuildExecutionSettings(commandLineArgs);
        }

        internal enum ReturnCodes
        {
            Ok = 0,
            Failed = 2,
            RunError = 3,
            PlatformNotFoundReturnCode = 4
        }

        internal void SetUpCallbacks(ExecutionSettings executionSettings)
        {
            var resultSavingCallback = ScriptableObject.CreateInstance<ResultsSavingCallbacks>();
            resultSavingCallback.m_ResultFilePath = executionSettings.TestResultsFile;


            m_TestRunnerApi.RegisterCallbacks(resultSavingCallback);
            m_TestRunnerApi.RegisterCallbacks(ScriptableObject.CreateInstance<ExitCallbacks>());
            var timeoutCallbacks = ScriptableObject.CreateInstance<TimeoutCallbacks>();
            timeoutCallbacks.Init((action, time) => new DelayedCallback(action, time), m_LogErrorFormat, m_ExitEditorApplication);
            m_TestRunnerApi.RegisterCallbacks(timeoutCallbacks);
        }

        internal void ExitOnCompileErrors()
        {
            if (m_ScriptCompilationFailedCheck())
            {
                var handling = s_ExceptionHandlingMapping.First(h => h.m_ExceptionType == SetupException.ExceptionType.ScriptCompilationFailed);
                m_LogErrorFormat(handling.m_Message, new object[0]);
                m_ExitEditorApplication(handling.m_ReturnCode);
            }
        }

        void HandleSetupException(SetupException exception)
        {
            ExceptionHandling handling = s_ExceptionHandlingMapping.FirstOrDefault(h => h.m_ExceptionType == exception.Type) ?? new ExceptionHandling(exception.Type, "Unknown command line test run error. " + exception.Type, ReturnCodes.RunError);
            m_LogErrorFormat(handling.m_Message, exception.Details);
            m_ExitEditorApplication(handling.m_ReturnCode);
        }

        private class ExceptionHandling
        {
            internal SetupException.ExceptionType m_ExceptionType;
            internal string m_Message;
            internal int m_ReturnCode;
            public ExceptionHandling(SetupException.ExceptionType exceptionType, string message, ReturnCodes returnCode)
            {
                m_ExceptionType = exceptionType;
                m_Message = message;
                m_ReturnCode = (int)returnCode;
            }
        }

        static ExceptionHandling[] s_ExceptionHandlingMapping = new[]
        {
            new ExceptionHandling(SetupException.ExceptionType.ScriptCompilationFailed, "Scripts had compilation errors.", ReturnCodes.RunError),
            new ExceptionHandling(SetupException.ExceptionType.PlatformNotFound, "Test platform not found ({0}).", ReturnCodes.PlatformNotFoundReturnCode),
            new ExceptionHandling(SetupException.ExceptionType.TestSettingsFileNotFound, "Test settings file not found at {0}.", ReturnCodes.RunError)
        };
    }
}
