using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEditor;
using UnityEditor.TestTools.TestRunner;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class TestSettingsTests
    {
        [Test]
        public void TestSettings_AppliesSetSettings()
        {
            var settingsUnderTest = new TestSettings();
            settingsUnderTest.scriptingBackend = ScriptingImplementation.IL2CPP;

            settingsUnderTest.SetupProjectParameters();

            var scriptingBackend = PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.activeBuildTargetGroup);
            Assert.AreEqual(ScriptingImplementation.IL2CPP, scriptingBackend, "Incorrect scripting backend.");
        }

        [Test]
        public void TestSettings_DoesNotApplySettings()
        {
            PlayerSettings.SetScriptingBackend(EditorUserBuildSettings.activeBuildTargetGroup, ScriptingImplementation.Mono2x);
            var settingsUnderTest = new TestSettings();

            settingsUnderTest.SetupProjectParameters();

            var scriptingBackend = PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.activeBuildTargetGroup);
            Assert.AreEqual(ScriptingImplementation.Mono2x, scriptingBackend, "Incorrect scripting backend.");
        }

        [Test]
        public void TestSettings_CleanupSettings()
        {
            PlayerSettings.SetScriptingBackend(EditorUserBuildSettings.activeBuildTargetGroup, ScriptingImplementation.Mono2x);
            var settingsUnderTest = new TestSettings();
            settingsUnderTest.scriptingBackend = ScriptingImplementation.IL2CPP;

            settingsUnderTest.SetupProjectParameters();
            settingsUnderTest.Dispose();

            var scriptingBackend = PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.activeBuildTargetGroup);
            Assert.AreEqual(ScriptingImplementation.Mono2x, scriptingBackend, "Incorrect scripting backend.");
        }
    }
}
