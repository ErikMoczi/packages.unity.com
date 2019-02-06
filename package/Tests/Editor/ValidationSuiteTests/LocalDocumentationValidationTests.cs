using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.ValidationSuite.Tests
{
    internal class LocalDocumentationValidationTests
    {
        private const string testDirectory = "tempDocumentationFileValidationTest";
        private const string version = "1.1.0";
        private const string previewVersion = "1.1.0-preview.1";

        [SetUp]
        public void Setup()
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }

            Directory.CreateDirectory(testDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }

        private void CreateFile(string name, string cwd = "", string content = "")
        {
            var toCreatePath = Path.Combine(testDirectory, Path.Combine(cwd, name));

            if (content != "")
            {
                using (FileStream fs = File.Create(toCreatePath))
                {
                    Byte[] info = new UTF8Encoding(true).GetBytes(content);
                    // Add some information to the file.
                    fs.Write(info, 0, info.Length);
                }
            }
            else
            {
                File.Create(toCreatePath).Dispose();
            }
        }

        private void CreateFolder(string name, string cwd = "")
        {
            var toCreatePath = Path.Combine(testDirectory, Path.Combine(cwd, name));
            Directory.CreateDirectory(toCreatePath);
        }

        [TestCase(version)]
        [TestCase(previewVersion)]
        public void When_Local_Documentation_Missing_Validation_Folder_Fails(string currentVersion)
        {
            CreateFolder("folder");
            CreateFile("file1", "folder");
            var documentationValidation = new LocalDocumentationValidation();
            documentationValidation.Context = PrepareVettingContext(testDirectory, currentVersion);
            documentationValidation.RunTest();

            Assert.AreEqual(TestState.Failed, documentationValidation.TestState);
            List<string> messagesExpected = new List<string> { "Error: Your package must contain a \"Documentation~\" folder at the root, which holds your package's documentation." };
            Assert.AreEqual(messagesExpected, documentationValidation.TestOutput);
        }

        [TestCase(version)]
        [TestCase(previewVersion)]
        public void When_Local_Documentation_Has2Folders_Fails(string currentVersion)
        {
            CreateFolder("Documentation~");
            CreateFile("documentation.md", "Documentation~", "This is my documentation");
            CreateFolder(".Documentation");
            CreateFile("documentation.md", ".Documentation", "This is my documentation");
            var documentationValidation = new LocalDocumentationValidation();
            documentationValidation.Context = PrepareVettingContext(testDirectory, currentVersion);
            documentationValidation.RunTest();

            Assert.AreEqual(TestState.Failed, documentationValidation.TestState);
            List<string> messagesExpected = new List<string> { "Error: You have multiple documentation folders. Please keep only the one named \"Documentation~\"." };
            Assert.AreEqual(messagesExpected, documentationValidation.TestOutput);
        }

        [TestCase(version)]
        [TestCase(previewVersion)]
        public void When_Local_Documentation_Has2FoldersWithoutTilde_Fails(string currentVersion)
        {
            CreateFolder("Documentation");
            CreateFile("documentation.md", "Documentation", "This is my documentation");
            CreateFolder(".Documentation");
            CreateFile("documentation.md", ".Documentation", "This is my documentation");
            var documentationValidation = new LocalDocumentationValidation();
            documentationValidation.Context = PrepareVettingContext(testDirectory, currentVersion);
            documentationValidation.RunTest();

            Assert.AreEqual(TestState.Failed, documentationValidation.TestState);
            List<string> messagesExpected = new List<string> { "Error: You have multiple documentation folders. Please keep only the one named \"Documentation~\"." };
            Assert.AreEqual(messagesExpected, documentationValidation.TestOutput);
        }

        [TestCase(version)]
        [TestCase(previewVersion)]
        public void When_Local_Documentation_Has2FoldersWithTilde_Fails(string currentVersion)
        {
            CreateFolder("Documentation~");
            CreateFile("documentation.md", "Documentation~", "This is my documentation");
            CreateFolder(".Documentation~");
            CreateFile("documentation.md", ".Documentation~", "This is my documentation");
            var documentationValidation = new LocalDocumentationValidation();
            documentationValidation.Context = PrepareVettingContext(testDirectory, currentVersion);
            documentationValidation.RunTest();

            Assert.AreEqual(TestState.Failed, documentationValidation.TestState);
            List<string> messagesExpected = new List<string> { "Error: You have multiple documentation folders. Please keep only the one named \"Documentation~\"." };
            Assert.AreEqual(messagesExpected, documentationValidation.TestOutput);
        }

        [TestCase(version)]
        [TestCase(previewVersion)]
        public void When_Local_Documentation_Missing_Validation_File_Fails(string currentVersion)
        {
            CreateFolder("Documentation~");
            var documentationValidation = new LocalDocumentationValidation();
            documentationValidation.Context = PrepareVettingContext(testDirectory, currentVersion);
            documentationValidation.RunTest();

            Assert.AreEqual(TestState.Failed, documentationValidation.TestState);
            List<string> messagesExpected = new List<string> { "Error: Your package must contain a \"Documentation~\" folder, with at least one \"*.md\" file in order for documentation to properly get built." };
            Assert.AreEqual(messagesExpected, documentationValidation.TestOutput);
        }

        [TestCase(version)]
        [TestCase(previewVersion)]
        public void When_Local_Documentation_Only_Contains_Default_File_Fails(string currentVersion)
        {
            CreateFolder("Documentation~");
            CreateFile("your-package-name.md", "Documentation~");
            var documentationValidation = new LocalDocumentationValidation();
            documentationValidation.Context = PrepareVettingContext(testDirectory, currentVersion);
            documentationValidation.RunTest();

            Assert.AreEqual(TestState.Failed, documentationValidation.TestState);
            List<string> messagesExpected = new List<string> { "Error: File \"your-package-name.md\" found in \"Documentation~\" directory, which comes from the package template.  Please take the time to work on your documentation." };
            Assert.AreEqual(messagesExpected, documentationValidation.TestOutput);
        }

        [TestCase(version)]
        [TestCase(previewVersion)]
        public void When_Local_Documentation_Contains_Default_File_Fails(string currentVersion)
        {
            CreateFolder("Documentation~");
            CreateFile("documentation.md", "Documentation~", "This is my documentation");
            CreateFile("your-package-name.md", "Documentation~");
            var documentationValidation = new LocalDocumentationValidation();
            documentationValidation.Context = PrepareVettingContext(testDirectory, currentVersion);
            documentationValidation.RunTest();

            Assert.AreEqual(TestState.Failed, documentationValidation.TestState);
            List<string> messagesExpected = new List<string> { "Error: File \"your-package-name.md\" found in \"Documentation~\" directory, which comes from the package template.  Please take the time to work on your documentation." };
            Assert.AreEqual(messagesExpected, documentationValidation.TestOutput);
        }

        [TestCase(version)]
        [TestCase(previewVersion)]
        public void When_Local_Documentation_InSubFolder_Validation_Fails(string currentVersion)
        {
            CreateFolder("folder1");
            CreateFolder("Documentation~", "folder1");
            CreateFile("documentation.md", "folder1/Documentation~", "This is my documentation");
            var documentationValidation = new LocalDocumentationValidation();
            documentationValidation.Context = PrepareVettingContext(testDirectory, currentVersion);
            documentationValidation.RunTest();

            Assert.AreEqual(TestState.Failed, documentationValidation.TestState);
            List<string> messagesExpected = new List<string> { "Error: Your package must contain a \"Documentation~\" folder at the root, which holds your package's documentation." };
            Assert.AreEqual(messagesExpected, documentationValidation.TestOutput);
        }

        [TestCase(version)]
        [TestCase(previewVersion)]
        public void When_Local_Documentation_Folder_StartingWithDot_Validation_Succeeds(string currentVersion)
        {
            CreateFolder(".Documentation~");
            CreateFile("documentation.md", ".Documentation~", "This is my documentation");
            var documentationValidation = new LocalDocumentationValidation();
            documentationValidation.Context = PrepareVettingContext(testDirectory, currentVersion);
            documentationValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, documentationValidation.TestState);
            Assert.AreEqual(0, documentationValidation.TestOutput.Count);
        }

        [TestCase(version)]
        [TestCase(previewVersion)]
        public void When_Local_Documentation_Folder_EndingWithTilde_Validation_Succeeds(string currentVersion)
        {
            CreateFolder("Documentation~");
            CreateFile("documentation.md", "Documentation~", "This is my documentation");
            var documentationValidation = new LocalDocumentationValidation();
            documentationValidation.Context = PrepareVettingContext(testDirectory, currentVersion);
            documentationValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, documentationValidation.TestState);
            Assert.AreEqual(0, documentationValidation.TestOutput.Count);
        }

        private VettingContext PrepareVettingContext(string packagePath, string version)
        {
            return new VettingContext()
            {
                ProjectPackageInfo = new VettingContext.ManifestData()
                {
                    path = packagePath,
                    version = version
                },
                PublishPackageInfo = new VettingContext.ManifestData()
                {
                    path = packagePath,
                    version = version
                },
                PreviousPackageInfo = new VettingContext.ManifestData()
                {
                    path = packagePath,
                    version = version
                },
                ValidationType = ValidationType.Publishing
            };
        }
    }
}
