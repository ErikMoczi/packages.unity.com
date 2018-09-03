using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Experimental.U2D.IK;

namespace UnityEngine.Experimental.U2D.IK.Tests.CCD2DTests
{
    public class CCD2DTests
    {
        private FloatCompare floatCompare = new FloatCompare();
        private Vector3Compare vec3Compare = new Vector3Compare();
        private Vector3[] positions;
        private float[] lengths;

        private const int kIterations = 500;
        private const float kTolerance = 0.01f;
        private const float kVelocity = 0.5f;

        [SetUp]
        public void Setup()
        {
            positions = new Vector3[]
            {
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(3.0f, 0.0f, 0.0f),
                new Vector3(6.0f, 0.0f, 0.0f),
                new Vector3(10.0f, 0.0f, 0.0f),
            };
        }

        [TearDown]
        public void Teardown()
        {
        }

        [Test]
        public void TargetIsReachableForChain_EndPointReachesTarget()
        {
            var targetPosition = new Vector3(9.0f, 1.0f, 0.0f);

            var result = CCD2D.Solve(targetPosition, Vector3.forward, kIterations, kTolerance, kVelocity, ref positions);

            Assert.AreEqual(true, result);
            Assert.That(targetPosition, Is.EqualTo(positions[positions.Length - 1]).Using(vec3Compare));
            Assert.That(0.0f, Is.EqualTo((targetPosition - positions[positions.Length - 1]).magnitude).Using(floatCompare));
        }

        [Test]
        public void TargetIsLongerThanChain_EndPointIsAtClosestPointToTarget()
        {
            var targetPosition = new Vector3(0.0f, 12.0f, 0.0f);

            var result = CCD2D.Solve(targetPosition, Vector3.forward, kIterations, kTolerance, kVelocity, ref positions);

            Assert.AreEqual(true, result);
            Assert.That(targetPosition, Is.Not.EqualTo(positions[positions.Length - 1]).Using(vec3Compare));
            Assert.That(2.0f, Is.EqualTo((targetPosition - positions[positions.Length - 1]).magnitude).Using(floatCompare));
        }

        [Test]
        public void TargetHasReached_SolverDoesNotIterate()
        {
            var targetPosition = new Vector3(9.0f, 1.0f, 0.0f);

            var result = CCD2D.Solve(targetPosition, Vector3.forward, kIterations, kTolerance, kVelocity, ref positions);

            Assert.AreEqual(true, result);

            result = CCD2D.Solve(targetPosition, Vector3.forward, kIterations, kTolerance, kVelocity, ref positions);

            Assert.AreEqual(false, result);
        }
    }
}
