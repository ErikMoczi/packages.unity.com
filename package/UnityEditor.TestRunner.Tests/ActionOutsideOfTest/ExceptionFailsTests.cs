using System;
using System.Collections;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestRunner.NUnitExtensions.Runner;
using UnityEngine.TestTools;

namespace ActionOutsideOfTest
{
    public class ExceptionFailsTests
    {
        public static void PerformAction(string expectedPrefix = "")
        {
            ExpectedMessagePrefix = expectedPrefix;
            throw new Exception("Failed using Exception on purpose.");
        }

        private static string ExpectedMessage = "System.Exception : Failed using Exception on purpose.";
        private static ResultState ExpectedState = ResultState.Error;
        private static string ExpectedMessagePrefix = "";

        public abstract class SetUpTearDownTestBase
        {
            [Test, CheckStateAfterTest]
            public void Test()
            {
            }

            [UnityTest, CheckStateAfterTest]
            public IEnumerator UnityTest()
            {
                yield return null;
            }
        }

        public class FromSetup : SetUpTearDownTestBase
        {
            [SetUp]
            public void Setup()
            {
                PerformAction();
            }
        }

        public class FromUnitySetup : SetUpTearDownTestBase
        {
            [UnitySetUp]
            public IEnumerator UnitySetup()
            {
                PerformAction();
                yield return null;
            }
        }

        public class FromUnityTearDown : SetUpTearDownTestBase
        {
            [UnityTearDown]
            public IEnumerator UnityTearDown()
            {
                PerformAction();
                yield return null;
            }
        }

        public class FromTearDown : SetUpTearDownTestBase
        {
            [TearDown]
            public void TearDown()
            {
                PerformAction("TearDown : ");
            }
        }

        public class FromTest
        {
            [Test, CheckStateAfterTest]
            public void Test()
            {
                PerformAction();
            }

            [UnityTest, CheckStateAfterTest]
            public IEnumerator UnityTest()
            {
                PerformAction();
                yield return null;
            }
        }

        public class FromTestActionBeforeTest
        {
            [Test, ActionBefore, CheckStateAfterTest]
            public void Test()
            {
            }

            [UnityTest, ActionBefore, CheckStateAfterTest]
            public IEnumerator UnityTest()
            {
                yield return null;
            }
        }

        public class FromTestActionAfterTest
        {
            [Test, ActionAfter, CheckStateAfterTest]
            public void Test()
            {
            }

            [UnityTest, ActionAfter, CheckStateAfterTest]
            public IEnumerator UnityTest()
            {
                yield return null;
            }
        }


        public class FromOuterUnityTestAction
        {
            [Test, OuterActionBefore, CheckStateAfterTest]
            public void TestFromBeforeAction()
            {
            }

            [UnityTest, OuterActionBefore, CheckStateAfterTest]
            public IEnumerator UnityTestFromBeforeAction()
            {
                yield return null;
            }

            [Test, OuterActionAfter, CheckStateAfterTest]
            public void TestFromAfterAction()
            {
            }

            [UnityTest, OuterActionAfter, CheckStateAfterTest]
            public IEnumerator UnityTestFromAfterAction()
            {
                yield return null;
            }
        }

        public class ActionBeforeAttribute : NUnitAttribute, ITestAction
        {
            public void BeforeTest(ITest test)
            {
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
                PerformAction();
            }

            public ActionTargets Targets { get { return ActionTargets.Test; } }
        }

        public class OuterActionBeforeAttribute : NUnitAttribute, IOuterUnityTestAction
        {
            public IEnumerator BeforeTest(ITest test)
            {
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
                PerformAction();
                yield return null;
            }
        }

        public class CheckStateAfterTestAttribute : NUnitAttribute, IOuterUnityTestAction
        {
            public IEnumerator BeforeTest(ITest test)
            {
                yield return null;
            }

            public IEnumerator AfterTest(ITest test)
            {
                var result = UnityTestExecutionContext.CurrentContext.CurrentResult;
                if (Equals(result.ResultState, ExpectedState) && result.Message == ExpectedMessagePrefix + ExpectedMessage)
                {
                    result.SetResult(ResultState.Success);
                }
                else
                {
                    var msg = string.Format("Expected test to be \n\t'{0}' with message '{4}{1}', but got \n\t'{2}' with message '{3}'.",
                        ExpectedState, ExpectedMessage, result.ResultState, result.Message, ExpectedMessagePrefix);
                    result.SetResult(ResultState.Failure, msg);
                }

                yield return null;
            }
        }
    }
}
