using NUnit.Framework;
using UnityEditor;
using UnityEditor.TestTools.TestRunner;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace FrameworkTests
{
    public class PlayerLauncherContextSettingsTests
    {
        const string k_AssertMsgPrefix = "PlayerLauncherContextSettings have not correctly reset the following value on dispose: ";

        private EditorBuildSettingsScene m_SetupSceneValue1 = new EditorBuildSettingsScene("x", false);
        private EditorBuildSettingsScene m_SetupSceneValue2 = new EditorBuildSettingsScene("y", true);

        EditorBuildSettingsScene[] originalScenes;
#pragma warning disable 618
        ResolutionDialogSetting originalDisplayResolutionDialog;
#pragma warning restore 618
        bool originalRunInBackground;
        FullScreenMode originalFullScreenMode;
        string originalAotOptions;
        bool originalResizableWindow;
        bool originalSplashScreenShow;
        string originalProductName;
        Lightmapping.GIWorkflowMode originalGiWorkflowMode;

        [SetUp]
        public void Setup()
        {
            originalScenes = EditorBuildSettings.scenes;
#pragma warning disable 618
            originalDisplayResolutionDialog = PlayerSettings.displayResolutionDialog;
#pragma warning restore 618
            originalRunInBackground = PlayerSettings.runInBackground;
            originalFullScreenMode = PlayerSettings.fullScreenMode;
            originalAotOptions = PlayerSettings.aotOptions;
            originalResizableWindow = PlayerSettings.resizableWindow;
            originalSplashScreenShow = PlayerSettings.SplashScreen.show;
            originalProductName = PlayerSettings.productName;
            originalGiWorkflowMode = Lightmapping.giWorkflowMode;

            EditorBuildSettings.scenes = new[] { this.m_SetupSceneValue1, this.m_SetupSceneValue2 };
#pragma warning disable 618
            PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Enabled;
#pragma warning restore 618
            PlayerSettings.runInBackground = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.aotOptions = "testAot";
            PlayerSettings.resizableWindow = false;
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.productName = "testProduct";
            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.Iterative;
        }

        [TearDown]
        public void TearDown()
        {
            EditorBuildSettings.scenes = originalScenes;
#pragma warning disable 618
            PlayerSettings.displayResolutionDialog = originalDisplayResolutionDialog;
#pragma warning restore 618
            PlayerSettings.runInBackground = originalRunInBackground;
            PlayerSettings.fullScreenMode = originalFullScreenMode;
            PlayerSettings.aotOptions = originalAotOptions;
            PlayerSettings.resizableWindow = originalResizableWindow;
            PlayerSettings.SplashScreen.show = originalSplashScreenShow;
            PlayerSettings.productName = originalProductName;
            Lightmapping.giWorkflowMode = originalGiWorkflowMode;
        }

        [Test]
        public void PlayerLauncherContextSettingsTests_TestConstructor_WithoutSettings()
        {
            new PlayerLauncherContextSettings(null);

            Assert.AreEqual(true, PlayerSettings.runInBackground, "Incorrect PlayerSettings.runInBackground value.");
        }

        [Test]
        public void PlayerLauncherContextSettingsTests_TestConstructor_WithSettings()
        {
            settingsSetupCalls = 0;
            settingsDisposeCalls = 0;

            var contextSettings = new PlayerLauncherContextSettings(new SettingsMock());

            Assert.AreEqual(settingsSetupCalls, 1);
            Assert.AreEqual(settingsDisposeCalls, 0);

            contextSettings.Dispose();
        }

        [Test]
        public void PlayerLauncherContextSettingsTests_TestDispose_WithoutSettings()
        {
            var contextSettings = new PlayerLauncherContextSettings(null);

            contextSettings.Dispose();

            Assert.AreEqual(2, EditorBuildSettings.scenes.Length, "Incorrect size of the editorBuildSettings.scenes");
            Assert.AreEqual(this.m_SetupSceneValue1.guid, EditorBuildSettings.scenes[0].guid, "Scene value not reset in dispose.");
            Assert.AreEqual(this.m_SetupSceneValue2.guid, EditorBuildSettings.scenes[1].guid, "Scene value not reset in dispose.");
#pragma warning disable 618
            Assert.AreEqual(ResolutionDialogSetting.Enabled, PlayerSettings.displayResolutionDialog, k_AssertMsgPrefix + "displayResolutionDialog");
#pragma warning restore 618
            Assert.AreEqual(true, PlayerSettings.runInBackground, k_AssertMsgPrefix + "runInBackground");
            Assert.AreEqual(FullScreenMode.FullScreenWindow, PlayerSettings.fullScreenMode, k_AssertMsgPrefix + "fullScreenMode");
            Assert.AreEqual("testAot", PlayerSettings.aotOptions, k_AssertMsgPrefix + "displayResolutionDialog");
            Assert.AreEqual(false, PlayerSettings.resizableWindow, k_AssertMsgPrefix + "resizableWindow");
            Assert.AreEqual(true, PlayerSettings.SplashScreen.show, k_AssertMsgPrefix + "SplashScreen.show");
            Assert.AreEqual("testProduct", PlayerSettings.productName, k_AssertMsgPrefix + "productName");
            Assert.AreEqual(Lightmapping.GIWorkflowMode.Iterative, Lightmapping.giWorkflowMode, k_AssertMsgPrefix + "Lightmapping.giWorkflowMode");
        }

        [Test]
        public void PlayerLauncherContextSettingsTests_TestDispose_WithSettings()
        {
            var contextSettings = new PlayerLauncherContextSettings(new SettingsMock());
            settingsSetupCalls = 0;
            settingsDisposeCalls = 0;

            contextSettings.Dispose();

            Assert.AreEqual(settingsSetupCalls, 0);
            Assert.AreEqual(settingsDisposeCalls, 1);
        }

        private static int settingsDisposeCalls = 0;
        private static int settingsSetupCalls = 0;
        private class SettingsMock : ITestRunSettings
        {
            public void Dispose()
            {
                settingsDisposeCalls++;
            }

            public void Apply()
            {
                settingsSetupCalls++;
            }

            public ApiCompatibilityLevel? apiProfile { get; set; }
        }
    }
}
