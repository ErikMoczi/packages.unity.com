using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite.Tests
{
    internal class ManifestValidationTests
    {
        private string testDirectory = Path.Combine(Path.GetTempPath(), "tempManifestValidationTests");

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

        [Test]
        public void When_Manifest_WrongFormat_Validation_Fails()
        {
            var projectPackageInfo = new VettingContext.ManifestData
            {
                path = Path.Combine(testDirectory, "package.json")
            };

            var manifestValidation = new ManifestValidation();
            var vettingContext = new VettingContext
            {
                ProjectPackageInfo = projectPackageInfo,
                PublishPackageInfo = projectPackageInfo,
                PreviousPackageInfo = null,
                ValidationType = ValidationType.Publishing
            };
            manifestValidation.Context = vettingContext;
            manifestValidation.Setup();
            manifestValidation.RunTest();

            Assert.AreEqual(TestState.Failed, manifestValidation.TestState);
            Assert.Greater(manifestValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_Manifest_OK_Validation_Succeeds()
        {
            var manifestData = GenerateValidManifestData();
            var manifestValidation = SetupTestManifestAndRunValidation(manifestData, null, ValidationType.Publishing);

            Assert.AreEqual(TestState.Succeeded, manifestValidation.TestState);
            Assert.AreEqual(0, manifestValidation.TestOutput.Count);
        }

        [Test]
        public void When_Manifest_No_References_Validation_Succeeds()
        {
            var manifestData = GenerateValidManifestData();
            var manifestValidation = SetupTestManifestAndRunValidation(manifestData, null, ValidationType.Publishing);

            Assert.AreEqual(TestState.Succeeded, manifestValidation.TestState);
            Assert.AreEqual(0, manifestValidation.TestOutput.Count);
        }

        [Test]
        public void When_Name_Valid_Validation_Succeeds()
        {
            var manifestData = GenerateValidManifestData();

            manifestData.name = "com.unity.name";
            var manifestValidation = SetupTestManifestAndRunValidation(manifestData, null, ValidationType.Publishing);

            Assert.AreEqual(TestState.Succeeded, manifestValidation.TestState);
            Assert.AreEqual(0, manifestValidation.TestOutput.Count);

            manifestData.name = "com.autodesk.name";
            manifestValidation = SetupTestManifestAndRunValidation(manifestData, null, ValidationType.Publishing);

            Assert.AreEqual(TestState.Succeeded, manifestValidation.TestState);
            Assert.AreEqual(manifestValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_Name_Invalid_Validation_Fails()
        {
            var manifestData = GenerateValidManifestData();

            // Put in a bad name
            manifestData.name = "com.unity.[your package name]";
            var manifestValidation = SetupTestManifestAndRunValidation(manifestData, null, ValidationType.Publishing);

            Assert.AreEqual(TestState.Failed, manifestValidation.TestState);
            Assert.AreEqual(1, manifestValidation.TestOutput.Count);

            // Put in a bad name
            manifestData.name = "com.bad.name";
            manifestValidation = SetupTestManifestAndRunValidation(manifestData, null, ValidationType.Publishing);

            Assert.AreEqual(TestState.Failed, manifestValidation.TestState);
            Assert.AreEqual(1, manifestValidation.TestOutput.Count);

            // Put in capital letters
            manifestData.name = "com.unity.ProjectName";
            manifestValidation = SetupTestManifestAndRunValidation(manifestData, null, ValidationType.Publishing);

            Assert.AreEqual(TestState.Failed, manifestValidation.TestState);
            Assert.Greater(manifestValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_Description_Short_Validation_Fails()
        {
            var manifestData = GenerateValidManifestData();

            // Put in a bad name
            manifestData.description = "short description";
            var manifestValidation = SetupTestManifestAndRunValidation(manifestData);

            Assert.AreEqual(TestState.Failed, manifestValidation.TestState);
            Assert.Greater(manifestValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_PackageVersion_WrongFormat_Validation_Fails()
        {
            var manifestData = GenerateValidManifestData();

            // Put in a bad name
            manifestData.version = "1.a.2";
            var manifestValidation = SetupTestManifestAndRunValidation(manifestData);

            Assert.AreEqual(TestState.Failed, manifestValidation.TestState);
            Assert.Greater(manifestValidation.TestOutput.Count, 0);
        }

        [Test, Ignore("Don't know whether this is a valuable test.")]
        public void When_UnityVersion_GreaterThanCurrent_Validation_Fails()
        {
            var manifestData = GenerateValidManifestData();

            // Put in a bad name
            manifestData.unity = "2099.0";
            var manifestValidation = SetupTestManifestAndRunValidation(manifestData);

            Assert.AreEqual(TestState.Failed, manifestValidation.TestState);
        }

        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInMinor")]
        public void DependencyAdded_FailsInMinor(ReleaseType releaseType, bool errorExpected)
        {
            var previousManifestData = GenerateValidManifestData();
            var projectManifestData = GenerateValidManifestData();

            projectManifestData.dependencies = new Dictionary<string, string>
            {
                {"package1", "1.0.0"}
            };
            projectManifestData.version = VersionComparisonTestUtilities.VersionForReleaseType(releaseType);

            var messagesExpected = new List<string>
            {
                "Warning: Package dependency package1@1.0.0 must be published to production before this package is published to production.  (Except for core packages)",
                @"New dependency: ""package1"": ""1.0.0""", "Error: Adding package dependencies requires a new major version."
            };

            if (errorExpected)
            {
                messagesExpected.Insert(0, "Skipping Git tags check as this is a package in development.");
            }

            var manifestValidation = SetupTestManifestAndRunValidation(projectManifestData, previousManifestData);

            ExpectResult(errorExpected, manifestValidation, messagesExpected);
        }

        static IEnumerable<TestCaseData> When_DependencyChangedToDifferentVersion_Cases_Error()
        {
            yield return new TestCaseData(ReleaseType.Patch, false, ReleaseType.Minor);
            yield return new TestCaseData(ReleaseType.Patch, false, ReleaseType.Patch);
            yield return new TestCaseData(ReleaseType.Minor, false, ReleaseType.Minor);
            yield return new TestCaseData(ReleaseType.Minor, false, ReleaseType.Patch);
            yield return new TestCaseData(ReleaseType.Major, true, ReleaseType.Minor);
            yield return new TestCaseData(ReleaseType.Major, true, ReleaseType.Patch);
            yield return new TestCaseData(ReleaseType.Major, false, ReleaseType.Major);
        }

        [Test]
        [TestCaseSource("When_DependencyChangedToDifferentVersion_Cases_Error")]
        public void When_DependencyChangedToDifferentVersionError(ReleaseType packageReleaseType, bool errorExpected, ReleaseType dependencyReleaseType)
        {
            var previousManifestData = GenerateValidManifestData();
            var projectManifestData = GenerateValidManifestData();

            previousManifestData.dependencies = new Dictionary<string, string>
            {
                { "package1", "0.0.1-preview" }
            };
            projectManifestData.dependencies = new Dictionary<string, string>
            {
                { "package1", VersionComparisonTestUtilities.VersionForReleaseType(dependencyReleaseType) }
            };
            projectManifestData.version = VersionComparisonTestUtilities.VersionForReleaseType(packageReleaseType);

            var messagesExpected = new List<string>
            {
                string.Format(@"Error: This production quality package has a dependency on preview package ""{0}"".  Production quality packages can only depend on other production quality packages.", projectManifestData.dependencies["package1"]),
                string.Format(@"Warning: Package dependency package1@{0} must be published to production before this package is published to production.  (Except for core packages)", projectManifestData.dependencies["package1"])
            };

            if (errorExpected)
            {
                messagesExpected.Insert(0, "Skipping Git tags check as this is a package in development.");
            }

            var manifestValidation = SetupTestManifestAndRunValidation(projectManifestData, previousManifestData);

            ExpectResult(errorExpected, manifestValidation, messagesExpected);
        }

        static IEnumerable<TestCaseData> When_DependencyChangedToDifferentVersion_CasesWarning()
        {
            yield return new TestCaseData(ReleaseType.Patch, true, ReleaseType.Major);
            yield return new TestCaseData(ReleaseType.Minor, true, ReleaseType.Major);
        }

        [Test]
        [TestCaseSource("When_DependencyChangedToDifferentVersion_CasesWarning")]
        public void When_DependencyChangedToDifferentVersionWarning(ReleaseType packageReleaseType, bool errorExpected, ReleaseType dependencyReleaseType)
        {
            var previousManifestData = GenerateValidManifestData();
            var projectManifestData = GenerateValidManifestData();

            previousManifestData.dependencies = new Dictionary<string, string>
            {
                { "package1", "0.0.1-preview" }
            };
            projectManifestData.dependencies = new Dictionary<string, string>
            {
                { "package1", VersionComparisonTestUtilities.VersionForReleaseType(dependencyReleaseType) }
            };
            projectManifestData.version = VersionComparisonTestUtilities.VersionForReleaseType(packageReleaseType);

            var messagesExpected = new List<string>
            {
                "Skipping Git tags check as this is a package in development.",
                string.Format("Warning: Package dependency package1@{0} must be published to production before this package is published to production.  (Except for core packages)", projectManifestData.dependencies["package1"]),
                string.Format(@"Error: Dependency major versions may only change in major releases. ""package1"": ""{0}"" -> ""1.0.0""", previousManifestData.dependencies["package1"])
            };

            var manifestValidation = SetupTestManifestAndRunValidation(projectManifestData, previousManifestData);

            ExpectResult(errorExpected, manifestValidation, messagesExpected);
        }

        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInPatch")]
        public void DependencyRemoved_FailsInPatch(ReleaseType releaseType, bool errorExpected)
        {
            var previousManifestData = GenerateValidManifestData();
            var projectManifestData = GenerateValidManifestData();

            previousManifestData.dependencies = new Dictionary<string, string>
            {
                { "package1", "0.0.1-preview" }
            };
            projectManifestData.version = VersionComparisonTestUtilities.VersionForReleaseType(releaseType);

            var messagesExpected = new List<string>
            {
                "Skipping Git tags check as this is a package in development.",
                "Error: Removing dependencies is not forwards-compatible and requires a new major or minor version. Removed dependency: package1"
            };

            var manifestValidation = SetupTestManifestAndRunValidation(projectManifestData, previousManifestData);

            ExpectResult(errorExpected, manifestValidation, messagesExpected);
        }

        [Test]
        public void InvalidDependencyVersionString_Fails()
        {
            var projectManifestData = GenerateValidManifestData();
            projectManifestData.dependencies = new Dictionary<string, string>
            {
                { "package1", "0.0.a" }
            };

            var messagesExpected = new List<string>
            {
                "Skipping Git tags check as this is a package in development.",
                "Warning: Package dependency package1@0.0.a must be published to production before this package is published to production.  (Except for core packages)",
                @"Error: Invalid version number in dependency ""package1"" : ""0.0.a"""
            };

            var manifestValidation = SetupTestManifestAndRunValidation(projectManifestData);

            ExpectResult(true, manifestValidation, messagesExpected);
        }

        private static void ExpectResult(bool errorExpected, ManifestValidation manifestValidation, List<string> messagesExpected)
        {
            if (errorExpected)
            {
                Assert.AreEqual(TestState.Failed, manifestValidation.TestState);
                Assert.That(manifestValidation.TestOutput, Is.EquivalentTo(messagesExpected));
            }
            else
                Assert.AreEqual(TestState.Succeeded, manifestValidation.TestState);
        }

        private ManifestValidation SetupTestManifestAndRunValidation(VettingContext.ManifestData projectManifestData, VettingContext.ManifestData previousManifestData = null, ValidationType validationType = ValidationType.LocalDevelopment)
        {
            CreateAndWriteManifest(projectManifestData, "Project");
            if (previousManifestData != null)
                CreateAndWriteManifest(previousManifestData, "Previous");

            var manifestValidation = new ManifestValidation();
            var vettingContext = new VettingContext
            {
                ProjectPackageInfo = projectManifestData,
                PublishPackageInfo = projectManifestData,
                PreviousPackageInfo = previousManifestData,
                IsCore = false,
                ValidationType = validationType
            };
            manifestValidation.Context = vettingContext;
            manifestValidation.Setup();
            manifestValidation.RunTest();

            return manifestValidation;
        }

        private void CreateAndWriteManifest(VettingContext.ManifestData projectManifestData, string directoryName)
        {
            var packageJsonPath = Path.Combine(testDirectory + directoryName, "package.json");
            Directory.CreateDirectory(Path.GetDirectoryName(packageJsonPath));
            var contents = JsonUtility.ToJson(projectManifestData);
            var deps = string.Join(",\n",
                projectManifestData.dependencies.Select(d => string.Format("\"{0}\":\"{1}\"", d.Key, d.Value)).ToArray());
            contents.Insert(contents.LastIndexOf("}"), @"""dependencies"": { " + deps + " }");
            File.WriteAllText(packageJsonPath, contents);
            projectManifestData.path = packageJsonPath;
        }

        private VettingContext.ManifestData GenerateValidManifestData()
        {
            var manifest = new VettingContext.ManifestData()
            {
                displayName = "My Test Package",
                name = "com.unity.mytestpackage",
                version = "0.0.1-preview",
                unity = UnityEngine.Application.unityVersion.Substring(0, UnityEngine.Application.unityVersion.LastIndexOf(".")),
                description = "This is a test description which needs to be long enough so the test passes.",
                repository = new Dictionary<string, string>()
            };

            manifest.repository["revision"] = "1234567890123456789012345678909123456789";
            manifest.repository["url"] = "http://test";

            return manifest;
        }
    }
}
