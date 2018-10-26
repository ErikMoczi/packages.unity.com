using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite.Tests
{
    internal class ManifestValidationTests
    {
        private string testDirectory = "tempManifestValidationTests";

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
            var packageJsonPath = Path.Combine(testDirectory, "package.json");
            File.WriteAllText(packageJsonPath, "Test is not a package.json file");
            var manifestValidation = new ManifestValidation();
            manifestValidation.Context = PrepareVettingContext(packageJsonPath);
            manifestValidation.Setup();
            manifestValidation.RunTest();

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
        public void When_UnityVersion_GreaterThanCurrent_Validation_Fails()
        {
            var manifestData = GenerateValidManifestData();

            // Put in a bad name
            manifestData.unity = "2099.0";
            var manifestValidation = SetupTestManifestAndRunValidation(manifestData);

            Assert.AreEqual(TestState.Failed, manifestValidation.TestState);
            Assert.AreEqual(1, manifestValidation.TestOutput.Count);
        }

        private ManifestValidation SetupTestManifestAndRunValidation(VettingContext.ManifestData manifestData)
        {
            var packageJsonPath = Path.Combine(testDirectory, "package.json");
            File.WriteAllText(packageJsonPath, JsonUtility.ToJson(manifestData));

            var manifestValidation = new ManifestValidation();
            manifestValidation.Context = PrepareVettingContext(packageJsonPath);
            manifestValidation.Setup();
            manifestValidation.RunTest();

            return manifestValidation;
        }

        private VettingContext.ManifestData GenerateValidManifestData()
        {
            return new VettingContext.ManifestData()
            {
                name = "com.unity.mytestpackage",
                version = "0.1.1-preview",
                unity = UnityEngine.Application.unityVersion.Substring(0, UnityEngine.Application.unityVersion.LastIndexOf(".")),
                description = "This is a test description which needs to be long enough so the test passes."
            };
        }

        private VettingContext PrepareVettingContext(string packagePath)
        {
            var packageJsonPath = Path.Combine(testDirectory, "package.json");
            var packageJson = File.ReadAllText(packageJsonPath);
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
                PreviousPackageInfo = manifestData
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
