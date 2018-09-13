using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Management;

using UnityEditor;
using UnityEngine.SceneManagement;

namespace ManagementTests.Runtime {


    [TestFixture(0, -1)] // No loaders, should never have any results
    [TestFixture(1, -1)] // 1 loader, fails so no active loaders
    [TestFixture(1, 0)] // All others, make sure the active loader is expected loader.
    [TestFixture(2, 0)]
    [TestFixture(2, 1)]
    [TestFixture(3, 2)]
    class ManualLifetimeTests {
        GameObject gameManager = null;
        private List<XRLoader> loaders = new List<XRLoader>();
        private int _loaderCount;
        private int _loaderIndexToWin;

        public ManualLifetimeTests(int loaderCount, int loaderIndexToWin)
        {
            _loaderCount = loaderCount;
            _loaderIndexToWin = loaderIndexToWin;
        }


        [SetUp]
        public void SetupXRManagerTest () {
            gameManager = new GameObject ();
            XRManager manager = gameManager.AddComponent<XRManager> () as XRManager;
            manager.ManageActiveLoaderLifetime = false;

            loaders = new List<XRLoader>();

            for (int i = 0; i < _loaderCount; i++)
            {
                DummyLoader dl = ScriptableObject.CreateInstance(typeof(DummyLoader)) as DummyLoader;
                dl.m_Id = i;
                dl.m_ShouldFail = (i != _loaderIndexToWin);
                loaders.Add(dl);
                manager.Loaders.Add(dl);
            }
        }

        [TearDown]
        public void TeardownXRManagerTest () {
            Object.Destroy(gameManager);
            gameManager = null;
        }


        [UnityTest]
        public IEnumerator CheckActivatedLoader ()
        {
            Assert.IsNotNull (gameManager);

            XRManager manager = gameManager.GetComponent<XRManager> () as XRManager;
            Assert.IsNotNull (manager);

            yield return manager.InitializeLoader();

            if (_loaderIndexToWin < 0 || _loaderIndexToWin >= loaders.Count)
            {
                Assert.IsNull (XRManager.ActiveLoader);
            }
            else
            {
                Assert.IsNotNull (XRManager.ActiveLoader);
                Assert.AreEqual(loaders[_loaderIndexToWin], XRManager.ActiveLoader);
            }

            manager.DeinitializeLoader();

            Assert.IsNull (XRManager.ActiveLoader);

            manager.Loaders.Clear();
        }

    }

}
