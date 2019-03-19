using System;
using NUnit.Framework;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Unity.Physics.Tests.Authoring
{
    class PhysicsShapeExtensions_IntegrationTests : BaseHierarchyConversionTest
    {
        [Test]
        public void GetPrimaryBody_WhenHierarchyContainsMultipleBodies_ReturnsFirstParent(
            [Values(typeof(Rigidbody), typeof(PhysicsBody))]Type rootBodyType,
            [Values(typeof(Rigidbody), typeof(PhysicsBody))]Type parentBodyType
        )
        {
            CreateHierarchy(new[] { rootBodyType }, new[] { parentBodyType }, Array.Empty<Type>());

            var primaryBody = PhysicsShapeExtensions.GetPrimaryBody(Child);

            Assert.That(primaryBody, Is.EqualTo(Parent));
        }

        [Test]
        public void GetPrimaryBody_WhenHierarchyContainsNoBodies_ReturnsTopMostShape(
            [Values(typeof(UnityEngine.BoxCollider), typeof(PhysicsShape))]Type rootShapeType,
            [Values(typeof(UnityEngine.BoxCollider), typeof(PhysicsShape))]Type parentShapeType,
            [Values(typeof(UnityEngine.BoxCollider), typeof(PhysicsShape))]Type childShapeType
        )
        {
            CreateHierarchy(new[] { rootShapeType }, new[] { parentShapeType }, new[] { childShapeType });

            var primaryBody = PhysicsShapeExtensions.GetPrimaryBody(Child);

            Assert.That(primaryBody, Is.EqualTo(Root));
        }
    }
}
