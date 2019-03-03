using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ActionOutsideOfTest
{
    public class ActionOrderUnityTest : ActionOrderTestBase, IPostBuildCleanup
    {
        public static string s_TempDirPath = "Assets/TempFiles";

        [UnityTest, OuterTest, TestActionOnTest, AssertLogInOuterTest]
        public IEnumerator CheckForSetupAndTeardown()
        {
            Log("Test part 1");
            if (!Directory.Exists(s_TempDirPath))
            {
                Directory.CreateDirectory(s_TempDirPath);
            }

            var file = File.CreateText(Path.Combine(s_TempDirPath, Guid.NewGuid() + ".cs"));
            file.Close();
            yield return new RecompileScripts();
            Log("Test part 2");
        }

        public void Cleanup()
        {
            if (Directory.Exists(s_TempDirPath))
            {
                foreach (var file in Directory.GetFiles(s_TempDirPath))
                {
                    File.Delete(file);
                }
                Directory.Delete(s_TempDirPath, true);
            }
        }
    }


    [TestActionOnSuite]
    public abstract class ActionOrderTestBase : ActionOrderBaseForInheritanceTest, IPrebuildSetup
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Log("OneTimeSetUp");
        }

        [SetUp]
        public void SetUp()
        {
            Log("SetUp");
        }

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            Log("UnitySetup");
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Log("TearDown");
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            Log("UnityTearDown");
            yield return null;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Log("OneTimeTearDown");
        }

        [OneTimeTearDown]
        public void AssertResult()
        {
            AssertLog(true);
        }

        public class OuterTestAttribute : NUnitAttribute, IOuterUnityTestAction
        {
            public IEnumerator BeforeTest(ITest test)
            {
                Log("OuterTestAttribute BeforeTest");
                yield return null;
            }

            public IEnumerator AfterTest(ITest test)
            {
                Log("OuterTestAttribute AfterTest");
                yield return null;
            }
        }


        public class TestActionOnTestAttribute : NUnitAttribute, ITestAction
        {
            public void BeforeTest(ITest test)
            {
                Log("TestAction OnTest BeforeTest");
            }

            public void AfterTest(ITest test)
            {
                Log("TestAction OnTest AfterTest");
            }

            public ActionTargets Targets { get { return ActionTargets.Test; } }
        }

        public class TestActionOnSuiteAttribute : NUnitAttribute, ITestAction
        {
            public void BeforeTest(ITest test)
            {
                Log("TestAction OnSuite BeforeTest");
            }

            public void AfterTest(ITest test)
            {
                Log("TestAction OnSuite AfterTest");
            }

            public ActionTargets Targets { get { return ActionTargets.Suite; } }
        }

        private static void AssertLog(bool afterTest = false)
        {
            var expectedLog = new[]
            {
                "OneTimeSetUp Base",
                "OneTimeSetUp",
                "TestAction OnSuite BeforeTest",
                "OuterTestAttribute BeforeTest",
                "UnitySetup Base",
                "UnitySetup",
                "SetUp Base",
                "SetUp",
                "TestAction OnTest BeforeTest",
                "Test part 1",
                "OneTimeSetUp Base",
                "OneTimeSetUp",
                "TestAction OnSuite BeforeTest",
                "SetUp Base",
                "SetUp",
                "TestAction OnTest BeforeTest",
                "Test part 2",
                "TestAction OnTest AfterTest",
                "TearDown Base",
                "TearDown",
                "UnityTearDown Base",
                "UnityTearDown",
                "OuterTestAttribute AfterTest"
                // OneTimeTearDown cannot be validated, because the test is done at that point.
            };

            if (afterTest)
            {
                var list = expectedLog.ToList();
                list.Add("TestAction OnSuite AfterTest");
                expectedLog = list.ToArray();
            }

            var output = new StringWriter();
            output.WriteLine("Expected:                     Actual:");
            var actualLog = LogHolder.instance.Log.ToArray();
            var passed = true;
            for (int i = 0; i < Math.Max(expectedLog.Length, actualLog.Length); i++)
            {
                var expected = "null";
                if (i < expectedLog.Length)
                {
                    expected = expectedLog[i];
                }
                var actual = "null";
                if (i < actualLog.Length)
                {
                    actual = actualLog[i];
                }

                output.Write(expected);
                output.Write(new string(' ', Math.Max(29 - expectedLog[i].Length, 0)));
                if (expected == actual)
                {
                    output.Write(" == ");
                }
                else
                {
                    passed = false;
                    output.Write(" != ");
                }

                output.WriteLine(actual);
            }

            if (!passed)
            {
                Assert.Fail("Actual action order did not match the expected: \n" + output);
            }
        }

        public class AssertLogInOuterTestAttribute : NUnitAttribute, IOuterUnityTestAction
        {
            public IEnumerator BeforeTest(ITest test)
            {
                yield return null;
            }

            public IEnumerator AfterTest(ITest test)
            {
                AssertLog();
                yield return null;
            }
        }

        public void Setup()
        {
            LogHolder.instance.Log = new List<string>();
        }
    }

    public abstract class ActionOrderBaseForInheritanceTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Log("OneTimeSetUp Base");
        }

        [SetUp]
        public void SetUp()
        {
            Log("SetUp Base");
        }

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            Log("UnitySetup Base");
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Log("TearDown Base");
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            Log("UnityTearDown Base");
            yield return null;
        }

        public static void Log(string entry)
        {
            LogHolder.instance.Log.Add(entry);
        }

        public class LogHolder : ScriptableSingleton<LogHolder>
        {
            public List<string> Log;
        }
    }
}
