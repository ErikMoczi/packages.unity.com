using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestRunner.NUnitExtensions.Runner;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class PassingIntegrationTests
    {
        [SerializeField]
        private int fieldToSerialize;

        [UnityTest]
        public IEnumerator EnteringPlaymode_WillNotBreakTheRunner()
        {
            yield return new EnterPlayMode();
        }

        [UnityTest]
        public IEnumerator EnterAndExitPlaymode_WillNotBreakTheRunner()
        {
            yield return new EnterPlayMode();
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator EnteringPlaymode_WillSerializeObjectState()
        {
            fieldToSerialize = 5;
            yield return new EnterPlayMode();
            Assert.AreEqual(5, fieldToSerialize);
        }

        [UnityTest]
        public IEnumerator EnteringPlaymode_WillNotSerializeLocalState()
        {
            var localVar = 5;
            yield return new EnterPlayMode();
            Assert.AreNotEqual(5, localVar);
        }

        [UnityTest]
        public IEnumerator EnteringPlaymode_CanAssertLog_Before()
        {
            Debug.LogError("error log");
            LogAssert.Expect(LogType.Error, "error log");
            yield return new EnterPlayMode();
        }

        [UnityTest]
        public IEnumerator EnteringPlaymode_CanAssertLog_After()
        {
            yield return new EnterPlayMode();
            Debug.LogError("error log");
            LogAssert.Expect(LogType.Error, "error log");
        }

        string m_EmptyScriptFilePath = "Assets/EmptyCSFile.cs";
        [UnityTest]
        public IEnumerator RecompilingScript_WillNotBreakTheRunner()
        {
            if (File.Exists(m_EmptyScriptFilePath))
                AssetDatabase.DeleteAsset(m_EmptyScriptFilePath);
            else
                File.CreateText(m_EmptyScriptFilePath).Close();

            yield return new RecompileScripts();
        }

        [Ignore("This test is incomplete - something needs to trigger a sync compilation while we WaitForDomainReload otherwise it fails")]
        [UnityTest]
        public IEnumerator WaitingForDomainReload_WhenCompilingSynchronously_WillNotBreakTheRunner()
        {
            if (File.Exists(m_EmptyScriptFilePath))
                AssetDatabase.DeleteAsset(m_EmptyScriptFilePath);
            else
                File.CreateText(m_EmptyScriptFilePath).Close();

            // Domain reload will happen on next tick
            yield return new WaitForDomainReload();
        }

        [UnityTest]
        public IEnumerator WaitingForDomainReload_WhenCompilingAsynchronously_WillNotBreakTheRunner()
        {
            if (File.Exists(m_EmptyScriptFilePath))
                AssetDatabase.DeleteAsset(m_EmptyScriptFilePath);
            else
                File.CreateText(m_EmptyScriptFilePath).Close();

            AssetDatabase.Refresh();
            yield return new WaitForDomainReload();
        }

        [OneTimeTearDown]
        public void RemoveEmptyScriptFile()
        {
            if (!EditorApplication.isCompiling && File.Exists(m_EmptyScriptFilePath))
                AssetDatabase.DeleteAsset(m_EmptyScriptFilePath);
        }

        [UnityTest]
        public IEnumerator TestOutputIsSerialized()
        {
            Debug.Log("Some output");
            yield return new EnterPlayMode();
            StringAssert.Contains("Some output", UnityTestExecutionContext.CurrentContext.CurrentResult.Output);
        }
    }
}
