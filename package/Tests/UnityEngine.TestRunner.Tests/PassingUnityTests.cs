using UnityEngine;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace FrameworkTests.CustomRunner {}

namespace FrameworkTests
{
    public class PassingUnityTests
    {
        static void CheckUnityAPI()
        {
            var go = new GameObject();
            Object.DestroyImmediate(go);
        }

        [Test]
        public void BasicTest()
        {
            CheckUnityAPI();
            Debug.Log("Void_Pass log message");
        }

        [UnityTest]
        public IEnumerator UnityTest()
        {
            var framNo = Time.frameCount;
            yield return null;
            CheckUnityAPI();
            yield return null;
            Debug.Log("IEnumerator_Pass log message");
            yield return null;
            Assert.AreNotEqual(framNo, Time.frameCount);
        }

        static LogType[] m_LogTypes = new LogType[] { LogType.Log };
        [UnityTest]
        [Description("Test is built correctly")]
        public IEnumerator UnityTest_TestWithSourceValues([ValueSource("m_LogTypes")] LogType logType)
        {
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnityTest_ReturnsNull()
        {
            return null;
        }

        [UnityTest]
        public IEnumerator UnityTest_MyMonoBehaviourTest()
        {
            yield return new MonoBehaviourTest<MyMonoBehaviourTest>();
        }

        public class MyMonoBehaviourTest : MonoBehaviour, IMonoBehaviourTest
        {
            public bool IsTestFinished { get { return true; } }
        }
    }
}
