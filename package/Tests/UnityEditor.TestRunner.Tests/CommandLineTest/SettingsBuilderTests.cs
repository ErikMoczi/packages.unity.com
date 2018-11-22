using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.TestTools.TestRunner;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor.TestTools.TestRunner.CommandLineTest;

public class SettingsBuilderTests
{
    List<string> m_Logs;
    List<string> m_Warnings;
    string m_ExpectedPlatform;
    string m_ExpectedTestResultsFilePath;
    string[] m_ExpectedTestFilters;
    string[] m_ExpectedTestCatagories;
    string m_ExpectedTestSettingsFilePath;
    TestSettingsMock m_ExpectedTestSettings;
    SettingsDeserializerMock m_TestSettingsDeserializerMock;
    SettingsBuilder m_SettingsBuilderUnderTest;
    bool m_HasScriptCompilationFailed;

    [SetUp]
    public void Setup()
    {
        m_HasScriptCompilationFailed = false;
        m_Logs = new List<string>();
        m_Warnings = new List<string>();
        m_ExpectedPlatform = "Android";
        m_ExpectedTestResultsFilePath = "c:\\result.json";
        m_ExpectedTestFilters = new[] { "filterA", "filterB" };
        m_ExpectedTestCatagories = new[] { "testCatagoryA", "testCatagoryB" };
        m_ExpectedTestSettingsFilePath = "c:\\settings.json";
        m_ExpectedTestSettings = new TestSettingsMock();
        m_TestSettingsDeserializerMock = new SettingsDeserializerMock(m_ExpectedTestSettings);
        m_SettingsBuilderUnderTest = new SettingsBuilder(m_TestSettingsDeserializerMock, log => m_Logs.Add(log), warning => m_Warnings.Add(warning), path => path == m_ExpectedTestSettingsFilePath, () => m_HasScriptCompilationFailed);
    }

    [Test]
    public void SettingsBuilderBuildsExpectedApiTestSettings()
    {
        var arguments = new[]
        {
            "-testPlatform", m_ExpectedPlatform,
            "-testFilter", string.Join(";", m_ExpectedTestFilters),
            "-testCategory", string.Join(";", m_ExpectedTestCatagories),
            "-testSettingsFile", m_ExpectedTestSettingsFilePath
        };

        var settings = m_SettingsBuilderUnderTest.BuildApiExecutionSettings(arguments);

        Assert.AreEqual(m_ExpectedTestSettingsFilePath, m_TestSettingsDeserializerMock.JsonFilePathProvided);
        Assert.AreEqual(BuildTarget.Android, settings.targetPlatform);
        Assert.IsNotNull(settings.filter);
        Assert.AreEqual(m_ExpectedTestFilters, settings.filter.groupNames);
        Assert.AreEqual(m_ExpectedTestCatagories, settings.filter.categoryNames);
        Assert.AreEqual(TestMode.PlayMode, settings.filter.testMode);

        Assert.AreEqual(0, m_ExpectedTestSettings.SetupInvokeCount);
        settings.overloadTestRunSettings.Apply();
        Assert.AreEqual(1, m_ExpectedTestSettings.SetupInvokeCount);
        Assert.AreEqual(0, m_ExpectedTestSettings.DisposeInvokeCount);
        settings.overloadTestRunSettings.Dispose();
        Assert.AreEqual(1, m_ExpectedTestSettings.DisposeInvokeCount);
    }

    [Test]
    public void SettingsBuilderBuildsExpectedTestExecutionSettings()
    {
        var arguments = new[]
        {
            "-testResults", m_ExpectedTestResultsFilePath
        };

        var settings = m_SettingsBuilderUnderTest.BuildExecutionSettings(arguments);

        Assert.AreEqual(m_ExpectedTestResultsFilePath, settings.TestResultsFile);
    }

    [Test]
    public void SettingsBuilderLogsExpectedLogs()
    {
        var arguments = new[]
        {
            "-testPlatform", m_ExpectedPlatform,
            "-testFilter", string.Join(";", m_ExpectedTestFilters),
            "-testCategory", string.Join(";", m_ExpectedTestCatagories),
            "-testSettingsFile", m_ExpectedTestSettingsFilePath
        };

        m_SettingsBuilderUnderTest.BuildApiExecutionSettings(arguments);

        var expectedLogs = new[]
        {
            "Running tests for Android",
            "With test filter: " + string.Join(", ", m_ExpectedTestFilters),
            "With test categories: " + string.Join(", ", m_ExpectedTestCatagories),
            "With test settings file: " + m_ExpectedTestSettingsFilePath
        };

        CollectionAssert.AreEqual(expectedLogs, m_Logs);
        Assert.AreEqual(0, m_Warnings.Count);
    }

    [Test]
    public void SettingsBuilderBuildsExpectedTestSettingsForEditorArgs()
    {
        var arguments = new[]
        {
            "-testPlatform", "EdItMoDe",
            "-editorTestsFilter", string.Join(";", m_ExpectedTestFilters),
            "-editorTestsCategories", string.Join(";", m_ExpectedTestCatagories)
        };

        var settings = m_SettingsBuilderUnderTest.BuildApiExecutionSettings(arguments);

        Assert.IsNotNull(settings.filter);
        Assert.AreEqual(m_ExpectedTestFilters, settings.filter.groupNames);
        Assert.AreEqual(m_ExpectedTestCatagories, settings.filter.categoryNames);
        Assert.AreEqual(TestMode.EditMode, settings.filter.testMode);
        Assert.AreEqual(null, settings.targetPlatform);
    }

    [Test]
    public void SettingsBuilderBuildsExpectedTestSettingsWithNoArgs()
    {
        var settings = m_SettingsBuilderUnderTest.BuildApiExecutionSettings(new string[0]);

        Assert.IsNotNull(settings.filter);
        Assert.AreEqual(null, settings.filter.groupNames);
        Assert.AreEqual(null, settings.filter.categoryNames);
        Assert.AreEqual(TestMode.EditMode, settings.filter.testMode);
        Assert.AreEqual(null, settings.targetPlatform);
    }

    [Test]
    public void SettingsBuilderBuildWithNoArgsGivesExpectedLogs()
    {
        m_SettingsBuilderUnderTest.BuildApiExecutionSettings(new string[0]);

        var expectedLogs = new[]
        {
            "Running tests for EditMode"
        };

        CollectionAssert.AreEqual(expectedLogs, m_Logs);
        Assert.AreEqual(0, m_Warnings.Count);
    }

    [Test]
    public void SettingsBuilderWithQuitArgumentLogsWarning()
    {
        var arguments = new[]
        {
            "-testPlatform", "editmode",
            "-quit",
        };

        m_SettingsBuilderUnderTest.BuildApiExecutionSettings(arguments);

        var expectedWarnings = new[] { "Running tests from command line arguments will not work when \"quit\" is specified." };
        CollectionAssert.AreEqual(expectedWarnings, m_Warnings);
    }

    [Test]
    public void SettingsBuilderBuildsExpectedTestSettingsForPlaymodeInEditor()
    {
        var arguments = new[]
        {
            "-testPlatform", "pLaYmOde"
        };

        var settings = m_SettingsBuilderUnderTest.BuildApiExecutionSettings(arguments);

        Assert.IsNotNull(settings.filter);
        Assert.AreEqual(null, settings.filter.groupNames);
        Assert.AreEqual(null, settings.filter.categoryNames);
        Assert.AreEqual(TestMode.PlayMode, settings.filter.testMode);
        Assert.AreEqual(null, settings.targetPlatform);
    }

    [Test]
    public void SettingsBuilderBuildWithNonExistingSettingsPathThrows()
    {
        var nonExistingPath = "c:\\nonExisting.json";
        var arguments = new[]
        {
            "-testSettingsFile", nonExistingPath
        };

        try
        {
            m_SettingsBuilderUnderTest.BuildApiExecutionSettings(arguments);
            Assert.Fail("Expected to get a " + typeof(SetupException).Name + " exception. No exception was thrown.");
        }
        catch (SetupException exception)
        {
            Assert.AreEqual(SetupException.ExceptionType.TestSettingsFileNotFound, exception.Type);
            CollectionAssert.AreEqual(new[] { nonExistingPath}, exception.Details);
        }
    }

    [Test]
    public void SettingsBuilderBuildWithUnknownPlatformThrows()
    {
        var nonExistingPlatform = "NonExistingPlatform";
        var arguments = new[]
        {
            "-testPlatform", nonExistingPlatform
        };

        try
        {
            m_SettingsBuilderUnderTest.BuildApiExecutionSettings(arguments);
            Assert.Fail("Expected to get a " + typeof(SetupException).Name + " exception. No exception was thrown.");
        }
        catch (SetupException exception)
        {
            Assert.AreEqual(SetupException.ExceptionType.PlatformNotFound, exception.Type);
            CollectionAssert.AreEqual(new[] { nonExistingPlatform}, exception.Details);
        }
    }

    [Test]
    public void SettingsBuilderBuildWithCompilationFailedThrows()
    {
        m_HasScriptCompilationFailed = true;

        try
        {
            m_SettingsBuilderUnderTest.BuildApiExecutionSettings(new string[0]);
            Assert.Fail("Expected to get a " + typeof(SetupException).Name + " exception. No exception was thrown.");
        }
        catch (SetupException exception)
        {
            Assert.AreEqual(SetupException.ExceptionType.ScriptCompilationFailed, exception.Type);
            CollectionAssert.AreEqual(new string[0], exception.Details);
        }
    }

    private class SettingsDeserializerMock : ITestSettingsDeserializer
    {
        private ITestSettings m_TestSettings;
        public SettingsDeserializerMock(ITestSettings testSettings)
        {
            m_TestSettings = testSettings;
        }

        public string JsonFilePathProvided { get; private set; }
        public ITestSettings GetSettingsFromJsonFile(string jsonFilePath)
        {
            JsonFilePathProvided = jsonFilePath;
            return m_TestSettings;
        }
    }

    private class TestSettingsMock : ITestSettings
    {
        public int SetupInvokeCount { get; private set; }
        public int DisposeInvokeCount { get; private set; }
        public void Dispose()
        {
            DisposeInvokeCount++;
        }

        public void SetupProjectParameters()
        {
            SetupInvokeCount++;
        }

        public ScriptingImplementation? scriptingBackend { get; set; }
        public string Architecture { get; set; }
        public bool? useLatestScriptingRuntimeVersion { get; set; }
        public ApiCompatibilityLevel? apiProfile { get; set; }
        public bool? appleEnableAutomaticSigning { get; set; }
        public string appleDeveloperTeamID { get; set; }
        public ProvisioningProfileType? iOSManualProvisioningProfileType { get; set; }
        public string iOSManualProvisioningProfileID { get; set; }
        public ProvisioningProfileType? tvOSManualProvisioningProfileType { get; set; }
        public string tvOSManualProvisioningProfileID { get; set; }
    }
}
