using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEditor.TestTools.TestRunner;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine.TestTools;

namespace Assets.editor
{
    public class CachingTestListProviderTests
    {
        private Mock<ITestListProvider> m_TestListProviderMock;
        private Mock<ITestListCache> m_TestListCacheMock;
        private Mock<ITestAdaptorFactory> m_TestAdaptorFactory;
        private Mock<ITestAdaptor> m_TestAdaptor;
        private Mock<ITest> m_NunitTestMock;

        [SetUp]
        public void Setup()
        {
            m_TestListProviderMock = new Mock<ITestListProvider>();
            m_TestListCacheMock = new Mock<ITestListCache>();
            m_TestAdaptorFactory = new Mock<ITestAdaptorFactory>();
            m_TestAdaptor = new Mock<ITestAdaptor>();
        }

        [Test]
        public void CachingTestListProvidesListFromCache()
        {
            m_TestListCacheMock.Setup(cache => cache.GetTestFromCacheAsync(TestPlatform.EditMode)).Returns(Enumerator(m_TestAdaptor.Object));

            var listProviderUnderTest = new CachingTestListProvider(m_TestListProviderMock.Object, m_TestListCacheMock.Object, m_TestAdaptorFactory.Object);
            var testList = listProviderUnderTest.GetTestListAsync(TestPlatform.EditMode);

            Assert.AreEqual(m_TestAdaptor.Object, GetFromEnumerator(testList));
        }

        [Test]
        public void CachingTestListProvidesListFromTestListProvider()
        {
            m_NunitTestMock = new Mock<ITest>();

            m_NunitTestMock.Setup(nunitTest => nunitTest.Tests).Returns(new List<ITest>());
            ITestAdaptor testAdaptor = null;
            m_TestListCacheMock.Setup(cache => cache.GetTestFromCacheAsync(TestPlatform.EditMode)).Returns((TestPlatform mode) => Enumerator(testAdaptor));
            m_TestListProviderMock.Setup(testListProvider => testListProvider.GetTestListAsync(TestPlatform.EditMode)).Returns(Enumerator(m_NunitTestMock.Object));
            m_TestAdaptorFactory.Setup(testAdaptorFactory => testAdaptorFactory.Create(m_NunitTestMock.Object)).Returns(m_TestAdaptor.Object);

            var listProviderUnderTest = new CachingTestListProvider(m_TestListProviderMock.Object, m_TestListCacheMock.Object, m_TestAdaptorFactory.Object);
            var testList = listProviderUnderTest.GetTestListAsync(TestPlatform.EditMode);

            var testListValue = GetFromEnumerator(testList);
            Assert.AreEqual(m_TestAdaptor.Object, testListValue);
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
