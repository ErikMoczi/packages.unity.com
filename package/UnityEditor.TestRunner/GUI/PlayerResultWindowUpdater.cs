using UnityEditor.TestTools.TestRunner;
using UnityEngine.TestTools.TestRunner.GUI;

namespace UnityEditor.TestRunner.GUI
{
    internal class PlayerResultWindowUpdater : ScriptableSingleton<PlayerResultWindowUpdater>
    {
        public void ResetTestState()
        {
            if (TestRunnerWindow.s_Instance == null)
            {
                return;
            }

            foreach (var testRunnerResult in TestRunnerWindow.s_Instance.m_SelectedTestTypes.newResultList)
            {
                testRunnerResult.resultStatus = TestRunnerResult.ResultStatus.NotRun;
                TestRunnerWindow.s_Instance.m_SelectedTestTypes.UpdateResult(testRunnerResult);
            }
            TestRunnerWindow.UpdateWindow();
        }

        public void TestDone(TestRunnerResult testRunnerResult)
        {
            if (TestRunnerWindow.s_Instance == null)
            {
                return;
            }

            TestRunnerWindow.s_Instance.m_SelectedTestTypes.UpdateResult(testRunnerResult);
            TestRunnerWindow.s_Instance.RebuildUIFilter();
            TestRunnerWindow.UpdateWindow();
        }

        public void Error()
        {
            foreach (var testRunnerResult in TestRunnerWindow.s_Instance.m_SelectedTestTypes.newResultList)
            {
                testRunnerResult.resultStatus = TestRunnerResult.ResultStatus.Failed;
                TestRunnerWindow.s_Instance.m_SelectedTestTypes.UpdateResult(testRunnerResult);
            }
            TestRunnerWindow.UpdateWindow();
        }
    }
}
