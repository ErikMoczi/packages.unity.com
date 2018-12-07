using NSubstitute;
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using UnityEngine.Experimental.U2D;
using UnityEditor.Experimental.U2D;
using UnityEditor.Experimental.U2D.Animation;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation.Test.Weights
{
    internal class WeightEditorTestBase
    {
        protected SpriteMeshData m_SpriteMeshData;
        protected SpriteMeshDataController m_SpriteMeshDataController = new SpriteMeshDataController();
        protected WeightEditor m_WeightEditor;
        protected ISelection<int> m_Selection;
        protected ICacheUndo m_CacheUndo;
        protected Triangulator m_Triangulator;

        [SetUp]
        public void Setup()
        {
            m_Triangulator = new Triangulator();
            m_Selection = Substitute.For<ISelection<int>>();
            m_CacheUndo = Substitute.For<ICacheUndo>();

            m_SpriteMeshData = new SpriteMeshData();
            m_SpriteMeshData.bones = CreateBones();
            m_SpriteMeshData.vertices = CreateVertices();
            m_SpriteMeshData.edges = CreateEdges();

            m_SpriteMeshDataController.spriteMeshData = m_SpriteMeshData;
            Triangulate();

            m_WeightEditor = new WeightEditor();
            m_WeightEditor.spriteMeshData = m_SpriteMeshData;
            m_WeightEditor.selection = m_Selection;
            m_WeightEditor.cacheUndo = m_CacheUndo;
        }

        protected void Triangulate()
        {
            m_SpriteMeshDataController.Triangulate(m_Triangulator);
        }

        private List<SpriteBoneData> CreateBones()
        {
            SpriteBone[] spriteBones = new SpriteBone[]
            {
                new SpriteBone() {
                    name = "root",
                    length = 1f,
                    parentId = -1,
                    position = Vector2.zero,
                    rotation = Quaternion.identity
                },
                new SpriteBone() {
                    name = "bone 0",
                    length = 1f,
                    parentId = 0,
                    position = Vector2.right,
                    rotation = Quaternion.identity
                }
            };

            return ModuleUtility.CreateSpriteBoneData(spriteBones, Matrix4x4.identity);
        }

        private List<Vertex2D> CreateVertices()
        {
            BoneWeight boneWeight = default(BoneWeight);

            List<Vertex2D> vertices = new List<Vertex2D>();

            vertices.Add(new Vertex2D(new Vector2(0f, -1f), boneWeight));
            vertices.Add(new Vertex2D(new Vector2(0f, 1f), boneWeight));
            vertices.Add(new Vertex2D(new Vector2(1f, -1f), boneWeight));
            vertices.Add(new Vertex2D(new Vector2(1f, 1f), boneWeight));
            vertices.Add(new Vertex2D(new Vector2(2f, -1f), boneWeight));
            vertices.Add(new Vertex2D(new Vector2(2f, 1f), boneWeight));

            return vertices;
        }

        private List<Edge> CreateEdges()
        {
            List<Edge> edges = new List<Edge>();

            edges.Add(new Edge(0, 1));
            edges.Add(new Edge(1, 3));
            edges.Add(new Edge(3, 5));
            edges.Add(new Edge(5, 4));
            edges.Add(new Edge(4, 2));
            edges.Add(new Edge(2, 0));

            return edges;
        }

        public void SetMaxWeightToFirstBone()
        {
            BoneWeight boneWeight = default(BoneWeight);
            boneWeight.weight0 = 1f;

            foreach (Vertex2D v in m_SpriteMeshData.vertices)
                v.editableBoneWeight.SetFromBoneWeight(boneWeight);
        }

        protected void AssertBoneWeightContainsChannels(BoneWeight expected, BoneWeight actual)
        {
            var m_BoneWeightDataList = new List<BoneWeightData>();

            for (var i = 0; i < 4; ++i)
                m_BoneWeightDataList.Add(new BoneWeightData()
                {
                    boneIndex = expected.GetBoneIndex(i),
                    weight = expected.GetWeight(i)
                });

            for (var i = 0; i < 4; ++i)
                Assert.IsTrue(m_BoneWeightDataList.Contains(new BoneWeightData() { boneIndex = actual.GetBoneIndex(i), weight = actual.GetWeight(i) }));
        }

        public void CodifyWeights(BoneWeight[] boneWeights)
        {
            string output = "BoneWeight[] expected = new BoneWeight[] {\n";

            for (int i = 0; i < boneWeights.Length; ++i)
            {
                BoneWeight bw = boneWeights[i];
                string weight = "                new BoneWeight()\n                {\n                    " +
                    "boneIndex0 = " + bw.boneIndex0 + ",\n                    " +
                    "boneIndex1 = " + bw.boneIndex1 + ",\n                    " +
                    "boneIndex2 = " + bw.boneIndex2 + ",\n                    " +
                    "boneIndex3 = " + bw.boneIndex3 + ",\n                    " +
                    "weight0 = " + String.Format("{0:R}", bw.weight0) + "f,\n                    " +
                    "weight1 = " + String.Format("{0:R}", bw.weight1) + "f,\n                    " +
                    "weight2 = " + String.Format("{0:R}", bw.weight2) + "f,\n                    " +
                    "weight3 = " + String.Format("{0:R}", bw.weight3) + "f\n                }";

                if (i == boneWeights.Length - 1)
                    weight += "\n";
                else
                    weight += ",\n";

                output += weight;
            }

            output += "            };";

            EditorGUIUtility.systemCopyBuffer = output;

            Debug.Log(output);
        }
    }

    [TestFixture]
    internal class WeightEditorTest : WeightEditorTestBase
    {
        [Test]
        public void SetupCreateSixVerticesWithDefaultBoneWeights()
        {
            BoneWeight expected = default(BoneWeight);

            Assert.AreEqual(6, m_SpriteMeshData.vertices.Count, "There should be 6 vertices");

            for (int i = 0; i < m_SpriteMeshData.vertices.Count; ++i)
                AssertBoneWeightContainsChannels(expected, m_SpriteMeshData.vertices[i].editableBoneWeight.ToBoneWeight(false));
        }

        [Test]
        public void SetBone1To025f_SetsBone0To075f_EmptySelectionEditsAll()
        {
            SetMaxWeightToFirstBone();
            m_WeightEditor.mode = WeightEditorMode.AddAndSubtract;
            m_WeightEditor.boneIndex = 1;
            m_WeightEditor.emptySelectionEditsAll = true;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(0.25f);
            m_WeightEditor.OnEditEnd();

            BoneWeight expected = new BoneWeight();
            expected.boneIndex0 = 0;
            expected.weight0 = 0.75f;
            expected.boneIndex1 = 1;
            expected.weight1 = 0.25f;

            for (int i = 0; i < m_SpriteMeshData.vertices.Count; ++i)
                AssertBoneWeightContainsChannels(expected, m_SpriteMeshData.vertices[i].editableBoneWeight.ToBoneWeight(true));
        }

        [Test]
        public void SetBone1To05f_SetsBone0To05f_EmptySelectionEditsAllTrue()
        {
            SetMaxWeightToFirstBone();
            m_WeightEditor.mode = WeightEditorMode.AddAndSubtract;
            m_WeightEditor.boneIndex = 1;
            m_WeightEditor.emptySelectionEditsAll = true;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(0.5f);
            m_WeightEditor.OnEditEnd();

            BoneWeight expected = new BoneWeight();
            expected.boneIndex0 = 0;
            expected.weight0 = 0.5f;
            expected.boneIndex1 = 1;
            expected.weight1 = 0.5f;

            for (int i = 0; i < m_SpriteMeshData.vertices.Count; ++i)
                AssertBoneWeightContainsChannels(expected, m_SpriteMeshData.vertices[i].editableBoneWeight.ToBoneWeight(true));
        }

        [Test]
        public void SetBone1To075f_SetsBone0To025f_EmptySelectionEditsAllTrue()
        {
            SetMaxWeightToFirstBone();
            m_WeightEditor.mode = WeightEditorMode.AddAndSubtract;
            m_WeightEditor.boneIndex = 1;
            m_WeightEditor.emptySelectionEditsAll = true;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(0.75f);
            m_WeightEditor.OnEditEnd();

            BoneWeight expected = new BoneWeight();
            expected.boneIndex0 = 1;
            expected.weight0 = 0.75f;
            expected.boneIndex1 = 0;
            expected.weight1 = 0.25f;

            for (int i = 0; i < m_SpriteMeshData.vertices.Count; ++i)
                AssertBoneWeightContainsChannels(expected, m_SpriteMeshData.vertices[i].editableBoneWeight.ToBoneWeight(true));
        }

        [Test]
        public void SetBone1To1f_SetsBone0To0f_EmptySelectionEditsAllTrue()
        {
            SetMaxWeightToFirstBone();
            m_WeightEditor.mode = WeightEditorMode.AddAndSubtract;
            m_WeightEditor.boneIndex = 1;
            m_WeightEditor.emptySelectionEditsAll = true;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(1f);
            m_WeightEditor.OnEditEnd();

            BoneWeight expected = new BoneWeight();
            expected.boneIndex0 = 1;
            expected.weight0 = 1f;

            for (int i = 0; i < m_SpriteMeshData.vertices.Count; ++i)
                AssertBoneWeightContainsChannels(expected, m_SpriteMeshData.vertices[i].editableBoneWeight.ToBoneWeight(true));
        }

        [Test]
        public void SetBone1To1f_SelectionEmpty_EmptySelectionEditsAllFalse_NothingChanges()
        {
            SetMaxWeightToFirstBone();
            m_WeightEditor.mode = WeightEditorMode.AddAndSubtract;
            m_WeightEditor.boneIndex = 1;
            m_WeightEditor.emptySelectionEditsAll = false;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(1f);
            m_WeightEditor.OnEditEnd();

            BoneWeight expected = new BoneWeight();
            expected.boneIndex0 = 0;
            expected.weight0 = 1f;

            for (int i = 0; i < m_SpriteMeshData.vertices.Count; ++i)
                AssertBoneWeightContainsChannels(expected, m_SpriteMeshData.vertices[i].editableBoneWeight.ToBoneWeight(true));
        }

        [Test]
        public void SetBone1To025f_SetsBone0To075f_UsingSelection()
        {
            SetMaxWeightToFirstBone();
            m_Selection.Count.Returns(3);
            m_Selection.Contains(0).Returns(true);
            m_Selection.Contains(1).Returns(true);
            m_Selection.Contains(2).Returns(true);

            m_WeightEditor.mode = WeightEditorMode.AddAndSubtract;
            m_WeightEditor.boneIndex = 1;
            m_WeightEditor.emptySelectionEditsAll = false;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(0.25f);
            m_WeightEditor.OnEditEnd();

            BoneWeight expectedNotSelected = new BoneWeight();
            expectedNotSelected.boneIndex0 = 0;
            expectedNotSelected.weight0 = 1f;

            BoneWeight expectedSelected = new BoneWeight();
            expectedSelected.boneIndex0 = 0;
            expectedSelected.weight0 = 0.75f;
            expectedSelected.boneIndex1 = 1;
            expectedSelected.weight1 = 0.25f;

            AssertBoneWeightContainsChannels(expectedSelected, m_SpriteMeshData.vertices[0].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedSelected, m_SpriteMeshData.vertices[1].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedSelected, m_SpriteMeshData.vertices[2].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedNotSelected, m_SpriteMeshData.vertices[3].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedNotSelected, m_SpriteMeshData.vertices[4].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedNotSelected, m_SpriteMeshData.vertices[5].editableBoneWeight.ToBoneWeight(true));
        }

        [Test]
        public void SetBone1To05f_SetsBone0To05f_UsingSelection()
        {
            SetMaxWeightToFirstBone();
            m_Selection.Count.Returns(3);
            m_Selection.Contains(0).Returns(true);
            m_Selection.Contains(1).Returns(true);
            m_Selection.Contains(2).Returns(true);

            m_WeightEditor.mode = WeightEditorMode.AddAndSubtract;
            m_WeightEditor.boneIndex = 1;
            m_WeightEditor.emptySelectionEditsAll = false;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(0.5f);
            m_WeightEditor.OnEditEnd();

            BoneWeight expectedNotSelected = new BoneWeight();
            expectedNotSelected.boneIndex0 = 0;
            expectedNotSelected.weight0 = 1f;

            BoneWeight expectedSelected = new BoneWeight();
            expectedSelected.boneIndex0 = 0;
            expectedSelected.weight0 = 0.5f;
            expectedSelected.boneIndex1 = 1;
            expectedSelected.weight1 = 0.5f;

            AssertBoneWeightContainsChannels(expectedSelected, m_SpriteMeshData.vertices[0].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedSelected, m_SpriteMeshData.vertices[1].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedSelected, m_SpriteMeshData.vertices[2].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedNotSelected, m_SpriteMeshData.vertices[3].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedNotSelected, m_SpriteMeshData.vertices[4].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedNotSelected, m_SpriteMeshData.vertices[5].editableBoneWeight.ToBoneWeight(true));
        }

        [Test]
        public void SetBone1To075f_SetsBone0To025f_UsingSelection()
        {
            SetMaxWeightToFirstBone();
            m_Selection.Count.Returns(3);
            m_Selection.Contains(0).Returns(true);
            m_Selection.Contains(1).Returns(true);
            m_Selection.Contains(2).Returns(true);

            m_WeightEditor.mode = WeightEditorMode.AddAndSubtract;
            m_WeightEditor.boneIndex = 1;
            m_WeightEditor.emptySelectionEditsAll = false;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(0.75f);
            m_WeightEditor.OnEditEnd();

            BoneWeight expectedNotSelected = new BoneWeight();
            expectedNotSelected.boneIndex0 = 0;
            expectedNotSelected.weight0 = 1f;

            BoneWeight expectedSelected = new BoneWeight();
            expectedSelected.boneIndex0 = 1;
            expectedSelected.weight0 = 0.75f;
            expectedSelected.boneIndex1 = 0;
            expectedSelected.weight1 = 0.25f;

            AssertBoneWeightContainsChannels(expectedSelected, m_SpriteMeshData.vertices[0].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedSelected, m_SpriteMeshData.vertices[1].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedSelected, m_SpriteMeshData.vertices[2].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedNotSelected, m_SpriteMeshData.vertices[3].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedNotSelected, m_SpriteMeshData.vertices[4].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedNotSelected, m_SpriteMeshData.vertices[5].editableBoneWeight.ToBoneWeight(true));
        }

        [Test]
        public void SetBone1To1f_SetsBone0To0f_UsingSelection()
        {
            SetMaxWeightToFirstBone();
            m_Selection.Count.Returns(3);
            m_Selection.Contains(0).Returns(true);
            m_Selection.Contains(1).Returns(true);
            m_Selection.Contains(2).Returns(true);

            m_WeightEditor.mode = WeightEditorMode.AddAndSubtract;
            m_WeightEditor.boneIndex = 1;
            m_WeightEditor.emptySelectionEditsAll = false;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(1f);
            m_WeightEditor.OnEditEnd();

            BoneWeight expectedNotSelected = new BoneWeight();
            expectedNotSelected.boneIndex0 = 0;
            expectedNotSelected.weight0 = 1f;

            BoneWeight expectedSelected = new BoneWeight();
            expectedSelected.boneIndex0 = 1;
            expectedSelected.weight0 = 1f;

            AssertBoneWeightContainsChannels(expectedSelected, m_SpriteMeshData.vertices[0].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedSelected, m_SpriteMeshData.vertices[1].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedSelected, m_SpriteMeshData.vertices[2].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedNotSelected, m_SpriteMeshData.vertices[3].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedNotSelected, m_SpriteMeshData.vertices[4].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedNotSelected, m_SpriteMeshData.vertices[5].editableBoneWeight.ToBoneWeight(true));
        }

        [Test]
        public void IncrementBone1_EmptySelectionEditAllFalse_NothingChanges()
        {
            SetMaxWeightToFirstBone();
            m_WeightEditor.boneIndex = 1;
            m_WeightEditor.emptySelectionEditsAll = false;

            //First we set some influence
            m_WeightEditor.mode = WeightEditorMode.AddAndSubtract;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(0.1f);
            m_WeightEditor.OnEditEnd();

            //Second we increment it
            m_WeightEditor.mode = WeightEditorMode.GrowAndShrink;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(0.75f);
            m_WeightEditor.OnEditEnd();

            BoneWeight expected = new BoneWeight();
            expected.boneIndex0 = 0;
            expected.weight0 = 1f;

            for (int i = 0; i < m_SpriteMeshData.vertices.Count; ++i)
                AssertBoneWeightContainsChannels(expected, m_SpriteMeshData.vertices[i].editableBoneWeight.ToBoneWeight(true));
        }

        [Test]
        public void IncrementBone1_EmptySelectionEditAllTrue_IncrementsAllVertices()
        {
            SetMaxWeightToFirstBone();
            m_WeightEditor.boneIndex = 1;
            m_WeightEditor.emptySelectionEditsAll = true;

            //First we set some influence
            m_WeightEditor.mode = WeightEditorMode.AddAndSubtract;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(0.1f);
            m_WeightEditor.OnEditEnd();

            //Second we increment it
            m_WeightEditor.mode = WeightEditorMode.GrowAndShrink;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(0.75f);
            m_WeightEditor.OnEditEnd();

            BoneWeight expected = new BoneWeight();
            expected.boneIndex0 = 1;
            expected.weight0 = 0.850000024f;
            expected.boneIndex1 = 0;
            expected.weight1 = 0.149999976f;

            for (int i = 0; i < m_SpriteMeshData.vertices.Count; ++i)
                AssertBoneWeightContainsChannels(expected, m_SpriteMeshData.vertices[i].editableBoneWeight.ToBoneWeight(true));
        }

        [Test]
        public void IncrementBone1_UsingSelection_IncrementsOnlyTheSelectedVertices()
        {
            SetMaxWeightToFirstBone();
            m_WeightEditor.boneIndex = 1;
            m_WeightEditor.emptySelectionEditsAll = true;

            //First we set some influence
            m_WeightEditor.mode = WeightEditorMode.AddAndSubtract;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(0.1f);
            m_WeightEditor.OnEditEnd();

            m_Selection.Count.Returns(3);
            m_Selection.Contains(0).Returns(true);
            m_Selection.Contains(1).Returns(true);
            m_Selection.Contains(2).Returns(true);

            //Second we increment it
            m_WeightEditor.mode = WeightEditorMode.GrowAndShrink;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(0.75f);
            m_WeightEditor.OnEditEnd();

            BoneWeight expectedNotSelected = new BoneWeight();
            expectedNotSelected.boneIndex0 = 0;
            expectedNotSelected.weight0 = 0.899999976f;
            expectedNotSelected.boneIndex1 = 1;
            expectedNotSelected.weight1 = 0.100000001f;

            BoneWeight expectedSelected = new BoneWeight();
            expectedSelected.boneIndex0 = 1;
            expectedSelected.weight0 = 0.850000024f;
            expectedSelected.boneIndex1 = 0;
            expectedSelected.weight1 = 0.149999976f;

            AssertBoneWeightContainsChannels(expectedSelected, m_SpriteMeshData.vertices[0].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedSelected, m_SpriteMeshData.vertices[1].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedSelected, m_SpriteMeshData.vertices[2].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedNotSelected, m_SpriteMeshData.vertices[3].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedNotSelected, m_SpriteMeshData.vertices[4].editableBoneWeight.ToBoneWeight(true));
            AssertBoneWeightContainsChannels(expectedNotSelected, m_SpriteMeshData.vertices[5].editableBoneWeight.ToBoneWeight(true));
        }

        [Test]
        public void SmoothVertices_EmptySelectionEditAllTrue_SmoothAllVertices()
        {
            SetMaxWeightToFirstBone();
            m_Selection.Count.Returns(2);
            m_Selection.Contains(2).Returns(true);
            m_Selection.Contains(3).Returns(true);

            m_WeightEditor.mode = WeightEditorMode.AddAndSubtract;
            m_WeightEditor.boneIndex = 1;
            m_WeightEditor.emptySelectionEditsAll = false;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(1f);
            m_WeightEditor.OnEditEnd();

            m_Selection.Clear();
            m_Selection.Count.Returns(0);
            m_Selection.Contains(2).Returns(false);
            m_Selection.Contains(3).Returns(false);

            m_WeightEditor.mode = WeightEditorMode.Smooth;
            m_WeightEditor.emptySelectionEditsAll = true;
            m_WeightEditor.OnEditStart(false);
            m_WeightEditor.DoEdit(0.5f);
            m_WeightEditor.OnEditEnd();

            BoneWeight expected = new BoneWeight();
            expected.boneIndex0 = 0;
            expected.weight0 = 0.75f;
            expected.boneIndex1 = 1;
            expected.weight1 = 0.25f;

            AssertBoneWeightContainsChannels(expected, m_SpriteMeshData.vertices[0].editableBoneWeight.ToBoneWeight(false));

            expected = new BoneWeight();
            expected.boneIndex0 = 0;
            expected.weight0 = 0.625f;
            expected.boneIndex1 = 1;
            expected.weight1 = 0.375f;

            AssertBoneWeightContainsChannels(expected, m_SpriteMeshData.vertices[1].editableBoneWeight.ToBoneWeight(false));

            expected = new BoneWeight();
            expected.boneIndex0 = 1;
            expected.weight0 = 0.666666687f;
            expected.boneIndex1 = 0;
            expected.weight1 = 0.333333313f;

            AssertBoneWeightContainsChannels(expected, m_SpriteMeshData.vertices[2].editableBoneWeight.ToBoneWeight(false));

            expected = new BoneWeight();
            expected.boneIndex0 = 1;
            expected.weight0 = 0.666666687f;
            expected.boneIndex1 = 0;
            expected.weight1 = 0.333333313f;

            AssertBoneWeightContainsChannels(expected, m_SpriteMeshData.vertices[3].editableBoneWeight.ToBoneWeight(false));

            expected = new BoneWeight();
            expected.boneIndex0 = 0;
            expected.weight0 = 0.625f;
            expected.boneIndex1 = 1;
            expected.weight1 = 0.375f;

            AssertBoneWeightContainsChannels(expected, m_SpriteMeshData.vertices[4].editableBoneWeight.ToBoneWeight(false));

            expected = new BoneWeight();
            expected.boneIndex0 = 0;
            expected.weight0 = 0.75f;
            expected.boneIndex1 = 1;
            expected.weight1 = 0.25f;

            AssertBoneWeightContainsChannels(expected, m_SpriteMeshData.vertices[5].editableBoneWeight.ToBoneWeight(true));
        }
    }
}
