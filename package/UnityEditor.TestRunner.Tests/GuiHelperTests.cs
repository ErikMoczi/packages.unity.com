using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Moq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.GUI;
using UnityEngine;
using UnityEngine.TestTools;

namespace Assets.editor
{
    public class GuiHelperTests
    {
        public Type TypeInfo;
        public string TestFileName;
        internal Mock<IMonoCecilHelper> CecilHelperMock;
        internal Mock<IAssetsDatabaseHelper> AssetsHelperMock;

        public MethodInfo MethodInfo
        {
            get { return TypeInfo.GetMethod("Fake"); }
        }

        [SetUp]
        public void TestSetup()
        {
            TypeInfo = GetType();
            TestFileName = new StackTrace(true).GetFrame(0).GetFileName();

            CecilHelperMock = new Mock<IMonoCecilHelper>();
            AssetsHelperMock = new Mock<IAssetsDatabaseHelper>();
        }

        [Test]
        public void GetFileOpenInfo_ReturnsUnifiedRelativeFilePath_WhenCecilReturnsNonEmptyPath()
        {
            var openInfo = new FileOpenInfo { FilePath = TestFileName };
            var guiHelper = BuildGuiHelperForCecilReturn(openInfo);

            var fileOpenInfo = guiHelper.GetFileOpenInfo(TypeInfo, MethodInfo);

            Assert.That(fileOpenInfo.FilePath, Is.Not.Empty);
            Assert.That(fileOpenInfo.FilePath, Does.Contain(TypeInfo.Name));
            Assert.That(TestFileName.Length, Is.GreaterThan(fileOpenInfo.FilePath.Length));
        }

        [Test]
        [Ignore("Case 1102138")]
        public void GetFileOpenInfo_ReturnsUnifiedRelativeFilePath_WhenCecilReturnsEmptyPathButFoundOneOnFileSystem()
        {
            var guiHelper = BuildDefaultGuiHelper();

            var fileOpenInfo = guiHelper.GetFileOpenInfo(TypeInfo, MethodInfo);

            Assert.That(fileOpenInfo.FilePath, Is.Not.Empty);
            Assert.That(fileOpenInfo.FilePath, Does.Contain(TypeInfo.Name));
            Assert.That(TestFileName.Length, Is.GreaterThan(fileOpenInfo.FilePath.Length));
        }

        [Test]
        public void GetFileOpenInfo_CallsTryGetCecilFileOpenInfoOnce_InAnyCase()
        {
            BuildDefaultGuiHelper().GetFileOpenInfo(TypeInfo, MethodInfo);

            CecilHelperMock.Verify(x => x.TryGetCecilFileOpenInfo(TypeInfo, MethodInfo), Times.Once());
        }

        [Test]
        public void OpenScriptInExternalEditor_LogsWarning_WhenNoFilePathWasFound()
        {
            TypeInfo = typeof(FakeType);

            BuildDefaultGuiHelper().OpenScriptInExternalEditor(TypeInfo, MethodInfo);

            LogAssert.Expect(LogType.Warning, new Regex("Failed to open*"));
        }

        [Test]
        public void OpenScriptInExternalEditor_DoesntCallOpenAssetInEditor_WhenNoFilePathWasFound()
        {
            TypeInfo = typeof(FakeType);
            var guiHelper = BuildDefaultGuiHelper();
            var fileOpenInfo = guiHelper.GetFileOpenInfo(TypeInfo, MethodInfo);

            guiHelper.OpenScriptInExternalEditor(TypeInfo, MethodInfo);

            AssetsHelperMock.Verify(x => x.OpenAssetInItsDefaultExternalEditor(fileOpenInfo.FilePath, fileOpenInfo.LineNumber), Times.Never());
        }

        [Test]
        [Ignore("Case 1102138")]
        public void OpenScriptInExternalEditor_LogsWarning_WhenGetFileOpenInfoDidntFindMethodLine()
        {
            var guiHelper = BuildDefaultGuiHelper();

            guiHelper.OpenScriptInExternalEditor(TypeInfo, MethodInfo);

            LogAssert.Expect(LogType.Warning, new Regex("Failed to get a line number*"));
        }

        [Test]
        [Ignore("Case 1102138")]
        public void OpenScriptInExternalEditor_CallsOpenAssetInEditorOnFirstLine_WhenGetFileOpenInfoDidntFindMethodLine()
        {
            var guiHelper = BuildDefaultGuiHelper();
            var filePath = guiHelper.FilePathToAssetsRelativeAndUnified(TestFileName);

            guiHelper.OpenScriptInExternalEditor(TypeInfo, MethodInfo);

            CecilHelperMock.Verify(x => x.TryGetCecilFileOpenInfo(TypeInfo, MethodInfo), Times.Once());
            AssetsHelperMock.Verify(x => x.OpenAssetInItsDefaultExternalEditor(filePath, 1), Times.Once());
        }

        [TestCase("")]
        [TestCase(null)]
        public void OpenScriptInExternalEditor_ReturnsFalse_WhenStackTraceIsNullOrEmpty(string stacktrace)
        {
            var guiHelper = BuildDefaultGuiHelper();

            var isEditorOpened = guiHelper.OpenScriptInExternalEditor(stacktrace);

            Assert.That(isEditorOpened, Is.False);
        }

        [Test]
        public void OpenScriptInExternalEditor_ReturnsTrue_WhenMatchingFileExistsForStackTrace()
        {
            var testStackTrace = GetTestStackTrace();

            var isEditorOpened = BuildDefaultGuiHelper().OpenScriptInExternalEditor(testStackTrace);

            Assert.That(isEditorOpened, Is.True);
        }

        [TestCase("test.cs:1")]
        [TestCase("at test.cs:1")]
        [TestCase("in test.cs: 1")]
        public void OpenScriptInExternalEditor_ReturnsFalse_WhenNoLineInStackTraceMatchesPattern(string testStackTrace)
        {
            var expectedRegex = new Regex("in (?<path>.*):{1}(?<line>[0-9]+)");
            var guiHelper = BuildDefaultGuiHelper();

            var isEditorOpened = guiHelper.OpenScriptInExternalEditor(testStackTrace);

            Assert.That(isEditorOpened, Is.False);
            Assert.That(expectedRegex.IsMatch(testStackTrace), Is.False);
        }

        IGuiHelper BuildDefaultGuiHelper()
        {
            return BuildGuiHelperForCecilReturn(null);
        }

        IGuiHelper BuildGuiHelperForCecilReturn(IFileOpenInfo cecilFileOpenInfoReturn)
        {
            cecilFileOpenInfoReturn = cecilFileOpenInfoReturn ?? new FileOpenInfo();

            CecilHelperMock.Setup(x => x.TryGetCecilFileOpenInfo(TypeInfo, MethodInfo)).Returns(cecilFileOpenInfoReturn);

            AssetsHelperMock.Setup(x => x.OpenAssetInItsDefaultExternalEditor(cecilFileOpenInfoReturn.FilePath, cecilFileOpenInfoReturn.LineNumber));

            return new GuiHelper(CecilHelperMock.Object, AssetsHelperMock.Object);
        }

        public void Fake() {}

        string GetTestStackTrace()
        {
            try
            {
                throw new Exception("Test");
            }
            catch (Exception e)
            {
                return e.StackTrace;
            }
        }

        class FakeType
        {
            public void Fake() {}
        }
    }
}
