using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Management;

using UnityEditor;
using UnityEngine.SceneManagement;

namespace ManagementTests.Runtime
{
    [TestFixture(0, -1)] // No loaders, should never have any results
    [TestFixture(1, -1)] // 1 loader, fails so no active loaders
    [TestFixture(1, 0)] // All others, make sure the active loader is expected loader.
    [TestFixture(2, 0)]
    [TestFixture(2, 1)]
    [TestFixture(3, 2)]
    class ManualLifetimeTests
    {
        GameObject m_GameManager = null;
        List<XRLoader> m_Loaders = new List<XRLoader>();
        int m_LoaderCount;
        int m_LoaderIndexToWin;

        public ManualLifetimeTests(int loaderCount, int loaderIndexToWin)
        {
            m_LoaderCount = loaderCount;
            m_LoaderIndexToWin = loaderIndexToWin;
        }

        [SetUp]
        public void SetupXRManagerTest()
        {
            m_GameManager = new GameObject();
            XRManager manager = m_GameManager.AddComponent<XRManager>() as XRManager;
            manager.automaticLoading = false;

            m_Loaders = new List<XRLoader>();

            for (int i = 0; i < m_LoaderCount; i++)
            {
                DummyLoader dl = ScriptableObject.CreateInstance(typeof(DummyLoader)) as DummyLoader;
                dl.id = i;
                dl.shouldFail = (i != m_LoaderIndexToWin);
                m_Loaders.Add(dl);
                manager.loaders.Add(dl);
            }
        }

        [TearDown]
        public void TeardownXRManagerTest()
        {
            Object.Destroy(m_GameManager);
            m_GameManager = null;
        }

        [UnityTest]
        public IEnumerator CheckActivatedLoader()
        {
            Assert.IsNotNull(m_GameManager);

            XRManager manager = m_GameManager.GetComponent<XRManager>() as XRManager;
            Assert.IsNotNull(manager);

            yield return manager.InitializeLoader();

            if (m_LoaderIndexToWin < 0 || m_LoaderIndexToWin >= m_Loaders.Count)
            {
                Assert.IsNull(XRManager.activeLoader);
            }
            else
            {
                Assert.IsNotNull(XRManager.activeLoader);
                Assert.AreEqual(m_Loaders[m_LoaderIndexToWin], XRManager.activeLoader);
            }

            manager.DeinitializeLoader();

            Assert.IsNull(XRManager.activeLoader);

            manager.loaders.Clear();
        }
    }
}
