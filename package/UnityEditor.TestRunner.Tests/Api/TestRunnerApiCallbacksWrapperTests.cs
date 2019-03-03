using System;
using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestTools;
using ITest = NUnit.Framework.Interfaces.ITest;

public class TestRunnerApiCallbacksWrapperTests
{
    [Test]
    public void CallbackWrapperConvertsTestAndInvokesEvent()
    {
        var callbackWrapperUnderTest = CallbacksDelegator.instance;
        var test = new TestMock();
        var testCallback = new TestCallback();
        CallbacksHolder.instance.Add(testCallback, 0);

        callbackWrapperUnderTest.RunStarted(test);

        Assert.AreEqual(1, testCallback.RunStartedInvoked);
        Assert.AreEqual(0, testCallback.RunFinishedInvoked);
        Assert.AreEqual(0, testCallback.TestStartedInvoked);
        Assert.AreEqual(0, testCallback.TestFinishedInvoked);
    }

    [Test]
    public void CallbackWrapperConvertsTestAndInvokesEventsInCorrectOrder()
    {
        var callbackWrapperUnderTest = CallbacksDelegator.instance;
        var test = new TestMock();
        var testCallback = new TestOrderCallback();
        var testCallback2 = new TestOrderCallback();
        CallbacksHolder.instance.Add(testCallback, 1);
        CallbacksHolder.instance.Add(testCallback2, 0);

        callbackWrapperUnderTest.RunStarted(test);

        Assert.IsTrue(testCallback.First);
        Assert.IsFalse(testCallback2.First);
    }

    [Test]
    public void CallbackWrapperInvokesEventsWhenPreviousEventHandlerThrows()
    {
        var callbackWrapperUnderTest = CallbacksDelegator.instance;
        var test = new TestMock();
        var testCallback1 = new TestCallback();
        testCallback1.ThrowException = true;
        CallbacksHolder.instance.Add(testCallback1, 0);
        LogAssert.Expect(LogType.Exception, "Exception: test");
        var testCallback2 = new TestCallback();
        testCallback2.ThrowException = true;
        CallbacksHolder.instance.Add(testCallback2, 0);
        LogAssert.Expect(LogType.Exception, "Exception: test");

        callbackWrapperUnderTest.RunStarted(test);

        Assert.AreEqual(1, testCallback1.RunStartedInvoked);
        Assert.AreEqual(0, testCallback1.RunFinishedInvoked);
        Assert.AreEqual(0, testCallback1.TestStartedInvoked);
        Assert.AreEqual(0, testCallback1.TestFinishedInvoked);
        Assert.AreEqual(1, testCallback2.RunStartedInvoked);
        Assert.AreEqual(0, testCallback2.RunFinishedInvoked);
        Assert.AreEqual(0, testCallback2.TestStartedInvoked);
        Assert.AreEqual(0, testCallback2.TestFinishedInvoked);
    }

    private ICallbacks[] originalCallbacks;

    [SetUp]
    public void SetUp()
    {
        originalCallbacks = CallbacksHolder.instance.GetAll();
        CallbacksHolder.instance.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var testRunnerApiCallback in originalCallbacks)
        {
            CallbacksHolder.instance.Add(testRunnerApiCallback, 0);
        }
    }

    private class TestCallback : ICallbacks
    {
        public int RunStartedInvoked;
        public int RunFinishedInvoked;
        public int TestStartedInvoked;
        public int TestFinishedInvoked;
        public bool ThrowException;

        public void RunStarted(ITestAdaptor testsToRun)
        {
            RunStartedInvoked++;
            if (ThrowException)
            {
                throw new Exception("test");
            }
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            RunFinishedInvoked++;
        }

        public void TestStarted(ITestAdaptor test)
        {
            TestStartedInvoked++;
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            TestFinishedInvoked++;
        }
    }

    private class TestOrderCallback : ICallbacks
    {
        public bool First;
        static bool Called;

        public void RunStarted(ITestAdaptor testsToRun)
        {
            if (!Called)
            {
                Called = true;
                First = true;
            }
        }

        public void RunFinished(ITestResultAdaptor result)
        {
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
        }
    }

    private class TestMock : ITest
    {
        public TestMock()
        {
            Tests = new List<ITest>();
            Properties = new PropertyBag();
            Properties[PropertyNames.Category] = new string[0];
        }

        public TNode ToXml(bool recursive)
        {
            throw new NotImplementedException();
        }

        public TNode AddToXml(TNode parentNode, bool recursive)
        {
            throw new NotImplementedException();
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public string ClassName { get; private set; }
        public string MethodName { get; private set; }
        public ITypeInfo TypeInfo { get; private set; }
        public IMethodInfo Method { get; private set; }
        public NUnit.Framework.Interfaces.RunState RunState { get; private set; }
        public int TestCaseCount { get; private set; }
        public IPropertyBag Properties { get; private set; }
        public ITest Parent { get; private set; }
        public bool IsSuite { get; private set; }
        public bool HasChildren { get; private set; }
        public IList<ITest> Tests { get; private set; }
        public object Fixture { get; private set; }
    }
}
