using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Management;

using UnityEditor;

namespace ManagementTests.Runtime {
    class RuntimeTests {
        GameObject gameManager = null;
        [SetUp]
        public void SetupXRManagerTest () {
            gameManager = new GameObject ();
            XRManager manager = gameManager.AddComponent<XRManager> () as XRManager;
        }

        [TearDown]
        public void TeardownXRManagerTest () {
            Object.Destroy(gameManager);
            gameManager = null;
        }

        [UnityTest]
        public IEnumerator CheckFirstLoaderWins () {

            Assert.IsNotNull (gameManager);

            XRManager manager = gameManager.GetComponent<XRManager> () as XRManager;
            Assert.IsNotNull (manager);

            DummyLoader loader1 = new DummyLoader ();
            DummyLoader loader2 = new DummyLoader ();
            manager.AddLoader (loader1);
            manager.AddLoader (loader2);

            yield return new EnterPlayMode ();

            Assert.IsNotNull (XRManager.ActiveLoader && XRManager.ActiveLoader == loader1);

            yield return new ExitPlayMode ();

            Assert.IsNull (XRManager.ActiveLoader);

            yield return null;
        }

        [UnityTest]
        public IEnumerator CheckSecondLoaderWins () {

            Assert.IsNotNull (gameManager);

            XRManager manager = gameManager.GetComponent<XRManager> () as XRManager;
            Assert.IsNotNull (manager);

            DummyLoader loader1 = new DummyLoader (true);
            DummyLoader loader2 = new DummyLoader ();
            manager.AddLoader (loader1);
            manager.AddLoader (loader2);

            yield return new EnterPlayMode ();

            Assert.IsNotNull (XRManager.ActiveLoader && XRManager.ActiveLoader == loader2);

            yield return new ExitPlayMode ();

            Assert.IsNull (XRManager.ActiveLoader);

            yield return null;
        }
    }
}
