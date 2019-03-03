using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FrameworkTests.CustomRunner
{
    [PrebuildSetup("SceneLoadingTestsSetup")]
    internal class SetupAndTearDownEnumeratorTests
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("TestRunner-TestScene");
        }

        [Test]
        public void TestIfSetupIsRan()
        {
            Assert.IsTrue(SceneManager.GetActiveScene().name == "TestRunner-TestScene");
        }
    }
}
