using System;
using UnityEngine;
using System.Collections;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    [Description("Tests that should always fail in the runner")]
    public class FailingAssertionTests
    {
        [TestExpectedToFail]
        public void Test_WhenFailingLogIsLogged_FailsTheTest([ValueSource(typeof(LogAssertTestsHelper), "s_FailingLogTypes")] LogType logType)
        {
            LogAssertTestsHelper.LogMessage(logType);
        }

        [UnityTestExpectedToFail]
        public IEnumerator Test_WhenFailingLogIsLogged_FailsTheTest_UnityTest([ValueSource(typeof(LogAssertTestsHelper), "s_FailingLogTypes")] LogType logType)
        {
            yield return null;
            LogAssertTestsHelper.LogMessage(logType);
        }

        [UnityTestExpectedToFail]
        public IEnumerator FailingLog_WhenLoggedBetweenYields_FailsTheTest()
        {
            var logType = LogType.Error;
            yield return null;
            LogAssertTestsHelper.LogMessage(logType);
            yield return null;
        }

        [UnityTestExpectedToFail]
        public IEnumerator LogAssert_WhenAssertedInNextFrame_FailsTheTest()
        {
            var logType = LogType.Error;
            yield return null;
            LogAssertTestsHelper.LogMessage(logType);
        }

        [UnityTestExpectedToFail]
        public IEnumerator LogAssert_WhenAssertedInNextFrame_FailsTheTest2()
        {
            var logType = LogType.Error;
            LogAssertTestsHelper.LogMessage(logType);
            yield return null;
            LogAssertTestsHelper.AssertMessage(logType);
        }

        [UnityTestExpectedToFail]
        public IEnumerator LogAssert_WhenAssertedBeforeEnterPlaymode_FailsTheTest()
        {
            var logType = LogType.Error;
            LogAssertTestsHelper.LogMessage(logType);
            yield return new EnterPlayMode();
        }

        [UnityTestExpectedToFail]
        public IEnumerator LogAssert_WhenAssertedInNextFrame_FailsTheTest3()
        {
            var logType = LogType.Error;
            LogAssertTestsHelper.LogMessage(logType);
            yield return null;
            yield return null;
            yield return null;
            LogAssertTestsHelper.AssertMessage(logType);
        }

        [TestExpectedToFail]
        public void LogAssertExpect_WhenNoLogAppears_FailsTheTest([ValueSource(typeof(LogAssertTestsHelper), "s_FailingLogTypes")] LogType logType)
        {
            LogAssertTestsHelper.AssertMessage(logType);
        }

        [UnityTestExpectedToFail]
        public IEnumerator LogAssertExpect_WhenNoLogAppears_FailsTheTest_UnityTest()
        {
            var logType = LogType.Error;
            LogAssertTestsHelper.AssertMessage(logType);
            yield return null;
        }

        [UnityTestExpectedToFail]
        public IEnumerator LogAssertExpect_WhenNoLogAppearsAfterDomainReload_FailsTheTest_UnityTest()
        {
            var logType = LogType.Error;
            LogAssertTestsHelper.AssertMessage(logType);
            yield return new EnterPlayMode();
        }

        [UnityTestExpectedToFail]
        public IEnumerator LogAssertExpect_WhenNoLogOfCorrectTypeAppearsAfterDomainReload_FailsTheTest_UnityTest()
        {
            LogAssert.Expect(LogType.Error, "Test");
            yield return new EnterPlayMode();
            Debug.LogWarning("Test");
        }

        [TestExpectedToFail]
        public void LogAssertExpect_WhenDifferentMessageTypeAppears_FailsTheTest()
        {
            Debug.Log("Any some message");
            LogAssert.Expect(LogType.Log, "Warning message");
        }

        [TestExpectedToFail]
        public void LogAssertExpect_WhenDifferentLogMessageAppears_FailsTheTest()
        {
            Debug.Log("Message1");
            Debug.LogError("Message1");
            Debug.LogError("Message1");
            LogAssert.Expect(LogType.Error, "Message1");
        }

        [TestExpectedToFail]
        public void LogAssertExpect_WhenExpectingTwiceNormalLog_FailsTheTest()
        {
            Debug.Log("Any some message");
            LogAssert.Expect(LogType.Log, "Any some message");
            LogAssert.Expect(LogType.Log, "Any some message");
        }

        [TestExpectedToFail]
        public void LogAssertExpect_WhenLogAppearsTwice_FailsTheTest()
        {
            LogAssert.Expect(LogType.Error, "Warning message");
            Debug.LogError("Warning message");
            Debug.LogError("Warning message");
        }

        [TestExpectedToFail]
        public void LogAssertExpect_WhenLogIsExpectedTwice_FailsTheTest()
        {
            LogAssert.Expect(LogType.Error, "Warning message");
            LogAssert.Expect(LogType.Error, "Warning message");
            Debug.LogError("Warning message");
        }

        [TestExpectedToFail]
        public void LogAssertExpect_WhenLogOfDifferentType_FailsTheTest()
        {
            LogAssert.Expect(LogType.Warning, "A message");
            Debug.LogError("A message");
        }

        [TestExpectedToFail]
        [Ignore("Catching exceptions from different thread is not implemented")]
        public void Exception_WhenThrownFromDifferentThread_FailsTheTest()
        {
            var t = new Thread(() =>
            {
                throw new Exception("Some exception");
            });
            t.Start();
            t.Join();
        }
    }
}
