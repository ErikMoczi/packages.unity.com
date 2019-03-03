using UnityEngine;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Networking;
using UnityEngine.TestTools;

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
        [Explicit]
        public IEnumerator AsyncOperationTest()
        {
            var www = new UnityWebRequest(@"invalid%domain");
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();
            Assert.IsTrue(www.isHttpError);
        }
    }

    public class PassingUnityTestsNested
    {
        private string testString = "";

        // 1 - 2 - 19 - 20
        // |
        // 3 - 10 - 11 - 12 - 13 - 18
        // |                   |
        // 4 - 5 - 8 - 9      15 - 16
        //     |                    |
        //     6 - 7               17

        public IEnumerator UnityTestYieldLevel5()
        {
            yield return null;
            testString += "17 ";
        }

        public IEnumerator UnityTestYieldLevel4_1()
        {
            yield return null;
            testString += "6 ";
            yield return null;
            yield return null;
            yield return null;
            testString += "7 ";
            yield return null;
        }

        public IEnumerator UnityTestYieldLevel4_2()
        {
            testString += "15 ";
            yield return null;
            testString += "16 ";
            yield return UnityTestYieldLevel5();
        }

        public IEnumerator UnityTestYieldLevel3_1()
        {
            testString += "4 ";
            yield return null;
            testString += "5 ";
            yield return UnityTestYieldLevel4_1();
            testString += "8 ";
            yield return null;
            testString += "9 ";
        }

        public IEnumerator UnityTestYieldLevel3_2()
        {
            yield return null;
            testString += "14 ";
            yield return UnityTestYieldLevel4_2();
        }

        public IEnumerator UnityTestYieldLevel2()
        {
            testString += "3 ";
            yield return UnityTestYieldLevel3_1();
            testString += "10 ";
            yield return null;
            testString += "11 ";
            yield return null;
            testString += "12 ";
            yield return null;
            testString += "13 ";
            yield return UnityTestYieldLevel3_2();
            testString += "18 ";
        }

        [UnityTest]
        public IEnumerator UnityTestYieldNestedEnumeratorWithYieldNull()
        {
            testString = "";
            yield return null;
            testString += "1 ";
            yield return null;
            testString += "2 ";
            yield return UnityTestYieldLevel2();
            testString += "19 ";
            yield return null;
            testString += "20";

            Assert.AreEqual("1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20", testString);
        }
    }
}
