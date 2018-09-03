using System.Collections.Generic;
using UnityEditor.TestTools.TestRunner.Api;

namespace UnityEditor.TestTools.TestRunner.UnityTestProtocol
{
    interface ITestRunnerApiMapper
    {
        string GetRunStateFromResultNunitXml(ITestResult result);
        TestState GetTestStateFromResult(ITestResult result);
        List<string> FlattenTestNames(ITest testsToRun);
        TestPlanMessage MapTestToTestPlanMessage(ITest testsToRun);
        TestStartedMessage MapTestToTestStartedMessage(ITest test);
        TestFinishedMessage TestResultToTestFinishedMessage(ITestResult result);
    }
}
