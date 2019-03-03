using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.TestTools.TestRunner;
using UnityEngine;
using UnityEngine.TestTools;

namespace Assets.editor
{
    public class TestSettingsDeserializerTests : IPostBuildCleanup
    {
        const string filePath = ".\\Test.json";

        [Test]
        public void TestSettingsDeserializer_DeserializesCorrectSettingsFromFile()
        {
            var fileContent =
@"{
                ""scriptingBackend"": ""WinRTDotNET"",
                ""architecture"": ""X86"",
                ""useLatestScriptingRuntimeVersion"": true,
            }";
            File.WriteAllText(filePath, fileContent);
            var deserializerUnderTest = new TestSettingsDeserializer(() => new TestSettingsMock());

            var settings = deserializerUnderTest.GetSettingsFromJsonFile(filePath);

            Assert.AreEqual(ScriptingImplementation.WinRTDotNET, settings.scriptingBackend, "Incorrect scriptingBackend value");
            Assert.AreEqual("X86", settings.Architecture, "Incorrect Architecture value");
            Assert.AreEqual(true, settings.useLatestScriptingRuntimeVersion, "Incorrect useLatestScriptingRuntimeVersion value");
        }

        [Test]
        public void TestSettingsDeserializer_IgnoresCasingInValues()
        {
            var fileContent =
@"{
                ""scriptingBackend"": ""winrtdotnet"",
            }";
            File.WriteAllText(filePath, fileContent);
            var deserializerUnderTest = new TestSettingsDeserializer(() => new TestSettingsMock());

            var settings = deserializerUnderTest.GetSettingsFromJsonFile(filePath);

            Assert.AreEqual(ScriptingImplementation.WinRTDotNET, settings.scriptingBackend, "Incorrect scriptingBackend value");
        }

        [Test]
        public void TestSettingsDeserializer_LogFailedCastOfValues()
        {
            var fileContent =
@"{
                ""scriptingBackend"": ""wrongValue"",
                ""useLatestScriptingRuntimeVersion"": 42,
            }";
            File.WriteAllText(filePath, fileContent);
            var deserializerUnderTest = new TestSettingsDeserializer(() => new TestSettingsMock());

            deserializerUnderTest.GetSettingsFromJsonFile(filePath);

            LogAssert.Expect(LogType.Log, "Could not convert 'scriptingBackend' argument 'wrongValue' to a valid ScriptingImplementation. Accepted values: Mono2x, IL2CPP, WinRTDotNET.");
            LogAssert.Expect(LogType.Log, "Could not convert 'useLatestScriptingRuntimeVersion' argument '42' to a valid Boolean.");
        }

        public void Cleanup()
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            if (File.Exists(filePath + ".meta"))
            {
                File.Delete(filePath + ".meta");
            }
        }

        private class TestSettingsMock : ITestSettings
        {
            public void Dispose()
            {
                throw new System.NotImplementedException();
            }

            public void SetupProjectParameters()
            {
                throw new System.NotImplementedException();
            }

            public ScriptingImplementation? scriptingBackend { get; set; }
            public string Architecture { get; set; }
            public ApiCompatibilityLevel? apiProfile { get; set; }
            public bool? useLatestScriptingRuntimeVersion { get; set; }
            public bool? appleEnableAutomaticSigning { get; set; }
            public string appleDeveloperTeamID { get; set; }
            public ProvisioningProfileType? iOSManualProvisioningProfileType { get; set; }
            public string iOSManualProvisioningProfileID { get; set; }
            public ProvisioningProfileType? tvOSManualProvisioningProfileType { get; set; }
            public string tvOSManualProvisioningProfileID { get; set; }
        }
    }
}
