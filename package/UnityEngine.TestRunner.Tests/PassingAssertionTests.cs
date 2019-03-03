using System;
using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class PassingAssertionTests
    {
        [Test]
        public void LogAssert_WhenFailingLogIsAsserted([ValueSource(typeof(LogAssertTestsHelper), "s_FailingLogTypes")] LogType logType)
        {
            LogAssertTestsHelper.AssertMessage(logType);
            LogAssertTestsHelper.LogMessage(logType);
        }

        [Test]
        public void LogAssert_WhenExpectingNormalMessageTwice()
        {
            Debug.Log("Any some message");
            Debug.Log("Any some message");
            LogAssert.Expect(LogType.Log, "Any some message");
        }

        [Test]
        public void LogAssert_ExpectingNormalMessageWhithDifferentMessages()
        {
            Debug.Log("Message1");
            Debug.Log("Message2");
            Debug.Log("Message3");

            LogAssert.Expect(LogType.Log, "Message3");
        }

        [Test]
        public void LogAssert_ExpectingMessageWhithDifferentMessagesOfDifferentType()
        {
            Debug.Log("Message1");
            Debug.LogError("Message1");
            Debug.Log("Message1");
            LogAssert.Expect(LogType.Error, "Message1");
        }

        [Test]
        public void LogAssert_LogCanBeAssertedBeforeItsLogged([ValueSource(typeof(LogAssertTestsHelper), "s_FailingLogTypes")] LogType logType)
        {
            LogAssertTestsHelper.AssertMessage(logType);
            LogAssertTestsHelper.LogMessage(logType);
        }

        [Test]
        public void LogAssert_LogCanBeAssertedAfterItsLogged([ValueSource(typeof(LogAssertTestsHelper), "s_FailingLogTypes")] LogType logType)
        {
            LogAssertTestsHelper.LogMessage(logType);
            LogAssertTestsHelper.AssertMessage(logType);
        }

        [UnityTest]
        public IEnumerator LogAssert_AssertedLogBeforeEnumerator_AfterYield()
        {
            var logType = LogType.Error;
            yield return null;
            LogAssertTestsHelper.AssertMessage(logType);
            LogAssertTestsHelper.LogMessage(logType);
        }

        [UnityTest]
        public IEnumerator LogAssert_AssertedLogAfterEnumerator_AfterYield()
        {
            var logType = LogType.Error;
            yield return null;
            LogAssertTestsHelper.LogMessage(logType);
            LogAssertTestsHelper.AssertMessage(logType);
        }

        [UnityTest]
        public IEnumerator LogAssert_AssertedLogBeforeEnumerator_BetweenYield()
        {
            var logType = LogType.Error;
            yield return null;
            LogAssertTestsHelper.AssertMessage(logType);
            LogAssertTestsHelper.LogMessage(logType);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LogAssert_AssertedLogAfterEnumerator_BetweenYield()
        {
            var logType = LogType.Error;
            yield return null;
            LogAssertTestsHelper.LogMessage(logType);
            LogAssertTestsHelper.AssertMessage(logType);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LogAssert_AssertInPreviousFrame()
        {
            var logType = LogType.Error;
            LogAssertTestsHelper.AssertMessage(logType);
            yield return null;
            yield return null;
            yield return null;
            LogAssertTestsHelper.LogMessage(logType);
        }

        [Test]
        public void LogAssert_ExpectMultipleLogMessagesInCorrectOrder()
        {
            LogAssert.Expect(LogType.Error, "MyLog1");
            Debug.LogError("MyLog1");
            Debug.LogError("MyLog2");
            LogAssert.Expect(LogType.Error, "MyLog2");
        }

        [UnityTest]
        public IEnumerator Assert_Pass_InUnityTest_IsReportedCorrectly()
        {
            Assert.Pass("Message for\nPass");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Assert_Ignore_InUnityTest_IsReportedCorrectly()
        {
            Assert.Ignore("Message for\nIgnore");
            yield return null;
        }

        [UnityTest]
        [Explicit]
        public IEnumerator Assert_Inconclusive_InUnityTest_IsReportedCorrectly()
        {
            Assert.Inconclusive("Message for\nInconclusive");
            yield return null;
        }
    }
}
