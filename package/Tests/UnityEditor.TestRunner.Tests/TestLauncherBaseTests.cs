using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class TestLauncherBaseTests
    {
        private TestableTestLauncher m_TestableLauncher = new TestableTestLauncher();
        ITest testSuiteMock;
        ITestFilter testFilterMock;

        [SetUp]
        public void Setup()
        {
            var testWithoutAttributes = new TestMock();
            var testFilteredOut = new TestMock();
            var testWithSetupAttribute = new TestMock(new MethodInfoMock(new PrebuildSetupAttribute(typeof(PreBuildSetupClass))));
            var testWithCleanupAttribute = new TestMock(new MethodInfoMock(new PostBuildCleanupAttribute(typeof(PostBuildCleanupClass))));

            var subSuiteTestWithoutAttributes = new TestMock();
            var subSuiteTestWithSetupAttribute = new TestMock(new MethodInfoMock(new PrebuildSetupAttribute(typeof(PreBuildSetupClass2))));
            var subSuiteTestWithCleanupAttribute = new TestMock(new MethodInfoMock(new PostBuildCleanupAttribute(typeof(PostBuildCleanupClass))));

            var testBeingSubSuiteOfTests = new TestMock(new[] { subSuiteTestWithoutAttributes, subSuiteTestWithSetupAttribute, subSuiteTestWithCleanupAttribute });
            testSuiteMock = new TestMock(new[] { testWithoutAttributes, testFilteredOut, testWithSetupAttribute, testWithCleanupAttribute, testBeingSubSuiteOfTests});

            testFilterMock = new TestFilterMock(testFilteredOut);
        }

        [Test]
        public void TestPreBuildSetup()
        {
            PreBuildSetupClass.SetupCalledCount = 0;
            PreBuildSetupClass2.SetupCalledCount = 0;
            PostBuildCleanupClass.CleanUpCalledCount = 0;

            m_TestableLauncher.ExposedExecutePreBuildSetupMethods(testSuiteMock, testFilterMock);

            Assert.AreEqual(1, PreBuildSetupClass.SetupCalledCount, "Unexpected number of calls to first setup.");
            Assert.AreEqual(1, PreBuildSetupClass.SetupCalledCount, "Unexpected number of calls to second distrinct setup.");
            Assert.AreEqual(0, PostBuildCleanupClass.CleanUpCalledCount, "Unexpected number of calls to cleanup");
        }

        [Test]
        public void TestPostBuildCleanup()
        {
            PreBuildSetupClass.SetupCalledCount = 0;
            PreBuildSetupClass2.SetupCalledCount = 0;
            PostBuildCleanupClass.CleanUpCalledCount = 0;

            m_TestableLauncher.ExposedExecutePostBuildCleanupMethods(testSuiteMock, testFilterMock);

            Assert.AreEqual(0, PreBuildSetupClass.SetupCalledCount, "Unexpected number of calls to first setup.");
            Assert.AreEqual(0, PreBuildSetupClass2.SetupCalledCount, "Unexpected number of calls to second setup.");
            Assert.AreEqual(1, PostBuildCleanupClass.CleanUpCalledCount, "Unexpected number of calls to cleanup");
        }
    }
}
