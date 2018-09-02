using System.IO;
using NUnit.Framework;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite.Tests
{
    internal class ManifestValidationTests
    {
        private string testDirectory = "tempTest";
        private string packagesJsonPath;

        [SetUp]
        public void Setup()
        {
            packagesJsonPath = Path.Combine(testDirectory, "package.json");
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
            File.WriteAllText(packagesJsonPath, "Test is not a package.json file");
            var manifestValidation = new ManifestValidation();

            manifestValidation.Setup();
            manifestValidation.Run(packagesJsonPath);

            Assert.AreEqual(TestState.Failed, manifestValidation.TestState);
            Assert.AreEqual(1, manifestValidation.TestOutput.Count);
        }

        [Test]
        public void When_Manifest_OK_Validation_Succeeds()
        {
            var manifestData = GenerateValidManifestData();
            var manifestValidation = SetupTestManifestAndRunValidation(manifestData);

            Assert.AreEqual(TestState.Succeeded, manifestValidation.TestState);
            Assert.AreEqual(0, manifestValidation.TestOutput.Count);
        }

        [Test]
        public void When_Name_Invalid_Validation_Fails()
        {
            var manifestData = GenerateValidManifestData();

            // Put in a bad name
            manifestData.name = "com.bad.name";
            var manifestValidation = SetupTestManifestAndRunValidation(manifestData);

            Assert.AreEqual(TestState.Failed, manifestValidation.TestState);
            Assert.AreEqual(1, manifestValidation.TestOutput.Count);

            // Put in capital letters
            manifestData.name = "com.unity.ProjectName";
            manifestValidation = SetupTestManifestAndRunValidation(manifestData);

            Assert.AreEqual(TestState.Failed, manifestValidation.TestState);
            Assert.AreEqual(2, manifestValidation.TestOutput.Count);
        }

        [Test]
        public void When_Description_Short_Validation_Fails()
        {
            var manifestData = GenerateValidManifestData();

            // Put in a bad name
            manifestData.description = "short description";
            var manifestValidation = SetupTestManifestAndRunValidation(manifestData);

            Assert.AreEqual(TestState.Failed, manifestValidation.TestState);
            Assert.AreEqual(1, manifestValidation.TestOutput.Count);
        }

        [Test]
        public void When_PackageVersion_WrongFormat_Validation_Fails()
        {
            var manifestData = GenerateValidManifestData();

            // Put in a bad name
            manifestData.version = "1.a.2";
            var manifestValidation = SetupTestManifestAndRunValidation(manifestData);

            Assert.AreEqual(TestState.Failed, manifestValidation.TestState);
            Assert.AreEqual(1, manifestValidation.TestOutput.Count);
        }

        [Test]
        public void When_UnityVersion_NotMatching_Validation_Fails()
        {
            var manifestData = GenerateValidManifestData();

            // Put in a bad name
            manifestData.unity = "2017.1";
            var manifestValidation = SetupTestManifestAndRunValidation(manifestData);

            Assert.AreEqual(TestState.Failed, manifestValidation.TestState);
            Assert.AreEqual(1, manifestValidation.TestOutput.Count);
        }

        private ManifestValidation SetupTestManifestAndRunValidation(ManifestValidation.ManifestData manifestData)
        {
            File.WriteAllText(packagesJsonPath, JsonUtility.ToJson(manifestData));

            var manifestValidation = new ManifestValidation();
            manifestValidation.Setup();
            manifestValidation.Run(testDirectory);

            return manifestValidation;
        }

        private ManifestValidation.ManifestData GenerateValidManifestData()
        {
            return new ManifestValidation.ManifestData()
            {
                name = "com.unity.mytestpackage",
                version = "0.1.1-preview",
                unity = UnityEngine.Application.unityVersion.Substring(0, UnityEngine.Application.unityVersion.LastIndexOf(".")),
                description = "This is a test description which needs to be long enough so the test passes."
            };

        }
    }
}
