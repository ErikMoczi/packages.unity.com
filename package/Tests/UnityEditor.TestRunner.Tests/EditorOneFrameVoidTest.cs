using System.Collections;
using NUnit.Framework;
using UnityEditor;

namespace FrameworkTests.CustomRunner
{
    public class EditModeOneFrameForFixtureVoidTest
    {
        protected int m_FixtureFrameCount;

        [OneTimeTearDown]
        public void TestThatAllTestsAreExecutedInOneFrame()
        {
            Assert.IsTrue(m_FixtureFrameCount == 0);
        }

        [SetUp]
        public void SomeSetupMethod()
        {
        }

        public EditModeOneFrameForFixtureVoidTest()
        {
            EditorApplication.update += OnUpdate;
        }

        private void OnUpdate()
        {
            m_FixtureFrameCount++;
        }

        private static IEnumerable ManyTests()
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
        }

        [TestCaseSource("ManyTests")]
        public void ManyVoidTests(object something)
        {
            Assert.Pass();
        }

        [Test]
        public void SomeOtherVoidTests()
        {
            Assert.Pass();
        }
    }
}
