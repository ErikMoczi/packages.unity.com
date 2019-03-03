using System.IO;
using NUnit.Framework;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using UnityEngine;


namespace UnityEditor.PackageManager.ValidationSuite.Tests
{
    internal class MetaFilesValidationTests
    {
        private const string testDirectory = "tempMetaFileValidationTest";

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

        private void CreateFileOrFolder(bool folder, string name, bool withMeta, string cwd = "")
        {
            var toCreatePath = Path.Combine(testDirectory, Path.Combine(cwd, name));
            if (folder)
                Directory.CreateDirectory(toCreatePath);
            else
                File.Create(toCreatePath).Dispose();

            if (withMeta)
                File.Create(toCreatePath + ".meta").Dispose();
        }

        [Test]
        public void When_File_Meta_Missing_Validation_Fails()
        {
            CreateFileOrFolder(false, "file1", false);
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Failed, metaFilesValidation.TestState);
            Assert.AreEqual(1, metaFilesValidation.TestOutput.Count);
        }

        [Test]
        public void When_Folder_Meta_Missing_Validation_Fails()
        {
            CreateFileOrFolder(true, "folder1", false);
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Failed, metaFilesValidation.TestState);
            Assert.AreEqual(1, metaFilesValidation.TestOutput.Count);
        }

        [Test]
        public void When_ManyFiles_Meta_Missing_Validation_Fails()
        {
            CreateFileOrFolder(false, "file1", false);
            CreateFileOrFolder(false, "file2", false);
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Failed, metaFilesValidation.TestState);
            Assert.AreEqual(2, metaFilesValidation.TestOutput.Count);
        }

        [Test]
        public void When_ManyFolders_Meta_Missing_Validation_Fails()
        {
            CreateFileOrFolder(true, "folder1", false);
            CreateFileOrFolder(true, "folder2", false);
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Failed, metaFilesValidation.TestState);
            Assert.AreEqual(2, metaFilesValidation.TestOutput.Count);
        }

        [Test]
        public void When_Folder_and_File_Meta_Missing_Validation_Fails()
        {
            CreateFileOrFolder(true, "folder1", false);
            CreateFileOrFolder(false, "file", false);
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Failed, metaFilesValidation.TestState);
            Assert.AreEqual(2, metaFilesValidation.TestOutput.Count);
        }

        [Test]
        public void When_File_InSubFolder_Meta_Missing_Validation_Fails()
        {
            CreateFileOrFolder(true, "folder1", true);
            CreateFileOrFolder(false, "file", false, "folder1");
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Failed, metaFilesValidation.TestState);
            Assert.AreEqual(1, metaFilesValidation.TestOutput.Count);
        }

        [Test]
        public void When_File_And_Folder_InSubFolder_Meta_Missing_Validation_Fails()
        {
            CreateFileOrFolder(true, "folder1", true);
            CreateFileOrFolder(false, "file", false, "folder1");
            CreateFileOrFolder(true, "folder2", false, "folder1");
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Failed, metaFilesValidation.TestState);
            Assert.AreEqual(2, metaFilesValidation.TestOutput.Count);
        }

        [Test]
        public void When_Folder_InSubFolder_Meta_Missing_Validation_Fails()
        {
            CreateFileOrFolder(true, "folder1", true);
            CreateFileOrFolder(true, "folder2", false, "folder1");
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Failed, metaFilesValidation.TestState);
            Assert.AreEqual(1, metaFilesValidation.TestOutput.Count);
        }

        [Test]
        public void When_File_Meta_IsPresent_Validation_Succeeds()
        {
            CreateFileOrFolder(false, "file1", true);
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, metaFilesValidation.TestState);
            Assert.AreEqual(0, metaFilesValidation.TestOutput.Count);
        }

        [Test]
        public void When_Folder_Meta_IsPresent_Validation_Succeeds()
        {
            CreateFileOrFolder(true, "folder1", true);
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, metaFilesValidation.TestState);
            Assert.AreEqual(0, metaFilesValidation.TestOutput.Count);
        }

        [Test]
        public void When_Folder_and_Files_Meta_IsPresent_Validation_Succeeds()
        {
            CreateFileOrFolder(true, "folder1", true);
            CreateFileOrFolder(false, "file1", true);
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, metaFilesValidation.TestState);
            Assert.AreEqual(0, metaFilesValidation.TestOutput.Count);
        }

        [Test]
        public void When_File_And_Folder_InSubFolder_Meta_IsPresent_Validation_Succeeds()
        {
            CreateFileOrFolder(true, "folder1", true);
            CreateFileOrFolder(false, "file", true, "folder1");
            CreateFileOrFolder(true, "folder2", true, "folder1");
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, metaFilesValidation.TestState);
            Assert.AreEqual(0, metaFilesValidation.TestOutput.Count);
        }

        [Test]
        public void When_File_StartingWithDot_Meta_IsMissing_Validation_Succeeds()
        {
            CreateFileOrFolder(false, ".file", false);
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, metaFilesValidation.TestState);
            Assert.AreEqual(0, metaFilesValidation.TestOutput.Count);
        }

        [Test]
        public void When_Folder_StartingWithDot_Meta_IsMissing_Validation_Succeeds()
        {
            CreateFileOrFolder(true, ".folder", false);
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, metaFilesValidation.TestState);
            Assert.AreEqual(0, metaFilesValidation.TestOutput.Count);
        }

        [Test]
        public void When_Files_In_Folder_StartingWithDot_Meta_IsMissing_Validation_Succeeds()
        {
            CreateFileOrFolder(true, ".folder", false);
            CreateFileOrFolder(false, "file1", false, ".folder");
            CreateFileOrFolder(false, "file2", true);
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, metaFilesValidation.TestState);
            Assert.AreEqual(0, metaFilesValidation.TestOutput.Count);
        }

        [Test]
        public void When_Folder_EndingWithTilde_Meta_IsMissing_Validation_Succeeds()
        {
            CreateFileOrFolder(true, "folder~", false);
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, metaFilesValidation.TestState);
            Assert.AreEqual(0, metaFilesValidation.TestOutput.Count);
        }

        [Test]
        public void When_Files_In_Folder_EndingWithTilde_Meta_IsMissing_Validation_Succeeds()
        {
            CreateFileOrFolder(true, "folder~", false);
            CreateFileOrFolder(false, "file1", false, "folder~");
            CreateFileOrFolder(false, "file2", true);
            var metaFilesValidation = new MetaFilesValidation();
            metaFilesValidation.Context = PrepareVettingContext(testDirectory);
            metaFilesValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, metaFilesValidation.TestState);
            Assert.AreEqual(0, metaFilesValidation.TestOutput.Count);
        }

        private VettingContext PrepareVettingContext(string packagePath)
        {
            return new VettingContext()
            {
                ProjectPackageInfo = new VettingContext.ManifestData()
                {
                    path = packagePath
                },
                PublishPackageInfo = new VettingContext.ManifestData()
                {
                    path = packagePath
                },
                PreviousPackageInfo = new VettingContext.ManifestData()
                {
                    path = packagePath
                },
                ValidationType = ValidationType.Publishing
            };
        }
    }
}
