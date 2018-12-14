using NUnit.Framework;
using UnityEngine.Experimental.U2D.IK;
using System.Collections.Generic;

namespace UnityEngine.Experimental.U2D.IK.Tests.Solver2DTests
{
    public class Solver2DTests
    {
        public class WeightTestCase
        {
            public float weight;
            public Vector3 targetPosition;
            public Vector3 expectedEffectorPosition;

            public WeightTestCase(float w, Vector3 t, Vector3 ex)
            {
                weight = w;
                targetPosition = t;
                expectedEffectorPosition = ex;
            }

            public override string ToString()
            {
                return "Weight: " + weight + " targetPosition: " + targetPosition + " expectedEffectorPosition: " + expectedEffectorPosition;
            }
        }

        private Vector3Compare vec3Compare = new Vector3Compare();
        private FloatCompare floatCompare = new FloatCompare();

        private GameObject go;
        private GameObject effectorGO;
        private GameObject ikGO;
        private GameObject targetGO;

        private IKManager2D manager;
        private Solver2D solver;

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

            // Using FABRIKSolver2D here, but tests are generalised for all Solver2Ds
            solver = lsGO.AddComponent<FabrikSolver2D>();
            lsGO.transform.parent = ikGO.transform;

            targetGO = new GameObject();
            targetGO.transform.parent = solver.transform;

            var chain = solver.GetChain(0);
            chain.effector = effectorGO.transform;
            chain.target = targetGO.transform;
            chain.transformCount = 5;
            chain.Initialize();

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
            Assert.AreEqual(true, solver.constrainRotation);
            Assert.AreEqual(true, solver.solveFromDefaultPose);
            Assert.AreEqual(1f, solver.weight);
        }

        [Test]
        public void TargetIsReachableForChain_EndPointReachesTarget()
        {
            var targetPosition = new Vector3(9.0f, 1.0f, 0.0f);

            targetGO.transform.position = targetPosition;

            manager.UpdateManager();

            var chain = solver.GetChain(0);

            Assert.That(targetPosition, Is.EqualTo(chain.effector.position).Using(vec3Compare));
            Assert.That(0.0f, Is.EqualTo((targetPosition - chain.effector.position).magnitude).Using(floatCompare));
        }

        [Test]
        public void TargetIsLongerThanChain_EndPointIsAtClosestPointToTarget()
        {
            var targetPosition = new Vector3(0.0f, 12.0f, 0.0f);

            targetGO.transform.position = targetPosition;

            manager.UpdateManager();

            var chain = solver.GetChain(0);

            Assert.That(targetPosition, Is.Not.EqualTo(chain.effector.position).Using(vec3Compare));
            Assert.That(2.0f, Is.EqualTo((targetPosition - chain.effector.position).magnitude).Using(floatCompare));
        }

        [Test]
        public void NoWeightSetOnSolver_SolverDoesNotIterate()
        {
            var targetPosition = new Vector3(9.0f, 1.0f, 0.0f);

            targetGO.transform.position = targetPosition;

            solver.weight = 0f;
            Assert.AreEqual(0f, solver.weight);

            manager.UpdateManager();

            var chain = solver.GetChain(0);
            Assert.That(targetPosition, Is.Not.EqualTo(chain.effector.position).Using(vec3Compare));
            Assert.That(new Vector3(0f, 0f, 0f), Is.EqualTo(chain.transforms[0].position).Using(vec3Compare));
            Assert.That(new Vector3(1f, 0f, 0f), Is.EqualTo(chain.transforms[1].position).Using(vec3Compare));
            Assert.That(new Vector3(3f, 0f, 0f), Is.EqualTo(chain.transforms[2].position).Using(vec3Compare));
            Assert.That(new Vector3(6f, 0f, 0f), Is.EqualTo(chain.transforms[3].position).Using(vec3Compare));
            Assert.That(new Vector3(10f, 0f, 0f), Is.EqualTo(chain.transforms[4].position).Using(vec3Compare));
        }

        [Test]
        public void NoWeightSetOnManager_SolverDoesNotIterate()
        {
            var targetPosition = new Vector3(9.0f, 1.0f, 0.0f);

            targetGO.transform.position = targetPosition;

            manager.weight = 0f;
            Assert.AreEqual(0f, manager.weight);

            manager.UpdateManager();

            var chain = solver.GetChain(0);
            Assert.That(targetPosition, Is.Not.EqualTo(chain.effector.position).Using(vec3Compare));
            Assert.That(new Vector3(0f, 0f, 0f), Is.EqualTo(chain.transforms[0].position).Using(vec3Compare));
            Assert.That(new Vector3(1f, 0f, 0f), Is.EqualTo(chain.transforms[1].position).Using(vec3Compare));
            Assert.That(new Vector3(3f, 0f, 0f), Is.EqualTo(chain.transforms[2].position).Using(vec3Compare));
            Assert.That(new Vector3(6f, 0f, 0f), Is.EqualTo(chain.transforms[3].position).Using(vec3Compare));
            Assert.That(new Vector3(10f, 0f, 0f), Is.EqualTo(chain.transforms[4].position).Using(vec3Compare));
        }

        [Test]
        [TestCase(0.13f)]
        [TestCase(0.25f)]
        [TestCase(0.5f)]
        [TestCase(0.79f)]
        [TestCase(0.99f)]
        public void SomeWeightSet_SolverBlendsPositions(float weight)
        {
            var targetPosition = new Vector3(0f, 12f, 0f);

            targetGO.transform.position = targetPosition;

            solver.weight = weight;
            Assert.AreEqual(weight, solver.weight);

            manager.UpdateManager();

            var weightedPosition = Vector3.Slerp(new Vector3(10f, 0f, 0f), new Vector3(0f, 10f, 0f), weight);

            var chain = solver.GetChain(0);
            Assert.That(targetPosition, Is.Not.EqualTo(chain.effector.position).Using(vec3Compare));
            Assert.That(0.0f, Is.EqualTo((chain.effector.position - weightedPosition).magnitude).Using(floatCompare));
        }

        [Test]
        [TestCase(0.99f, 0.13f)]
        [TestCase(0.79f, 0.25f)]
        [TestCase(0.25f, 0.5f)]
        [TestCase(0.1f, 0.79f)]
        [TestCase(0.5f, 0.99f)]
        public void SomeWeightSetOnSolver_SomeWeightSetOnManager_SolverBlendsPositions(float solverWeight, float managerWeight)
        {
            var targetPosition = new Vector3(0f, 12f, 0f);

            targetGO.transform.position = targetPosition;

            solver.weight = solverWeight;
            Assert.AreEqual(solverWeight, solver.weight);

            manager.weight = managerWeight;
            Assert.AreEqual(managerWeight, manager.weight);

            manager.UpdateManager();

            var weightedPosition = Vector3.Slerp(new Vector3(10f, 0f, 0f), new Vector3(0f, 10f, 0f), solverWeight * managerWeight);

            var chain = solver.GetChain(0);
            Assert.That(targetPosition, Is.Not.EqualTo(chain.effector.position).Using(vec3Compare));
            Assert.That(0.0f, Is.EqualTo((chain.effector.position - weightedPosition).magnitude).Using(floatCompare));
        }

        [Test]
        public void ConstrainRotation_ConstrainsRotation()
        {
            var targetPosition = new Vector3(0.0f, 12.0f, 0.0f);
            var effectorRotation = Quaternion.Euler(52f, 34f, 44f);

            targetGO.transform.position = targetPosition;
            targetGO.transform.rotation = effectorRotation;

            solver.constrainRotation = true;
            Assert.AreEqual(true, solver.constrainRotation);

            manager.UpdateManager();

            var chain = solver.GetChain(0);
            Assert.That(effectorRotation.x, Is.EqualTo(chain.effector.rotation.x).Using(floatCompare));
            Assert.That(effectorRotation.y, Is.EqualTo(chain.effector.rotation.y).Using(floatCompare));
            Assert.That(effectorRotation.z, Is.EqualTo(chain.effector.rotation.z).Using(floatCompare));
            Assert.That(effectorRotation.w, Is.EqualTo(chain.effector.rotation.w).Using(floatCompare));
        }

        [Test]
        public void NoConstrainRotation_DoesNotConstrainRotation()
        {
            var targetPosition = new Vector3(0.0f, 12.0f, 0.0f);
            var targetRotation = Quaternion.Euler(52f, 34f, 44f);

            targetGO.transform.position = targetPosition;
            targetGO.transform.rotation = targetRotation;

            solver.constrainRotation = false;
            Assert.AreEqual(false, solver.constrainRotation);

            manager.UpdateManager();

            var chain = solver.GetChain(0);
            Assert.That(0f, Is.EqualTo(chain.effector.localRotation.x).Using(floatCompare));
            Assert.That(0f, Is.EqualTo(chain.effector.localRotation.y).Using(floatCompare));
            Assert.That(0f, Is.EqualTo(chain.effector.localRotation.z).Using(floatCompare));
            Assert.That(1f, Is.EqualTo(chain.effector.localRotation.w).Using(floatCompare));
        }

        [Test]
        public void RestoreDefaultPoseSet_DoesNotIterateToTarget()
        {
            // Fabrik will not iterate to the target within 1 iteration
            var fabrik = solver as FabrikSolver2D;
            fabrik.iterations = 1;

            var targetPosition = new Vector3(9.0f, 1.0f, 0.0f);

            targetGO.transform.position = targetPosition;

            // With the positions set to default every iteration, solver will never reach target
            for (int i = 0; i < 10; ++i)
                manager.UpdateManager();

            var chain = solver.GetChain(0);

            Assert.That(targetPosition, Is.Not.EqualTo(chain.effector.position).Using(vec3Compare));
            Assert.That(0.0f, Is.Not.EqualTo((targetPosition - chain.effector.position).magnitude).Using(floatCompare));
        }

        [Test]
        public void RestoreDefaultPoseNotSet_IteratesToTarget()
        {
            var fabrik = solver as FabrikSolver2D;
            fabrik.iterations = 1;
            solver.solveFromDefaultPose = false;

            var targetPosition = new Vector3(9.0f, 1.0f, 0.0f);

            targetGO.transform.position = targetPosition;

            for (int i = 0; i < 10; ++i)
                manager.UpdateManager();

            var chain = solver.GetChain(0);

            Assert.That(targetPosition, Is.EqualTo(chain.effector.position).Using(vec3Compare));
            Assert.That(0.0f, Is.EqualTo((targetPosition - chain.effector.position).magnitude).Using(floatCompare));
        }

        private static IEnumerable<WeightTestCase> WeightTestCases()
        {
            yield return new WeightTestCase(0.0f, new Vector3(9f, 2f, 0f), new Vector3(10f, 0f, 0f));
            yield return new WeightTestCase(0.25f, new Vector3(9f, 2f, 0f), new Vector3(9.93551f, 0.5509112f, 0f));
            yield return new WeightTestCase(0.5f, new Vector3(9f, 2f, 0f), new Vector3(9.744007f, 1.080582f, 0f));
            yield return new WeightTestCase(0.75f, new Vector3(9f, 2f, 0f), new Vector3(9.430839f, 1.570619f, 0f));
            yield return new WeightTestCase(1.0f, new Vector3(9f, 2f, 0f), new Vector3(9.004435f, 2.003679f, 0f));
        }

        [Test]
        public void SetWeight_SolverUpdates([ValueSource("WeightTestCases")] WeightTestCase testCase)
        {
            var chain = solver.GetChain(0);
            chain.target.position = testCase.targetPosition;
            solver.weight = testCase.weight;
            
            solver.UpdateIK(1f);
            Assert.That(chain.effector.position, Is.EqualTo(testCase.expectedEffectorPosition).Using(vec3Compare)); 
        }

        [Test]
        public void SetWeight_NoTarget_PositionListProvided_SolverUpdates([ValueSource("WeightTestCases")] WeightTestCase testCase)
        {
            var chain = solver.GetChain(0);
            chain.target = null;
            solver.weight = testCase.weight;

            var positions = new List<Vector3>();
            positions.Add(testCase.targetPosition);

            solver.UpdateIK(positions, 1f);
            Assert.That(chain.effector.position, Is.EqualTo(testCase.expectedEffectorPosition).Using(vec3Compare)); 
        }
    }
}
