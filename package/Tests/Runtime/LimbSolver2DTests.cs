using NUnit.Framework;
using UnityEngine.Experimental.U2D.IK;

namespace UnityEngine.Experimental.U2D.IK.Tests.LimbSolver2DTests
{
    public class LimbSolver2DTests
    {
        private FloatCompare floatCompare = new FloatCompare();

        private GameObject go;
        private GameObject ikGO;
        private GameObject targetGO;

        private IKManager2D manager;
        private LimbSolver2D solver;
        private IKChain2D chain;

        [SetUp]
        public void Setup()
        {
            go = new GameObject();
            var childGO = new GameObject();
            childGO.transform.parent = go.transform;

            var effectorGO = new GameObject();
            effectorGO.transform.parent = childGO.transform;

            go.transform.position = Vector3.zero;
            childGO.transform.position = new Vector3(1.0f, 0.0f, 0.0f);
            effectorGO.transform.position = new Vector3(3.0f, 0.0f, 0.0f);

            ikGO = new GameObject();
            manager = ikGO.AddComponent<IKManager2D>();
            var lsGO = new GameObject();
            solver = lsGO.AddComponent<LimbSolver2D>();
            lsGO.transform.parent = ikGO.transform;

            this.targetGO = new GameObject();
            this.targetGO.transform.parent = solver.transform;

            chain = solver.GetChain(0);
            chain.effector = effectorGO.transform;
            chain.target = this.targetGO.transform;

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
            Assert.AreEqual(false, solver.flip);
        }

        [Test]
        public void TransformCount_IsCorrectForLimbSolver()
        {
            Assert.AreEqual(3, chain.transformCount);
        }

        [Test]
        public void TargetIsReachableForChain_EndPointReachesTarget()
        {
            var targetPosition = new Vector2(1.0f, 2.0f);

            targetGO.transform.position = targetPosition;

            manager.UpdateManager();

            Assert.That(126.87f, Is.EqualTo(chain.transforms[0].localRotation.eulerAngles.z).Using(floatCompare));
            Assert.That(270f, Is.EqualTo(chain.transforms[1].localRotation.eulerAngles.z).Using(floatCompare));
        }

        [Test]
        public void TargetIsReachableForChain_FlipIsEnabled_EndPointReachesTargetFlipped()
        {
            var targetPosition = new Vector2(1.0f, 2.0f);

            solver.flip = true;
            Assert.AreEqual(true, solver.flip);

            targetGO.transform.position = targetPosition;

            manager.UpdateManager();

            Assert.That(0f, Is.EqualTo(chain.transforms[0].localRotation.eulerAngles.z).Using(floatCompare));
            Assert.That(90f, Is.EqualTo(chain.transforms[1].localRotation.eulerAngles.z).Using(floatCompare));
        }

        [Test]
        public void TargetIsLongerThanChain_EndPointIsAtClosestPointToTarget()
        {
            var targetPosition = new Vector2(0.0f, 4.0f);

            targetGO.transform.position = targetPosition;

            manager.UpdateManager();

            Assert.That(90f, Is.EqualTo(chain.transforms[0].localRotation.eulerAngles.z).Using(floatCompare));
            Assert.That(0f, Is.EqualTo(chain.transforms[1].localRotation.eulerAngles.z).Using(floatCompare));
        }
    }
}
