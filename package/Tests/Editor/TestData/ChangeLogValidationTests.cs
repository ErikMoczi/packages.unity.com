using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using UnityEngine;
using UnityEditor.PackageManager.ValidationSuite;


namespace UnityEditor.PackageManager.ValidationSuite.Tests
{
    internal class ChangeLogValidationTests
    {
        private string testDirectory = "tempChangeLogValidationTests";

        [SetUp]
        public void Setup()
        {
            if (!Directory.Exists(testDirectory))
            {
                Directory.CreateDirectory(testDirectory);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }

        private void CreateChangeLog(string content)
        {
            var changeLogPath = Path.Combine(testDirectory, Utilities.ChangeLogFilename);
            File.AppendAllText(changeLogPath, content);
        }

        private void CreatePackageJsonFile(string version)
        {
            var packageJsonPath = Path.Combine(testDirectory, "package.json");
            File.WriteAllText(packageJsonPath, "{\"version\":\"" + version + "\"}");
        }

        [Test]
        public void When_ChangeLog_IsMissing_Validation_Fails()
        {
            CreatePackageJsonFile("1.0.0");
            var changeLogValiation = new ChangeLogValidation();
            changeLogValiation.Context = PrepareVettingContext(testDirectory);
            changeLogValiation.RunTest();
            Assert.AreEqual(TestState.Failed, changeLogValiation.TestState);
            Assert.AreEqual(1, changeLogValiation.TestOutput.Count);
        }

        [Test]
        public void When_PackageJson_IsMissing_Validation_Fails()
        {
            CreatePackageJsonFile("1.0.0");
            var changeLogValiation = new ChangeLogValidation();
            changeLogValiation.Context = PrepareVettingContext(testDirectory);
            changeLogValiation.RunTest();
            Assert.AreEqual(TestState.Failed, changeLogValiation.TestState);
            Assert.AreEqual(1, changeLogValiation.TestOutput.Count);
        }

        [Test]
        public void When_Version_IsMissing_In_ChangeLog_Validation_Fails()
        {
            CreatePackageJsonFile("1.0.0");
            CreateChangeLog("## [2.0.0] - 2033-12-31");

            var changeLogValiation = new ChangeLogValidation();
            changeLogValiation.Context = PrepareVettingContext(testDirectory);
            changeLogValiation.RunTest();
            Assert.AreEqual(TestState.Failed, changeLogValiation.TestState);
            Assert.AreEqual(1, changeLogValiation.TestOutput.Count);
        }

        [Test]
        public void When_Date_IsMissing_In_ChangeLog_Validation_Fails()
        {
            CreatePackageJsonFile("1.0.0");
            CreateChangeLog("## [2.0.0]");

            var changeLogValiation = new ChangeLogValidation();
            changeLogValiation.Context = PrepareVettingContext(testDirectory);
            changeLogValiation.RunTest();
            Assert.AreEqual(TestState.Failed, changeLogValiation.TestState);
            Assert.AreEqual(1, changeLogValiation.TestOutput.Count);
        }

        [Test]
        public void When_DateFormat_IsWrong_In_ChangeLog_Validation_Fails()
        {
            CreatePackageJsonFile("1.0.0");
            CreateChangeLog("## [2.0.0] - 31-12-2033");

            var changeLogValiation = new ChangeLogValidation();
            changeLogValiation.Context = PrepareVettingContext(testDirectory);
            changeLogValiation.RunTest();
            Assert.AreEqual(TestState.Failed, changeLogValiation.TestState);
            Assert.AreEqual(1, changeLogValiation.TestOutput.Count);
        }

        [Test]
        public void When_LogFormat_IsWrong_In_ChangeLog_Validation_Fails()
        {
            CreatePackageJsonFile("1.0.0");
            CreateChangeLog("## 31-12-2033 - [2.0.0]");

            var changeLogValiation = new ChangeLogValidation();
            changeLogValiation.Context = PrepareVettingContext(testDirectory);
            changeLogValiation.RunTest();
            Assert.AreEqual(TestState.Failed, changeLogValiation.TestState);
            Assert.AreEqual(1, changeLogValiation.TestOutput.Count);
        }

        [Test]
        public void When_Version_and_Date_ArePresent_In_ChangeLog_Validation_Succeeds()
        {
            CreatePackageJsonFile("1.0.1");
            CreateChangeLog("## [1.0.1] - 2033-12-31");

            var changeLogValiation = new ChangeLogValidation();
            changeLogValiation.Context = PrepareVettingContext(testDirectory);
            changeLogValiation.RunTest();
            Assert.AreEqual(TestState.Succeeded, changeLogValiation.TestState);
            Assert.AreEqual(0, changeLogValiation.TestOutput.Count);
        }

        [Test]
        public void When_Version_and_Date_ArePresent_But_NotFirst_In_ChangeLog_Validation_Fails()
        {
            CreatePackageJsonFile("1.0.1");
            CreateChangeLog("## [5.0.1] - 2033-12-31");
            CreateChangeLog("## [4.0.1] - 2033-12-31");
            CreateChangeLog("## [3.0.1] - 2033-12-31");
            CreateChangeLog("## [2.0.1] - 2033-12-31");
            CreateChangeLog("## [1.0.1] - 2033-12-31");

            var changeLogValiation = new ChangeLogValidation();
            changeLogValiation.Context = PrepareVettingContext(testDirectory);
            changeLogValiation.RunTest();
            Assert.AreEqual(TestState.Failed, changeLogValiation.TestState);
            Assert.AreEqual(1, changeLogValiation.TestOutput.Count);
        }

        [Test]
        public void When_Version_and_Date_ArePresent_And_First_In_ChangeLog_Validation_Succeeds()
        {
            CreatePackageJsonFile("5.0.1");
            CreateChangeLog("## [5.0.1] - 2033-12-31");
            CreateChangeLog("## [4.0.1] - 2033-12-31");
            CreateChangeLog("## [3.0.1] - 2033-12-31");
            CreateChangeLog("## [2.0.1] - 2033-12-31");
            CreateChangeLog("## [1.0.1] - 2033-12-31");

            var changeLogValiation = new ChangeLogValidation();
            changeLogValiation.Context = PrepareVettingContext(testDirectory);
            changeLogValiation.RunTest();
            Assert.AreEqual(TestState.Succeeded, changeLogValiation.TestState);
            Assert.AreEqual(0, changeLogValiation.TestOutput.Count);
        }

        [Test]
        public void When_Version_and_Date_IsMissing_In_Big_ChangeLog_Validation_Fails()
        {
            CreatePackageJsonFile("6.0.1");
            CreateChangeLog("## [5.0.1] - 2033-12-31");
            CreateChangeLog("## [4.0.1] - 2033-12-31");
            CreateChangeLog("## [3.0.1] - 2033-12-31");
            CreateChangeLog("## [2.0.1] - 2033-12-31");
            CreateChangeLog("## [1.0.1] - 2033-12-31");

            var changeLogValiation = new ChangeLogValidation();
            changeLogValiation.Context = PrepareVettingContext(testDirectory);
            changeLogValiation.RunTest();
            Assert.AreEqual(TestState.Failed, changeLogValiation.TestState);
            Assert.AreEqual(1, changeLogValiation.TestOutput.Count);
        }

        private VettingContext PrepareVettingContext(string packagePath)
        {
            var packageJson = File.ReadAllText(Path.Combine(packagePath, "package.json"));
            VettingContext.ManifestData manifestData = null;
            try
            {
                manifestData = JsonUtility.FromJson<VettingContext.ManifestData>(packageJson);
            }
            catch (Exception)
            {
            }

            var vettingContext = new VettingContext
            {
                ProjectPackageInfo = manifestData,
                PublishPackageInfo = manifestData,
                PreviousPackageInfo = manifestData,
                ValidationType = ValidationType.Publishing
            };

            if (manifestData != null)
            {
                vettingContext.ProjectPackageInfo.path = packagePath;
                vettingContext.PublishPackageInfo.path = packagePath;
                vettingContext.PreviousPackageInfo.path = packagePath;
            }

            return vettingContext;
        }
    }
}
