using System.Collections.Generic;

using UnityEditor;
using UnityEngine.Experimental.U2D;

using NUnit.Framework;
using NSubstitute;
using UnityEngine;
using System.Linq;

namespace UnityEditor.Experimental.U2D.Animation.Test.Bone
{
    [TestFixture]
    internal class BoneCacheTests
    {
        protected GUID m_ID1;
        protected GUID m_ID2;

        protected BoneCacheManager m_CacheManager;
        protected ISpriteBoneDataProvider m_BoneDPMock;
        protected ISpriteMeshDataProvider m_MeshDPMock;

        [SetUp]
        public void Setup()
        {
            m_ID1 = GUID.Generate();
            m_ID2 = GUID.Generate();

            m_BoneDPMock = Substitute.For<ISpriteBoneDataProvider>();
            m_MeshDPMock = Substitute.For<ISpriteMeshDataProvider>();

            m_BoneDPMock.GetBones(m_ID1).Returns(new List<SpriteBone>());
            m_BoneDPMock.GetBones(m_ID2).Returns(new List<SpriteBone>());

            m_CacheManager = new BoneCacheManager(m_BoneDPMock, m_MeshDPMock);
        }

        [Test]
        public void GetData_LoadFromDataProvider()
        {
            m_CacheManager.GetSpriteBoneRawData(m_ID1);

            m_BoneDPMock.ReceivedWithAnyArgs(1).GetBones(Arg.Any<GUID>());
            m_BoneDPMock.Received(1).GetBones(m_ID1);
        }

        [Test]
        public void GetDataTwice_DoNotLoadFromDataProviderTwice()
        {
            m_CacheManager.GetSpriteBoneRawData(m_ID1);
            m_CacheManager.GetSpriteBoneRawData(m_ID1);

            m_BoneDPMock.ReceivedWithAnyArgs(1).GetBones(Arg.Any<GUID>());
            m_BoneDPMock.Received(1).GetBones(m_ID1);
        }

        [Test]
        public void SetData_DoNotTriggerDataProvider()
        {
            m_CacheManager.SetSpriteBoneRawData(m_ID1, new List<UniqueSpriteBone>());

            m_BoneDPMock.DidNotReceiveWithAnyArgs().GetBones(Arg.Any<GUID>());
            m_BoneDPMock.DidNotReceiveWithAnyArgs().SetBones(Arg.Any<GUID>(), Arg.Any<List<SpriteBone>>());
        }

        [Test]
        public void NeverGetAnyData_ApplyDoNotTriggerDataProvider()
        {
            m_CacheManager.Apply();

            m_BoneDPMock.DidNotReceiveWithAnyArgs().SetBones(Arg.Any<GUID>(), Arg.Any<List<SpriteBone>>());
        }

        [Test]
        public void GetDataOnce_ApplyWillOnlySetTheBoneForCachedData()
        {
            m_CacheManager.GetSpriteBoneRawData(m_ID1);
            m_CacheManager.Apply();

            m_BoneDPMock.ReceivedWithAnyArgs(1).SetBones(Arg.Any<GUID>(), Arg.Any<List<SpriteBone>>());
            m_BoneDPMock.Received(1).SetBones(m_ID1, Arg.Any<List<SpriteBone>>());
        }

        [Test]
        public void GetDifferentData_ApplyWillSetTheBoneForAllCachedData()
        {
            m_CacheManager.GetSpriteBoneRawData(m_ID1);
            m_CacheManager.GetSpriteBoneRawData(m_ID2);
            m_CacheManager.Apply();

            m_BoneDPMock.ReceivedWithAnyArgs(2).SetBones(Arg.Any<GUID>(), Arg.Any<List<SpriteBone>>());
            m_BoneDPMock.Received(1).SetBones(m_ID1, Arg.Any<List<SpriteBone>>());
            m_BoneDPMock.Received(1).SetBones(m_ID2, Arg.Any<List<SpriteBone>>());
        }
        
        [Test]
        public void SetNewData_ApplyWillSetTheNewBone()
        {
            var data = m_CacheManager.GetSpriteBoneRawData(m_ID1);
            var newBone = new UniqueSpriteBone();
            newBone.name = "root";
            data.Add(newBone);

            m_CacheManager.SetSpriteBoneRawData(m_ID1, data);
            m_CacheManager.Apply();

            m_BoneDPMock.ReceivedWithAnyArgs(1).SetBones(Arg.Any<GUID>(), Arg.Any<List<SpriteBone>>());
            m_BoneDPMock.Received(1).SetBones(m_ID1, Arg.Is<List<SpriteBone>>(x => x[0].name == "root"));
        }
    }

    [TestFixture]
    internal class FixingWeightTests
    {
        protected BoneCacheManager m_CacheManager;
        protected ISpriteBoneDataProvider m_BoneDPMock;
        protected ISpriteMeshDataProvider m_MeshDPMock;

        protected GUID m_SpriteId;
        protected IBoneModel m_Model;
        protected Vertex2DMetaData[] m_OriginalVertices;
        protected Vertex2DMetaData[] m_ExpectedVertices;


        [SetUp]
        public void Setup()
        {
            m_SpriteId = GUID.Generate();

            m_BoneDPMock = Substitute.For<ISpriteBoneDataProvider>();
            m_MeshDPMock = Substitute.For<ISpriteMeshDataProvider>();

            m_CacheManager = new BoneCacheManager(m_BoneDPMock, m_MeshDPMock);

            m_OriginalVertices = new Vertex2DMetaData[10]
            {
                //0
                new Vertex2DMetaData() { boneWeight = new BoneWeight() 
                    { boneIndex0 = 0, weight0 = 1.0f } },
                //1
                new Vertex2DMetaData() { boneWeight = new BoneWeight() 
                    { boneIndex0 = 0, weight0 = 0.5f,
                      boneIndex1 = 1, weight1 = 0.5f  } },
                //2
                new Vertex2DMetaData() { boneWeight = new BoneWeight() 
                    { boneIndex0 = 0, weight0 = 0.25f, 
                      boneIndex1 = 2, weight1 = 0.25f,
                      boneIndex2 = 3, weight2 = 0.25f,
                      boneIndex3 = 4, weight3 = 0.25f  } },
                //3
                new Vertex2DMetaData() { boneWeight = new BoneWeight() 
                    { boneIndex0 = 2, weight0 = 0.3f, 
                      boneIndex1 = 3, weight1 = 0.3f,
                      boneIndex2 = 4, weight2 = 0.3f } },
                //4
                new Vertex2DMetaData() { boneWeight = new BoneWeight() 
                    { boneIndex0 = 1, weight0 = 0.5f,
                      boneIndex1 = 3, weight1 = 0.5f  } },
                //5
                new Vertex2DMetaData() { boneWeight = new BoneWeight() 
                    { boneIndex0 = 4, weight0 = 1.0f } },
                //6
                new Vertex2DMetaData() { boneWeight = new BoneWeight() 
                    { boneIndex0 = 3, weight0 = 0.5f, 
                      boneIndex1 = 4, weight1 = 0.5f } },
                //7
                new Vertex2DMetaData() { boneWeight = new BoneWeight() 
                    { boneIndex0 = 0, weight0 = 0.3f,
                      boneIndex1 = 1, weight1 = 0.3f,
                      boneIndex2 = 5, weight2 = 0.3f  } },
                //8
                new Vertex2DMetaData() { boneWeight = new BoneWeight() 
                    { boneIndex0 = 3, weight0 = 0.5f, 
                      boneIndex1 = 5, weight1 = 0.5f } },
                //9
                new Vertex2DMetaData() { boneWeight = new BoneWeight() 
                    { boneIndex0 = 0, weight0 = 0.25f, 
                      boneIndex1 = 2, weight1 = 0.25f,
                      boneIndex2 = 4, weight2 = 0.25f,
                      boneIndex3 = 5, weight3 = 0.25f } }
            };

            m_ExpectedVertices = new Vertex2DMetaData[m_OriginalVertices.Length];
            m_OriginalVertices.CopyTo(m_ExpectedVertices, 0);

            var spriteBones = new List<SpriteBone>();
            spriteBones.Add(new SpriteBone() { name = "root", parentId = -1, rotation = Quaternion.identity });
            spriteBones.Add(new SpriteBone() { name = "child_1", parentId = 0, rotation = Quaternion.identity });
            spriteBones.Add(new SpriteBone() { name = "child_1_1", parentId = 1, rotation = Quaternion.identity });
            spriteBones.Add(new SpriteBone() { name = "child_1_2", parentId = 1, rotation = Quaternion.identity });
            spriteBones.Add(new SpriteBone() { name = "child_1_2_1", parentId = 3, rotation = Quaternion.identity });
            spriteBones.Add(new SpriteBone() { name = "child_1_2_2", parentId = 3, rotation = Quaternion.identity });
            
            m_BoneDPMock.GetBones(m_SpriteId).Returns(spriteBones);
            m_MeshDPMock.GetVertices(m_SpriteId).Returns(m_OriginalVertices);

            var uniqueBone = m_CacheManager.GetSpriteBoneRawData(m_SpriteId);
            m_Model = new BoneModel(() => { });
            m_Model.SetRawData(uniqueBone, Vector3.zero);
        }

        [Test]
        public void RemovingABone_InvalidateWeightForThatBone_WeightBoneIndexUpdate()
        {
            InvalidateBoneIndex(1, m_ExpectedVertices);
            ChangeBoneIndex(new int[4] {2, 3, 4, 5}, new int[4] {1, 2, 3, 4}, m_ExpectedVertices);

            var root = m_Model.bones.ElementAt(0);
            var child_1 = m_Model.bones.ElementAt(1);
            var child_1_1 = m_Model.bones.ElementAt(2);
            var child_1_2 = m_Model.bones.ElementAt(3);

            m_Model.Parent(child_1_1, root);
            m_Model.Parent(child_1_2, root);
            m_Model.DeleteBone(child_1);

            m_CacheManager.SetSpriteBoneRawData(m_SpriteId, m_Model.GetRawData());
            m_CacheManager.Apply();

            m_MeshDPMock.Received(1).SetVertices(m_SpriteId, Arg.Is<Vertex2DMetaData[]>(x => CompareVertices(m_ExpectedVertices, x)));
        }

        [Test]
        public void SplitABone_WeightBoneIndexUpdate_NoWeightForNewBone() 
        {
            ChangeBoneIndex(new int[5] {1, 2, 3, 4, 5}, new int[5] {2, 3, 4, 5, 6}, m_ExpectedVertices);

            var root = m_Model.bones.ElementAt(0);
            var child_1 = m_Model.bones.ElementAt(1);

            var newChild = m_Model.CreateNewChildBone(root, Vector2.one);
            m_Model.Parent(child_1, newChild);

            m_CacheManager.SetSpriteBoneRawData(m_SpriteId, m_Model.GetRawData());
            m_CacheManager.Apply();

            m_MeshDPMock.Received(1).SetVertices(m_SpriteId, Arg.Is<Vertex2DMetaData[]>(x => CompareVertices(m_ExpectedVertices, x)));
        }

        [Test]
        public void AddNewBoneToTail_NoWeightForNewBone() 
        { 
            var child_1_2_2 = m_Model.bones.ElementAt(5);

            m_Model.CreateNewChildBone(child_1_2_2, Vector2.one);

            m_CacheManager.SetSpriteBoneRawData(m_SpriteId, m_Model.GetRawData());
            m_CacheManager.Apply();

            m_MeshDPMock.DidNotReceiveWithAnyArgs().SetVertices(Arg.Any<GUID>(), Arg.Any<Vertex2DMetaData[]>());
        }

        [Test]
        public void AddNewChildAtMiddleOfHierarchy_WeightBoneIndexUpdate_NoWeightForNewBone() 
        {
            ChangeBoneIndex(new int[2] { 4, 5 }, new int[2] { 5, 6 }, m_ExpectedVertices);

            var child_1 = m_Model.bones.ElementAt(1);

            m_Model.CreateNewChildBone(child_1, Vector2.one);

            m_CacheManager.SetSpriteBoneRawData(m_SpriteId, m_Model.GetRawData());
            m_CacheManager.Apply();

            m_MeshDPMock.Received(1).SetVertices(m_SpriteId, Arg.Is<Vertex2DMetaData[]>(x => CompareVertices(m_ExpectedVertices, x)));
        }

        [Test]
        public void Reparent_WeightBoneIndexUpdate() 
        { 
            ChangeBoneIndex(new int[2] {3, 2}, new int[2] {2, 3}, m_ExpectedVertices);
            
            var root = m_Model.bones.ElementAt(0);
            var child_1_2 = m_Model.bones.ElementAt(3);

            m_Model.Parent(child_1_2, root);

            m_CacheManager.SetSpriteBoneRawData(m_SpriteId, m_Model.GetRawData());
            m_CacheManager.Apply();

            m_MeshDPMock.Received(1).SetVertices(m_SpriteId, Arg.Is<Vertex2DMetaData[]>(x => CompareVertices(m_ExpectedVertices, x)));
        }

        [Test]
        public void DeleteEverythingAndReconstruct_AllWeightInvalidated() 
        { 
            InvalidateBoneIndex(0, m_ExpectedVertices);
            InvalidateBoneIndex(1, m_ExpectedVertices);
            InvalidateBoneIndex(2, m_ExpectedVertices);
            InvalidateBoneIndex(3, m_ExpectedVertices);
            InvalidateBoneIndex(4, m_ExpectedVertices);
            InvalidateBoneIndex(5, m_ExpectedVertices);

            var root = m_Model.bones.ElementAt(0);
            var child_1 = m_Model.bones.ElementAt(1);
            var child_1_1 = m_Model.bones.ElementAt(2);
            var child_1_2 = m_Model.bones.ElementAt(3);
            var child_1_2_1 = m_Model.bones.ElementAt(4);
            var child_1_2_2 = m_Model.bones.ElementAt(5);

            m_Model.DeleteBone(child_1_2_2);
            m_Model.DeleteBone(child_1_2_1);
            m_Model.DeleteBone(child_1_2);
            m_Model.DeleteBone(child_1_1);
            m_Model.DeleteBone(child_1);
            m_Model.DeleteBone(root);

            var newRoot = m_Model.CreateNewRoot(Vector2.zero);
            m_Model.CreateNewChildBone(newRoot, Vector2.zero);

            m_CacheManager.SetSpriteBoneRawData(m_SpriteId, m_Model.GetRawData());
            m_CacheManager.Apply();

            m_MeshDPMock.Received(1).SetVertices(m_SpriteId, Arg.Is<Vertex2DMetaData[]>(x => CompareVertices(m_ExpectedVertices, x)));
        }

        [Test]
        public void JustMovingBones_WeightRemained_NeverCallSetMesh() 
        { 
            var root = m_Model.bones.ElementAt(0);
            var child_1 = m_Model.bones.ElementAt(1);
            var child_1_2 = m_Model.bones.ElementAt(3);
            var child_1_2_2 = m_Model.bones.ElementAt(5);

            m_Model.MoveBone(root, Vector2.one);
            m_Model.MoveBone(child_1, Vector2.one);
            m_Model.MoveTip(child_1_2, Vector2.one);
            m_Model.MoveTip(child_1_2_2, Vector2.one);

            m_CacheManager.SetSpriteBoneRawData(m_SpriteId, m_Model.GetRawData());
            m_CacheManager.Apply();

            m_MeshDPMock.DidNotReceiveWithAnyArgs().SetVertices(Arg.Any<GUID>(), Arg.Any<Vertex2DMetaData[]>());
        }

        [Test]
        public void DeleteATail_AddBackATail_WeightStillInvalidedForFirstTail()
        {
            InvalidateBoneIndex(5, m_ExpectedVertices);

            var child_1_2 = m_Model.bones.ElementAt(3);
            var child_1_2_2 = m_Model.bones.ElementAt(5);
            
            m_Model.DeleteBone(child_1_2_2);
            var newBone = m_Model.CreateNewChildBone(child_1_2, Vector2.zero);
            // Rename to preserve order
            m_Model.SetBoneName(newBone, "child_1_2_2");

            m_CacheManager.SetSpriteBoneRawData(m_SpriteId, m_Model.GetRawData());
            m_CacheManager.Apply();

            m_MeshDPMock.Received(1).SetVertices(m_SpriteId, Arg.Is<Vertex2DMetaData[]>(x => CompareVertices(m_ExpectedVertices, x)));
        }        

        [Test]
        public void NoBones_AllWeightInvalidated() 
        {
            for(int i = 0; i < m_ExpectedVertices.Length; ++i)
                m_ExpectedVertices[i].boneWeight = new BoneWeight();

            m_BoneDPMock.GetBones(m_SpriteId).Returns(new List<SpriteBone>());
            m_CacheManager = new BoneCacheManager(m_BoneDPMock, m_MeshDPMock);

            m_CacheManager.GetSpriteBoneRawData(m_SpriteId);

            m_Model = new BoneModel(() => { });
            m_CacheManager.Apply();

            m_MeshDPMock.Received(1).SetVertices(m_SpriteId, Arg.Is<Vertex2DMetaData[]>(x => CompareVertices(m_ExpectedVertices, x)));
        }

        private void InvalidateBoneIndex(int boneIndex, Vertex2DMetaData[] weights)
        {
            for (var i = 0; i < weights.Length; ++i)
            {
                var w = weights[i];
                if (w.boneWeight.boneIndex0 == boneIndex)
                {
                    w.boneWeight.boneIndex0 = 0;
                    w.boneWeight.weight0 = 0.0f;
                    weights[i] = w;
                }
                else if (w.boneWeight.boneIndex1 == boneIndex)
                {
                    w.boneWeight.boneIndex1 = 0;
                    w.boneWeight.weight1 = 0.0f;
                    weights[i] = w;
                }
                else if (w.boneWeight.boneIndex2 == boneIndex)
                {
                    w.boneWeight.boneIndex2 = 0;
                    w.boneWeight.weight2 = 0.0f;
                    weights[i] = w;
                }
                else if (w.boneWeight.boneIndex3 == boneIndex)
                {
                    w.boneWeight.boneIndex3 = 0;
                    w.boneWeight.weight3 = 0.0f;
                    weights[i] = w;
                }
            }
        }

        private void ChangeBoneIndex(int[] originalIndices, int[] newIndices, Vertex2DMetaData[] weights)
        {
            for (var i = 0; i < weights.Length; ++i)
            {
                var w = weights[i];
                for (var j = 0; j < originalIndices.Length; ++j)
                {
                    var originalIndex = originalIndices[j];
                    var newIndex = newIndices[j];
                    if (w.boneWeight.boneIndex0 == originalIndex)
                    {
                        w.boneWeight.boneIndex0 = newIndex;
                        weights[i] = w;
                        break;
                    }
                }
                for (var j = 0; j < originalIndices.Length; ++j)
                {
                    var originalIndex = originalIndices[j];
                    var newIndex = newIndices[j];
                    if (w.boneWeight.boneIndex1 == originalIndex)
                    {
                        w.boneWeight.boneIndex1 = newIndex;
                        weights[i] = w;
                        break;                        
                    }
                }
                for (var j = 0; j < originalIndices.Length; ++j)
                {
                    var originalIndex = originalIndices[j];
                    var newIndex = newIndices[j];
                    if (w.boneWeight.boneIndex2 == originalIndex)
                    {
                        w.boneWeight.boneIndex2 = newIndex;
                        weights[i] = w;
                        break;
                    }
                }
                for (var j = 0; j < originalIndices.Length; ++j)
                {
                    var originalIndex = originalIndices[j];
                    var newIndex = newIndices[j];
                    if (w.boneWeight.boneIndex3 == originalIndex)
                    {
                        w.boneWeight.boneIndex3 = newIndex;
                        weights[i] = w;
                        break;
                    }
                }
            }
        }

        private bool CompareVertices(Vertex2DMetaData[] expected, Vertex2DMetaData[] actual)
        {
            if (expected.Length != actual.Length)
                return false;

            for (var i = 0; i < expected.Length; ++i)
            {
                if (expected[i].boneWeight != actual[i].boneWeight)
                    return false;
            }

            return true;
        }

        [Test]
        public void HelperMethodVerification_Unchanged()
        {
            Assert.AreEqual(0, m_ExpectedVertices[0].boneWeight.boneIndex0);
            Assert.AreEqual(0, m_ExpectedVertices[0].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[0].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[0].boneWeight.boneIndex3);
            Assert.AreEqual(1.0f, m_ExpectedVertices[0].boneWeight.weight0);
            Assert.AreEqual(0.0f, m_ExpectedVertices[0].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[0].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[0].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[1].boneWeight.boneIndex0);
            Assert.AreEqual(1, m_ExpectedVertices[1].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[1].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[1].boneWeight.boneIndex3);
            Assert.AreEqual(0.5f, m_ExpectedVertices[1].boneWeight.weight0);
            Assert.AreEqual(0.5f, m_ExpectedVertices[1].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[1].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[1].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[2].boneWeight.boneIndex0);
            Assert.AreEqual(2, m_ExpectedVertices[2].boneWeight.boneIndex1);
            Assert.AreEqual(3, m_ExpectedVertices[2].boneWeight.boneIndex2);
            Assert.AreEqual(4, m_ExpectedVertices[2].boneWeight.boneIndex3);
            Assert.AreEqual(0.25f, m_ExpectedVertices[2].boneWeight.weight0);
            Assert.AreEqual(0.25f, m_ExpectedVertices[2].boneWeight.weight1);
            Assert.AreEqual(0.25f, m_ExpectedVertices[2].boneWeight.weight2);
            Assert.AreEqual(0.25f, m_ExpectedVertices[2].boneWeight.weight3);

            Assert.AreEqual(2, m_ExpectedVertices[3].boneWeight.boneIndex0);
            Assert.AreEqual(3, m_ExpectedVertices[3].boneWeight.boneIndex1);
            Assert.AreEqual(4, m_ExpectedVertices[3].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[3].boneWeight.boneIndex3);
            Assert.AreEqual(0.3f, m_ExpectedVertices[3].boneWeight.weight0);
            Assert.AreEqual(0.3f, m_ExpectedVertices[3].boneWeight.weight1);
            Assert.AreEqual(0.3f, m_ExpectedVertices[3].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[3].boneWeight.weight3);

            Assert.AreEqual(1, m_ExpectedVertices[4].boneWeight.boneIndex0);
            Assert.AreEqual(3, m_ExpectedVertices[4].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[4].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[4].boneWeight.boneIndex3);
            Assert.AreEqual(0.5f, m_ExpectedVertices[4].boneWeight.weight0);
            Assert.AreEqual(0.5f, m_ExpectedVertices[4].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[4].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[4].boneWeight.weight3);

            Assert.AreEqual(4, m_ExpectedVertices[5].boneWeight.boneIndex0);
            Assert.AreEqual(0, m_ExpectedVertices[5].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[5].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[5].boneWeight.boneIndex3);
            Assert.AreEqual(1.0f, m_ExpectedVertices[5].boneWeight.weight0);
            Assert.AreEqual(0.0f, m_ExpectedVertices[5].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[5].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[5].boneWeight.weight3);

            Assert.AreEqual(3, m_ExpectedVertices[6].boneWeight.boneIndex0);
            Assert.AreEqual(4, m_ExpectedVertices[6].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[6].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[6].boneWeight.boneIndex3);
            Assert.AreEqual(0.5f, m_ExpectedVertices[6].boneWeight.weight0);
            Assert.AreEqual(0.5f, m_ExpectedVertices[6].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[6].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[6].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[7].boneWeight.boneIndex0);
            Assert.AreEqual(1, m_ExpectedVertices[7].boneWeight.boneIndex1);
            Assert.AreEqual(5, m_ExpectedVertices[7].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[7].boneWeight.boneIndex3);
            Assert.AreEqual(0.3f, m_ExpectedVertices[7].boneWeight.weight0);
            Assert.AreEqual(0.3f, m_ExpectedVertices[7].boneWeight.weight1);
            Assert.AreEqual(0.3f, m_ExpectedVertices[7].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[7].boneWeight.weight3);

            Assert.AreEqual(3, m_ExpectedVertices[8].boneWeight.boneIndex0);
            Assert.AreEqual(5, m_ExpectedVertices[8].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[8].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[8].boneWeight.boneIndex3);
            Assert.AreEqual(0.5f, m_ExpectedVertices[8].boneWeight.weight0);
            Assert.AreEqual(0.5f, m_ExpectedVertices[8].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[8].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[8].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[9].boneWeight.boneIndex0);
            Assert.AreEqual(2, m_ExpectedVertices[9].boneWeight.boneIndex1);
            Assert.AreEqual(4, m_ExpectedVertices[9].boneWeight.boneIndex2);
            Assert.AreEqual(5, m_ExpectedVertices[9].boneWeight.boneIndex3);
            Assert.AreEqual(0.25f, m_ExpectedVertices[9].boneWeight.weight0);
            Assert.AreEqual(0.25f, m_ExpectedVertices[9].boneWeight.weight1);
            Assert.AreEqual(0.25f, m_ExpectedVertices[9].boneWeight.weight2);
            Assert.AreEqual(0.25f, m_ExpectedVertices[9].boneWeight.weight3);
        }

        [Test]
        public void HelperMethodVerification_Delete()
        {
            InvalidateBoneIndex(3, m_ExpectedVertices);

            Assert.AreEqual(0, m_ExpectedVertices[0].boneWeight.boneIndex0);
            Assert.AreEqual(0, m_ExpectedVertices[0].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[0].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[0].boneWeight.boneIndex3);
            Assert.AreEqual(1.0f, m_ExpectedVertices[0].boneWeight.weight0);
            Assert.AreEqual(0.0f, m_ExpectedVertices[0].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[0].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[0].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[1].boneWeight.boneIndex0);
            Assert.AreEqual(1, m_ExpectedVertices[1].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[1].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[1].boneWeight.boneIndex3);
            Assert.AreEqual(0.5f, m_ExpectedVertices[1].boneWeight.weight0);
            Assert.AreEqual(0.5f, m_ExpectedVertices[1].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[1].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[1].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[2].boneWeight.boneIndex0);
            Assert.AreEqual(2, m_ExpectedVertices[2].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[2].boneWeight.boneIndex2);
            Assert.AreEqual(4, m_ExpectedVertices[2].boneWeight.boneIndex3);
            Assert.AreEqual(0.25f, m_ExpectedVertices[2].boneWeight.weight0);
            Assert.AreEqual(0.25f, m_ExpectedVertices[2].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[2].boneWeight.weight2);
            Assert.AreEqual(0.25f, m_ExpectedVertices[2].boneWeight.weight3);

            Assert.AreEqual(2, m_ExpectedVertices[3].boneWeight.boneIndex0);
            Assert.AreEqual(0, m_ExpectedVertices[3].boneWeight.boneIndex1);
            Assert.AreEqual(4, m_ExpectedVertices[3].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[3].boneWeight.boneIndex3);
            Assert.AreEqual(0.3f, m_ExpectedVertices[3].boneWeight.weight0);
            Assert.AreEqual(0.0f, m_ExpectedVertices[3].boneWeight.weight1);
            Assert.AreEqual(0.3f, m_ExpectedVertices[3].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[3].boneWeight.weight3);

            Assert.AreEqual(1, m_ExpectedVertices[4].boneWeight.boneIndex0);
            Assert.AreEqual(0, m_ExpectedVertices[4].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[4].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[4].boneWeight.boneIndex3);
            Assert.AreEqual(0.5f, m_ExpectedVertices[4].boneWeight.weight0);
            Assert.AreEqual(0.0f, m_ExpectedVertices[4].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[4].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[4].boneWeight.weight3);

            Assert.AreEqual(4, m_ExpectedVertices[5].boneWeight.boneIndex0);
            Assert.AreEqual(0, m_ExpectedVertices[5].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[5].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[5].boneWeight.boneIndex3);
            Assert.AreEqual(1.0f, m_ExpectedVertices[5].boneWeight.weight0);
            Assert.AreEqual(0.0f, m_ExpectedVertices[5].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[5].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[5].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[6].boneWeight.boneIndex0);
            Assert.AreEqual(4, m_ExpectedVertices[6].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[6].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[6].boneWeight.boneIndex3);
            Assert.AreEqual(0.0f, m_ExpectedVertices[6].boneWeight.weight0);
            Assert.AreEqual(0.5f, m_ExpectedVertices[6].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[6].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[6].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[7].boneWeight.boneIndex0);
            Assert.AreEqual(1, m_ExpectedVertices[7].boneWeight.boneIndex1);
            Assert.AreEqual(5, m_ExpectedVertices[7].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[7].boneWeight.boneIndex3);
            Assert.AreEqual(0.3f, m_ExpectedVertices[7].boneWeight.weight0);
            Assert.AreEqual(0.3f, m_ExpectedVertices[7].boneWeight.weight1);
            Assert.AreEqual(0.3f, m_ExpectedVertices[7].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[7].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[8].boneWeight.boneIndex0);
            Assert.AreEqual(5, m_ExpectedVertices[8].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[8].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[8].boneWeight.boneIndex3);
            Assert.AreEqual(0.0f, m_ExpectedVertices[8].boneWeight.weight0);
            Assert.AreEqual(0.5f, m_ExpectedVertices[8].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[8].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[8].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[9].boneWeight.boneIndex0);
            Assert.AreEqual(2, m_ExpectedVertices[9].boneWeight.boneIndex1);
            Assert.AreEqual(4, m_ExpectedVertices[9].boneWeight.boneIndex2);
            Assert.AreEqual(5, m_ExpectedVertices[9].boneWeight.boneIndex3);
            Assert.AreEqual(0.25f, m_ExpectedVertices[9].boneWeight.weight0);
            Assert.AreEqual(0.25f, m_ExpectedVertices[9].boneWeight.weight1);
            Assert.AreEqual(0.25f, m_ExpectedVertices[9].boneWeight.weight2);
            Assert.AreEqual(0.25f, m_ExpectedVertices[9].boneWeight.weight3);
        }

        [Test]
        public void HelperMethodVerification_Delete2_Reorder_5to4_4to3_3to2()
        {
            InvalidateBoneIndex(2, m_ExpectedVertices);            
            ChangeBoneIndex(new int[3]{5, 4, 3}, new int[3] {4, 3, 2}, m_ExpectedVertices);

            Assert.AreEqual(0, m_ExpectedVertices[0].boneWeight.boneIndex0);
            Assert.AreEqual(0, m_ExpectedVertices[0].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[0].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[0].boneWeight.boneIndex3);
            Assert.AreEqual(1.0f, m_ExpectedVertices[0].boneWeight.weight0);
            Assert.AreEqual(0.0f, m_ExpectedVertices[0].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[0].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[0].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[1].boneWeight.boneIndex0);
            Assert.AreEqual(1, m_ExpectedVertices[1].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[1].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[1].boneWeight.boneIndex3);
            Assert.AreEqual(0.5f, m_ExpectedVertices[1].boneWeight.weight0);
            Assert.AreEqual(0.5f, m_ExpectedVertices[1].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[1].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[1].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[2].boneWeight.boneIndex0);
            Assert.AreEqual(0, m_ExpectedVertices[2].boneWeight.boneIndex1);
            Assert.AreEqual(2, m_ExpectedVertices[2].boneWeight.boneIndex2);
            Assert.AreEqual(3, m_ExpectedVertices[2].boneWeight.boneIndex3);
            Assert.AreEqual(0.25f, m_ExpectedVertices[2].boneWeight.weight0);
            Assert.AreEqual(0.0f, m_ExpectedVertices[2].boneWeight.weight1);
            Assert.AreEqual(0.25f, m_ExpectedVertices[2].boneWeight.weight2);
            Assert.AreEqual(0.25f, m_ExpectedVertices[2].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[3].boneWeight.boneIndex0);
            Assert.AreEqual(2, m_ExpectedVertices[3].boneWeight.boneIndex1);
            Assert.AreEqual(3, m_ExpectedVertices[3].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[3].boneWeight.boneIndex3);
            Assert.AreEqual(0.0f, m_ExpectedVertices[3].boneWeight.weight0);
            Assert.AreEqual(0.3f, m_ExpectedVertices[3].boneWeight.weight1);
            Assert.AreEqual(0.3f, m_ExpectedVertices[3].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[3].boneWeight.weight3);

            Assert.AreEqual(1, m_ExpectedVertices[4].boneWeight.boneIndex0);
            Assert.AreEqual(2, m_ExpectedVertices[4].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[4].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[4].boneWeight.boneIndex3);
            Assert.AreEqual(0.5f, m_ExpectedVertices[4].boneWeight.weight0);
            Assert.AreEqual(0.5f, m_ExpectedVertices[4].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[4].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[4].boneWeight.weight3);

            Assert.AreEqual(3, m_ExpectedVertices[5].boneWeight.boneIndex0);
            Assert.AreEqual(0, m_ExpectedVertices[5].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[5].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[5].boneWeight.boneIndex3);
            Assert.AreEqual(1.0f, m_ExpectedVertices[5].boneWeight.weight0);
            Assert.AreEqual(0.0f, m_ExpectedVertices[5].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[5].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[5].boneWeight.weight3);

            Assert.AreEqual(2, m_ExpectedVertices[6].boneWeight.boneIndex0);
            Assert.AreEqual(3, m_ExpectedVertices[6].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[6].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[6].boneWeight.boneIndex3);
            Assert.AreEqual(0.5f, m_ExpectedVertices[6].boneWeight.weight0);
            Assert.AreEqual(0.5f, m_ExpectedVertices[6].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[6].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[6].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[7].boneWeight.boneIndex0);
            Assert.AreEqual(1, m_ExpectedVertices[7].boneWeight.boneIndex1);
            Assert.AreEqual(4, m_ExpectedVertices[7].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[7].boneWeight.boneIndex3);
            Assert.AreEqual(0.3f, m_ExpectedVertices[7].boneWeight.weight0);
            Assert.AreEqual(0.3f, m_ExpectedVertices[7].boneWeight.weight1);
            Assert.AreEqual(0.3f, m_ExpectedVertices[7].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[7].boneWeight.weight3);

            Assert.AreEqual(2, m_ExpectedVertices[8].boneWeight.boneIndex0);
            Assert.AreEqual(4, m_ExpectedVertices[8].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[8].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[8].boneWeight.boneIndex3);
            Assert.AreEqual(0.5f, m_ExpectedVertices[8].boneWeight.weight0);
            Assert.AreEqual(0.5f, m_ExpectedVertices[8].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[8].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[8].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[9].boneWeight.boneIndex0);
            Assert.AreEqual(0, m_ExpectedVertices[9].boneWeight.boneIndex1);
            Assert.AreEqual(3, m_ExpectedVertices[9].boneWeight.boneIndex2);
            Assert.AreEqual(4, m_ExpectedVertices[9].boneWeight.boneIndex3);
            Assert.AreEqual(0.25f, m_ExpectedVertices[9].boneWeight.weight0);
            Assert.AreEqual(0.0f, m_ExpectedVertices[9].boneWeight.weight1);
            Assert.AreEqual(0.25f, m_ExpectedVertices[9].boneWeight.weight2);
            Assert.AreEqual(0.25f, m_ExpectedVertices[9].boneWeight.weight3);
        }

        [Test]
        public void HelperMethodVerification_Reorder_1to5_5to3_3to1()
        {
            ChangeBoneIndex(new int[3]{1, 3, 5}, new int[3] {5, 1, 3}, m_ExpectedVertices);

            Assert.AreEqual(0, m_ExpectedVertices[0].boneWeight.boneIndex0);
            Assert.AreEqual(0, m_ExpectedVertices[0].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[0].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[0].boneWeight.boneIndex3);
            Assert.AreEqual(1.0f, m_ExpectedVertices[0].boneWeight.weight0);
            Assert.AreEqual(0.0f, m_ExpectedVertices[0].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[0].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[0].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[1].boneWeight.boneIndex0);
            Assert.AreEqual(5, m_ExpectedVertices[1].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[1].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[1].boneWeight.boneIndex3);
            Assert.AreEqual(0.5f, m_ExpectedVertices[1].boneWeight.weight0);
            Assert.AreEqual(0.5f, m_ExpectedVertices[1].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[1].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[1].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[2].boneWeight.boneIndex0);
            Assert.AreEqual(2, m_ExpectedVertices[2].boneWeight.boneIndex1);
            Assert.AreEqual(1, m_ExpectedVertices[2].boneWeight.boneIndex2);
            Assert.AreEqual(4, m_ExpectedVertices[2].boneWeight.boneIndex3);
            Assert.AreEqual(0.25f, m_ExpectedVertices[2].boneWeight.weight0);
            Assert.AreEqual(0.25f, m_ExpectedVertices[2].boneWeight.weight1);
            Assert.AreEqual(0.25f, m_ExpectedVertices[2].boneWeight.weight2);
            Assert.AreEqual(0.25f, m_ExpectedVertices[2].boneWeight.weight3);

            Assert.AreEqual(2, m_ExpectedVertices[3].boneWeight.boneIndex0);
            Assert.AreEqual(1, m_ExpectedVertices[3].boneWeight.boneIndex1);
            Assert.AreEqual(4, m_ExpectedVertices[3].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[3].boneWeight.boneIndex3);
            Assert.AreEqual(0.3f, m_ExpectedVertices[3].boneWeight.weight0);
            Assert.AreEqual(0.3f, m_ExpectedVertices[3].boneWeight.weight1);
            Assert.AreEqual(0.3f, m_ExpectedVertices[3].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[3].boneWeight.weight3);

            Assert.AreEqual(5, m_ExpectedVertices[4].boneWeight.boneIndex0);
            Assert.AreEqual(1, m_ExpectedVertices[4].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[4].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[4].boneWeight.boneIndex3);
            Assert.AreEqual(0.5f, m_ExpectedVertices[4].boneWeight.weight0);
            Assert.AreEqual(0.5f, m_ExpectedVertices[4].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[4].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[4].boneWeight.weight3);

            Assert.AreEqual(4, m_ExpectedVertices[5].boneWeight.boneIndex0);
            Assert.AreEqual(0, m_ExpectedVertices[5].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[5].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[5].boneWeight.boneIndex3);
            Assert.AreEqual(1.0f, m_ExpectedVertices[5].boneWeight.weight0);
            Assert.AreEqual(0.0f, m_ExpectedVertices[5].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[5].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[5].boneWeight.weight3);

            Assert.AreEqual(1, m_ExpectedVertices[6].boneWeight.boneIndex0);
            Assert.AreEqual(4, m_ExpectedVertices[6].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[6].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[6].boneWeight.boneIndex3);
            Assert.AreEqual(0.5f, m_ExpectedVertices[6].boneWeight.weight0);
            Assert.AreEqual(0.5f, m_ExpectedVertices[6].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[6].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[6].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[7].boneWeight.boneIndex0);
            Assert.AreEqual(5, m_ExpectedVertices[7].boneWeight.boneIndex1);
            Assert.AreEqual(3, m_ExpectedVertices[7].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[7].boneWeight.boneIndex3);
            Assert.AreEqual(0.3f, m_ExpectedVertices[7].boneWeight.weight0);
            Assert.AreEqual(0.3f, m_ExpectedVertices[7].boneWeight.weight1);
            Assert.AreEqual(0.3f, m_ExpectedVertices[7].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[7].boneWeight.weight3);

            Assert.AreEqual(1, m_ExpectedVertices[8].boneWeight.boneIndex0);
            Assert.AreEqual(3, m_ExpectedVertices[8].boneWeight.boneIndex1);
            Assert.AreEqual(0, m_ExpectedVertices[8].boneWeight.boneIndex2);
            Assert.AreEqual(0, m_ExpectedVertices[8].boneWeight.boneIndex3);
            Assert.AreEqual(0.5f, m_ExpectedVertices[8].boneWeight.weight0);
            Assert.AreEqual(0.5f, m_ExpectedVertices[8].boneWeight.weight1);
            Assert.AreEqual(0.0f, m_ExpectedVertices[8].boneWeight.weight2);
            Assert.AreEqual(0.0f, m_ExpectedVertices[8].boneWeight.weight3);

            Assert.AreEqual(0, m_ExpectedVertices[9].boneWeight.boneIndex0);
            Assert.AreEqual(2, m_ExpectedVertices[9].boneWeight.boneIndex1);
            Assert.AreEqual(4, m_ExpectedVertices[9].boneWeight.boneIndex2);
            Assert.AreEqual(3, m_ExpectedVertices[9].boneWeight.boneIndex3);
            Assert.AreEqual(0.25f, m_ExpectedVertices[9].boneWeight.weight0);
            Assert.AreEqual(0.25f, m_ExpectedVertices[9].boneWeight.weight1);
            Assert.AreEqual(0.25f, m_ExpectedVertices[9].boneWeight.weight2);
            Assert.AreEqual(0.25f, m_ExpectedVertices[9].boneWeight.weight3);
        }
    }
}