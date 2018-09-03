using NUnit.Framework;

namespace UnityEngine.Experimental.U2D.IK.Tests.FABRIK2DCompositeTests
{
    public class FABRIK2DCompositeTests
    {
        private Vector2Compare vec2Compare = new Vector2Compare();
        private FloatCompare floatCompare = new FloatCompare();

        private FABRIKChain2D[] chains;

        private const int kIterations = 10;
        private const float kTolerance = 0.01f;

        [SetUp]
        public void Setup()
        {
            // Setup a Y-Split chain
            chains = new FABRIKChain2D[3];
            chains[0] = new FABRIKChain2D
            {
                origin = new Vector2(0.0f, -1.0f),
                target = Vector2.zero,
                sqrTolerance = kTolerance * kTolerance,
                positions = new Vector2[]
                {
                    new Vector2(0.0f, -1.0f),
                    new Vector2(0.0f, 0.0f),
                },
                lengths = new float[]
                {
                    1.0f,
                },
                subChainIndices = new int[]
                {
                    1,
                    2,
                },
                // worldPositions is unused
            };
            chains[1] = new FABRIKChain2D
            {
                origin = Vector2.zero,
                target = Vector2.zero,
                sqrTolerance = kTolerance * kTolerance,
                positions = new Vector2[]
                {
                    new Vector2(0.0f, 0.0f),
                    new Vector2(-1.0f, 0.0f),
                    new Vector2(-3.0f, 0.0f),
                    new Vector2(-6.0f, 0.0f),
                    new Vector2(-10.0f, 0.0f),
                },
                lengths = new float[]
                {
                    1.0f,
                    2.0f,
                    3.0f,
                    4.0f,
                },
                subChainIndices = new int[0],
                // worldPositions is unused
            };
            chains[2] = new FABRIKChain2D
            {
                origin = Vector2.zero,
                target = Vector2.zero,
                sqrTolerance = kTolerance * kTolerance,
                positions = new Vector2[]
                {
                    new Vector2(0.0f, 0.0f),
                    new Vector2(1.0f, 0.0f),
                    new Vector2(3.0f, 0.0f),
                    new Vector2(6.0f, 0.0f),
                    new Vector2(10.0f, 0.0f),
                },
                lengths = new float[]
                {
                    1.0f,
                    2.0f,
                    3.0f,
                    4.0f,
                },
                subChainIndices = new int[0],
                // worldPositions is unused
            };
        }

        [TearDown]
        public void Teardown()
        {
        }

        [Test]
        public void TargetIsReachableForChain_EndPointReachesTarget()
        {
            var targetPosition1 = new Vector2(-9.0f, 1.0f);
            var targetPosition2 = new Vector2(9.0f, 1.0f);

            chains[1].target = targetPosition1;
            chains[2].target = targetPosition2;

            var result = FABRIK2D.SolveChain(kIterations, ref chains);

            Assert.AreEqual(true, result);
            Assert.That(targetPosition1, Is.EqualTo(chains[1].positions[chains[1].positions.Length - 1]).Using(vec2Compare));
            Assert.That(0.0f, Is.EqualTo((targetPosition1 - chains[1].positions[chains[1].positions.Length - 1]).magnitude).Using(floatCompare));
            Assert.That(targetPosition2, Is.EqualTo(chains[2].positions[chains[2].positions.Length - 1]).Using(vec2Compare));
            Assert.That(0.0f, Is.EqualTo((targetPosition2 - chains[2].positions[chains[2].positions.Length - 1]).magnitude).Using(floatCompare));
        }

        [Test]
        public void TargetIsLongerThanChain_EndPointIsAtClosestPointToTarget()
        {
            var targetPosition1 = new Vector2(0f, 12.0f);
            var targetPosition2 = new Vector2(0f, 12.0f);

            chains[1].target = targetPosition1;
            chains[2].target = targetPosition2;

            var result = FABRIK2D.SolveChain(kIterations, ref chains);

            Assert.AreEqual(true, result);
            Assert.That(targetPosition1, Is.Not.EqualTo(chains[1].positions[chains[1].positions.Length - 1]).Using(vec2Compare));
            Assert.That(2.0f, Is.EqualTo((targetPosition1 - chains[1].positions[chains[1].positions.Length - 1]).magnitude).Using(floatCompare));
            Assert.That(targetPosition2, Is.Not.EqualTo(chains[2].positions[chains[2].positions.Length - 1]).Using(vec2Compare));
            Assert.That(2.0f, Is.EqualTo((targetPosition2 - chains[2].positions[chains[2].positions.Length - 1]).magnitude).Using(floatCompare));
        }

        [Test]
        public void TargetHasReached_SolverDoesNotIterate()
        {
            var targetPosition1 = new Vector2(-9.0f, 1.0f);
            var targetPosition2 = new Vector2(9.0f, 1.0f);

            chains[1].target = targetPosition1;
            chains[2].target = targetPosition2;

            var result = FABRIK2D.SolveChain(kIterations, ref chains);

            Assert.AreEqual(true, result);

            result = FABRIK2D.SolveChain(kIterations, ref chains);

            Assert.AreEqual(false, result);
        }
    }
}
