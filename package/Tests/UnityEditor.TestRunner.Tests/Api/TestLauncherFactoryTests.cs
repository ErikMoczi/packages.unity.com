using NUnit.Framework;
using UnityEditor;
using UnityEditor.TestTools.TestRunner;
using UnityEditor.TestTools.TestRunner.Api;

public class TestLauncherFactoryTests
{
    [Test]
    public void TestLauncherFactoryWithEdimodeProducesEditModeLauncher()
    {
        TestLauncherFactory factoryUnderTest = new TestLauncherFactory();

        var testRunnerApiExecutionSettings = new ExecutionSettings()
        {
            filter = new Filter()
            {
                testMode = TestMode.EditMode
            }
        };

        var launcher = factoryUnderTest.GetLauncher(testRunnerApiExecutionSettings);

        Assert.AreEqual(typeof(EditModeLauncher), launcher.GetType());
    }

    [Test]
    public void TestLauncherFactoryWithPlaymodeProducesPlaymodeLauncher()
    {
        TestLauncherFactory factoryUnderTest = new TestLauncherFactory();

        var testRunnerApiExecutionSettings = new ExecutionSettings()
        {
            filter = new Filter()
            {
                testMode = TestMode.PlayMode
            }
        };
        PlayerSettings.runPlayModeTestAsEditModeTest = false;

        var launcher = factoryUnderTest.GetLauncher(testRunnerApiExecutionSettings);

        Assert.AreEqual(typeof(PlaymodeLauncher), launcher.GetType());
    }

    [Test]
    public void TestLauncherFactoryWithPlaymodeAsEditmodeProducesEditmodeLauncher()
    {
        TestLauncherFactory factoryUnderTest = new TestLauncherFactory();
        var testRunnerApiExecutionSettings = new ExecutionSettings()
        {
            filter = new Filter()
            {
                testMode = TestMode.PlayMode
            }
        };
        PlayerSettings.runPlayModeTestAsEditModeTest = true;

        var launcher = factoryUnderTest.GetLauncher(testRunnerApiExecutionSettings);

        Assert.AreEqual(typeof(EditModeLauncher), launcher.GetType());
    }

    [Test]
    public void TestLauncherFactoryWithTargetPlatformProducesPlayerLauncher()
    {
        TestLauncherFactory factoryUnderTest = new TestLauncherFactory();
        var testRunnerApiExecutionSettings = new ExecutionSettings()
        {
            filter = new Filter()
            {
                testMode = TestMode.PlayMode
            },
            targetPlatform = BuildTarget.Android
        };

        var launcher = factoryUnderTest.GetLauncher(testRunnerApiExecutionSettings);

        Assert.AreEqual(typeof(PlayerLauncher), launcher.GetType());
    }
}
