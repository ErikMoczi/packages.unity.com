using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEditor;
using NSubstitute;
using NUnit.Framework.Constraints;

namespace UnityEditor.Experimental.U2D.Animation.Test.Skeleton
{
    [TestFixture]
    public class SkeletonCacheTest
    {
        private class QuaternionCompare : IEqualityComparer<Quaternion>
        {
            public bool Equals(Quaternion a, Quaternion b)
            {
                return Quaternion.Dot(a, b) > 1f - Epsilon;
            }

            public int GetHashCode(Quaternion v)
            {
                return v.GetHashCode();
            }

            private static readonly float Epsilon = 0.001f;
        }

        private class Vector3Compare : IEqualityComparer<Vector3>
        {
            public bool Equals(Vector3 a, Vector3 b)
            {
                return Vector3.Distance(a, b) < Epsilon;
            }

            public int GetHashCode(Vector3 v)
            {
                return v.GetHashCode();
            }

            private static readonly float Epsilon = 0.001f;
        }

        private QuaternionCompare quatCompare = new QuaternionCompare();
        private Vector3Compare vec3Compare = new Vector3Compare();

        private SkinningCache m_SkinningCache;
        private SkeletonCache m_Skeleton;
        private SkeletonController m_Controller;
        private ISkeletonView m_View;
        private ISelection<BoneCache> m_Selection;

        [SetUp]
        public void Setup()
        {
            m_SkinningCache = Cache.Create<SkinningCache>();
            m_Skeleton = m_SkinningCache.CreateCache<SkeletonCache>();

            m_View = Substitute.For<ISkeletonView>();

            m_Controller = new SkeletonController();
            m_Controller.view = m_View;
            m_Controller.skeleton = m_Skeleton;
        }

        [TearDown]
        public void TearDown()
        {
            Cache.Destroy(m_SkinningCache);
        }

        private void CreateChain()
        
        {
            var root = m_Skeleton.CreateBone(null, Vector3.zero, Vector3.right, false, "root");
            var chain0 = m_Skeleton.CreateBone(root,root.endPosition, root.endPosition + Vector3.right, true, "chain0");
            m_Skeleton.CreateBone(chain0, chain0.endPosition, chain0.endPosition + Vector3.right, true, "chain1");
        }

        private void CreateChainReversedCreationOrder()
        {
            var chain1 = m_Skeleton.CreateBone(null, Vector3.zero, Vector3.right, true, "chain1");
            var chain0 = m_Skeleton.CreateBone(null, Vector3.zero, Vector3.right, true, "chain0");
            var root = m_Skeleton.CreateBone(null, Vector3.zero, Vector3.right, false, "root");

            chain1.SetParent(chain0);
            chain0.SetParent(root);

            chain1.position = chain0.endPosition;
            chain0.position = root.endPosition;
        }

        [Test]
        public void CreateBone_CreatesValidChain()
        {
            CreateChain();

            Assert.NotNull(m_Skeleton.GetBone(0), "Bone was not created");
            Assert.NotNull(m_Skeleton.GetBone(1), "Bone was not created");
            Assert.NotNull(m_Skeleton.GetBone(2), "Bone was not created");
            Assert.IsNull(m_Skeleton.GetBone(0).parentBone, "Not a root bone");
            Assert.AreEqual(m_Skeleton.GetBone(1), m_Skeleton.GetBone(0).chainedChild, "Incorrect chained child");
            Assert.AreEqual(m_Skeleton.GetBone(0), m_Skeleton.GetBone(1).parentBone, "Incorrect parent bone");
            Assert.AreEqual(m_Skeleton.GetBone(0).endPosition, m_Skeleton.GetBone(1).position, "Incorrect position");
            Assert.AreEqual(m_Skeleton.GetBone(0).name, "root", "Incorrect name");
            Assert.AreEqual(m_Skeleton.GetBone(1).name, "chain0", "Incorrect name");
            Assert.AreEqual(m_Skeleton.GetBone(2).name, "chain1", "Incorrect name");
            Assert.AreEqual(3, m_Skeleton.BoneCount, "Incorrect BoneCount");
        }

        [Test]
        public void DestroyRootBone_ChildBecomesTheRoot()
        {
            CreateChain();

            var first = m_Skeleton.GetBone(0);
            var child = first.chainedChild;

            m_Skeleton.DestroyBone(first);

            Assert.IsTrue(m_SkinningCache.IsRemoved(first), "Object not destoyed");
            Assert.AreEqual(2, m_Skeleton.BoneCount, "Incorrect BoneCount");
            Assert.IsNull(child.parentBone, "Invalid parent bone");
        }

        [Test]
        public void DestroyBones_DestroysArray()
        {
            CreateChain();

            var bones = m_Skeleton.bones;

            m_Skeleton.DestroyBones(bones);

            Assert.AreEqual(0, m_Skeleton.BoneCount, "Incorrect BoneCount");
            Assert.IsTrue(m_SkinningCache.IsRemoved(bones[0]), "Object not destoyed");
            Assert.IsTrue(m_SkinningCache.IsRemoved(bones[1]), "Object not destoyed");
            Assert.IsTrue(m_SkinningCache.IsRemoved(bones[2]), "Object not destoyed");
        }

        [Test]
        public void Clear_DestroyBones()
        {
            CreateChain();

            var bones = m_Skeleton.bones;

            m_Skeleton.Clear();

            Assert.AreEqual(0, m_Skeleton.BoneCount, "Incorrect BoneCount");
            Assert.IsTrue(m_SkinningCache.IsRemoved(bones[0]), "Object not destoyed");
            Assert.IsTrue(m_SkinningCache.IsRemoved(bones[1]), "Object not destoyed");
            Assert.IsTrue(m_SkinningCache.IsRemoved(bones[2]), "Object not destoyed");
        }

        [Test]
        public void RestoreDefaultPose_RestoresPreviousSetDefaultPose()
        {
            CreateChain();

            m_Skeleton.SetDefaultPose();

            var bonePose = m_Skeleton.GetLocalPose();

            m_Skeleton.GetBone(0).localRotation = Quaternion.AngleAxis(30f, Vector3.forward);
            m_Skeleton.GetBone(1).localRotation = Quaternion.AngleAxis(30f, Vector3.forward);
            m_Skeleton.GetBone(2).localRotation = Quaternion.AngleAxis(30f, Vector3.forward);

            var newBonePose = m_Skeleton.GetLocalPose();

            Assert.IsFalse(bonePose[0].pose == newBonePose[0].pose, "Incorrect Pose");
            Assert.IsFalse(bonePose[1].pose == newBonePose[1].pose, "Incorrect Pose");
            Assert.IsFalse(bonePose[2].pose == newBonePose[2].pose, "Incorrect Pose");
            Assert.IsFalse(bonePose[0] == newBonePose[0], "Incorrect Pose");

            m_Skeleton.RestoreDefaultPose();

            newBonePose = m_Skeleton.GetLocalPose();

            Assert.IsTrue(bonePose[0].pose == newBonePose[0].pose, "Incorrect Pose");
            Assert.IsTrue(bonePose[1].pose == newBonePose[1].pose, "Incorrect Pose");
            Assert.IsTrue(bonePose[2].pose == newBonePose[2].pose, "Incorrect Pose");
            Assert.IsTrue(bonePose[0] == newBonePose[0], "Incorrect Pose");
        }

        [Test]
        public void SetDefaultPose_SetsBindPose()
        {
            CreateChain();

            m_Skeleton.GetBone(0).localRotation = Quaternion.AngleAxis(30f, Vector3.forward);
            m_Skeleton.GetBone(1).localRotation = Quaternion.AngleAxis(30f, Vector3.forward);
            m_Skeleton.GetBone(2).localRotation = Quaternion.AngleAxis(30f, Vector3.forward);

            m_Skeleton.SetDefaultPose();

            var bindpose0 = m_Skeleton.GetBone(0).bindPose;
            var bindpose1 = m_Skeleton.GetBone(1).bindPose;
            var bindpose2 = m_Skeleton.GetBone(2).bindPose;

            Assert.AreEqual(Pose.Create(new Vector3(0f, 0f, 0f), Quaternion.AngleAxis(30, Vector3.forward)), bindpose0, "Incorrect pose");
            Assert.AreEqual(Pose.Create(new Vector3(0.8660254f, 0.5f, 0f), Quaternion.AngleAxis(60, Vector3.forward)), bindpose1, "Incorrect pose");
            Assert.AreEqual(Pose.Create(new Vector3(1.366025f, 1.366025f, 0f), Quaternion.AngleAxis(90, Vector3.forward)), bindpose2, "Incorrect pose");
        }

        [Test]
        public void SetLocalPose_SetsLocalPositionRotationLength()
        {
            CreateChain();

            var localPose = new BonePose[3]
            {
                BonePose.Create(Pose.Create(Vector3.zero, Quaternion.AngleAxis(30, Vector3.forward)), 0f),
                BonePose.Create(Pose.Create(Vector3.zero, Quaternion.AngleAxis(30, Vector3.forward)), 0f),
                BonePose.Create(Pose.Create(Vector3.zero, Quaternion.AngleAxis(30, Vector3.forward)), 0f),
            };

            m_Skeleton.SetLocalPose(localPose);

            Assert.AreEqual(localPose[0].pose.position, m_Skeleton.GetBone(0).localPosition, "Incorrect position");
            Assert.AreEqual(localPose[1].pose.position, m_Skeleton.GetBone(1).localPosition, "Incorrect position");
            Assert.AreEqual(localPose[2].pose.position, m_Skeleton.GetBone(2).localPosition, "Incorrect position");

            Assert.That(m_Skeleton.GetBone(0).localRotation, Is.EqualTo(localPose[0].pose.rotation).Using(quatCompare), "Incorrect rotation");
            Assert.That(m_Skeleton.GetBone(1).localRotation, Is.EqualTo(localPose[1].pose.rotation).Using(quatCompare), "Incorrect rotation");
            Assert.That(m_Skeleton.GetBone(2).localRotation, Is.EqualTo(localPose[2].pose.rotation).Using(quatCompare), "Incorrect rotation");

            Assert.AreEqual(localPose[0], m_Skeleton.GetBone(0).localPose, "Incorrect pose");
            Assert.AreEqual(localPose[1], m_Skeleton.GetBone(1).localPose, "Incorrect pose");
            Assert.AreEqual(localPose[2], m_Skeleton.GetBone(2).localPose, "Incorrect pose");
        }

        [Test]
        public void SetWorldPose_SetsWorldPositionRotationLength()
        {
            CreateChain();

            var worldPose = new BonePose[3]
            {
                BonePose.Create(Pose.Create(Vector3.zero, Quaternion.AngleAxis(30, Vector3.forward)), 0f),
                BonePose.Create(Pose.Create(Vector3.zero, Quaternion.AngleAxis(30, Vector3.forward)), 0f),
                BonePose.Create(Pose.Create(Vector3.zero, Quaternion.AngleAxis(30, Vector3.forward)), 0f),
            };

            m_Skeleton.SetWorldPose(worldPose);

            Assert.AreEqual(worldPose[0].pose.position, m_Skeleton.GetBone(0).position, "Incorrect position");
            Assert.AreEqual(worldPose[1].pose.position, m_Skeleton.GetBone(1).position, "Incorrect position");
            Assert.AreEqual(worldPose[2].pose.position, m_Skeleton.GetBone(2).position, "Incorrect position");

            Assert.That(m_Skeleton.GetBone(0).rotation, Is.EqualTo(worldPose[0].pose.rotation).Using(quatCompare), "Incorrect rotation");
            Assert.That(m_Skeleton.GetBone(1).rotation, Is.EqualTo(worldPose[1].pose.rotation).Using(quatCompare), "Incorrect rotation");
            Assert.That(m_Skeleton.GetBone(2).rotation, Is.EqualTo(worldPose[2].pose.rotation).Using(quatCompare), "Incorrect rotation");

            Assert.AreEqual(worldPose[0], m_Skeleton.GetBone(0).worldPose, "Incorrect pose");
            Assert.AreEqual(worldPose[1], m_Skeleton.GetBone(1).worldPose, "Incorrect pose");
            Assert.AreEqual(worldPose[2], m_Skeleton.GetBone(2).worldPose, "Incorrect pose");
        }

        [Test]
        public void SetWorldPose_SetsWorldPositionRotationLength_ChainReversedCreationOrder()
        {
            CreateChainReversedCreationOrder();

            var worldPose = new BonePose[3]
            {
                BonePose.Create(Pose.Create(Vector3.zero, Quaternion.AngleAxis(30, Vector3.forward)), 0f),
                BonePose.Create(Pose.Create(Vector3.zero, Quaternion.AngleAxis(30, Vector3.forward)), 0f),
                BonePose.Create(Pose.Create(Vector3.zero, Quaternion.AngleAxis(30, Vector3.forward)), 0f),
            };

            m_Skeleton.SetWorldPose(worldPose);

            Assert.AreEqual(worldPose[0].pose.position, m_Skeleton.GetBone(0).position, "Incorrect position");
            Assert.AreEqual(worldPose[1].pose.position, m_Skeleton.GetBone(1).position, "Incorrect position");
            Assert.AreEqual(worldPose[2].pose.position, m_Skeleton.GetBone(2).position, "Incorrect position");

            Assert.That(m_Skeleton.GetBone(0).rotation, Is.EqualTo(worldPose[0].pose.rotation).Using(quatCompare), "Incorrect rotation");
            Assert.That(m_Skeleton.GetBone(1).rotation, Is.EqualTo(worldPose[1].pose.rotation).Using(quatCompare), "Incorrect rotation");
            Assert.That(m_Skeleton.GetBone(2).rotation, Is.EqualTo(worldPose[2].pose.rotation).Using(quatCompare), "Incorrect rotation");

            Assert.AreEqual(worldPose[0], m_Skeleton.GetBone(0).worldPose, "Incorrect pose");
            Assert.AreEqual(worldPose[1], m_Skeleton.GetBone(1).worldPose, "Incorrect pose");
            Assert.AreEqual(worldPose[2], m_Skeleton.GetBone(2).worldPose, "Incorrect pose");
        }

        [Test]
        public void RotateBones()
        {
            CreateChain();

            var bones = m_Skeleton.bones;

            m_Skeleton.RotateBones(bones, 30);

            Assert.That(bones[0].localRotation, Is.EqualTo(Quaternion.AngleAxis(30, Vector3.forward)).Using(quatCompare), "Incorrect rotation");
            Assert.That(bones[1].localRotation, Is.EqualTo(Quaternion.AngleAxis(30, Vector3.forward)).Using(quatCompare), "Incorrect rotation");
            Assert.That(bones[2].localRotation, Is.EqualTo(Quaternion.AngleAxis(30, Vector3.forward)).Using(quatCompare), "Incorrect rotation");
        }

        [Test]
        public void MoveBones()
        {
            CreateChain();

            var worldPose = m_Skeleton.GetWorldPose();
            var bones = m_Skeleton.bones;

            m_Skeleton.MoveBones(bones, Vector3.up);

            Assert.That(bones[0].position, Is.EqualTo(worldPose[0].pose.position + Vector3.up).Using(vec3Compare));
            Assert.That(bones[1].position, Is.EqualTo(worldPose[1].pose.position + Vector3.up * 2f).Using(vec3Compare));
            Assert.That(bones[2].position, Is.EqualTo(worldPose[2].pose.position + Vector3.up * 3f).Using(vec3Compare));
        }

        [Test]
        public void FreeMoveBones()
        {
            CreateChain();

            var worldPose = m_Skeleton.GetWorldPose();

            m_Skeleton.FreeMoveBones(new BoneCache[] { m_Skeleton.GetBone(1) }, Vector3.up);

            Assert.That(m_Skeleton.GetBone(0).position, Is.EqualTo(worldPose[0].pose.position).Using(vec3Compare));
            Assert.That(m_Skeleton.GetBone(1).position, Is.EqualTo(worldPose[1].pose.position + Vector3.up).Using(vec3Compare));
            Assert.That(m_Skeleton.GetBone(2).position, Is.EqualTo(worldPose[2].pose.position).Using(vec3Compare));

            Assert.That(m_Skeleton.GetBone(0).rotation, Is.EqualTo(worldPose[0].pose.rotation).Using(quatCompare), "Incorrect rotation");
            Assert.That(m_Skeleton.GetBone(1).rotation, Is.EqualTo(worldPose[1].pose.rotation).Using(quatCompare), "Incorrect rotation");
            Assert.That(m_Skeleton.GetBone(2).rotation, Is.EqualTo(worldPose[2].pose.rotation).Using(quatCompare), "Incorrect rotation");
        }

        [Test]
        public void MoveJoints()
        {
            CreateChain();

            var worldPose = m_Skeleton.GetWorldPose();

            m_Skeleton.MoveJoints(new BoneCache[] { m_Skeleton.GetBone(1) }, Vector3.up);

            Assert.That(m_Skeleton.GetBone(0).position, Is.EqualTo(worldPose[0].pose.position).Using(vec3Compare));
            Assert.That(m_Skeleton.GetBone(1).position, Is.EqualTo(worldPose[1].pose.position + Vector3.up).Using(vec3Compare));
            Assert.That(m_Skeleton.GetBone(2).position, Is.EqualTo(worldPose[2].pose.position).Using(vec3Compare));

            Assert.That(m_Skeleton.GetBone(0).rotation, Is.EqualTo(Quaternion.AngleAxis(45f, Vector3.forward)).Using(quatCompare), "Incorrect rotation");
            Assert.That(m_Skeleton.GetBone(1).rotation, Is.EqualTo(Quaternion.AngleAxis(45f, Vector3.back)).Using(quatCompare), "Incorrect rotation");
            Assert.That(m_Skeleton.GetBone(2).rotation, Is.EqualTo(worldPose[2].pose.rotation).Using(quatCompare), "Incorrect rotation");
        }

        [Test]
        public void SplitBone_BoneAtMiddleOfChain()
        {
            CreateChain();

            var boneToSplit = m_Skeleton.GetBone(1);

            var newBone = m_Skeleton.SplitBone(boneToSplit, boneToSplit.length * 0.25f, "newBone");

            Assert.AreEqual(4, m_Skeleton.BoneCount, "Incorrect bone count");
            Assert.That(newBone.position, Is.EqualTo(new Vector3(1.25f, 0f, 0f)).Using(vec3Compare), "Incorrect position");
            Assert.AreEqual(0.25f, boneToSplit.length, "Incorrect length");
            Assert.AreEqual("newBone", newBone.name, "Incorrect length");
        }
    }
}
