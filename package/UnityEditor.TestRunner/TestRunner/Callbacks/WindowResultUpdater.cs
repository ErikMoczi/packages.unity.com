using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestTools.TestRunner;
using UnityEngine.TestTools.TestRunner.GUI;

namespace UnityEditor.TestTools.TestRunner.GUI
{
    internal class WindowResultUpdater : ScriptableObject, ITestRunnerListener
    {
        public void RunStarted(ITest testsToRun)
        {
        }

        public void RunFinished(ITestResult testResults)
        {
            if (TestRunnerWindow.s_Instance != null)
            {
                TestRunnerWindow.s_Instance.RebuildUIFilter();
            }
        }

        public void TestStarted(ITest testName)
        {
        }

        public void TestFinished(ITestResult test)
        {
            if (TestRunnerWindow.s_Instance == null)
                return;

            var result = new TestRunnerResult(test);
            TestRunnerWindow.s_Instance.m_SelectedTestTypes.UpdateResult(result);
            TestRunnerWindow.s_Instance.m_SelectedTestTypes.Repaint();
            TestRunnerWindow.s_Instance.Repaint();
        }
    }
}
