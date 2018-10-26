using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite
{
    internal class ValidationTestReport
    {
        public string TestName;
        public string TestDescription;
        public string TestResult;
        public string[] TestOutput;
        public string StartTime;
        public string EndTime;
        public int Elpased;
    }
    
    internal class ValidationSuiteReport
    {
        public static readonly string resultsPath = "ValidationSuiteResults";

        private readonly string txtReportPath;
        private readonly string jsonReportPath;

        public ValidationSuiteReport()
        { }

        public ValidationSuiteReport(string packageId, string packageName, string packageVersion, string packagePath)
        {
            if (!Directory.Exists(resultsPath))
                Directory.CreateDirectory(resultsPath);

            if (File.Exists(txtReportPath))
                File.Delete(txtReportPath);

            if (File.Exists(jsonReportPath))
                File.Delete(jsonReportPath);

            txtReportPath = Path.Combine(resultsPath, packageId + ".txt");
            jsonReportPath = Path.Combine(resultsPath, packageId + ".json");
            File.WriteAllText(txtReportPath, string.Format("Validation Suite Results for package \"{0}\"\r\n - Path: {1}\r\n - Version: {2}\r\n - Test Time: {3}\r\n\r\n", packageName, packagePath, packageVersion, DateTime.Now));
        }

        private ValidationTestReport[] BuildReport(ValidationSuite suite)
        {
            var testReports = new ValidationTestReport[suite.ValidationTests.Count()];
            var i = 0;
            foreach (var validationTest in suite.ValidationTests)
            {
                testReports[i] = new ValidationTestReport();
                testReports[i].TestName = validationTest.TestName;
                testReports[i].TestDescription = validationTest.TestDescription;
                testReports[i].TestResult = validationTest.TestState.ToString();
                testReports[i].TestOutput = validationTest.TestOutput.ToArray();
                testReports[i].StartTime = validationTest.StartTime.ToString();
                testReports[i].EndTime = validationTest.EndTime.ToString();
                var span = validationTest.EndTime - validationTest.StartTime;
                testReports[i].Elpased = span.TotalMilliseconds > 1 ? (int)(span.TotalMilliseconds): 1;
                i++;
            }

            return testReports;
        }

        public static string TextReportPath(string packageId)
        {
            return Path.Combine(resultsPath, packageId + ".txt");
        }

        public static string DiffsReportPath(string packageId)
        {
            return Path.Combine(resultsPath, packageId + ".delta");
        }

        public static bool ReportExists(string packageId)
        {
            var txtReportPath = Path.Combine(resultsPath, packageId + ".txt");
            return File.Exists(txtReportPath);
        }

        public static bool DiffsReportExists(string packageId)
        {
            var deltaReportPath = Path.Combine(resultsPath, packageId + ".delta");
            return File.Exists(deltaReportPath);
        }

        public void OutputErrorReport(string error)
        {
            File.AppendAllText(txtReportPath, error);
        }

        public void OutputTextReport(ValidationSuite suite)
        {
            SaveTestResult(suite, TestState.Failed);
            SaveTestResult(suite, TestState.Succeeded);
            SaveTestResult(suite, TestState.NotRun);
            SaveTestResult(suite, TestState.NotImplementedYet);
        }

        public void OutputJsonReport(ValidationSuite suite)
        {
            var testReports = BuildReport(suite);
            var span = suite.EndTime - suite.StartTime;
            var report = string.Format
                ("{{\"TestResult\":\"{0}\", \"StartTime\":\"{1}\", \"EndTime\":\"{2}\", \"Elapsed\":{3}, \"Tests\":",
                suite.testSuiteState.ToString(),
                suite.StartTime.ToString(),
                suite.EndTime.ToString(),
                span.TotalMilliseconds > 1 ? (int)(span.TotalMilliseconds): 1);

            File.WriteAllText(jsonReportPath, report);
            if (testReports.Length == 0)
            {
                File.AppendAllText(jsonReportPath, "[]}");
                return;
            }
            
            File.AppendAllText(jsonReportPath, "[");
            for (var i = 0 ; i < testReports.Length ; i++)
            {
                File.AppendAllText(jsonReportPath, JsonUtility.ToJson(testReports[i], false));
                if (i < testReports.Length - 1)
                    File.AppendAllText(jsonReportPath, ",");
            }
            File.AppendAllText(jsonReportPath, "]}");
        }

        private void SaveTestResult(ValidationSuite suite, TestState testState)
        {
            foreach (var testResult in suite.ValidationTests.Where(t => t.TestState == testState))
            {
                File.AppendAllText(txtReportPath, string.Format("\r\n{0} - \"{1}\"\r\n", testResult.TestState, testResult.TestName));
                if (testResult.TestOutput.Any())
                    File.AppendAllText(txtReportPath, string.Join("\r\n", testResult.TestOutput.ToArray()) + "\r\n");
            }
        }
    }
}