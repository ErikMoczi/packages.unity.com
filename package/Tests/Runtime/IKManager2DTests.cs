using NUnit.Framework;
using UnityEngine.Experimental.U2D.IK;

namespace UnityEngine.Experimental.U2D.IK.Tests.IKManager2DTests
{
    public class IKManager2DTests
    {
        private GameObject ikGO;
        private IKManager2D manager;

        [SetUp]
        public void Setup()
        {
            ikGO = new GameObject();
            manager = ikGO.AddComponent<IKManager2D>();
        }

        [TearDown]
        public void Teardown()
        {
            UnityEngine.Object.Destroy(ikGO);
        }

        [Test]
        public void NewManager_DefaultsAreSet()
        {
            Assert.AreEqual(1f, manager.weight);
        }

        [Test]
        [TestCase(-1.0f, 0.0f)]
        [TestCase(0.0f, 0.0f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(1.0f, 1.0f)]
        [TestCase(2.0f, 1.0f)]
        public void SetWeight_ManagerClampsWeight(float weight, float expected)
        {
            manager.weight = weight;
            Assert.AreEqual(expected, manager.weight);
        }

        [Test]
        public void AddSolversToManager_ManagerHasSolverInList()
        {
            var go1 = new GameObject();
            var solver1 = go1.AddComponent<LimbSolver2D>();
            go1.transform.parent = ikGO.transform;

            var go2 = new GameObject();
            var solver2 = go1.AddComponent<LimbSolver2D>();
            go2.transform.parent = ikGO.transform;

            manager.AddSolver(solver1);
            manager.AddSolver(solver2);

            Assert.IsTrue(manager.solvers.Contains(solver1));
            Assert.IsTrue(manager.solvers.Contains(solver2));
        }

        [Test]
        public void RemoveSolverFromManager_ManagerDoesNotHaveSolverInList()
        {
            var go1 = new GameObject();
            var solver1 = go1.AddComponent<LimbSolver2D>();
            go1.transform.parent = ikGO.transform;

            var go2 = new GameObject();
            var solver2 = go1.AddComponent<LimbSolver2D>();
            go2.transform.parent = ikGO.transform;

            manager.AddSolver(solver1);
            manager.AddSolver(solver2);
            manager.RemoveSolver(solver2);

            Assert.IsTrue(manager.solvers.Contains(solver1));
            Assert.IsFalse(manager.solvers.Contains(solver2));
        }
    }
}
