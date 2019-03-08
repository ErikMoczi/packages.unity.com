using System.Collections;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ActionOutsideOfTest
{
    public class ActionTimingTests
    {
        private static int setupTime;
        private static int actionBeforeTime;
        private static int testTime;
        private static int actionAfterTime;
        private static int tearDownTime;

        [SetUp]
        public void Setup()
        {
            setupTime = Time.frameCount;
        }

        [Test, TestActionAttribute]
        public void CheckActionTiming()
        {
            testTime = Time.frameCount;
        }

        [TearDown]
        public void TearDown()
        {
            tearDownTime = Time.frameCount;
            AssertTimes();
        }

        private static void AssertTimes()
        {
            if (testTime != setupTime ||
                testTime != actionBeforeTime ||
                testTime != actionAfterTime ||
                testTime != tearDownTime)
            {
                var output = new StringWriter();
                output.WriteLine("Timing failed. Actions and tests happened at different frames.");
                output.WriteLine("Recorded frame count:");
                output.WriteLine("Setup " + setupTime);
                output.WriteLine("ActionBefore " + actionBeforeTime);
                output.WriteLine("Test " + testTime);
                output.WriteLine("ActionAfter " + actionAfterTime);
                output.WriteLine("TearDown " + tearDownTime);
                Assert.Fail(output.ToString());
            }
        }

        public class TestActionAttribute : NUnitAttribute, ITestAction
        {
            public void BeforeTest(ITest test)
            {
                actionBeforeTime = Time.frameCount;
            }

            public void AfterTest(ITest test)
            {
                actionAfterTime = Time.frameCount;
            }

            public ActionTargets Targets { get { return ActionTargets.Test; } }
        }
    }
}
