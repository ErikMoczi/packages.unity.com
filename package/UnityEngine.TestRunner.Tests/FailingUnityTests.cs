using System;
using UnityEngine;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class FailingUnityTests
    {
        [TestExpectedToFail]
        public void Assert_OnFailure_FailsTheTest()
        {
            Assert.IsTrue(false);
        }

        [TestExpectedToFail]
        public void Assert_OnException_FailsTheTest()
        {
            throw new Exception("Void_Failed_Exception");
        }

        [UnityTestExpectedToFail]
        public IEnumerator Assert_OnFailure_FailsTheTest_UnityTest()
        {
            yield return null;
            Assert.IsTrue(false);
            yield return null;
        }

        [UnityTestExpectedToFail]
        public IEnumerator Assert_OnException_FailsTheTest_UnityTest()
        {
            yield return null;
            throw new Exception("IEnumerator_Failed_Exception");
        }

        [TestExpectedToFail]
        public void Test_WhenLogError_FailsTheTest()
        {
            Debug.LogError("Error log from test");
        }

        [TestExpectedToFail]
        public void Test_WhenLogAssertion_FailsTheTest()
        {
            Debug.LogAssertion("Error log from test");
        }

        [TestExpectedToFail]
        public void Test_WhenLogException_FailsTheTest()
        {
            Debug.LogException(new Exception("Log exception"));
        }

        [UnityTestExpectedToFail]
        public IEnumerator Test_WhenLogError_FailsTheTest_UnityTest()
        {
            yield return null;
            Debug.LogError("Error log from test");
            yield return null;
        }

        [UnityTestExpectedToFail]
        public IEnumerator Test_WhenLogAssertion_FailsTheTest_UnityTest()
        {
            yield return null;
            Debug.LogAssertion("Error log from test");
            yield return null;
        }

        [UnityTestExpectedToFail]
        public IEnumerator Test_WhenLogException_FailsTheTest_UnityTest()
        {
            yield return null;
            Debug.LogException(new Exception("Log exception"));
            yield return null;
        }

        [TestExpectedToFail]
        public void LogAssert_ExpectMultipleLogsInWrongOrder_FailsTheTest()
        {
            LogAssert.Expect(LogType.Error, "MyLog1");
            LogAssert.Expect(LogType.Error, "MyLog2");
            Debug.LogError("MyLog2");
            Debug.LogError("MyLog1");
        }

        [UnityTestExpectedToFail]
        public IEnumerator LogAssert_ExpectMultipleLogsInWrongOrder_FailsTheTest_UnityTest()
        {
            yield return null;
            LogAssert.Expect(LogType.Error, "MyLog1");
            LogAssert.Expect(LogType.Error, "MyLog2");
            Debug.LogError("MyLog2");
            Debug.LogError("MyLog1");
            yield return null;
        }

        [Ignore("Ignored UnityTest")]
        [UnityTestExpectedToFail]
        [Description("UnityTest should be ignored properly")]
        public IEnumerator UnityTest_ThatIsIgnored_IsRenderedAsIgnored()
        {
            yield return null;
        }

        [UnityTestExpectedToFail]
        public IEnumerator Test_WithAssertFailAfterReturn_FailsTheTest_UnityTest()
        {
            yield return null;
            Assert.Fail();
        }

        [UnityTestExpectedToFail]
        public IEnumerator Test_WithAssertFailBeforeReturn_FailsTheTest_UnityTest()
        {
            Assert.Fail();
            yield return null;
        }
    }
}
