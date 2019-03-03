using System.Collections;
using System.Text.RegularExpressions;
using Moq;
using NUnit.Framework;
using UnityEditor.TestTools.TestRunner.GUI;
using UnityEngine;
using UnityEngine.TestTools;

namespace Assets.editor
{
    /// <summary>
    /// Covers a couple of happy-pass scenarios using actual Mono Cecil
    /// Note: make sure to keep this particular type-file naming consistent
    /// </summary>
    public class GuiHelperIntegrationTests
    {
        [SetUp]
        public void BeforEach()
        {
            AssetsHelperMock = new Mock<IAssetsDatabaseHelper>();
        }

        Mock<IAssetsDatabaseHelper> AssetsHelperMock { get; set; }

        IGuiHelper GuiHelper
        {
            get
            {
                AssetsHelperMock.Setup(x => x.OpenAssetInItsDefaultExternalEditor(string.Empty, 1));
                return new GuiHelper(new MonoCecilHelper(), AssetsHelperMock.Object);
            }
        }

#if !NET_4_6 && !NET_STANDARD_2_0
        [Test]
#endif
        public void GetFileOpenInfo_ReturnsCorrectFilePathAndMethodLineNumber_ForUnitTest()
        {
            var typeInfo = GetType();
            var methodInfo = typeInfo.GetMethod("FakeTest");

            var fileOpenInfo = GuiHelper.GetFileOpenInfo(typeInfo, methodInfo);

            Assert.That(fileOpenInfo.FilePath, Is.Not.Empty);
            Assert.That(fileOpenInfo.FilePath, Does.Contain(typeInfo.Name));

            // default is 1 and whatever is greater will be considered ok, since it's what Cecil gives anyway
            Assert.That(fileOpenInfo.LineNumber, Is.GreaterThan(1));
        }

        [Test]
        [Ignore("Case 1102138")]
        public void GetFileOpenInfo_ReturnsCorrectFilePathAndDefaultLineNumber_ForUnityTest()
        {
            var typeInfo = GetType();
            var methodInfo = typeInfo.GetMethod("FakeUnityTest");

            var fileOpenInfo = GuiHelper.GetFileOpenInfo(typeInfo, methodInfo);

            Assert.That(fileOpenInfo.FilePath, Is.Not.Empty);
            Assert.That(fileOpenInfo.FilePath, Does.Contain(typeInfo.Name));
            Assert.That(fileOpenInfo.LineNumber, Is.EqualTo(1));
        }

        [Test]
        public void OpenScriptInExternalEditor_LogsWarning_ForUnityTestIsInNestedOrMisnamedType()
        {
            var typeInfo = typeof(TestType);
            var methodInfo = typeInfo.GetMethod("FakeUnityTest");

            GuiHelper.OpenScriptInExternalEditor(typeInfo, methodInfo);

            LogAssert.Expect(LogType.Log, new Regex("No SequencePoints*"));
            LogAssert.Expect(LogType.Warning, new Regex("Failed to open*"));
        }

#if !NET_4_6 && !NET_STANDARD_2_0
        [Test]
#endif
        public void OpenScriptInExternalEditor_DoesAttemptToOpenFile_ForUnitTestIsInNestedType()
        {
            var typeInfo = typeof(TestType);
            var methodInfo = typeInfo.GetMethod("FakeTest");

            var fileOpenInfo = GuiHelper.GetFileOpenInfo(typeInfo, methodInfo);
            Assert.That(fileOpenInfo.FilePath, Does.Contain(GetType().Name));
            Assert.That(fileOpenInfo.LineNumber, Is.GreaterThan(1));

            GuiHelper.OpenScriptInExternalEditor(typeInfo, methodInfo);

            AssetsHelperMock.Verify(m => m.OpenAssetInItsDefaultExternalEditor(fileOpenInfo.FilePath, fileOpenInfo.LineNumber), Times.Once());
        }

        /// <summary>
        /// Is here for testing purposes. Used through reflection
        /// </summary>
        public void FakeTest() {}

        /// <summary>
        /// Is here for testing purposes. Used through reflection
        /// </summary>
        public IEnumerator FakeUnityTest()
        {
            yield return null;
        }

        /// <summary>
        /// Is here for testing purposes. Used through reflection
        /// </summary>
        class TestType
        {
            public IEnumerator FakeUnityTest()
            {
                yield return null;
            }

            public void FakeTest() {}
        }
    }
}
