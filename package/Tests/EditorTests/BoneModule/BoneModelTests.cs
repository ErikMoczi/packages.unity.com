using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Experimental.U2D;

using NUnit.Framework;

namespace UnityEditor.Experimental.U2D.Animation.Test.Bone
{
    internal class ModelTestBase
    {
        protected readonly Vector3 k_Offset = Vector2.one * 5;

        protected IBoneModel m_Model;

        [SetUp]
        public void Setup()
        {
            m_Model = new BoneModel(() => { });
        }

        //
        //  R
        //  |  (root tip pointing to child)
        //  C
        //
        protected List<UniqueSpriteBone> GenerateNormalTwoBoneRawData()
        {
            var rawData = new List<UniqueSpriteBone>();

            var root = new UniqueSpriteBone();
            root.name = "root";
            root.position = Vector2.one;
            root.rotation = Quaternion.Euler(0.0f, 0.0f, 270.0f);
            root.length = 0.5f;
            root.parentId = -1;

            var child = new UniqueSpriteBone();
            child.name = "child";
            child.position = Vector2.right;
            child.rotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
            child.length = 1.5f;
            child.parentId = 0;

            rawData.Add(root);
            rawData.Add(child);

            return rawData;
        }

        //
        //  R--C1--C2--
        //      |(C1 tip pointed to C2, C2 & C3 are children of C1)
        //     C3--C4--
        //
        protected List<UniqueSpriteBone> GenerateComplexBoneRawData()
        {
            var rawData = new List<UniqueSpriteBone>();

            var root = new UniqueSpriteBone();
            root.name = "root";
            root.position = Vector2.up;
            root.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            root.length = 1f;
            root.parentId = -1;

            var child1 = new UniqueSpriteBone();
            child1.name = "child1";
            child1.position = Vector2.one;
            child1.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            child1.length = 1.0f;
            child1.parentId = 0;

            var child2 = new UniqueSpriteBone();
            child2.name = "child2";
            child2.position = Vector2.one + Vector2.right;
            child2.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            child2.length = 1.0f;
            child2.parentId = 1;

            var child3 = new UniqueSpriteBone();
            child3.name = "child3";
            child3.position = Vector2.right;
            child3.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            child3.length = 1.0f;
            child3.parentId = 1;

            var child4 = new UniqueSpriteBone();
            child4.name = "child4";
            child4.position = Vector2.right * 2;
            child4.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            child4.length = 1.0f;
            child4.parentId = 3;

            rawData.Add(root);
            rawData.Add(child1);
            rawData.Add(child2);
            rawData.Add(child3);
            rawData.Add(child4);

            return rawData;
        }

        protected void VerifyApproximatedSpriteBones(List<UniqueSpriteBone> expected, List<UniqueSpriteBone> actual)
        {
            const double kLooseEqual = 0.01;
            Assert.AreEqual(expected.Count, actual.Count);
            for (var i = 0; i < expected.Count; ++i)
            {
                var expectedBone = expected[i];
                var actualBone = actual[i].spriteBone;

                Assert.AreEqual(expectedBone.name, actualBone.name, "Name not matched at #{0}", i);
                Assert.AreEqual(expectedBone.parentId, actualBone.parentId, "ParentId not matched at #{0}", i);
                Assert.AreEqual(expectedBone.length, actualBone.length, kLooseEqual, "Length not matched at #{0}", i);
                Assert.AreEqual(expectedBone.position.x, actualBone.position.x, kLooseEqual, "Position X not matched at #{0}", i);
                Assert.AreEqual(expectedBone.position.y, actualBone.position.y, kLooseEqual, "Position Y not matched at #{0}", i);
                Assert.AreEqual(expectedBone.position.z, actualBone.position.z, kLooseEqual, "Position Z not matched at #{0}", i);

                var expectedEuler = expectedBone.rotation.eulerAngles;
                var actualEuler = actualBone.rotation.eulerAngles;
                Assert.AreEqual(expectedEuler.x, actualEuler.x, kLooseEqual, "Rotation X not matched at #{0}", i);
                Assert.AreEqual(expectedEuler.y, actualEuler.y, kLooseEqual, "Rotation Y not matched at #{0}", i);
                Assert.AreEqual(expectedEuler.z, actualEuler.z, kLooseEqual, "Rotation Z not matched at #{0}", i);
            }
        }
    }

    [TestFixture]
    internal class ModelRawDataTests : ModelTestBase
    {
        [Test]
        public void GivenCorrectRawData_NoUnintentedDataAlteration()
        {
            var expectedRawData = GenerateNormalTwoBoneRawData();

            // Pass in offset to simulate a real situation that sprite rect unlikely start at 0,0
            // This offset should not affect the output of the rawdata in any situation.
            m_Model.SetRawData(expectedRawData, k_Offset);

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }

        // TODO : To be implement
        // public void GivenRawDataWithNoRoot_ThrowException()
        // public void GivenRawDataWith2Roots_ThrowException()
        // public void GivenRawDataWithBrokenHierarchy_ThrowException()
        // public void GivenCorrectRawData_EnumerationCorrectly()
    }

    [TestFixture]
    internal class ModelCreationTests : ModelTestBase
    {
        [Test]
        public void CreateNewRootInBlank_GetARootWithDefaultValue()
        {
            m_Model.CreateNewRoot(Vector2.left);
            
            var expectedRawData = new List<UniqueSpriteBone>();
            var root = new UniqueSpriteBone();
            root.name = "root";
            root.position = Vector2.left;
            root.rotation = Quaternion.identity;
            root.length = 0.0f;
            root.parentId = -1;
            expectedRawData.Add(root);

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }

        [Test]
        public void CreateNewRootInBlank_DefineANewTip_GetARotatedAndLengthenRoot()
        {
            var root = m_Model.CreateNewRoot(Vector2.zero);
            m_Model.MoveTip(root, Vector2.up);
            
            var expectedRawData = new List<UniqueSpriteBone>();
            var sbRoot = new UniqueSpriteBone();
            sbRoot.name = "root";
            sbRoot.position = Vector2.zero;
            sbRoot.rotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
            sbRoot.length = 1.0f;
            sbRoot.parentId = -1;
            expectedRawData.Add(sbRoot);

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }

        [Test]
        public void CreateNewRootInBlankTwice_ThrowException_GetOnlyOneRoot()
        {
            m_Model.CreateNewRoot(Vector2.zero);

            Assert.Throws<InvalidOperationException>(
                () => { m_Model.CreateNewRoot(Vector2.one); },
                "Creating a new root when there are bones in this sprite");

            var expectedRawData = new List<UniqueSpriteBone>();
            var sbRoot = new UniqueSpriteBone();
            sbRoot.name = "root";
            sbRoot.position = Vector2.zero;
            sbRoot.rotation = Quaternion.identity;
            sbRoot.length = 0.0f;
            sbRoot.parentId = -1;
            expectedRawData.Add(sbRoot);

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }

        [Test]
        public void CreateNewRootInExistingHierarchy_ThrowException_GetBackTheSameHierarchy()
        {
            var expectedRawData = GenerateNormalTwoBoneRawData();
            m_Model.SetRawData(expectedRawData, k_Offset);

            Assert.Throws<InvalidOperationException>(
                () => { m_Model.CreateNewRoot(Vector2.one); },
                "Creating a new root when there are bones in this sprite");

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }

        [Test]
        public void CreateNewRoot_CreateAChild_GetCorrectData()
        {
            var root = m_Model.CreateNewRoot(Vector2.zero);
            var child = m_Model.CreateNewChildBone(root, Vector2.one);

            var expectedRawData = new List<UniqueSpriteBone>();

            var sbRoot = new UniqueSpriteBone();
            sbRoot.name = "root";
            sbRoot.position = Vector2.zero;
            sbRoot.rotation = Quaternion.identity;
            sbRoot.length = 0.0f;
            sbRoot.parentId = -1;

            var sbChild = new UniqueSpriteBone();
            sbChild.name = child.name;
            sbChild.position = Vector2.one;
            sbChild.rotation = Quaternion.identity;
            sbChild.length = 0.0f;
            sbChild.parentId = 0;

            expectedRawData.Add(sbRoot);
            expectedRawData.Add(sbChild);

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }

        [Test]
        public void CreateNewChildWithNullParentInBlank_ThrowException_NoNewData()
        {
            Assert.Throws<InvalidOperationException>(
                () => { m_Model.CreateNewChildBone(null, Vector2.one); },
                "Creating a bone with an invalid parent");

            var actualRawData = m_Model.GetRawData();
            Assert.AreEqual(actualRawData.Count, 0);
        }

        [Test]
        public void RenameExisitngBone_RawDataNameChanged()
        {
            var originalRawData = GenerateNormalTwoBoneRawData();
            m_Model.SetRawData(originalRawData, Vector2.zero);

            var root = m_Model.bones.ElementAt(0);
            var child = m_Model.bones.ElementAt(1);
            m_Model.SetBoneName(root, "dad");
            m_Model.SetBoneName(child, "son");

            var expectedRawData = new List<UniqueSpriteBone>(originalRawData);
            var expectedRoot = expectedRawData[0];
            var expectedChild = expectedRawData[1];
            expectedRoot.name = "dad";
            expectedChild.name = "son";
            expectedRawData[0] = expectedRoot;
            expectedRawData[1] = expectedChild;

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }

        // TODO : Direct children has the same name should throw warning / error.

        [Test]
        public void DeleteAChildBone_DataLeftOutTheDeleted()
        {
            var originalRawData = GenerateNormalTwoBoneRawData();
            m_Model.SetRawData(originalRawData, Vector2.zero);

            var child = m_Model.bones.ElementAt(1);
            m_Model.DeleteBone(child);

            var expectedRawData = new List<UniqueSpriteBone>();
            expectedRawData.Add(originalRawData[0]);

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }

        [Test]
        public void DeleteAParentWithChild_ThrowException_DataIntact()
        {
            var expectedRawData = GenerateNormalTwoBoneRawData();
            m_Model.SetRawData(expectedRawData, Vector2.zero);

            var root = m_Model.bones.ElementAt(0);
            Assert.Throws<InvalidOperationException>(
                () => { m_Model.DeleteBone(root); },
                "Cannot delete a parent bone with children. Children should all be removed/transfered first.");
            
            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }
    }

    [TestFixture]
    internal class ModelMoveRotateTests : ModelTestBase
    {
        [Test]
        public void MoveChildBoneNode_RootDataUnchanged_ChildDataPositionChanged()
        {
            var originalRawData = GenerateNormalTwoBoneRawData();
            m_Model.SetRawData(originalRawData, Vector2.zero);

            var child = m_Model.bones.ElementAt(1);
            m_Model.MoveBone(child, Vector2.down);

            var expectedRawData = new List<UniqueSpriteBone>(originalRawData);
            var expectedChild = expectedRawData[1];
            expectedChild.position = new Vector2(2.0f, -1.0f);
            expectedRawData[1] = expectedChild;

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }

        [Test]
        public void MoveChildBoneTip_RootDataUnchanged_ChildDataRotatedAndLengthen()
        {
            var originalRawData = GenerateNormalTwoBoneRawData();
            m_Model.SetRawData(originalRawData, Vector2.zero);

            var child = m_Model.bones.ElementAt(1);
            m_Model.MoveTip(child, Vector2.up);

            var expectedRawData = new List<UniqueSpriteBone>(originalRawData);
            var expectedChild = expectedRawData[1];
            expectedChild.rotation = Quaternion.Euler(0.0f, 0.0f, 225.0f);
            expectedChild.length = 1.4142f;
            expectedRawData[1] = expectedChild;

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }
        
        [Test]
        public void MoveRootBoneNode_BothRootAndChildPositionChanged()
        {
            var originalRawData = GenerateNormalTwoBoneRawData();
            m_Model.SetRawData(originalRawData, Vector2.zero);

            Vector3 newPosition = Vector2.right;
            var root = m_Model.bones.ElementAt(0);
            m_Model.MoveBone(root, newPosition, false);

            var expectedRawData = new List<UniqueSpriteBone>(originalRawData);
            var expectedRoot = expectedRawData[0];
            expectedRoot.position = newPosition;
            expectedRawData[0] = expectedRoot;
            
            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }
        
        [Test]
        public void MoveRootBoneTip_RootRotatedAndChildPositionAndRotationChanged()
        {
            var originalRawData = GenerateNormalTwoBoneRawData();
            m_Model.SetRawData(originalRawData, Vector2.zero);

            Vector3 newPosition = Vector2.one + Vector2.right;
            var root = m_Model.bones.ElementAt(0);
            m_Model.MoveTip(root, newPosition, false);

            var expectedRawData = new List<UniqueSpriteBone>(originalRawData);
            var expectedRoot = expectedRawData[0];
            var expectedChild = expectedRawData[1];

            expectedRoot.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            expectedRoot.length = 1.0f;
            expectedChild.position = Vector2.right;

            expectedRawData[0] = expectedRoot;
            expectedRawData[1] = expectedChild;

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }

        [Test]
        public void MoveBoneTipToOppositeDirection_RotateAlongZAxisCorrectly()
        {
            var root = m_Model.CreateNewRoot(Vector2.zero);

            // This will trigger a 180 degree rotation, as bone is pointed at Vector.right.
            // this rotation must happen on Z axis.
            m_Model.MoveTip(root, Vector2.left);
            
            var expectedRawData = new List<UniqueSpriteBone>();
            var sbRoot = new UniqueSpriteBone();
            sbRoot.name = "root";
            sbRoot.position = Vector2.zero;
            sbRoot.rotation = Quaternion.Euler(0.0f, 0.0f, 180.0f);
            sbRoot.length = 1.0f;
            sbRoot.parentId = -1;
            expectedRawData.Add(sbRoot);

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }


        [Test]
        public void MoveRootBoneNode_KeepOffspringWorldPosition_RootAndFirstChildPositionChanged()
        {
            var originalRawData = GenerateComplexBoneRawData();
            m_Model.SetRawData(originalRawData, Vector2.zero);

            var root = m_Model.bones.ElementAt(0);
            m_Model.MoveBone(root, Vector2.zero);

            var expectedRawData = new List<UniqueSpriteBone>(originalRawData);
            var expectedRoot = expectedRawData[0];
            expectedRoot.position = Vector2.zero;
            expectedRawData[0] = expectedRoot;

            var expectedChild1 = expectedRawData[1];
            expectedChild1.position = new Vector2(1.0f, 2.0f);
            expectedRawData[1] = expectedChild1;

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }

        [Test]
        public void MoveRootBoneTip_KeepOffspringWorldPosition_RootRotationChanged_FirstChildPositionChanged()
        {
            var originalRawData = GenerateComplexBoneRawData();
            m_Model.SetRawData(originalRawData, Vector2.zero);

            var root = m_Model.bones.ElementAt(0);
            m_Model.MoveTip(root, Vector2.zero);

            var expectedRawData = new List<UniqueSpriteBone>(originalRawData);
            var expectedRoot = expectedRawData[0];
            expectedRoot.rotation = Quaternion.Euler(0.0f, 0.0f, 270.0f);
            expectedRawData[0] = expectedRoot;

            var expectedChild1 = expectedRawData[1];
            expectedChild1.position = new Vector2(-1.0f, 1.0f);
            expectedChild1.rotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
            expectedRawData[1] = expectedChild1;

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }
    }

    [TestFixture]
    internal class ModelHierarchyOrderTests : ModelTestBase
    {
        [Test]
        public void ChildrenOfSameLevelShouldSortWithNames()
        {
            var root = m_Model.CreateNewRoot(Vector2.zero);
            var child2 = m_Model.CreateNewChildBone(root, Vector2.one);
            var child3 = m_Model.CreateNewChildBone(root, Vector2.one);
            var child1 = m_Model.CreateNewChildBone(root, Vector2.one);

            m_Model.SetBoneName(child2, "child2");
            m_Model.SetBoneName(child3, "child3");
            m_Model.SetBoneName(child1, "child1");

            var rawData = m_Model.GetRawData();

            Assert.AreEqual(root.name, rawData[0].name);
            Assert.AreEqual(child1.name, rawData[1].name);
            Assert.AreEqual(child2.name, rawData[2].name);
            Assert.AreEqual(child3.name, rawData[3].name);
        }

        [Test]
        public void CreatingHierarchyInMixedOrder_RawDataWillBeSorted()
        {
            var root = m_Model.CreateNewRoot(Vector2.zero);
            var uncle = m_Model.CreateNewChildBone(root, Vector2.one);
            var nephew1 = m_Model.CreateNewChildBone(uncle, Vector2.one);
            var auntie = m_Model.CreateNewChildBone(root, Vector2.one);
            var niece2 = m_Model.CreateNewChildBone(auntie, Vector2.one);
            var niece3 = m_Model.CreateNewChildBone(auntie, Vector2.one);
            var nephew2 = m_Model.CreateNewChildBone(uncle, Vector2.one);
            var niece1 = m_Model.CreateNewChildBone(auntie, Vector2.one);
            
            m_Model.SetBoneName(uncle, "uncle");
            m_Model.SetBoneName(nephew1, "nephew1");
            m_Model.SetBoneName(auntie, "auntie");
            m_Model.SetBoneName(niece2, "niece2");
            m_Model.SetBoneName(niece3, "niece3");
            m_Model.SetBoneName(nephew2, "nephew2");
            m_Model.SetBoneName(niece1, "niece1");

            var rawData = m_Model.GetRawData();

            Assert.AreEqual(root.name, rawData[0].name);
            Assert.AreEqual(auntie.name, rawData[1].name);
            Assert.AreEqual(uncle.name, rawData[2].name);
            Assert.AreEqual(niece1.name, rawData[3].name);
            Assert.AreEqual(niece2.name, rawData[4].name);
            Assert.AreEqual(niece3.name, rawData[5].name);
            Assert.AreEqual(nephew1.name, rawData[6].name);
            Assert.AreEqual(nephew2.name, rawData[7].name);
        }

        [Test]
        public void ParentToASameLevelBone_ParentIdChange_OrderIntact_ChildWithNewParentChangedPosition()
        {
            var originalRawData = GenerateComplexBoneRawData();
            m_Model.SetRawData(originalRawData, Vector2.zero);

            var child2 = m_Model.bones.ElementAt(2);
            var child4 = m_Model.bones.ElementAt(4);

            m_Model.Parent(child4, child2);
            
            var expectedRawData = new List<UniqueSpriteBone>(originalRawData);
            var expectedChild4 = expectedRawData[4];
            expectedChild4.parentId = 2;
            expectedChild4.position = new Vector2(1.0f, -1.0f);
            expectedRawData[4] = expectedChild4;

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }

        [Test]
        public void ParentToAHigherLevelBone_ParentIdChange_OrderResorted()
        {
            var originalRawData = GenerateComplexBoneRawData();
            m_Model.SetRawData(originalRawData, Vector2.zero);

            var root = m_Model.bones.ElementAt(0);
            var child1 = m_Model.bones.ElementAt(1);
            var child2 = m_Model.bones.ElementAt(2);
            var child3 = m_Model.bones.ElementAt(3);
            var child4 = m_Model.bones.ElementAt(4);

            m_Model.Parent(child4, root);

            var rawData = m_Model.GetRawData();
            
            Assert.AreEqual(root.name, rawData[0].name);
            Assert.AreEqual(child1.name, rawData[1].name);
            Assert.AreEqual(child4.name, rawData[2].name);
            Assert.AreEqual(child2.name, rawData[3].name);
            Assert.AreEqual(child3.name, rawData[4].name);

            Assert.AreEqual(0, rawData[2].parentId);
        }

        [Test]
        public void ParentABoneWithOffspring_KeepAllOffSpring()
        {
            var originalRawData = GenerateComplexBoneRawData();
            m_Model.SetRawData(originalRawData, Vector2.zero);

            var root = m_Model.bones.ElementAt(0);
            var child1 = m_Model.bones.ElementAt(1);
            var child2 = m_Model.bones.ElementAt(2);
            var child3 = m_Model.bones.ElementAt(3);
            var child4 = m_Model.bones.ElementAt(4);

            m_Model.Parent(child3, child2);

            var rawData = m_Model.GetRawData();

            Assert.AreEqual(root.name, rawData[0].name);
            Assert.AreEqual(child1.name, rawData[1].name);
            Assert.AreEqual(child2.name, rawData[2].name);
            Assert.AreEqual(child3.name, rawData[3].name);
            Assert.AreEqual(child4.name, rawData[4].name);

            Assert.AreEqual(2, rawData[3].parentId);
            Assert.AreEqual(3, rawData[4].parentId);
        }

        [Test]
        public void ParentANodeToItself_ThrowException_DataIntact()
        {
            var expectedRawData = GenerateComplexBoneRawData();
            m_Model.SetRawData(expectedRawData, Vector2.zero);

            var child3 = m_Model.bones.ElementAt(3);
            Assert.Throws<InvalidOperationException>(
                () => { m_Model.Parent(child3, child3); },
                "Cannot parent a bone to itself.");

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }

        [Test]
        public void ParentANodeToOffspring_ThrowException_DataIntact()
        {
            var expectedRawData = GenerateComplexBoneRawData();
            m_Model.SetRawData(expectedRawData, Vector2.zero);

            var child1 = m_Model.bones.ElementAt(1);
            var child3 = m_Model.bones.ElementAt(3);
            Assert.Throws<InvalidOperationException>(
                () => { m_Model.Parent(child1, child3); },
                "Cannot parent {0} to {1}. This will create a loop.", child1.name, child3.name);

            var actualRawData = m_Model.GetRawData();

            VerifyApproximatedSpriteBones(expectedRawData, actualRawData);
        }
    }
}