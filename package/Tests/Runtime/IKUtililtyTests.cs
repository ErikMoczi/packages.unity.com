using NUnit.Framework;
using UnityEngine.Experimental.U2D.IK;

namespace UnityEngine.Experimental.U2D.IK.Tests.IKUtililtyTests
{
    public class IKUtililtyTests
    {
        private GameObject rootGO;
        private GameObject childGO;
        private GameObject grandChildGO;
        private GameObject otherGO;
        private GameObject otherChildGO;

        [SetUp]
        public void Setup()
        {
            rootGO = new GameObject();
            childGO = new GameObject();
            grandChildGO = new GameObject();
            otherGO = new GameObject();
            otherChildGO = new GameObject();

            childGO.transform.parent = rootGO.transform;
            grandChildGO.transform.parent = childGO.transform;

            otherChildGO.transform.parent = otherGO.transform;
        }

        [TearDown]
        public void Teardown()
        {
            UnityEngine.Object.Destroy(rootGO);
            UnityEngine.Object.Destroy(otherGO);
        }

        [Test]
        public void ChildGO_IsDescendentOfRootGO()
        {
            var result = IKUtility.IsDescendentOf(childGO.transform, rootGO.transform);
            Assert.AreEqual(true, result);
        }

        [Test]
        public void GrandChildGO_IsDescendentOfChildGO()
        {
            var result = IKUtility.IsDescendentOf(grandChildGO.transform, childGO.transform);
            Assert.AreEqual(true, result);
        }

        [Test]
        public void GrandChildGO_IsDescendentOfRootGO()
        {
            var result = IKUtility.IsDescendentOf(grandChildGO.transform, rootGO.transform);
            Assert.AreEqual(true, result);
        }

        [Test]
        public void OtherChildGO_IsDescendentOfOtherGO()
        {
            var result = IKUtility.IsDescendentOf(otherChildGO.transform, otherGO.transform);
            Assert.AreEqual(true, result);
        }

        [Test]
        public void GrandChildGO_IsNotDescendentOfOtherGO()
        {
            var result = IKUtility.IsDescendentOf(grandChildGO.transform, otherGO.transform);
            Assert.AreEqual(false, result);
        }

        [Test]
        public void GrandChildGO_IsNotDescendentOfOtherChildGO()
        {
            var result = IKUtility.IsDescendentOf(grandChildGO.transform, otherChildGO.transform);
            Assert.AreEqual(false, result);
        }

        [Test]
        public void RootGO_IsNotDescendentOfRootGO()
        {
            var result = IKUtility.IsDescendentOf(rootGO.transform, rootGO.transform);
            Assert.AreEqual(false, result);
        }

        [Test]
        public void RootGO_IsNotDescendentOfChildGO()
        {
            var result = IKUtility.IsDescendentOf(rootGO.transform, childGO.transform);
            Assert.AreEqual(false, result);
        }

        [Test]
        public void RootGO_IsNotDescendentOfGrandChildGO()
        {
            var result = IKUtility.IsDescendentOf(rootGO.transform, grandChildGO.transform);
            Assert.AreEqual(false, result);
        }

        [Test]
        public void GrandChildGO_HasTwoAncestors()
        {
            var result = IKUtility.GetAncestorCount(grandChildGO.transform);
            Assert.AreEqual(2, result);
        }

        [Test]
        public void ChildGO_HasOneAncestor()
        {
            var result = IKUtility.GetAncestorCount(childGO.transform);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void RootGO_HasNoAncestors()
        {
            var result = IKUtility.GetAncestorCount(rootGO.transform);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void GetChainCountForGrandChildGO_IsCorrect()
        {
            var ikGO = new GameObject();
            var solver = ikGO.AddComponent<FabrikSolver2D>();
            var chain = solver.GetChain(0);
            chain.effector = grandChildGO.transform;

            Assert.AreEqual(3, IKUtility.GetMaxChainCount(chain));

            UnityEngine.Object.Destroy(ikGO);
        }

        [Test]
        public void GetChainCountForOtherChildGO_IsCorrect()
        {
            var ikGO = new GameObject();
            var solver = ikGO.AddComponent<FabrikSolver2D>();
            var chain = solver.GetChain(0);
            chain.effector = otherChildGO.transform;

            Assert.AreEqual(2, IKUtility.GetMaxChainCount(chain));

            UnityEngine.Object.Destroy(ikGO);
        }
    }
}
