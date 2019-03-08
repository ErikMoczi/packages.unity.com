using System;
using System.Collections;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine.TestRunner.NUnitExtensions.Runner;
using UnityEngine.TestTools;

namespace ActionOutsideOfTest
{
    public class AssumeThatGivesInconclusiveTests
    {
        public static void PerformAction(ResultState expectedState = null, string expectedMessage = "  Expected: True\n  But was:  False\n")
        {
            ExpectedMessage = expectedMessage.Replace("\n", Environment.NewLine);
            ExpectedState = expectedState ?? ResultState.Inconclusive;
            Assume.That(() => false);
        }

        public static bool ExpectTestToRun = true;
        private static string ExpectedMessage;
        private static ResultState ExpectedState;

        public abstract class SetUpTearDownTestBase
        {
            [Test, CheckStateAfterTest]
            public void Test()
            {
                TestHasRun = true;
            }

            [UnityTest, CheckStateAfterTest]
            public IEnumerator UnityTest()
            {
                TestHasRun = true;
                yield return null;
            }
        }

        public class FromSetup : SetUpTearDownTestBase
        {
            [SetUp]
            public void Setup()
            {
                ExpectTestToRun = false;
                PerformAction();
            }
        }

        public class FromUnitySetup : SetUpTearDownTestBase
        {
            [UnitySetUp]
            public IEnumerator UnitySetup()
            {
                ExpectTestToRun = false;
                PerformAction();
                yield return null;
            }
        }

        public class FromUnityTearDown : SetUpTearDownTestBase
        {
            [UnityTearDown]
            public IEnumerator UnityTearDown()
            {
                ExpectTestToRun = true;
                PerformAction();
                yield return null;
            }
        }

        public class FromTearDown : SetUpTearDownTestBase
        {
            [TearDown]
            public void TearDown()
            {
                ExpectTestToRun = true;
                PerformAction(ResultState.Error, "TearDown : NUnit.Framework.InconclusiveException :   Expected: True\n  But was:  False\n");
            }
        }

        public class FromTest
        {
            [Test, CheckStateAfterTest]
            public void Test()
            {
                ExpectTestToRun = false;
                PerformAction();
                TestHasRun = true;
            }

            [UnityTest, CheckStateAfterTest]
            public IEnumerator UnityTest()
            {
                ExpectTestToRun = false;
                PerformAction();
                yield return null;
                TestHasRun = true;
                yield return null;
            }
        }

        public class FromTestAction
        {
            [Test, ActionBefore, CheckStateAfterTest]
            public void TestFromBeforeAction()
            {
                TestHasRun = true;
            }

            [UnityTest, ActionBefore, CheckStateAfterTest]
            public IEnumerator UnityTestFromBeforeAction()
            {
                TestHasRun = true;
                yield return null;
            }

            [Test, ActionAfter, CheckStateAfterTest]
            public void TestFromAfterAction()
            {
                TestHasRun = true;
            }

            [UnityTest, ActionAfter, CheckStateAfterTest]
            public IEnumerator UnityTestFromAfterAction()
            {
                TestHasRun = true;
                yield return null;
            }
        }

        public class FromOuterUnityTestAction
        {
            [Test, OuterActionBefore, CheckStateAfterTest]
            public void TestFromBeforeAction()
            {
                TestHasRun = true;
            }

            [UnityTest, OuterActionBefore, CheckStateAfterTest]
            public IEnumerator UnityTestFromBeforeAction()
            {
                TestHasRun = true;
                yield return null;
            }

            [Test, OuterActionAfter, CheckStateAfterTest]
            public void TestFromAfterAction()
            {
                TestHasRun = true;
            }

            [UnityTest, OuterActionAfter, CheckStateAfterTest]
            public IEnumerator UnityTestFromAfterAction()
            {
                TestHasRun = true;
                yield return null;
            }
        }

        public class OuterActionBeforeAttribute : NUnitAttribute, IOuterUnityTestAction
        {
            public IEnumerator BeforeTest(ITest test)
            {
                ExpectTestToRun = false;
                PerformAction();
                yield return null;
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
                ExpectTestToRun = true;
                PerformAction();
                yield return null;
            }
        }

        public class ActionBeforeAttribute : NUnitAttribute, ITestAction
        {
            public void BeforeTest(ITest test)
            {
                ExpectTestToRun = false;
                PerformAction();
            }

            public void AfterTest(ITest test)
            {
            }

            public ActionTargets Targets { get { return ActionTargets.Test; } }
        }

        public class ActionAfterAttribute : NUnitAttribute, ITestAction
        {
            public void BeforeTest(ITest test)
            {
            }

            public void AfterTest(ITest test)
            {
                ExpectTestToRun = true;
                PerformAction();
            }

            public ActionTargets Targets { get { return ActionTargets.Test; } }
        }

        public class CheckStateAfterTestAttribute : NUnitAttribute, IOuterUnityTestAction
        {
            public IEnumerator BeforeTest(ITest test)
            {
                yield return null;
            }

            public IEnumerator AfterTest(ITest test)
            {
                CheckState();

                yield return null;
            }
        }

        public static bool TestHasRun = false;
        public static void CheckState()
        {
            var testHasRun = TestHasRun;
            var expectedTestToRun = ExpectTestToRun;
            TestHasRun = false;
            ExpectTestToRun = true;

            Assert.AreEqual(expectedTestToRun, testHasRun, "TestRunning not as expected.");

            var result = UnityTestExecutionContext.CurrentContext.CurrentResult;
            if (Equals(result.ResultState, ExpectedState) && result.Message == ExpectedMessage)
            {
                result.SetResult(ResultState.Success);
                foreach (var resultChild in result.Children)
                {
                    (resultChild as TestResult).SetResult(ResultState.Success);
                }
            }
            else
            {
                var msg = string.Format("Expected test to be \n\t'{0}' with message '{1}', but got \n\t'{2}' with message '{3}'.",
                    ExpectedState, ExpectedMessage, result.ResultState, result.Message);
                result.SetResult(ResultState.Failure, msg);
            }
        }
    }
}
