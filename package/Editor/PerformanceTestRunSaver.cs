#if UNITY_2018_3_OR_NEWER
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor.TestTools.TestRunner.CommandLineTest;

namespace Unity.PerformanceTesting.Editor
{
    [InitializeOnLoad]
    public class TestRunnerInitializer
    {
        static TestRunnerInitializer()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var obj = ScriptableObject.CreateInstance<PerformanceTestRunSaver>();
            api.RegisterCallbacks(obj);
        }
    }

    [Serializable]
    public class PerformanceTestRunSaver : ScriptableObject, ICallbacks
    {
        private PerformanceTestRun m_Run;

        void ICallbacks.RunStarted(ITest testsToRun)
        {
        }

        void ICallbacks.RunFinished(ITestResult result)
        {
            var resultWriter = new ResultsWriter();
            string xmlPath = Path.Combine(Application.streamingAssetsPath, "TestResults.xml");
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "PerformanceTestResults.json");
            resultWriter.WriteResultToFile(result, xmlPath);
            var xmlParser = new TestResultXmlParser();
            var run = xmlParser.GetPerformanceTestRunFromXml(xmlPath);
            File.WriteAllText(jsonPath, JsonUtility.ToJson(run, true));
        }

        void ICallbacks.TestStarted(ITest test)
        {
        }

        void ICallbacks.TestFinished(ITestResult result)
        {
        }
    }
}
#endif
