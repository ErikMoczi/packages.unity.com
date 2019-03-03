using System;
using System.Collections;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestTools;

namespace FrameworkTests.CustomRunner
{
    public class SetupTearDownTests
    {
        [SerializeField]
        private string log;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            log += "OneTimeSetUp\n";
        }

        [SetUp]
        public void SetUp()
        {
            log += "SetUp\n";
        }

        [TearDown]
        public void TearDown()
        {
            log += "TearDown\n";
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            log += "OneTimeTearDown";

            AssertLog();
        }

        private void AssertLog()
        {
            var splitLog = log.Split('\n');
            Assert.AreEqual("OneTimeSetUp", splitLog[0]);
            Assert.AreEqual("SetUp", splitLog[1]);
            Assert.AreEqual("Test part 1", splitLog[2]);
            Assert.AreEqual("OneTimeSetUp", splitLog[3]);
            Assert.AreEqual("SetUp", splitLog[4]);
            Assert.AreEqual("Test part 2", splitLog[5]);
            Assert.AreEqual("TearDown", splitLog[6]);
            Assert.AreEqual("OneTimeTearDown", splitLog[7]);
            Assert.AreEqual(8, splitLog.Length);
        }

        [UnityTest]
        public IEnumerator CheckForSetupAndTeardown()
        {
            log += "Test part 1\n";
            yield return new EnterPlayMode();
            log += "Test part 2\n";
        }
    }

    public class SetupTearDownTests_ScriptableObject : ScriptableObject
    {
        [SerializeField]
        private string log;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            log += "OneTimeSetUp\n";
        }

        [SetUp]
        public void SetUp()
        {
            log += "SetUp\n";
        }

        [TearDown]
        public void TearDown()
        {
            log += "TearDown\n";
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            log += "OneTimeTearDown";

            AssertLog();
        }

        private void AssertLog()
        {
            var splitLog = log.Split('\n');
            Assert.AreEqual("OneTimeSetUp", splitLog[0]);
            Assert.AreEqual("SetUp", splitLog[1]);
            Assert.AreEqual("Test part 1", splitLog[2]);
            Assert.AreEqual("OneTimeSetUp", splitLog[3]);
            Assert.AreEqual("SetUp", splitLog[4]);
            Assert.AreEqual("Test part 2", splitLog[5]);
            Assert.AreEqual("TearDown", splitLog[6]);
            Assert.AreEqual("OneTimeTearDown", splitLog[7]);
            Assert.AreEqual(8, splitLog.Length);
        }

        [UnityTest]
        public IEnumerator CheckForSetupAndTeardown()
        {
            log += "Test part 1\n";
            yield return new EnterPlayMode();
            log += "Test part 2\n";
        }
    }
}
