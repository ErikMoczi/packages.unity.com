using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Experimental.U2D.IK;

namespace UnityEngine.Experimental.U2D.IK.Tests.Limb2DTests
{
    public class LimbTests
    {
        private FloatCompare floatCompare = new FloatCompare();
        private Vector3[] positions;
        private float[] lengths;
        private float[] angles;

        [SetUp]
        public void Setup()
        {
            positions = new Vector3[]
            {
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(3.0f, 0.0f, 0.0f),
            };
            lengths = new float[positions.Length - 1];
            for (int i = 1; i < positions.Length; ++i)
            {
                lengths[i - 1] = (positions[i] - positions[i - 1]).magnitude;
            }

            angles = new float[]
            {
                0.0f,
                0.0f
            };
        }

        [TearDown]
        public void Teardown()
        {
        }

        [Test]
        public void TargetIsReachableForChain_EndPointReachesTarget()
        {
            var targetPosition = new Vector2(1.0f, 2.0f);

            var result = Limb.Solve(targetPosition, lengths, positions, ref angles);

            Assert.AreEqual(true, result);
            Assert.That(63.4341518f, Is.EqualTo(angles[0]).Using(floatCompare));
            Assert.That(90f, Is.EqualTo(angles[1]).Using(floatCompare));
        }

        [Test]
        public void TargetIsLongerThanChain_EndPointIsAtClosestPointToTarget()
        {
            var targetPosition = new Vector2(0.0f, 4.0f);

            var result = Limb.Solve(targetPosition, lengths, positions, ref angles);

            Assert.AreEqual(true, result);
            Assert.That(0f, Is.EqualTo(angles[0]).Using(floatCompare));
            Assert.That(0f, Is.EqualTo(angles[1]).Using(floatCompare));
        }

        [Test]
        public void InvalidLengths_SolverFails()
        {
            var targetPosition = new Vector2(0.0f, 4.0f);

            lengths[0] = 0.0f;

            var result = Limb.Solve(targetPosition, lengths, positions, ref angles);

            Assert.AreEqual(false, result);
        }
    }
}
