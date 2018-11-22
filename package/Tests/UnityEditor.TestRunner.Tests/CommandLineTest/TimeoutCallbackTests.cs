using System;
using System.Collections.Generic;
using System.Linq;
using FrameworkTests;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.CommandLineTest;
using UnityEngine;

public class TimeoutCallbackTests
{
    List<DelayedCallbackMock> callBackMocks;
    TimeoutCallbacks callBackUnderTest;
    List<string> errorsLogged;
    List<int> exitApplicationCalls;

    [SetUp]
    public void Setup()
    {
        callBackMocks = new List<DelayedCallbackMock>();
        errorsLogged = new List<string>();
        exitApplicationCalls = new List<int>();
        callBackUnderTest = ScriptableObject.CreateInstance<TimeoutCallbacks>();
        callBackUnderTest.Init(
            (action, delay) =>
            {
                var mock = new DelayedCallbackMock(action, delay);
                callBackMocks.Add(mock);
                return mock;
            },
            (error, args) =>
            {
                errorsLogged.Add(string.Format(error, args));
            },
            (exitCode) =>
            {
                exitApplicationCalls.Add(exitCode);
            });
    }

    [Test]
    public void TimeoutCallbacksSetsUpANewTimeoutOnRunStarted()
    {
        callBackUnderTest.RunStarted(new TestRunnerTestMock(0));

        Assert.AreEqual(1, callBackMocks.Count);
        var delayedCallback = callBackMocks.First();
        Assert.AreEqual(600, delayedCallback.Delay);
        Assert.AreEqual(0, delayedCallback.ClearInvokes);
        Assert.AreEqual(0, delayedCallback.ResetInvokes);
    }

    [Test]
    public void TimeoutCallbacksSetsUpANewTimeoutOnTestStartedWithDefaultValue()
    {
        callBackUnderTest.TestStarted(new TestRunnerTestMock(0));

        Assert.AreEqual(1, callBackMocks.Count);
        var delayedCallback = callBackMocks.First();
        Assert.AreEqual(600, delayedCallback.Delay);
    }

    [Test]
    public void TimeoutCallbacksSetsUpANewTimeoutOnTestStartedWithDelayValueFromTestCaseTimeout()
    {
        callBackUnderTest.TestStarted(new TestRunnerTestMock(2000)); // 2000 ms delay

        Assert.AreEqual(1, callBackMocks.Count);
        var delayedCallback = callBackMocks.First();
        Assert.AreEqual(600 + 2, delayedCallback.Delay);
    }

    [Test]
    public void TimeoutCallbacksResetsCallbackOnNewTestStarted()
    {
        callBackUnderTest.TestStarted(new TestRunnerTestMock(0));
        callBackUnderTest.TestStarted(new TestRunnerTestMock(0));

        Assert.AreEqual(1, callBackMocks.Count);
        var delayedCallback = callBackMocks.First();
        Assert.AreEqual(600, delayedCallback.Delay);
        Assert.AreEqual(0, delayedCallback.ClearInvokes);
        Assert.AreEqual(1, delayedCallback.ResetInvokes);
    }

    [Test]
    public void TimeoutCallbacksSetsupNewCallbackOnNewTestStartedWithDifferentDelay()
    {
        callBackUnderTest.TestStarted(new TestRunnerTestMock(0));
        callBackUnderTest.TestStarted(new TestRunnerTestMock(4000));

        Assert.AreEqual(2, callBackMocks.Count);
        var firstDelayedCallback = callBackMocks[0];
        Assert.AreEqual(600, firstDelayedCallback.Delay);
        Assert.AreEqual(1, firstDelayedCallback.ClearInvokes);
        Assert.AreEqual(0, firstDelayedCallback.ResetInvokes);

        var secondDelayedCallback = callBackMocks[1];
        Assert.AreEqual(600 + 4, secondDelayedCallback.Delay);
        Assert.AreEqual(0, secondDelayedCallback.ClearInvokes);
        Assert.AreEqual(0, secondDelayedCallback.ResetInvokes);
    }

    [Test]
    public void TimeoutCallbacksResetsCallbackOnTestFinished()
    {
        callBackUnderTest.TestStarted(new TestRunnerTestMock(0));
        callBackUnderTest.TestFinished(new TestRunnerTestResultMock());

        Assert.AreEqual(1, callBackMocks.Count);
        var delayedCallback = callBackMocks.First();
        Assert.AreEqual(600, delayedCallback.Delay);
        Assert.AreEqual(0, delayedCallback.ClearInvokes);
        Assert.AreEqual(1, delayedCallback.ResetInvokes);
    }

    [Test]
    public void TimeoutCallbacksLogsErrorWhenTimeoutIsReached()
    {
        callBackUnderTest.RunStarted(new TestRunnerTestMock(0));

        callBackMocks.First().Action();

        Assert.AreEqual(1, errorsLogged.Count);
        Assert.AreEqual("Test execution timed out.", errorsLogged[0]);
    }

    [Test]
    public void TimeoutCallbacksExitsApplicationWhenTimeoutIsReached()
    {
        callBackUnderTest.RunStarted(new TestRunnerTestMock(0));

        callBackMocks.First().Action();

        Assert.AreEqual(1, exitApplicationCalls.Count);
        Assert.AreEqual((int)Executer.ReturnCodes.RunError, exitApplicationCalls[0]);
    }

    private class DelayedCallbackMock : IDelayedCallback
    {
        internal int ClearInvokes = 0;
        internal int ResetInvokes = 0;
        internal Action Action;
        internal double Delay;

        internal DelayedCallbackMock(Action action, double delay)
        {
            Action = action;
            Delay = delay;
        }

        public void Clear()
        {
            ClearInvokes++;
        }

        public void Reset()
        {
            ResetInvokes++;
        }
    }
}
