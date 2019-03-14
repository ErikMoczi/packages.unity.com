using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Castle.Components.DictionaryAdapter;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor.TestTools.TestRunner.CommandLineTest;
using ApiExecutionSettings = UnityEditor.TestTools.TestRunner.Api.ExecutionSettings;
using CmdExecutionSettings = UnityEditor.TestTools.TestRunner.CommandLineTest.ExecutionSettings;

public class ExecutorTests
{
    ApiMock m_ApiMock;
    ApiExecutionSettings m_ApiExecutionSettings;
    CmdExecutionSettings m_CmdExecutionSettings;
    ApiSettingsBuilder m_ApiSettingsBuilder;
    Executer m_ExecuterUnderTest;
    string[] m_CommandLineArgs;
    List<string> m_errors;
    List<Exception> m_Exceptions;
    int? m_ExitCall;
    bool m_CompilationFailed;

    [SetUp]
    public void Setup()
    {
        m_CommandLineArgs = new[] { "x" };
        m_ApiMock = new ApiMock();
        m_ApiExecutionSettings = new ApiExecutionSettings();
        m_CmdExecutionSettings = new CmdExecutionSettings();
        m_CmdExecutionSettings.TestResultsFile = "path";
        m_ApiSettingsBuilder = new ApiSettingsBuilder(m_ApiExecutionSettings, m_CmdExecutionSettings);
        m_errors = new List<string>();
        m_Exceptions = new List<Exception>();
        m_ExitCall = null;
        m_ExecuterUnderTest = new Executer(m_ApiMock, m_ApiSettingsBuilder, (error, args) => m_errors.Add(string.Format(error, args)), (exception) => m_Exceptions.Add(exception), (exit) => m_ExitCall = exit, () => m_CompilationFailed);
    }

    [Test]
    public void ExecutorInitializesAndExecutes()
    {
        m_ExecuterUnderTest.InitializeAndExecuteRun(m_CommandLineArgs);

        Assert.AreEqual(1, m_ApiSettingsBuilder.buildApiSettingsInvocations);
        Assert.AreEqual(0, m_ApiSettingsBuilder.buildExecutionSettingsInvocations);
        Assert.AreEqual(m_CommandLineArgs, m_ApiSettingsBuilder.providedCommandLineArgs);
        Assert.AreEqual(1, m_ApiMock.ExecuteInvoked);
        Assert.AreEqual(m_ApiExecutionSettings, m_ApiMock.usedExecutionSettings);
        Assert.AreEqual(0, m_ApiMock.registeredCallbacks.Count);
    }

    [Test]
    public void ExecutorBuildsExecutionSettings()
    {
        var executionSettings = m_ExecuterUnderTest.BuildExecutionSettings(m_CommandLineArgs);
        Assert.AreEqual(1, m_ApiSettingsBuilder.buildExecutionSettingsInvocations);
        Assert.AreEqual(m_CmdExecutionSettings, executionSettings);
    }

    [Test]
    public void ExecutorHandlesScriptCompilationException()
    {
        m_ApiSettingsBuilder.exceptionToThrow = new SetupException(SetupException.ExceptionType.ScriptCompilationFailed);

        m_ExecuterUnderTest.InitializeAndExecuteRun(m_CommandLineArgs);

        Assert.AreEqual(1, m_ApiSettingsBuilder.buildApiSettingsInvocations);
        Assert.AreEqual(0, m_ApiMock.ExecuteInvoked);
        Assert.AreEqual(1, m_errors.Count);
        Assert.AreEqual("Scripts had compilation errors.", m_errors.Single());
        Assert.AreEqual(3, m_ExitCall);
    }

    [Test]
    public void ExecutorHandlesPlatformNotFoundException()
    {
        m_ApiSettingsBuilder.exceptionToThrow = new SetupException(SetupException.ExceptionType.PlatformNotFound, "Android");

        m_ExecuterUnderTest.InitializeAndExecuteRun(m_CommandLineArgs);

        Assert.AreEqual(1, m_errors.Count);
        Assert.AreEqual("Test platform not found (Android).", m_errors.Single());
        Assert.AreEqual(4, m_ExitCall);
    }

    [Test]
    public void ExecutorHandlesTestSettingsNotFoundException()
    {
        m_ApiSettingsBuilder.exceptionToThrow = new SetupException(SetupException.ExceptionType.TestSettingsFileNotFound, "c:\\file.json");

        m_ExecuterUnderTest.InitializeAndExecuteRun(m_CommandLineArgs);

        Assert.AreEqual(1, m_errors.Count);
        Assert.AreEqual("Test settings file not found at c:\\file.json.", m_errors.Single());
        Assert.AreEqual(3, m_ExitCall);
    }

    [Test]
    public void ExecutorSetsUpCallsbacks()
    {
        var expectedPath = "thePath";
        var executionSettings = new CmdExecutionSettings()
        {
            TestResultsFile = expectedPath
        };

        m_ExecuterUnderTest.SetUpCallbacks(executionSettings);

        Assert.AreEqual(4, m_ApiMock.registeredCallbacks.Count);
        var xmlSavingCallback = m_ApiMock.registeredCallbacks.OfType<ResultsSavingCallbacks>().Single();
        Assert.AreEqual(expectedPath, xmlSavingCallback.m_ResultFilePath);
    }

    [Test]
    public void ExecutorExitsOnCompilationError()
    {
        m_CompilationFailed = true;
        m_ExecuterUnderTest.ExitOnCompileErrors();

        Assert.AreEqual(0, m_ApiSettingsBuilder.buildApiSettingsInvocations);
        Assert.AreEqual(0, m_ApiMock.ExecuteInvoked);
        Assert.AreEqual(1, m_errors.Count);
        Assert.AreEqual("Scripts had compilation errors.", m_errors.Single());
        Assert.AreEqual(3, m_ExitCall);
    }

    private class ApiMock : ITestRunnerApi
    {
        public int ExecuteInvoked;
        public ApiExecutionSettings usedExecutionSettings;
        public List<ICallbacks> registeredCallbacks = new List<ICallbacks>();
        public void Execute(ApiExecutionSettings executionSettings = null)
        {
            ExecuteInvoked++;
            usedExecutionSettings = executionSettings;
        }

        public void RegisterCallbacks<T>(T testCallbacks, int priority = 0) where T : ICallbacks
        {
            registeredCallbacks.Add(testCallbacks);
        }

        public void UnregisterCallbacks<T>(T testCallbacks) where T : ICallbacks
        {
            throw new NotImplementedException();
        }

        public void RetrieveTestList(ApiExecutionSettings executionSettings, Action<ITestAdaptor> callback)
        {
            throw new NotImplementedException();
        }
    }

    private class ApiSettingsBuilder : ISettingsBuilder
    {
        public SetupException exceptionToThrow;
        public int buildApiSettingsInvocations;
        public int buildExecutionSettingsInvocations;
        public string[] providedCommandLineArgs;

        private ApiExecutionSettings m_ApiSettingsToProvide;
        private CmdExecutionSettings m_ExecutionSettingsToProvide;

        public ApiSettingsBuilder(ApiExecutionSettings apiSettingsToProvide, CmdExecutionSettings executionSettingsToProvide)
        {
            m_ApiSettingsToProvide = apiSettingsToProvide;
            m_ExecutionSettingsToProvide = executionSettingsToProvide;
        }

        public ApiExecutionSettings BuildApiExecutionSettings(string[] commandLineArgs)
        {
            buildApiSettingsInvocations++;
            providedCommandLineArgs = commandLineArgs;

            if (exceptionToThrow != null)
            {
                throw exceptionToThrow;
            }

            return m_ApiSettingsToProvide;
        }

        public CmdExecutionSettings BuildExecutionSettings(string[] commandLineArgs)
        {
            buildExecutionSettingsInvocations++;
            providedCommandLineArgs = commandLineArgs;
            return m_ExecutionSettingsToProvide;
        }
    }
}
