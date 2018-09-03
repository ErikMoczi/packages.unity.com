using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Experimental.U2D;

using NUnit.Framework;

namespace UnityEditor.Experimental.U2D.Animation.Test.Bone
{
    [TestFixture]
    internal class ModelNamingTests
    {
        protected IBoneModel m_Model;

        [SetUp]
        public void Setup()
        {
            m_Model = new BoneModel(() => { });
        }

        [Test]
        public void CreateNewRoot_NamedAsRoot()
        {
            var root = m_Model.CreateNewRoot(Vector3.zero);

            Assert.AreEqual("root", root.name);
        }

        [Test]
        public void CreateANewHierarchy_BoneNameIncrementAutomatically()
        {
            var root = m_Model.CreateNewRoot(Vector3.zero);
            var bone1 = m_Model.CreateNewChildBone(root, Vector3.one);
            var bone2 = m_Model.CreateNewChildBone(bone1, Vector3.right);
            var bone3 = m_Model.CreateNewChildBone(bone2, Vector3.left);
            var bone1_1 = m_Model.CreateNewChildBone(bone1, Vector3.up);

            Assert.AreEqual("root", root.name);
            Assert.AreEqual("bone_1", bone1.name);
            Assert.AreEqual("bone_2", bone2.name);
            Assert.AreEqual("bone_3", bone3.name);
            Assert.AreEqual("bone_4", bone1_1.name);
        }

        [Test]
        public void LoadExistingHierarchy_CreatingNewBone_NameCounterCountinueFromLargest()
        {
            var rawData = new List<UniqueSpriteBone>();

            var root = new UniqueSpriteBone();
            root.name = "root";
            root.position = Vector2.up;
            root.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            root.length = 1f;
            root.parentId = -1;

            var child1 = new UniqueSpriteBone();
            child1.name = "bone_1";
            child1.position = Vector2.one;
            child1.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            child1.length = 1.0f;
            child1.parentId = 0;

            var child2 = new UniqueSpriteBone();
            child2.name = "bone_100";
            child2.position = Vector2.one + Vector2.right;
            child2.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            child2.length = 1.0f;
            child2.parentId = 1;

            var child3 = new UniqueSpriteBone();
            child3.name = "bone_2";
            child3.position = Vector2.right;
            child3.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            child3.length = 1.0f;
            child3.parentId = 1;

            var child4 = new UniqueSpriteBone();
            child4.name = "bone_54";
            child4.position = Vector2.right * 2;
            child4.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            child4.length = 1.0f;
            child4.parentId = 3;

            rawData.Add(root);
            rawData.Add(child1);
            rawData.Add(child2);
            rawData.Add(child3);
            rawData.Add(child4);

            m_Model.SetRawData(rawData, Vector2.zero);

            var rootBone = m_Model.bones.ElementAt(0);
            var bone101 = m_Model.CreateNewChildBone(rootBone, Vector3.one);

            Assert.AreEqual("bone_101", bone101.name);
        }

        [Test]
        public void SetNewBoneNameWithBiggerCounter_AutoNamingAlwaysIncrementFromBiggest()
        {
            var root = m_Model.CreateNewRoot(Vector3.zero);
            var bone1 = m_Model.CreateNewChildBone(root, Vector3.one);
            var bone2 = m_Model.CreateNewChildBone(bone1, Vector3.right);

            m_Model.SetBoneName(bone2, "bone_10");
            var bone11 = m_Model.CreateNewChildBone(bone2, Vector3.zero);

            Assert.AreEqual("bone_11", bone11.name);

            m_Model.SetBoneName(bone11, "bone_9");
            var boneNew11 = m_Model.CreateNewChildBone(bone2, Vector3.zero);

            Assert.AreEqual("bone_11", boneNew11.name);
        }

        [Test]
        public void InheritePrefixFromParent_UseTheGlobalCounter()
        {
            var root = m_Model.CreateNewRoot(Vector3.zero);
            var bone1 = m_Model.CreateNewChildBone(root, Vector3.one);

            m_Model.SetBoneName(bone1, "child_1");
            var child2 = m_Model.CreateNewChildBone(bone1, Vector3.right);

            Assert.AreEqual("child_2", child2.name);

            m_Model.SetBoneName(bone1, "branch_1");
            var branch3 = m_Model.CreateNewChildBone(bone1, Vector3.left);

            Assert.AreEqual("branch_3", branch3.name);

            // root always spawn "bone_"
            var bone4 = m_Model.CreateNewChildBone(root, Vector3.down);

            Assert.AreEqual("bone_4", bone4.name);
        }
    }
}