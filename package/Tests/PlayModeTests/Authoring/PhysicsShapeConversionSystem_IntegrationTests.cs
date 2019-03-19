﻿using System;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Unity.Physics.Tests.Authoring
{
    class PhysicsShapeConversionSystem_IntegrationTests : BaseHierarchyConversionTest
    {
        [Test]
        public void PhysicsShapeConversionSystem_WhenBodyHasOneSiblingShape_CreatesPrimitive()
        {
            CreateHierarchy(
                new[] { typeof(ConvertToEntity), typeof(PhysicsBody), typeof(PhysicsShape) },
                new[] { typeof(ConvertToEntity) },
                new[] { typeof(ConvertToEntity) }
            );
            Root.GetComponent<PhysicsShape>().SetBox(float3.zero, new float3(1,1,1), quaternion.identity);

            var world = new World("Test world");
            GameObjectConversionUtility.ConvertGameObjectHierarchy(Root, world);
            using (var group = world.EntityManager.CreateComponentGroup(typeof(PhysicsCollider)))
            {
                using (var colliders = group.ToComponentDataArray<PhysicsCollider>(Allocator.Persistent))
                {
                    Assume.That(colliders, Has.Length.EqualTo(1));
                    var collider = colliders[0].Value;

                    Assert.That(collider.Value.Type, Is.EqualTo(ColliderType.Box));
                }
            }
        }

        [Test]
        public void PhysicsShapeConversionSystem_WhenBodyHasOneDescendentShape_CreatesCompound()
        {
            CreateHierarchy(
                new[] { typeof(ConvertToEntity), typeof(PhysicsBody) },
                new[] { typeof(ConvertToEntity), typeof(PhysicsShape) },
                new[] { typeof(ConvertToEntity) }
            );
            Parent.GetComponent<PhysicsShape>().SetBox(float3.zero, new float3(1, 1, 1), quaternion.identity);

            var world = new World("Test world");
            GameObjectConversionUtility.ConvertGameObjectHierarchy(Root, world);
            using (var group = world.EntityManager.CreateComponentGroup(typeof(PhysicsCollider)))
            {
                using (var colliders = group.ToComponentDataArray<PhysicsCollider>(Allocator.Persistent))
                {
                    Assume.That(colliders, Has.Length.EqualTo(1));
                    var collider = colliders[0].Value;

                    Assert.That(collider.Value.Type, Is.EqualTo(ColliderType.Compound));
                    unsafe
                    {
                        var compoundCollider = (CompoundCollider*)(collider.GetUnsafePtr());
                        Assert.That(compoundCollider->Children, Has.Length.EqualTo(1));
                        Assert.That(compoundCollider->Children[0].Collider->Type, Is.EqualTo(ColliderType.Box));
                    }
                }
            }
        }

        [Test]
        public void PhysicsShapeConversionSystem_WhenBodyHasMultipleDescendentShapes_CreatesCompound()
        {
            CreateHierarchy(
                new[] { typeof(ConvertToEntity), typeof(PhysicsBody) },
                new[] { typeof(ConvertToEntity), typeof(PhysicsShape) },
                new[] { typeof(ConvertToEntity), typeof(PhysicsShape) }
            );
            Parent.GetComponent<PhysicsShape>().SetBox(float3.zero, new float3(1, 1, 1), quaternion.identity);
            Child.GetComponent<PhysicsShape>().SetSphere(float3.zero, 1.0f, quaternion.identity);

            var world = new World("Test world");
            GameObjectConversionUtility.ConvertGameObjectHierarchy(Root, world);
            using (var group = world.EntityManager.CreateComponentGroup(typeof(PhysicsCollider)))
            {
                using (var colliders = group.ToComponentDataArray<PhysicsCollider>(Allocator.Persistent))
                {
                    Assume.That(colliders, Has.Length.EqualTo(1));
                    var collider = colliders[0].Value;

                    Assert.That(collider.Value.Type, Is.EqualTo(ColliderType.Compound));
                    unsafe
                    {
                        var compoundCollider = (CompoundCollider*)(collider.GetUnsafePtr());

                        var childTypes = Enumerable.Range(0, compoundCollider->NumChildren)
                            .Select(i => compoundCollider->Children[i].Collider->Type)
                            .ToArray();
                        Assert.That(childTypes, Is.EquivalentTo(new[] { ColliderType.Box, ColliderType.Sphere }));
                    }
                }
            }
        }

        [Ignore("GameObjectConversionUtility does not yet support multiples of the same component type.")]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        public void PhysicsShapeConversionSystem_WhenBodyHasMultipleSiblingShapes_CreatesCompound(int shapeCount)
        {
            CreateHierarchy(
                new[] { typeof(ConvertToEntity), typeof(PhysicsBody) },
                new[] { typeof(ConvertToEntity) },
                new[] { typeof(ConvertToEntity) }
            );
            for (int i = 0; i < shapeCount; ++i)
            {
                Root.AddComponent<PhysicsShape>().SetBox(float3.zero, new float3(1, 1, 1), quaternion.identity);
            }

            var world = new World("Test world");
            GameObjectConversionUtility.ConvertGameObjectHierarchy(Root, world);
            using (var group = world.EntityManager.CreateComponentGroup(typeof(PhysicsCollider)))
            {
                using (var colliders = group.ToComponentDataArray<PhysicsCollider>(Allocator.Persistent))
                {
                    Assume.That(colliders, Has.Length.EqualTo(1));
                    var collider = colliders[0].Value;

                    Assert.That(collider.Value.Type, Is.EqualTo(ColliderType.Compound));
                    unsafe
                    {
                        var compoundCollider = (CompoundCollider*)(collider.GetUnsafePtr());
                        Assert.That(compoundCollider->Children, Has.Length.EqualTo(shapeCount));
                        for (int i = 0; i < compoundCollider->Children.Length; i++)
                        {
                            Assert.That(compoundCollider->Children[i].Collider->Type, Is.EqualTo(ColliderType.Box));
                        }
                    }
                }
            }
        }
    }
}
