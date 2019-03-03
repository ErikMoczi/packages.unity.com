using System;
using System.IO;
using System.Xml;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.CommandLineTest;
using UnityEngine;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class ResultsWriterTests
    {
        string expectedErrorString = "Saving result file failed.";

        [Test]
        public void ResultWriterWritingToStream()
        {
            var startTime = new DateTime(2017, 1, 2, 3, 4, 5);
            var endTime = new DateTime(2017, 1, 2, 3, 5, 6);
            var testResult = new TestRunnerTestResultMock()
            {
                ResultState = "Failed",
                SkipCount = 1,
                FailCount = 2,
                PassCount = 3,
                InconclusiveCount = 4,
                AssertCount = 5,
                StartTime = startTime,
                EndTime = endTime,
                Duration = 61
            };

            string clrVersion = (PlayerSettings.scriptingRuntimeVersion == ScriptingRuntimeVersion.Latest) ? "4.0.30319.42000" : "2.0.50727.1433";

            var expectedXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>\n<test-run id=""2"" testcasecount=""10"" result=""Failed"" total=""10"" passed=""3"" failed=""2"" inconclusive=""4"" skipped=""1"" asserts=""5"" engine-version=""3.5.0.0"" clr-version=""" + clrVersion + @""" start-time=""2017-01-02 03:04:05Z"" end-time=""2017-01-02 03:05:06Z"" duration=""61"">\n  <test-result>\n    <InnerValueOfTestResult />\n  </test-result>\n</test-run>";

            var xmlSettings = new XmlWriterSettings()
            {
                NewLineChars = Environment.NewLine
            };

            using (var memoryStream = new MemoryStream())
            {
                var writerUnderTest = new ResultsWriter();

                var streamWriter = new StreamWriter(memoryStream);

                writerUnderTest.WriteResultToStream(testResult, streamWriter, xmlSettings);

                streamWriter.Flush();
                memoryStream.Position = 0;
                string actualXml;

                using (var streamReader = new StreamReader(memoryStream))
                {
                    actualXml = streamReader.ReadToEnd();
                }

                Assert.AreEqual(expectedXml.Replace("\\n", Environment.NewLine), actualXml);
            }
        }

        [Test]
        public void ResultWriterHandlesInvalidFileName_NoPath()
        {
            var testResult = new TestRunnerTestResultMock();
            var writerUnderTest = new ResultsWriter();

            LogAssert.Expect(LogType.Error, expectedErrorString);
            LogAssert.Expect(LogType.Exception, "ArgumentException: Path is empty");

            writerUnderTest.WriteResultToFile(testResult, "Result.xml");
        }

        [Test]
        public void ResultWriterHandlesInvalidFileName_Empty()
        {
            var testResult = new TestRunnerTestResultMock();
            var writerUnderTest = new ResultsWriter();

            LogAssert.Expect(LogType.Error, expectedErrorString);
            LogAssert.Expect(LogType.Exception, "ArgumentException: Invalid path");

            writerUnderTest.WriteResultToFile(testResult, "");
        }
    }
}
