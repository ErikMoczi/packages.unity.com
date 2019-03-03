using System;
using System.Collections;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEditor;
using UnityEngine.TestRunner.NUnitExtensions.Runner;
using UnityEngine.TestTools;

namespace ActionOutsideOfTest
{
    public class DomainReloadOutsideOfTest : IPostBuildCleanup, IPrebuildSetup
    {
        public static bool DomainReloaded = true;
        public static void Log(string prefix, string action)
        {
            if (!string.IsNullOrEmpty(SavedData.instance.LogText))
            {
                SavedData.instance.LogText += "\n";
            }

            SavedData.instance.LogText += prefix + " " + action;
        }

        private static string s_TempDirPath = "Assets/TempFiles";
        public static void PerformAction(string prefix)
        {
            DomainReloaded = false;

            Log(prefix, "A");
            if (!Directory.Exists(s_TempDirPath))
            {
                Directory.CreateDirectory(s_TempDirPath);
            }

            SavedData.instance.ClassGuid = Guid.NewGuid().ToString().Replace("-", "");
            var file = File.CreateText(Path.Combine(s_TempDirPath, SavedData.instance.ClassGuid + ".cs"));
            file.WriteLine("public class C_" + SavedData.instance.ClassGuid + " { }");
            file.Close();
        }

        public void Setup()
        {
            SavedData.instance.LogText = "";
            SavedData.instance.ClassGuid = "";
        }

        public void Cleanup()
        {
            SavedData.instance.LogText = "";
            SavedData.instance.ClassGuid = "";

            if (Directory.Exists(s_TempDirPath))
            {
                Directory.Delete(s_TempDirPath, true);
            }

            AssetDatabase.Refresh();
            if (Directory.Exists(s_TempDirPath))
            {
                Directory.Delete(s_TempDirPath, true);
            }
        }

        [PostBuildCleanup(typeof(DomainReloadOutsideOfTest))]
        public class FromUnitySetup
        {
            [UnitySetUp]
            public IEnumerator UnitySetup()
            {
                PerformAction("UnitySetUp");
                yield return new RecompileScripts();
                Log("UnitySetUp", "B");
            }

            [Test, CheckLogAfterTest("UnitySetUp")]
            public void Test()
            {
            }

            [UnityTest, CheckLogAfterTest("UnitySetUp")]
            public IEnumerator UnityTest()
            {
                yield return null;
            }
        }

        [PostBuildCleanup(typeof(DomainReloadOutsideOfTest))]
        public class FromUnityTearDown
        {
            [UnityTearDown]
            public IEnumerator UnityTearDown()
            {
                PerformAction("UnityTearDown");
                yield return new RecompileScripts();
                Log("UnityTearDown", "B");
            }

            [Test, CheckLogAfterTest("UnityTearDown")]
            public void Test()
            {
            }

            [UnityTest, CheckLogAfterTest("UnityTearDown")]
            public IEnumerator UnityTest()
            {
                yield return null;
            }
        }

        [PostBuildCleanup(typeof(DomainReloadOutsideOfTest))]
        public class FromOuterUnityTestAction
        {
            [Test, OuterActionBefore, CheckLogAfterTest("OuterActionBefore")]
            public void TestFromBeforeAction()
            {
            }

            [UnityTest, OuterActionBefore, CheckLogAfterTest("OuterActionBefore")]
            public IEnumerator UnityTestFromBeforeAction()
            {
                yield return null;
            }

            [Test, OuterActionAfter, CheckLogAfterTest("OuterActionAfter")]
            public void TestFromAfterAction()
            {
            }

            [UnityTest, OuterActionAfter, CheckLogAfterTest("OuterActionAfter")]
            public IEnumerator UnityTestFromAfterAction()
            {
                yield return null;
            }
        }

        public class OuterActionBeforeAttribute : NUnitAttribute, IOuterUnityTestAction
        {
            public IEnumerator BeforeTest(ITest test)
            {
                PerformAction("OuterActionBefore");
                yield return new RecompileScripts();
                Log("OuterActionBefore", "B");
            }

            public IEnumerator AfterTest(ITest test)
            {
                yield return null;
            }
        }

        public class OuterActionAfterAttribute : NUnitAttribute, IOuterUnityTestAction
        {
            public IEnumerator BeforeTest(ITest test)
            {
                yield return null;
            }

            public IEnumerator AfterTest(ITest test)
            {
                PerformAction("OuterActionAfter");
                yield return new RecompileScripts();
                Log("OuterActionAfter", "B");
            }
        }

        public class CheckLogAfterTestAttribute : NUnitAttribute, IOuterUnityTestAction
        {
            private string m_ExpectedLog;
            public CheckLogAfterTestAttribute(string expectedLog)
            {
                m_ExpectedLog = expectedLog + " A\n" + expectedLog + " B";
            }

            public IEnumerator BeforeTest(ITest test)
            {
                yield return null;
            }

            public IEnumerator AfterTest(ITest test)
            {
                try
                {
                    var log = SavedData.instance.LogText;
                    SavedData.instance.LogText = "";

                    Assert.IsTrue(DomainReloaded, "Domain reload have not happened. Static value has not been reset.");
                    Assert.AreEqual(m_ExpectedLog, log);

                    var assets = AssetDatabase.FindAssets(SavedData.instance.ClassGuid);
                    Assert.IsTrue(assets.Length > 0, "Could not find asset created before reload scripts.");
                }
                catch (Exception ex)
                {
                    UnityTestExecutionContext.CurrentContext.CurrentResult.RecordException(ex);
                }

                yield return null;
            }
        }

        public class SavedData : ScriptableSingleton<SavedData>
        {
            public string LogText;
            public string ClassGuid;
        }
    }
}
