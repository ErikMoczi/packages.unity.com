using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TestsDerivedFromScriptableObject
{
    public class ScriptableObjectWithTests0 : ScriptableObject
    {
        public int someValue;

        [UnityTest]
        public IEnumerator SkipFrameTest()
        {
            someValue = 1;
            yield return null;
            Assert.AreEqual(1, someValue);
        }

        [UnityTest]
        public IEnumerator EnterPlaymodeTest()
        {
            someValue = 2;
            yield return new EnterPlayMode();
            Assert.AreEqual(2, someValue);
            someValue = 3;
            yield return new ExitPlayMode();
            Assert.AreEqual(3, someValue);
        }
    }
    public class ScriptableObjectWithTests1 : ScriptableObject
    {
        [UnityTest]
        public IEnumerator EnterPlaymodeTest()
        {
            yield return new EnterPlayMode();
        }
    }
    public class ScriptableObjectWithTests2 : ScriptableObject
    {
        [Test]
        public void Empty()
        {
        }
    }
    public class ScriptableObjectWithTests3 : ScriptableObject
    {
        [Test]
        public void Empty()
        {
        }
    }
    public class ScriptableObjectWithTests4
    {
        [Test]
        public void Empty()
        {
        }
    }
    public class ScriptableObjectWithTests5 : ScriptableObject
    {
        [Test]
        public void Empty()
        {
        }
    }
    public class ScriptableObjectWithTests6 : ScriptableObject
    {
        [Test]
        public void Empty()
        {
        }
    }
    public class ScriptableObjectWithTests7
    {
        [Test]
        [Description("This test is intended to run with the other tests from this namespace (it's assumed it will be executed as the last test)")]
        public void CheckForLeakedSOs()
        {
            Assert.IsEmpty(Resources.FindObjectsOfTypeAll<ScriptableObjectWithTests1>());
            Assert.IsEmpty(Resources.FindObjectsOfTypeAll<ScriptableObjectWithTests2>());
            Assert.IsEmpty(Resources.FindObjectsOfTypeAll<ScriptableObjectWithTests3>());
            Assert.IsEmpty(Resources.FindObjectsOfTypeAll<ScriptableObjectWithTests5>());
            Assert.IsEmpty(Resources.FindObjectsOfTypeAll<ScriptableObjectWithTests6>());
        }
    }


    // Followign 2 tests are meant to be executed together
    // We want to make sure that the SO will be serilized properly after a rerun happens on mocked tests (which happens when a domain reload is triggered)
    // For some reason NUnit won't skip a parametarized test with the Not + FullNameFilter which causes the ConstructDelegator reinstantiate the object
    // and kill the SO reference
    public class ScriptableObjectGetSerializedProperly_WhenExecutedAfterNormalClassTestWithParam0 : ScriptableObject
    {
        static LogType[] m_LogTypes = new LogType[] { LogType.Log };
        [UnityTest]
        public IEnumerator TestWithAValue([ValueSource("m_LogTypes")] LogType logType)
        {
            yield return null;
        }
    }

    public class ScriptableObjectGetSerializedProperly_WhenExecutedAfterNormalClassTestWithParam1 : ScriptableObject
    {
        private int val = 0;
        [UnityTest]
        public IEnumerator EnterPlaymodeTest()
        {
            val = 1;
            yield return new EnterPlayMode();
            Assert.AreEqual(1, val);
        }
    }
}
