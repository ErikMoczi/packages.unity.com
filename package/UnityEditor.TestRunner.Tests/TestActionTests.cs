using System;
using System.Collections;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestTools;

namespace FrameworkTests.CustomRunner
{
    public class TestActionTests
    {
        [SerializeField] internal static string log;

        [SetUp]
        public void SetUp()
        {
            log += "SetUp\n";
        }

        [TearDown]
        public void TearDown()
        {
            log += "TearDown\n";
            AssertLog();
        }

        private void AssertLog()
        {
            var splitLog = log.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
            Debug.Log(log);
            Assert.AreEqual("SetUp", splitLog[0]);
            Assert.AreEqual("TestActionBeforeTest", splitLog[1]);
            Assert.AreEqual("Test part 1", splitLog[2]);
            Assert.AreEqual("Test part 2", splitLog[3]);
            Assert.AreEqual("TestActionAfterTest", splitLog[4]);
            Assert.AreEqual("TearDown", splitLog[5]);
            Assert.AreEqual(6, splitLog.Length);
            log = "";
        }

        [Test, TestAction]
        public void TestAction_Test()
        {
            log += "Test part 1\n";
            log += "Test part 2\n";
        }

        [UnityTest, TestAction]
        public IEnumerator TestAction_UnityTest()
        {
            log += "Test part 1\n";
            yield return null;
            log += "Test part 2\n";
        }
    }

    public class TestActionAttribute : NUnitAttribute, ITestAction
    {
        public void BeforeTest(ITest test)
        {
            TestActionTests.log += "TestActionBeforeTest\n";
        }

        public void AfterTest(ITest test)
        {
            TestActionTests.log += "TestActionAfterTest\n";
        }

        public ActionTargets Targets { get; }
    }
}
