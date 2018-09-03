using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestTools.TestRunner;
using UnityEngine.TestTools.TestRunner.GUI;
using Debug = UnityEngine.Debug;

namespace UnityEditor.TestTools.TestRunner.UnityTestProtocol
{
    [Serializable]
    internal abstract class Message
    {
        public string type;

        // Milliseconds since unix epoch
        public ulong time;

        public int version = 2;

        public string phase = "Immediate";

        public int processId = Process.GetCurrentProcess().Id;
    }

    internal class TestPlanMessage : Message
    {
        public List<string> tests;
    }

    internal class TestStartedMessage : Message
    {
        public string name;
        public TestState state;
    }

    internal class TestFinishedMessage : Message
    {
        public string name;
        public TestState state;
        public string message;
        public ulong duration; // milliseconds
        public ulong durationMicroseconds;
    }

    // This matches the state definitions expected by the Perl code, which in turn matches the NUnit 2 values...
    internal enum TestState
    {
        Inconclusive = 0,
        NotRunnable = 1,
        Skipped = 2,
        Ignored = 3,
        Success = 4,
        Failure = 5,
        Error = 6
    }

    internal static class UnityTestProtocolServer
    {
        private static bool? _isEnabled;
        public static bool enabled
        {
            get
            {
                if (!_isEnabled.HasValue)
                    _isEnabled = Environment.GetCommandLineArgs().Contains("-automated");

                return _isEnabled.Value;
            }
        }

        private static void FlattenTestNames(ITest test, ICollection<string> results, ITestFilter filter = null)
        {
            if (test == null)
                return;

            if (!test.IsSuite && (filter == null || filter.Pass(test)))
                results.Add(test.FullName);

            foreach (var child in test.Tests)
                FlattenTestNames(child, results, filter);
        }

        public static void EmitMessage(Message msg)
        {
            msg.time = Convert.ToUInt64((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds);
            Debug.Log("\n##utp:" + JsonUtility.ToJson(msg));
        }

        public static void ReportTestPlan(ITest testsToRun, TestRunnerFilter filter)
        {
            if (!enabled)
                return;

            var msg = new TestPlanMessage
            {
                type = "TestPlan",
                tests = new List<string>()
            };
            FlattenTestNames(testsToRun, msg.tests, filter != null ? filter.BuildNUnitFilter() : null);

            EmitMessage(msg);
        }

        public static void ReportTestStart(ITest test)
        {
            if (!enabled)
                return;

            if (test.IsSuite)
                return;

            var msg = new TestStartedMessage
            {
                type = "TestStatus",
                phase = "Begin",
                state = TestState.Inconclusive,
                name = test.FullName
            };
            EmitMessage(msg);
        }

        private static bool EqualsIgnoringSite(ResultState a, ResultState b)
        {
            return a.Status == b.Status && a.Label == b.Label;
        }

        public static void ReportTestFinish(ITestResult result)
        {
            if (!enabled)
                return;

            if (result.Test.IsSuite)
                return;

            var msg = new TestFinishedMessage
            {
                type = "TestStatus",
                phase = "End",
                name = result.Test.FullName,
                duration = Convert.ToUInt64(result.Duration * 1000),
                durationMicroseconds = Convert.ToUInt64(result.Duration * 1000000),
                message = result.Message
            };

            var state = result.ResultState;

            switch (state.Status)
            {
                case TestStatus.Passed:
                    msg.state = TestState.Success;
                    break;
                case TestStatus.Inconclusive:
                    msg.state = TestState.Inconclusive;
                    break;
                default:
                    // We cannot distinguish the other states based on the Status field alone - we have to compare the label too
                    if (EqualsIgnoringSite(state, ResultState.NotRunnable))
                        msg.state = TestState.NotRunnable;
                    else if (EqualsIgnoringSite(state, ResultState.Skipped) || EqualsIgnoringSite(state, ResultState.Explicit))
                        msg.state = TestState.Skipped;
                    else if (EqualsIgnoringSite(state, ResultState.Ignored))
                        msg.state = TestState.Ignored;
                    else if (EqualsIgnoringSite(state, ResultState.Failure))
                        msg.state = TestState.Failure;
                    else
                        msg.state = TestState.Error;
                    break;
            }
            EmitMessage(msg);
        }
    }

    internal class UnityTestProtocolListener : ScriptableObject, ITestRunnerListener
    {
        public void RunStarted(ITest testsToRun)
        {
            UnityTestProtocolServer.ReportTestPlan(testsToRun, null);
        }

        public void RunFinished(ITestResult testResults)
        {
        }

        public void TestStarted(ITest test)
        {
            UnityTestProtocolServer.ReportTestStart(test);
        }

        public void TestFinished(ITestResult result)
        {
            UnityTestProtocolServer.ReportTestFinish(result);
        }
    }
}
