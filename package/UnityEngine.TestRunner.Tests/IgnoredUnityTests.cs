using System.Collections;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestRunner.NUnitExtensions.Runner;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class IgnoredUnityTests
    {
        [UnityTest, ExpectedToBeIgnored]
        public IEnumerator Assert_IgnoreFollowedByYieldReturnNull_IgnoresTheTest()
        {
            Ignore();
            yield return null;
        }

        [UnityTest, ExpectedToBeIgnored]
        public IEnumerator Assert_YieldReturnNullFollowedByIgnore_IgnoresTheTest()
        {
            yield return null;
            Ignore();
        }

        [UnityTest, ExpectedToBeIgnored]
        public IEnumerator Assert_IgnoreFollowedByCoroutine_IgnoresTheTest()
        {
            Ignore();
            return AnotherCoroutine();
        }

        static IEnumerator AnotherCoroutine()
        {
            yield return null;
        }

        [UnityTest, ExpectedToBeIgnored]
        public IEnumerator Assert_IgnoreInsideCoroutine_IgnoresTheTest()
        {
            return IgnoringCoroutine();
        }

        static IEnumerator IgnoringCoroutine()
        {
            Ignore();
            yield return null;
        }

        private static void Ignore()
        {
            Assert.Ignore("Ignored internationally");
        }

        [UnityTest, ExpectedToBeIgnored]
        public IEnumerator Assert_IgnoreInsideWaitUntil_IgnoresTheTest()
        {
            yield return new WaitUntil(() =>
            {
                Ignore();
                Assert.Fail("The fail should not be reached.");
                return false;
            });
        }

        public class ExpectedToBeIgnoredAttribute : NUnitAttribute, IOuterUnityTestAction
        {
            public IEnumerator BeforeTest(ITest test)
            {
                yield return null;
            }

            public IEnumerator AfterTest(ITest test)
            {
                var context = UnityTestExecutionContext.CurrentContext;
                if (Equals(context.CurrentResult.ResultState, ResultState.Ignored))
                {
                    context.CurrentResult.SetResult(ResultState.Success);
                }
                else
                {
                    var resultState = context.CurrentResult.ResultState;
                    context.CurrentResult.SetResult(ResultState.Failure, "Expected test to be " + ResultState.Ignored + ". Got result state " + resultState);
                }

                yield return null;
            }
        }
    }
}
