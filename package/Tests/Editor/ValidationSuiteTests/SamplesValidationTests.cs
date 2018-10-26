using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using UnityEngine;
using UnityEditor.PackageManager.ValidationSuite;


namespace UnityEditor.PackageManager.ValidationSuite.Tests
{
    internal class SamplesValidationTests
    {
        private string testDirectory = "tempSamplesValidationTests";
        private const string name = "com.unity.mypackage";

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

        private void CreatePackageJsonFile(string name, VettingContext.SampleData[] samples = null)
        {
            var packageJsonPath = Path.Combine(testDirectory, "package.json");
            var nameString = "\"name\":\"" + name + "\"";
            var samplesString = "\"samples\": [";
            if (samples != null)
            {
                for (var i = 0; i < samples.Length; i++)
                {
                    samplesString += "{\"path\":\"" + samples[i].path + "\",";
                    samplesString += "\"displayName\":\"" + samples[i].displayName + "\"}";
                    if(i != samples.Length - 1)
                        samplesString += ",";
                }
            }
            samplesString += "]";
            File.WriteAllText(packageJsonPath, "{" + nameString + "," + samplesString + "}");
        }

        private void CreateSamplesFolder(VettingContext.SampleData sample, bool createSampleJson = true)
        {
            var samplePath = Path.Combine(testDirectory, sample.path);
            Directory.CreateDirectory(samplePath);
            if (createSampleJson)
            {
                var packageJsonPath = Path.Combine(samplePath, ".sample.json");
                File.WriteAllText(packageJsonPath, "{\"displayName\":\"" + sample.displayName + "\"}");
            }
        }

        [Test]
        public void When_No_Samples_Validation_Not_Run()
        {
            CreatePackageJsonFile(name);

            var samplesValidation = new SamplesValidation();
            samplesValidation.Context = PrepareVettingContext(testDirectory);
            samplesValidation.RunTest();
            Assert.AreEqual(TestState.NotRun, samplesValidation.TestState);
        }

        [Test]
        public void When_Both_Samples_And_Samples_Tilde_Exist_Validation_Fails()
        {
            var samples = new VettingContext.SampleData[]
            { 
                new VettingContext.SampleData{path = "Samples", displayName = "Samples" },
                new VettingContext.SampleData{path = "Samples~", displayName = "Samples~" }
            };
            CreateSamplesFolder(samples[0]);
            CreateSamplesFolder(samples[1]);
            CreatePackageJsonFile(name, samples);

            var samplesValidation = new SamplesValidation();
            samplesValidation.Context = PrepareVettingContext(testDirectory);
            samplesValidation.RunTest();
            Assert.AreEqual(TestState.Failed, samplesValidation.TestState);
            Assert.Greater(samplesValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_Samples_Folder_Exists_In_Published_Package_Validation_Fails()
        {
            var samples = new VettingContext.SampleData[] { new VettingContext.SampleData{path = "Samples", displayName = "Samples" } };
            CreateSamplesFolder(samples[0]);
            CreatePackageJsonFile(name, samples);

            var samplesValidation = new SamplesValidation();
            samplesValidation.Context = PrepareVettingContext(testDirectory);
            samplesValidation.RunTest();
            Assert.AreEqual(TestState.Failed, samplesValidation.TestState);
            Assert.Greater(samplesValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_Samples_Tilde_Folder_Exists_In_Published_Package_Validation_Succeeds()
        {
            var samples = new VettingContext.SampleData[] { new VettingContext.SampleData{path = "Samples~", displayName = "Samples~" } };
            CreateSamplesFolder(samples[0]);
            CreatePackageJsonFile(name, samples);

            var samplesValidation = new SamplesValidation();
            samplesValidation.Context = PrepareVettingContext(testDirectory);
            samplesValidation.RunTest();
            Assert.AreEqual(TestState.Succeeded, samplesValidation.TestState);
        }

        [Test]
        public void When_Samples_Folder_Exists_In_Embedded_Package_Validation_Succeeds()
        {
            var samples = new VettingContext.SampleData[] { new VettingContext.SampleData{path = "Samples", displayName = "Samples" } };
            CreateSamplesFolder(samples[0]);
            CreatePackageJsonFile(name, samples);

            var samplesValidation = new SamplesValidation();
            samplesValidation.Context = PrepareVettingContext(testDirectory, false);
            samplesValidation.RunTest();
            Assert.AreEqual(TestState.Succeeded, samplesValidation.TestState);
        }

        [Test]
        public void When_Sample_Path_Not_Set_Validation_Fails()
        {
            var samples = new VettingContext.SampleData[] { new VettingContext.SampleData{path = "Samples~", displayName = "Samples~" } };
            CreateSamplesFolder(samples[0]);
            samples[0].path = string.Empty;
            CreatePackageJsonFile(name, samples);

            var samplesValidation = new SamplesValidation();
            samplesValidation.Context = PrepareVettingContext(testDirectory);
            samplesValidation.RunTest();
            Assert.AreEqual(TestState.Failed, samplesValidation.TestState);
            Assert.Greater(samplesValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_Sample_Display_Name_Not_Set_Validation_Fails()
        {
            var samples = new VettingContext.SampleData[] { new VettingContext.SampleData{path = "Samples~", displayName = "" } };
            CreateSamplesFolder(samples[0]);
            CreatePackageJsonFile(name, samples);

            var samplesValidation = new SamplesValidation();
            samplesValidation.Context = PrepareVettingContext(testDirectory);
            samplesValidation.RunTest();
            Assert.AreEqual(TestState.Failed, samplesValidation.TestState);
            Assert.Greater(samplesValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_Sample_Exist_But_Not_Set_In_Package_Json_Validation_Fails()
        {
            var samples = new VettingContext.SampleData[] { new VettingContext.SampleData{path = "Samples~", displayName = "Samples~" } };
            CreateSamplesFolder(samples[0]);
            CreatePackageJsonFile(name);

            var samplesValidation = new SamplesValidation();
            samplesValidation.Context = PrepareVettingContext(testDirectory);
            samplesValidation.RunTest();
            Assert.AreEqual(TestState.Failed, samplesValidation.TestState);
            Assert.Greater(samplesValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_Samples_Folder_Missing_But_Sample_Info_Found_In_Package_Json_Validation_Fails()
        {
            var samples = new VettingContext.SampleData[] { new VettingContext.SampleData{path = "Samples~", displayName = "Samples~" } };
            CreatePackageJsonFile(name, samples);

            var samplesValidation = new SamplesValidation();
            samplesValidation.Context = PrepareVettingContext(testDirectory);
            samplesValidation.RunTest();
            Assert.AreEqual(TestState.Failed, samplesValidation.TestState);
            Assert.Greater(samplesValidation.TestOutput.Count, 0);
        }

        public void When_Sample_Json_Missing_But_Sample_Info_Found_In_Package_Json_Validation_Fails()
        {
            var samples = new VettingContext.SampleData[] { new VettingContext.SampleData{path = "Samples~", displayName = "Samples~" } };
            CreateSamplesFolder(samples[0], false);
            CreatePackageJsonFile(name, samples);

            var samplesValidation = new SamplesValidation();
            samplesValidation.Context = PrepareVettingContext(testDirectory);
            samplesValidation.RunTest();
            Assert.AreEqual(TestState.Failed, samplesValidation.TestState);
            Assert.Greater(samplesValidation.TestOutput.Count, 0);
        }

        private VettingContext PrepareVettingContext(string packagePath, bool isPublished = true)
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
                IsPublished = isPublished
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
