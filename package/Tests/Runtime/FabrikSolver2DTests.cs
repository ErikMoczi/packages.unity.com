using NUnit.Framework;
using UnityEngine.Experimental.U2D.IK;

namespace UnityEngine.Experimental.U2D.IK.Tests.FabrikSolver2DTests
{
    public class FabrikSolver2DTests
    {
        private Vector3Compare vec3Compare = new Vector3Compare();
        private FloatCompare floatCompare = new FloatCompare();

        private GameObject go;
        private GameObject effectorGO;
        private GameObject ikGO;
        private GameObject targetGO;

        private IKManager2D manager;
        private FabrikSolver2D solver;
        private IKChain2D chain;

        [SetUp]
        public void Setup()
        {
            go = new GameObject();
            var child1GO = new GameObject();
            child1GO.transform.parent = go.transform;

            var child2GO = new GameObject();
            child2GO.transform.parent = child1GO.transform;

            var child3GO = new GameObject();
            child3GO.transform.parent = child2GO.transform;

            effectorGO = new GameObject();
            effectorGO.transform.parent = child3GO.transform;

            go.transform.position = Vector3.zero;
            child1GO.transform.position = new Vector3(1.0f, 0.0f, 0.0f);
            child2GO.transform.position = new Vector3(3.0f, 0.0f, 0.0f);
            child3GO.transform.position = new Vector3(6.0f, 0.0f, 0.0f);
            effectorGO.transform.position = new Vector3(10.0f, 0.0f, 0.0f);

            ikGO = new GameObject();
            manager = ikGO.AddComponent<IKManager2D>();
            var lsGO = new GameObject();
            solver = lsGO.AddComponent<FabrikSolver2D>();
            lsGO.transform.parent = ikGO.transform;

            targetGO = new GameObject();
            targetGO.transform.parent = solver.transform;

            chain = solver.GetChain(0);
            chain.effector = effectorGO.transform;
            chain.target = targetGO.transform;
            chain.transformCount = 5;

            solver.Initialize();

            manager.AddSolver(solver);
        }

        [TearDown]
        public void Teardown()
        {
            UnityEngine.Object.Destroy(go);
            UnityEngine.Object.Destroy(ikGO);
        }

        [Test]
        public void NewSolver_DefaultsAreSet()
        {
            Assert.AreEqual(10, solver.iterations);
            Assert.AreEqual(0.01f, solver.tolerance);
        }

        [Test]
        [TestCase(-1f, 0.001f)]
        [TestCase(0f, 0.001f)]
        [TestCase(0.01f, 0.01f)]
        [TestCase(0.04f, 0.04f)]
        [TestCase(0.1f, 0.1f)]
        [TestCase(666f, 666f)]
        public void SetTolerance_ClampsTolerance(float tolerance, float expected)
        {
            solver.tolerance = tolerance;
            Assert.AreEqual(expected, solver.tolerance);
        }

        [Test]
        [TestCase(-1, 1)]
        [TestCase(1, 1)]
        [TestCase(4, 4)]
        [TestCase(50, 50)]
        [TestCase(666, 666)]
        public void SetIterations_ClampsIterations(int iterations, int expected)
        {
            solver.iterations = iterations;
            Assert.AreEqual(expected, solver.iterations);
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void SetTransformCount_SetsCorrectRootTransform(int transformCount)
        {
            chain.transformCount = transformCount;
            chain.Initialize();

            Assert.AreEqual(transformCount, chain.transformCount);
            Assert.AreEqual(transformCount, chain.transforms.Length);

            var tr = effectorGO.transform;
            for (int i = 1; i < transformCount; ++i)
                tr = tr.parent;

            Assert.AreSame(tr, chain.rootTransform);
        }

        [Test]
        [TestCase(0)]
        [TestCase(2)]
        [TestCase(6)]
        [TestCase(666)]
        public void SetInvalidEffector_SetsNoRootTransform(int transformCount)
        {
            chain.effector = null;
            chain.transformCount = transformCount;
            chain.Initialize();

            Assert.AreEqual(transformCount, chain.transformCount);
            Assert.AreEqual(null, chain.rootTransform);
        }

        [Test]
        [TestCase(0)]
        [TestCase(6)]
        [TestCase(666)]
        public void SetInvalidTransformCount_SetsNoRootTransform(int transformCount)
        {
            chain.transformCount = transformCount;
            chain.Initialize();

            Assert.AreEqual(transformCount, chain.transformCount);
            Assert.AreEqual(null, chain.rootTransform);
        }

        [Test]
        public void TargetIsReachableForChain_EndPointReachesTarget()
        {
            var targetPosition = new Vector3(9.0f, 1.0f, 0.0f);

            targetGO.transform.position = targetPosition;

            manager.UpdateManager();

            Assert.That(targetPosition, Is.EqualTo(chain.effector.position).Using(vec3Compare));
            Assert.That(0.0f, Is.EqualTo((targetPosition - chain.effector.position).magnitude).Using(floatCompare));
        }

        [Test]
        public void TargetIsLongerThanChain_EndPointIsAtClosestPointToTarget()
        {
            var targetPosition = new Vector3(0.0f, 12.0f, 0.0f);

            targetGO.transform.position = targetPosition;

            manager.UpdateManager();

            Assert.That(targetPosition, Is.Not.EqualTo(chain.effector.position).Using(vec3Compare));
            Assert.That(2.0f, Is.EqualTo((targetPosition - chain.effector.position).magnitude).Using(floatCompare));
        }
    }
}
