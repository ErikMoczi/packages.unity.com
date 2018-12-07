using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestRunner.TestLaunchers;
using UnityEngine.TestTools;
using TestStatus = NUnit.Framework.Interfaces.TestStatus;

namespace Assets.editor.Api
{
    public class CallbacksDelegatorTests
    {
        [Test]
        public void CallbacksDelegator_InvokesRunStartedOnAllCallbacks()
        {
            var callbacks = GetCallbacks();

            var delegatorUnderTest = new CallbacksDelegator(() => callbacks.Cast<ICallbacks>().ToArray(), GetTestAdaptorFactory());

            LogAssert.Expect(LogType.Exception, "Exception: Exception in RunStarted");

            delegatorUnderTest.RunStarted(MockTest("id RunStarted"));

            for (int i = 0; i < callbacks.Length; i++)
            {
                Assert.AreEqual(1, callbacks[i].runStartedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].runFinishedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].testStartedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].testFinishedInvoked, "Callback " + i);
                Assert.NotNull(callbacks[i].runStartedTest, "RunStarted test not set in callback " + i);
                Assert.AreEqual("id RunStarted", callbacks[i].runStartedTest.Id);
            }
        }

        [Test]
        public void CallbacksDelegator_InvokesRunFinishedOnAllCallbacks()
        {
            var callbacks = GetCallbacks();

            var delegatorUnderTest = new CallbacksDelegator(() => callbacks.Cast<ICallbacks>().ToArray(), GetTestAdaptorFactory());

            LogAssert.Expect(LogType.Exception, "Exception: Exception in RunFinished");

            delegatorUnderTest.RunFinished(MockTestResult("id RunFinished"));

            for (int i = 0; i < callbacks.Length; i++)
            {
                Assert.AreEqual(0, callbacks[i].runStartedInvoked, "Callback " + i);
                Assert.AreEqual(1, callbacks[i].runFinishedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].testStartedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].testFinishedInvoked, "Callback " + i);
                Assert.NotNull(callbacks[i].runFinishedTest, "RunFinished test not set in callback " + i);
                Assert.AreEqual("id RunFinished", callbacks[i].runFinishedTest.Test.Id);
            }
        }

        [Test]
        public void CallbacksDelegator_InvokesTestStartedOnAllCallbacks()
        {
            var callbacks = GetCallbacks();

            var delegatorUnderTest = new CallbacksDelegator(() => callbacks.Cast<ICallbacks>().ToArray(), GetTestAdaptorFactory());

            LogAssert.Expect(LogType.Exception, "Exception: Exception in TestStarted");

            delegatorUnderTest.TestStarted(MockTest("id TestStarted"));

            for (int i = 0; i < callbacks.Length; i++)
            {
                Assert.AreEqual(0, callbacks[i].runStartedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].runFinishedInvoked, "Callback " + i);
                Assert.AreEqual(1, callbacks[i].testStartedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].testFinishedInvoked, "Callback " + i);
                Assert.NotNull(callbacks[i].testStartedTest, "TestStarted test not set in callback " + i);
                Assert.AreEqual("id TestStarted", callbacks[i].testStartedTest.Id);
            }
        }

        [Test]
        public void CallbacksDelegator_InvokesTestFinishedOnAllCallbacks()
        {
            var callbacks = GetCallbacks();

            var delegatorUnderTest = new CallbacksDelegator(() => callbacks.Cast<ICallbacks>().ToArray(), GetTestAdaptorFactory());

            LogAssert.Expect(LogType.Exception, "Exception: Exception in TestFinished");

            delegatorUnderTest.TestFinished(MockTestResult("id TestFinished"));

            for (int i = 0; i < callbacks.Length; i++)
            {
                Assert.AreEqual(0, callbacks[i].runStartedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].runFinishedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].testStartedInvoked, "Callback " + i);
                Assert.AreEqual(1, callbacks[i].testFinishedInvoked, "Callback " + i);
                Assert.NotNull(callbacks[i].testFinishedResult, "TestFinished test not set in callback " + i);
                Assert.AreEqual("id TestFinished", callbacks[i].testFinishedResult.Test.Id);
            }
        }

        [Test]
        public void CallbacksDelegator_InvokesRunStartedRemotelyOnAllCallbacks()
        {
            var callbacks = GetCallbacks();

            var delegatorUnderTest = new CallbacksDelegator(() => callbacks.Cast<ICallbacks>().ToArray(), GetTestAdaptorFactory());

            LogAssert.Expect(LogType.Exception, "Exception: Exception in RunStarted");

            delegatorUnderTest.RunStartedRemotely(MockTestRemotelySerialized("id RunStarted"));

            for (int i = 0; i < callbacks.Length; i++)
            {
                Assert.AreEqual(1, callbacks[i].runStartedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].runFinishedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].testStartedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].testFinishedInvoked, "Callback " + i);
                Assert.NotNull(callbacks[i].runStartedTest, "RunStarted test not set in callback " + i);
                Assert.AreEqual("id RunStarted", callbacks[i].runStartedTest.Id);
            }
        }

        [Test]
        public void CallbacksDelegator_InvokesRunFinishedRemotelyOnAllCallbacks()
        {
            var callbacks = GetCallbacks();

            var delegatorUnderTest = new CallbacksDelegator(() => callbacks.Cast<ICallbacks>().ToArray(), GetTestAdaptorFactory());

            LogAssert.Expect(LogType.Exception, "Exception: Exception in RunFinished");

            delegatorUnderTest.RunFinishedRemotely(MockTestResultRemotelySerialized("id RunFinished"));

            for (int i = 0; i < callbacks.Length; i++)
            {
                Assert.AreEqual(0, callbacks[i].runStartedInvoked, "Callback " + i);
                Assert.AreEqual(1, callbacks[i].runFinishedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].testStartedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].testFinishedInvoked, "Callback " + i);
                Assert.NotNull(callbacks[i].runFinishedTest, "RunFinished test not set in callback " + i);
                Assert.AreEqual("id RunFinished", callbacks[i].runFinishedTest.Test.Id);
            }
        }

        [Test]
        public void CallbacksDelegator_InvokesTestStartedRemotelyOnAllCallbacks()
        {
            var callbacks = GetCallbacks();

            var delegatorUnderTest = new CallbacksDelegator(() => callbacks.Cast<ICallbacks>().ToArray(), GetTestAdaptorFactory());

            LogAssert.Expect(LogType.Exception, "Exception: Exception in TestStarted");

            delegatorUnderTest.TestStartedRemotely(MockTestRemotelySerialized("id TestStarted"));

            for (int i = 0; i < callbacks.Length; i++)
            {
                Assert.AreEqual(0, callbacks[i].runStartedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].runFinishedInvoked, "Callback " + i);
                Assert.AreEqual(1, callbacks[i].testStartedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].testFinishedInvoked, "Callback " + i);
                Assert.NotNull(callbacks[i].testStartedTest, "TestStarted test not set in callback " + i);
                Assert.AreEqual("id TestStarted", callbacks[i].testStartedTest.Id);
            }
        }

        [Test]
        public void CallbacksDelegator_InvokesTestFinishedRemotelyOnAllCallbacks()
        {
            var callbacks = GetCallbacks();

            var delegatorUnderTest = new CallbacksDelegator(() => callbacks.Cast<ICallbacks>().ToArray(), GetTestAdaptorFactory());

            LogAssert.Expect(LogType.Exception, "Exception: Exception in TestFinished");

            delegatorUnderTest.TestFinishedRemotely(MockTestResultRemotelySerialized("id TestFinished"));

            for (int i = 0; i < callbacks.Length; i++)
            {
                Assert.AreEqual(0, callbacks[i].runStartedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].runFinishedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].testStartedInvoked, "Callback " + i);
                Assert.AreEqual(1, callbacks[i].testFinishedInvoked, "Callback " + i);
                Assert.NotNull(callbacks[i].testFinishedResult, "TestFinished test not set in callback " + i);
                Assert.AreEqual("id TestFinished", callbacks[i].testFinishedResult.Test.Id);
            }
        }

        [Test]
        public void CallbacksDelegator_InvokesRunFailedOnAllCallbacks()
        {
            var callbacks = GetCallbacks();

            var delegatorUnderTest = new CallbacksDelegator(() => callbacks.Cast<ICallbacks>().ToArray(), GetTestAdaptorFactory());

            LogAssert.Expect(LogType.Exception, "Exception: Exception in RunFinished");

            delegatorUnderTest.RunFailed("failure message");

            for (int i = 0; i < callbacks.Length; i++)
            {
                Assert.AreEqual(0, callbacks[i].runStartedInvoked, "Callback " + i);
                Assert.AreEqual(1, callbacks[i].runFinishedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].testStartedInvoked, "Callback " + i);
                Assert.AreEqual(0, callbacks[i].testFinishedInvoked, "Callback " + i);
                Assert.NotNull(callbacks[i].runFinishedTest, "RunFinished test not set in callback " + i);
                Assert.AreEqual("Failed:Error", callbacks[i].runFinishedTest.ResultState);
                Assert.AreEqual("failure message", callbacks[i].runFinishedTest.Message);
            }
        }

        private ITest MockTest(string id)
        {
            var testMock = new Mock<ITest>();
            testMock.Setup(test => test.Id).Returns(id);
            testMock.Setup(test => test.Tests).Returns(new ITest[0]);

            var propertyBagMock = new Mock<IPropertyBag>();
            propertyBagMock.SetupGet(bag => bag[PropertyNames.Category]).Returns(new string[0]);
            propertyBagMock.SetupGet(bag => bag["childIndex"]).Returns(new List<string>());
            testMock.Setup(test => test.Properties).Returns(propertyBagMock.Object);

            testMock.Setup(test => test.Id).Returns(id);
            return testMock.Object;
        }

        private ITestResult MockTestResult(string id)
        {
            var resultMock = new Mock<ITestResult>();
            var test = MockTest(id);
            resultMock.Setup(result => result.Test).Returns(test);
            resultMock.Setup(result => result.ResultState).Returns(new ResultState(TestStatus.Passed));
            resultMock.Setup(result => result.ToXml(true)).Returns(new TNode("test"));
            return resultMock.Object;
        }

        private ITestAdaptorFactory GetTestAdaptorFactory()
        {
            var factoryMock = new Mock<ITestAdaptorFactory>();

            factoryMock.Setup(factory => factory.Create(It.IsAny<ITest>())).Returns<ITest>(MockTestAdaptor);
            factoryMock.Setup(factory => factory.Create(It.IsAny<ITestResult>())).Returns<ITestResult>(MockTestResultAdaptor);
            factoryMock.Setup(factory => factory.BuildTree(It.IsAny<RemoteTestResultDataWithTestData>())).Returns<RemoteTestResultDataWithTestData>(MockTestAdaptorRemotely);
            factoryMock.Setup(factory => factory.Create(It.IsAny<RemoteTestResultData>(), It.IsAny<RemoteTestResultDataWithTestData>())).Returns<RemoteTestResultData, RemoteTestResultDataWithTestData>(MockTestResultAdaptorRemotely);

            return factoryMock.Object;
        }

        private ITestAdaptor MockTestAdaptor(ITest test)
        {
            var testAdaptorMock = new Mock<ITestAdaptor>();
            testAdaptorMock.Setup(adaptor => adaptor.Id).Returns(test.Id);
            return testAdaptorMock.Object;
        }

        private ITestAdaptor MockTestAdaptorRemotely(RemoteTestResultDataWithTestData data)
        {
            return MockTestAdaptor(MockTest(data.tests.First().id));
        }

        private ITestResultAdaptor MockTestResultAdaptor(ITestResult testResult)
        {
            var testAdaptor = MockTestAdaptor(testResult.Test);
            var testResultAdaptorMock = new Mock<ITestResultAdaptor>();
            testResultAdaptorMock.Setup(adaptor => adaptor.Test).Returns(testAdaptor);
            testResultAdaptorMock.Setup(adaptor => adaptor.ResultState).Returns(testResult.ResultState.ToString());
            testResultAdaptorMock.Setup(adaptor => adaptor.Message).Returns(testResult.Message);
            return testResultAdaptorMock.Object;
        }

        private ITestResultAdaptor MockTestResultAdaptorRemotely(RemoteTestResultData data, RemoteTestResultDataWithTestData allData)
        {
            return MockTestResultAdaptor(MockTestResult(data.testId));
        }

        private byte[] MockTestRemotelySerialized(string id)
        {
            var test = MockTest(id);
            var data = new RemoteTestResultDataWithTestData();
            data.tests = new RemoteTestData[] { new RemoteTestData(test), };
            return Serialize(data);
        }

        private byte[] MockTestResultRemotelySerialized(string id)
        {
            var testResult = MockTestResult(id);
            var data = new RemoteTestResultDataWithTestData();
            data.results = new RemoteTestResultData[] { new RemoteTestResultData(testResult), };
            return Serialize(data);
        }

        private static byte[] Serialize(object obj)
        {
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(obj));
        }

        private CallbacksMock[] GetCallbacks()
        {
            return new[] { new CallbacksMock(), new CallbacksMock(true), new CallbacksMock() };
        }

        private class CallbacksMock : ICallbacks
        {
            private readonly bool m_ThrowException;
            public int runStartedInvoked = 0;
            public int runFinishedInvoked = 0;
            public int testStartedInvoked = 0;
            public int testFinishedInvoked = 0;

            public ITestAdaptor runStartedTest;
            public ITestResultAdaptor runFinishedTest;
            public ITestAdaptor testStartedTest;
            public ITestResultAdaptor testFinishedResult;

            public CallbacksMock(bool throwException = false)
            {
                m_ThrowException = throwException;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                runStartedInvoked++;
                runStartedTest = testsToRun;

                if (m_ThrowException)
                {
                    throw new Exception("Exception in RunStarted");
                }
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                runFinishedInvoked++;
                runFinishedTest = result;

                if (m_ThrowException)
                {
                    throw new Exception("Exception in RunFinished");
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
                testStartedInvoked++;
                testStartedTest = test;

                if (m_ThrowException)
                {
                    throw new Exception("Exception in TestStarted");
                }
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                testFinishedInvoked++;
                testFinishedResult = result;

                if (m_ThrowException)
                {
                    throw new Exception("Exception in TestFinished");
                }
            }
        }
    }
}
