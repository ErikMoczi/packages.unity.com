using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class SceneLoadingTests
    {
        [PrebuildSetup("SceneLoadingTestsSetup")]
        [UnityTest]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene("TestRunner-TestScene", LoadSceneMode.Single);
            yield return null;
            Assert.IsNotNull(GameObject.Find("TestGameObject"));
        }

        [UnityTest]
        public IEnumerator MonoBehaviourTestSurvivesSingleSceneLoading()
        {
            var test = new MonoBehaviourTest<MonoBehaviourTestable>();
            yield return test;
            SceneManager.LoadScene("TestRunner-TestScene", LoadSceneMode.Single);
            yield return null;
            Assert.IsNotNull(test.gameObject);
            Assert.IsNotNull(test.component);
        }
    }

    public class MonoBehaviourTestable : MonoBehaviour, IMonoBehaviourTest
    {
        public bool IsTestFinished { get; private set; }

        public void Start()
        {
            IsTestFinished = true;
        }
    }
}
