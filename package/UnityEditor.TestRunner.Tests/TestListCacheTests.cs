using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEditor.TestTools.TestRunner;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine.TestRunner.TestLaunchers;
using UnityEngine.TestTools;

namespace Assets.editor
{
    public class TestListCacheTests
    {
        private Mock<ITest> m_TestMock;
        private Mock<IRemoteTestResultDataFactory> m_ResultDataFactoryMock;
        private RemoteTestResultDataWithTestData m_RemoteTestResultDataWithTestData;
        private CacheDataMock m_TestListCacheData;
        private Mock<ITestAdaptorFactory> m_TestAdaptorFactoryMock;
        private Mock<ITestAdaptor> m_TestAdaptorMock;
        private TestListCache m_TestListCacheUnderTest;

        [SetUp]
        public void Setup()
        {
            m_TestMock = new Mock<ITest>();
            m_ResultDataFactoryMock = new Mock<IRemoteTestResultDataFactory>();
            m_RemoteTestResultDataWithTestData = new RemoteTestResultDataWithTestData();
            m_TestListCacheData = new CacheDataMock();
            m_TestAdaptorFactoryMock = new Mock<ITestAdaptorFactory>();
            m_TestAdaptorMock = new Mock<ITestAdaptor>();

            m_ResultDataFactoryMock.Setup(dataFactory => dataFactory.CreateFromTest(m_TestMock.Object)).Returns(m_RemoteTestResultDataWithTestData);
            m_TestAdaptorFactoryMock.Setup(adaptorFactory => adaptorFactory.BuildTreeAsync(m_RemoteTestResultDataWithTestData)).Returns(Enumerator(m_TestAdaptorMock.Object));

            m_TestListCacheUnderTest = new TestListCache(m_TestAdaptorFactoryMock.Object, m_ResultDataFactoryMock.Object, m_TestListCacheData);
        }

        [Test]
        public void TestListCacheAddsTestsForNewPlatformToCache()
        {
            m_TestListCacheUnderTest.CacheTest(TestPlatform.EditMode, m_TestMock.Object);

            Assert.AreEqual(1, m_TestListCacheData.platforms.Count);
            Assert.AreEqual(TestPlatform.EditMode, m_TestListCacheData.platforms[0]);
            Assert.AreEqual(1, m_TestListCacheData.cachedData.Count);
            Assert.AreEqual(m_RemoteTestResultDataWithTestData, m_TestListCacheData.cachedData[0]);
        }

        [Test]
        public void TestListCacheUpdatesTestsForExistingPlatformInCache()
        {
            m_TestListCacheData.platforms.Add(TestPlatform.EditMode);
            m_TestListCacheData.cachedData.Add(new RemoteTestResultDataWithTestData());

            m_TestListCacheUnderTest.CacheTest(TestPlatform.EditMode, m_TestMock.Object);

            Assert.AreEqual(1, m_TestListCacheData.platforms.Count);
            Assert.AreEqual(TestPlatform.EditMode, m_TestListCacheData.platforms[0]);
            Assert.AreEqual(1, m_TestListCacheData.cachedData.Count);
            Assert.AreEqual(m_RemoteTestResultDataWithTestData, m_TestListCacheData.cachedData[0]);
        }

        [Test]
        public void TestListCacheGetTestsForExistingPlatformInCache()
        {
            m_TestListCacheData.platforms.Add(TestPlatform.EditMode);
            m_TestListCacheData.cachedData.Add(m_RemoteTestResultDataWithTestData);

            var test = GetFromEnumerator(m_TestListCacheUnderTest.GetTestFromCacheAsync(TestPlatform.EditMode));

            Assert.AreEqual(test, m_TestAdaptorMock.Object);
        }

        [Test]
        public void TestListCacheTriesToGetTestsFromPlatformNotInCache()
        {
            var test = GetFromEnumerator(m_TestListCacheUnderTest.GetTestFromCacheAsync(TestPlatform.EditMode));

            Assert.IsNull(test);
        }

        private class CacheDataMock : ITestListCacheData
        {
            public CacheDataMock()
            {
                platforms = new List<TestPlatform>();
                cachedData = new List<RemoteTestResultDataWithTestData>();
            }

            public List<TestPlatform> platforms { get; set; }
            public List<RemoteTestResultDataWithTestData> cachedData {  get; set; }
        }

        private static IEnumerator<T> Enumerator<T>(T value)
        {
            yield return value;
        }

        private static T GetFromEnumerator<T>(IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
            {
            }

            return enumerator.Current;
        }
    }
}
