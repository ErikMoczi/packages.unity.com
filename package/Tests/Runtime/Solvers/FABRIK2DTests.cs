using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Experimental.U2D.IK;

namespace UnityEngine.Experimental.U2D.IK.Tests.FABRIK2DTests
{
    public class FABRIK2DTests
    {
        private Vector2Compare vec2Compare = new Vector2Compare();
        private FloatCompare floatCompare = new FloatCompare();
        private Vector2[] positions;
        private float[] lengths;

        private const int kIterations = 10;
        private const float kTolerance = 0.01f;

        [SetUp]
        public void Setup()
        {
            positions = new Vector2[]
            {
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 0.0f),
                new Vector2(3.0f, 0.0f),
                new Vector2(6.0f, 0.0f),
                new Vector2(10.0f, 0.0f),
            };
            lengths = new float[positions.Length - 1];
            for (int i = 1; i < positions.Length; ++i)
            {
                lengths[i - 1] = (positions[i] - positions[i - 1]).magnitude;
            }
        }

        [TearDown]
        public void Teardown()
        {
        }

        [Test]
        public void TargetIsReachableForChain_EndPointReachesTarget()
        {
            var targetPosition = new Vector2(9.0f, 1.0f);

            var result = FABRIK2D.Solve(targetPosition, kIterations, kTolerance, lengths, ref positions);

            Assert.AreEqual(true, result);
            Assert.That(targetPosition, Is.EqualTo(positions[positions.Length - 1]).Using(vec2Compare));
            Assert.That(0.0f, Is.EqualTo((targetPosition - positions[positions.Length - 1]).magnitude).Using(floatCompare));
        }

        [Test]
        public void TargetIsLongerThanChain_EndPointIsAtClosestPointToTarget()
        {
            var targetPosition = new Vector2(0.0f, 12.0f);

            var result = FABRIK2D.Solve(targetPosition, kIterations, kTolerance, lengths, ref positions);

            Assert.AreEqual(true, result);
            Assert.That(targetPosition, Is.Not.EqualTo(positions[positions.Length - 1]).Using(vec2Compare));
            Assert.That(2.0f, Is.EqualTo((targetPosition - positions[positions.Length - 1]).magnitude).Using(floatCompare));
        }

        [Test]
        public void TargetHasReached_SolverDoesNotIterate()
        {
            var targetPosition = new Vector2(9.0f, 1.0f);

            var result = FABRIK2D.Solve(targetPosition, kIterations, kTolerance, lengths, ref positions);

            Assert.AreEqual(true, result);

            result = FABRIK2D.Solve(targetPosition, kIterations, kTolerance, lengths, ref positions);

            Assert.AreEqual(false, result);
        }
    }
}
