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
        public ValidationTestReport[] testReports;

        private ValidationSuite suite;

        public ValidationSuiteReport(ValidationSuite suite)
        {
            this.suite = suite;
            BuildReport();
        }

        private void BuildReport()
        {
            testReports = new ValidationTestReport[suite.validationTests.Count()];
            var i = 0;
            foreach (var validationTest in suite.validationTests)
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
        }
        
        public void OutputReport(string path)
        {
            var span = suite.EndTime - suite.StartTime;
            var report = string.Format
                ("{{\"TestResult\":\"{0}\", \"StartTime\":\"{1}\", \"EndTime\":\"{2}\", \"Elapsed\":{3}, \"Tests\":",
                suite.testSuiteState.ToString(),
                suite.StartTime.ToString(),
                suite.EndTime.ToString(),
                span.TotalMilliseconds > 1 ? (int)(span.TotalMilliseconds): 1);

            File.WriteAllText(path, report);
            if (testReports.Length == 0)
            {
                File.AppendAllText(path, "[]}");
                return;
            }
            
            File.AppendAllText(path, "[");
            for (var i = 0 ; i < testReports.Length ; i++)
            {
                File.AppendAllText(path, JsonUtility.ToJson(testReports[i], false));
                if (i < testReports.Length - 1)
                    File.AppendAllText(path, ",");
            }
            File.AppendAllText(path, "]}");
        }
    }
}