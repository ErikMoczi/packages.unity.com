using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEditor;
using NSubstitute;
using NUnit.Framework.Constraints;

namespace UnityEditor.Experimental.U2D.Animation.Test.SpriteMesh
{
    [TestFixture]
    public class SpriteMeshControllerTest
    {
        private SpriteMeshData m_SpriteMeshData;
        private SpriteMeshDataController m_SpriteMeshDataController = new SpriteMeshDataController();
        private SpriteMeshController m_SpriteMeshController;
        private ISpriteMeshView m_View;
        private ISelection<int> m_Selection;
        private ICacheUndo m_CacheUndo;
        private ITriangulator m_Triangulator;

        private Vector2 m_MousePosition;
        private int m_HoveredVertex;
        private int m_HoveredEdge;
        private int m_ClosestEdge;
        List<int> m_SelectedVertices;

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

        [SetUp]
        public void Setup()
        {
            m_MousePosition = Vector2.zero;
            m_HoveredVertex = -1;
            m_HoveredEdge = -1;
            m_ClosestEdge = -1;

            m_View = Substitute.For<ISpriteMeshView>();
            m_View.mouseWorldPosition.Returns(x => m_MousePosition);
            m_View.hoveredVertex.Returns(x => m_HoveredVertex);
            m_View.hoveredEdge.Returns(x => m_HoveredEdge);
            m_View.closestEdge.Returns(x => m_ClosestEdge);
            m_View.WorldToScreen(Arg.Any<Vector2>()).Returns(x => ((Vector2)x[0] * 100f));
            m_View.IsActionTriggered(Arg.Any<MeshEditorAction>()).Returns(true);

            m_SelectedVertices = new List<int>();
            m_Selection = Substitute.For<ISelection<int>>();
            m_Selection.Count.Returns(x => m_SelectedVertices.Count);
            m_Selection.Contains(Arg.Any<int>()).Returns(x => m_SelectedVertices.Contains((int)x[0]));
            m_Selection.elements.Returns(x => m_SelectedVertices.ToArray());
            m_Selection.activeElement.Returns(x =>
                {
                    if (m_SelectedVertices.Count == 0)
                        return -1;
                    return m_SelectedVertices[0];
                });

            m_CacheUndo = Substitute.For<ICacheUndo>();
            m_Triangulator = Substitute.For<ITriangulator>();

            m_SpriteMeshData = new SpriteMeshData();
            m_SpriteMeshData.frame = new Rect(0f, 0f, 100f, 100f);
            m_SpriteMeshController = new SpriteMeshController();
            m_SpriteMeshController.frame = m_SpriteMeshData.frame;
            m_SpriteMeshController.spriteMeshData = m_SpriteMeshData;
            m_SpriteMeshController.spriteMeshView = m_View;
            m_SpriteMeshController.selection = m_Selection;
            m_SpriteMeshController.cacheUndo = m_CacheUndo;
            m_SpriteMeshController.triangulator = m_Triangulator;

            m_SpriteMeshDataController.spriteMeshData = m_SpriteMeshData;
        }

        private void AssertEdge(Edge expected, Edge actual)
        {
            Assert.AreEqual(expected.index1, actual.index1, "Incorrect edge index1");
            Assert.AreEqual(expected.index2, actual.index2, "Incorrect edge index2");
        }

        [Test]
        public void CreateVertex_WithMousePositionInsideFrame_CreatesVertexFromMousePosition()
        {
            m_MousePosition = new Vector2(10f, 7f);

            m_View.DoCreateVertex().Returns(true);

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(1, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");
            Assert.AreEqual(m_MousePosition, m_SpriteMeshData.vertices[0].position, "Vertex position is incorrect");
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
            m_Triangulator.Received().Triangulate(Arg.Any<IList<Vector2>>(), Arg.Any<IList<Edge>>(), Arg.Any<IList<int>>());
        }

        [Test]
        public void CreateVertex_WithMousePositionOutsideFrame_CreatesVertexFromMousePositionClampedToFrame()
        {
            m_MousePosition = new Vector2(200f, 200f);
            Vector2 clampedPosition = MathUtility.ClampPositionToRect(m_MousePosition, m_SpriteMeshData.frame);

            m_View.DoCreateVertex().Returns(true);

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(1, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");
            Assert.AreEqual(clampedPosition, m_SpriteMeshData.vertices[0].position, "Vertex position is incorrect");
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
            m_Triangulator.Received().Triangulate(Arg.Any<IList<Vector2>>(), Arg.Any<IList<Edge>>(), Arg.Any<IList<int>>());
        }

        [Test]
        public void CreateVertex_SetsAverageWeightsFromNeighboursToNewVertex()
        {
            m_SpriteMeshDataController.CreateVertex(Vector2.zero);
            m_SpriteMeshDataController.CreateVertex(Vector2.right);
            m_SpriteMeshDataController.CreateVertex(Vector2.one);
            m_SpriteMeshDataController.CreateVertex(Vector2.up);

            m_SpriteMeshData.vertices[0].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { boneIndex0 = 0, weight0 = 1f });
            m_SpriteMeshData.vertices[1].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { boneIndex0 = 1, weight0 = 1f });
            m_SpriteMeshData.vertices[2].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { boneIndex0 = 2, weight0 = 1f });
            m_SpriteMeshData.vertices[3].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { boneIndex0 = 3, weight0 = 1f });

            m_SpriteMeshDataController.CreateEdge(0, 1);
            m_SpriteMeshDataController.CreateEdge(1, 2);
            m_SpriteMeshDataController.CreateEdge(2, 3);
            m_SpriteMeshDataController.CreateEdge(3, 0);

            m_SpriteMeshData.indices.AddRange(new int[] { 0, 1, 2, 0, 2, 3 });

            m_SpriteMeshData.bones.Add(new SpriteBoneData());
            m_SpriteMeshData.bones.Add(new SpriteBoneData());
            m_SpriteMeshData.bones.Add(new SpriteBoneData());
            m_SpriteMeshData.bones.Add(new SpriteBoneData());

            m_MousePosition = new Vector2(0.25f, 0.75f);

            m_View.DoCreateVertex().Returns(true);

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(5, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");

            var expected = new BoneWeight()
            {
                boneIndex0 = 3,
                boneIndex1 = 0,
                boneIndex2 = 2,
                boneIndex3 = 0,
                weight0 = 0.5f,
                weight1 = 0.25f,
                weight2 = 0.25f,
                weight3 = 0f
            };

            AssertBoneWeightContainsChannels(expected, m_SpriteMeshData.vertices[4].editableBoneWeight.ToBoneWeight(false));
        }

        [Test]
        public void SelectVertex_WithInvalidIndex_ThrowsException()
        {
            m_HoveredVertex = -1;

            bool additive;
            m_View.DoSelectVertex(out additive).Returns(x =>
                {
                    x[0] = false;
                    return true;
                });

            Assert.Throws<ArgumentException>(() => m_SpriteMeshController.OnGUI());
        }

        [Test]
        public void SelectVertex_WithAdditiveSelectionFalse_WithUnselectedHoveredVertex_ClearsSelection_SelectsHoveredVertex()
        {
            m_SpriteMeshDataController.CreateVertex(Vector2.zero);
            m_HoveredVertex = 0;

            bool additive;
            m_View.DoSelectVertex(out additive).Returns(x =>
                {
                    x[0] = false;
                    return true;
                });

            m_SpriteMeshController.OnGUI();

            m_Selection.Received(1).Clear();
            m_Selection.Received(1).Select(m_HoveredVertex, true);
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
        }

        [Test]
        public void SelectVertex_WithAdditiveSelectionTrue_WithSelectedHoveredVertex_DoesNotClearSelection_UnselectsHoveredVertex()
        {
            m_SpriteMeshDataController.CreateVertex(Vector2.zero);
            m_HoveredVertex = 0;
            m_SelectedVertices.Add(0);

            bool additive;
            m_View.DoSelectVertex(out additive).Returns(x =>
                {
                    x[0] = true;
                    return true;
                });

            m_SpriteMeshController.OnGUI();

            m_Selection.DidNotReceive().Clear();
            m_Selection.Received(1).Select(m_HoveredVertex, false);
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
        }

        [Test]
        public void MoveVertex_WithOneVertexSelected_MovesTheSelectedVertex()
        {
            Vector2 position = Vector2.one;
            m_SpriteMeshDataController.CreateVertex(position);

            m_SelectedVertices.Add(0);

            Vector2 deltaPosition = Vector2.one * 10f;
            Vector2 delta;
            m_View.DoMoveVertex(out delta).Returns(x =>
                {
                    x[0] = deltaPosition;
                    return true;
                });

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(1, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");
            Assert.AreEqual(position + deltaPosition, m_SpriteMeshData.vertices[0].position, "Vertex position is incorrect");
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
            m_Triangulator.Received().Triangulate(Arg.Any<IList<Vector2>>(), Arg.Any<IList<Edge>>(), Arg.Any<IList<int>>());
        }

        [Test]
        public void MoveVertex_WithMultipleVerticesSelected_MovesTheSelectedVertices()
        {
            Vector2 position1 = Vector2.one;
            Vector2 position2 = Vector2.one * 5f;
            Vector2 position3 = Vector2.one * 7f;
            m_SpriteMeshDataController.CreateVertex(position1);
            m_SpriteMeshDataController.CreateVertex(position2);
            m_SpriteMeshDataController.CreateVertex(position3);
            m_HoveredVertex = 0;

            m_SelectedVertices.Add(0);
            m_SelectedVertices.Add(1);

            Vector2 deltaPosition = Vector2.one * 10f;
            Vector2 delta;
            m_View.DoMoveVertex(out delta).Returns(x =>
                {
                    x[0] = deltaPosition;
                    return true;
                });

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(3, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");
            Assert.AreEqual(position1 + deltaPosition, m_SpriteMeshData.vertices[0].position, "Vertex position is incorrect");
            Assert.AreEqual(position2 + deltaPosition, m_SpriteMeshData.vertices[1].position, "Vertex position is incorrect");
            Assert.AreEqual(position3, m_SpriteMeshData.vertices[2].position, "Vertex position is incorrect");
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
            m_Triangulator.Received().Triangulate(Arg.Any<IList<Vector2>>(), Arg.Any<IList<Edge>>(), Arg.Any<IList<int>>());
        }

        [Test]
        public void CreateEdge_WithSingleSelectedVertex_WithNoHoveredVertexOrEdge_WithNoIntersection_CreatesNewVertexAndEdge()
        {
            Vector2 position = Vector2.zero;
            Vector2 createPosition = new Vector2(10f, 7f);
            m_SpriteMeshDataController.CreateVertex(position);

            m_HoveredVertex = -1;
            m_HoveredEdge = -1;
            m_MousePosition = createPosition;
            m_SelectedVertices.Add(0);

            m_View.DoCreateEdge().Returns(true);

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(2, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");
            Assert.AreEqual(1, m_SpriteMeshData.edges.Count, "Incorrect number of edges");
            AssertEdge(new Edge(0, 1), m_SpriteMeshData.edges[0]);
            Assert.AreEqual(position, m_SpriteMeshData.vertices[0].position, "Vertex position is incorrect");
            Assert.AreEqual(createPosition, m_SpriteMeshData.vertices[1].position, "Vertex position is incorrect");
            m_Selection.Received(1).Clear();
            m_Selection.Received(1).Select(1, true);
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
            m_Triangulator.Received().Triangulate(Arg.Any<IList<Vector2>>(), Arg.Any<IList<Edge>>(), Arg.Any<IList<int>>());
        }

        [Test]
        public void CreateEdge_WithSingleSelectedVertex_WithHoveredVertex_WithNoHoveredEdge_WithNoIntersection_CreatesNewEdgeToHoveredVertex()
        {
            Vector2 position1 = Vector2.zero;
            Vector2 position2 = Vector2.one * 10f;
            m_SpriteMeshDataController.CreateVertex(position1);
            m_SpriteMeshDataController.CreateVertex(position2);

            m_HoveredVertex = 1;
            m_HoveredEdge = -1;
            m_MousePosition = position2;
            m_SelectedVertices.Add(0);

            m_View.DoCreateEdge().Returns(true);

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(2, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");
            Assert.AreEqual(1, m_SpriteMeshData.edges.Count, "Incorrect number of edges");
            AssertEdge(new Edge(0, 1), m_SpriteMeshData.edges[0]);
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
            m_Triangulator.Received().Triangulate(Arg.Any<IList<Vector2>>(), Arg.Any<IList<Edge>>(), Arg.Any<IList<int>>());
        }

        [Test]
        public void CreateEdge_WithSingleSelectedVertex_WithNoHoveredVertex_WithNoHoveredEdge_WithIntersection_CreatesNewVertexAtIntersection_CreatesNewEdgeAtIntersection_SplitsIntersectingEdge()
        {
            Vector2 position1 = Vector2.zero;
            Vector2 position2 = Vector2.up * 10f;
            Vector2 position3 = Vector2.right * 10f;
            Vector2 createPosition = Vector2.one * 10f;
            m_SpriteMeshDataController.CreateVertex(position1);
            m_SpriteMeshDataController.CreateVertex(position2);
            m_SpriteMeshDataController.CreateVertex(position3);
            m_SpriteMeshDataController.CreateEdge(1, 2);

            m_HoveredVertex = -1;
            m_HoveredEdge = -1;
            m_MousePosition = createPosition;
            m_SelectedVertices.Add(0);

            m_View.DoCreateEdge().Returns(true);

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(4, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");
            Assert.AreEqual(3, m_SpriteMeshData.edges.Count, "Incorrect number of edges");
            AssertEdge(new Edge(1, 3), m_SpriteMeshData.edges[0]);
            AssertEdge(new Edge(2, 3), m_SpriteMeshData.edges[1]);
            AssertEdge(new Edge(0, 3), m_SpriteMeshData.edges[2]);
            Assert.AreEqual(new Vector2(5f, 5f), m_SpriteMeshData.vertices[3].position, "Vertex position is incorrect");
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
            m_Triangulator.Received().Triangulate(Arg.Any<IList<Vector2>>(), Arg.Any<IList<Edge>>(), Arg.Any<IList<int>>());
        }

        [Test]
        public void CreateEdge_WithSingleSelectedVertex_WithNoHoveredVertex_WithNoHoveredEdge_WithMultipleIntersections_CreatesNewVertexAtClosestIntersection_CreatesNewEdgeAtIntersection_SplitsIntersectingEdge()
        {
            Vector2 position0 = Vector2.zero;
            Vector2 position1 = Vector2.up * 20f;
            Vector2 position2 = Vector2.right * 20f;
            Vector2 position3 = Vector2.up * 10f;
            Vector2 position4 = Vector2.right * 10f;
            Vector2 createPosition = Vector2.one * 20f;
            m_SpriteMeshDataController.CreateVertex(position0);
            m_SpriteMeshDataController.CreateVertex(position1);
            m_SpriteMeshDataController.CreateVertex(position2);
            m_SpriteMeshDataController.CreateVertex(position3);
            m_SpriteMeshDataController.CreateVertex(position4);
            m_SpriteMeshDataController.CreateEdge(1, 2);
            m_SpriteMeshDataController.CreateEdge(3, 4);

            m_HoveredVertex = -1;
            m_HoveredEdge = -1;
            m_MousePosition = createPosition;
            m_SelectedVertices.Add(0);

            m_View.DoCreateEdge().Returns(true);

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(6, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");
            Assert.AreEqual(4, m_SpriteMeshData.edges.Count, "Incorrect number of edges");
            AssertEdge(new Edge(1, 2), m_SpriteMeshData.edges[0]);
            AssertEdge(new Edge(3, 5), m_SpriteMeshData.edges[1]);
            AssertEdge(new Edge(4, 5), m_SpriteMeshData.edges[2]);
            AssertEdge(new Edge(0, 5), m_SpriteMeshData.edges[3]);
            Assert.AreEqual(new Vector2(5f, 5f), m_SpriteMeshData.vertices[5].position, "Vertex position is incorrect");
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
            m_Triangulator.Received().Triangulate(Arg.Any<IList<Vector2>>(), Arg.Any<IList<Edge>>(), Arg.Any<IList<int>>());
        }

        [Test]
        public void CreateEdge_WithSingleSelectedVertex_WithNoHoveredVertex_WithHoveredEdge_WithNoIntersection_CreatesNewVertex_CreatesNewEdge_SplitsHoveredEdge()
        {
            Vector2 position1 = Vector2.zero;
            Vector2 position2 = Vector2.up * 10f;
            Vector2 position3 = Vector2.right * 10f;
            Vector2 createPosition = new Vector2(4.9f, 4.9f);
            m_SpriteMeshDataController.CreateVertex(position1);
            m_SpriteMeshDataController.CreateVertex(position2);
            m_SpriteMeshDataController.CreateVertex(position3);
            m_SpriteMeshDataController.CreateEdge(1, 2);

            m_HoveredVertex = -1;
            m_HoveredEdge = 0;
            m_MousePosition = createPosition;
            m_SelectedVertices.Add(0);

            m_View.DoCreateEdge().Returns(true);

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(4, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");
            Assert.AreEqual(3, m_SpriteMeshData.edges.Count, "Incorrect number of edges");
            AssertEdge(new Edge(1, 3), m_SpriteMeshData.edges[0]);
            AssertEdge(new Edge(2, 3), m_SpriteMeshData.edges[1]);
            AssertEdge(new Edge(0, 3), m_SpriteMeshData.edges[2]);
            Assert.AreEqual(createPosition, m_SpriteMeshData.vertices[3].position, "Vertex position is incorrect");
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
            m_Triangulator.Received().Triangulate(Arg.Any<IList<Vector2>>(), Arg.Any<IList<Edge>>(), Arg.Any<IList<int>>());
        }

        [Test]
        public void CreateEdge_WithSingleSelectedVertex_WithNoHoveredVertex_WithHoveredEdge_WithIntersection_CreatesNewVertex_CreatesNewEdge_SplitsHoveredEdge()
        {
            Vector2 position1 = Vector2.zero;
            Vector2 position2 = Vector2.up * 10f;
            Vector2 position3 = Vector2.right * 10f;
            Vector2 createPosition = new Vector2(5.1f, 5.1f);
            m_SpriteMeshDataController.CreateVertex(position1);
            m_SpriteMeshDataController.CreateVertex(position2);
            m_SpriteMeshDataController.CreateVertex(position3);
            m_SpriteMeshDataController.CreateEdge(1, 2);

            m_HoveredVertex = -1;
            m_HoveredEdge = 0;
            m_MousePosition = createPosition;
            m_SelectedVertices.Add(0);

            m_View.DoCreateEdge().Returns(true);

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(4, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");
            Assert.AreEqual(3, m_SpriteMeshData.edges.Count, "Incorrect number of edges");
            AssertEdge(new Edge(1, 3), m_SpriteMeshData.edges[0]);
            AssertEdge(new Edge(2, 3), m_SpriteMeshData.edges[1]);
            AssertEdge(new Edge(0, 3), m_SpriteMeshData.edges[2]);
            Assert.AreEqual(new Vector2(5.0f, 5.0f), m_SpriteMeshData.vertices[3].position, "Vertex position is incorrect");
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
            m_Triangulator.Received().Triangulate(Arg.Any<IList<Vector2>>(), Arg.Any<IList<Edge>>(), Arg.Any<IList<int>>());
        }

        [Test]
        public void CreateEdge_WithSingleSelectedVertex_WithNoHoveredVertex_WithNoHoveredEdge_WithIntersectionCloseToSomeVertex_CreatesNewEdgeAtIntersectingVertex()
        {
            Vector2 position1 = Vector2.zero;
            Vector2 position2 = Vector2.up * 10f;
            Vector2 position3 = Vector2.right * 10f;
            Vector2 createPosition = Vector2.right * 20f;
            m_SpriteMeshDataController.CreateVertex(position1);
            m_SpriteMeshDataController.CreateVertex(position2);
            m_SpriteMeshDataController.CreateVertex(position3);
            m_SpriteMeshDataController.CreateEdge(1, 2);

            m_HoveredVertex = -1;
            m_HoveredEdge = -1;
            m_MousePosition = createPosition;
            m_SelectedVertices.Add(0);

            m_View.DoCreateEdge().Returns(true);

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(3, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");
            Assert.AreEqual(2, m_SpriteMeshData.edges.Count, "Incorrect number of edges");
            AssertEdge(new Edge(1, 2), m_SpriteMeshData.edges[0]);
            AssertEdge(new Edge(0, 2), m_SpriteMeshData.edges[1]);
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
            m_Triangulator.Received().Triangulate(Arg.Any<IList<Vector2>>(), Arg.Any<IList<Edge>>(), Arg.Any<IList<int>>());
        }

        [Test]
        public void CreateEdge_WithSingleSelectedVertex_WithHoveredVertex_WithNoHoveredEdge_WithIntersectionCloseToSomeVertex_CreatesNewEdgeAtIntersectingVertex()
        {
            Vector2 position1 = Vector2.zero;
            Vector2 position2 = Vector2.up * 10f;
            Vector2 position3 = Vector2.right * 10f;
            Vector2 createPosition = Vector2.right * 10.01f;
            m_SpriteMeshDataController.CreateVertex(position1);
            m_SpriteMeshDataController.CreateVertex(position2);
            m_SpriteMeshDataController.CreateVertex(position3);
            m_SpriteMeshDataController.CreateEdge(1, 2);

            m_HoveredVertex = 2;
            m_HoveredEdge = -1;
            m_MousePosition = createPosition;
            m_SelectedVertices.Add(0);

            m_View.DoCreateEdge().Returns(true);

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(3, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");
            Assert.AreEqual(2, m_SpriteMeshData.edges.Count, "Incorrect number of edges");
            AssertEdge(new Edge(1, 2), m_SpriteMeshData.edges[0]);
            AssertEdge(new Edge(0, 2), m_SpriteMeshData.edges[1]);
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
            m_Triangulator.Received().Triangulate(Arg.Any<IList<Vector2>>(), Arg.Any<IList<Edge>>(), Arg.Any<IList<int>>());
        }

        [Test]
        public void SplitEdge_CreatesVertexAtMousePosition_CreatesTwoEdges_InterpolatesWeights()
        {
            Vector2 position1 = Vector2.up * 10f;
            Vector2 position2 = Vector2.right * 10f;
            Vector2 createPosition = Vector2.zero;
            m_SpriteMeshDataController.CreateVertex(position1);
            m_SpriteMeshDataController.CreateVertex(position2);
            m_SpriteMeshDataController.CreateEdge(0, 1);

            m_SpriteMeshData.vertices[0].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { boneIndex0 = 0, weight0 = 1f });
            m_SpriteMeshData.vertices[1].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { boneIndex0 = 1, weight0 = 1f });

            m_SpriteMeshData.bones.Add(new SpriteBoneData());
            m_SpriteMeshData.bones.Add(new SpriteBoneData());

            m_HoveredVertex = -1;
            m_HoveredEdge = -1;
            m_ClosestEdge = 0;
            m_MousePosition = createPosition;

            m_View.DoSplitEdge().Returns(true);

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(3, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");
            Assert.AreEqual(2, m_SpriteMeshData.edges.Count, "Incorrect number of edges");
            AssertEdge(new Edge(0, 2), m_SpriteMeshData.edges[0]);
            AssertEdge(new Edge(1, 2), m_SpriteMeshData.edges[1]);
            Assert.AreEqual(createPosition, m_SpriteMeshData.vertices[2].position, "Vertex position is incorrect");

            var expected = new BoneWeight()
            {
                boneIndex0 = 0,
                boneIndex1 = 1,
                boneIndex2 = 0,
                boneIndex3 = 0,
                weight0 = 0.5f,
                weight1 = 0.5f,
                weight2 = 0f,
                weight3 = 0f
            };

            AssertBoneWeightContainsChannels(expected, m_SpriteMeshData.vertices[2].editableBoneWeight.ToBoneWeight(false));

            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
            m_Triangulator.Received().Triangulate(Arg.Any<IList<Vector2>>(), Arg.Any<IList<Edge>>(), Arg.Any<IList<int>>());
        }

        [Test]
        public void SelectEdge_WithAdditiveSelectionFalse_WithUnselectedHoveredEdge_ClearsSelection_SelectsHoveredEdge()
        {
            m_SpriteMeshDataController.CreateVertex(Vector2.zero);
            m_SpriteMeshDataController.CreateVertex(Vector2.one * 10f);
            m_SpriteMeshDataController.CreateEdge(0, 1);

            m_HoveredEdge = 0;

            bool additive;
            m_View.DoSelectEdge(out additive).Returns(x =>
                {
                    x[0] = false;
                    return true;
                });

            m_SpriteMeshController.OnGUI();

            m_Selection.Received(1).Clear();
            m_Selection.Received().Select(0, true);
            m_Selection.Received().Select(1, true);
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
        }

        [Test]
        public void SelectEdge_WithAdditiveSelectionTrue_WithSelectedHoveredEdge_DoesNotClearSelection_UnselectsHoveredEdge()
        {
            m_SpriteMeshDataController.CreateVertex(Vector2.zero);
            m_SpriteMeshDataController.CreateVertex(Vector2.one * 10f);
            m_SpriteMeshDataController.CreateEdge(0, 1);

            m_HoveredEdge = 0;

            m_SelectedVertices.Add(0);
            m_SelectedVertices.Add(1);

            bool additive;
            m_View.DoSelectEdge(out additive).Returns(x =>
                {
                    x[0] = true;
                    return true;
                });

            m_SpriteMeshController.OnGUI();

            m_Selection.DidNotReceive().Clear();
            m_Selection.Received().Select(0, false);
            m_Selection.Received().Select(1, false);
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
        }

        [Test]
        public void MoveEdge_MovesSelectedVertices()
        {
            Vector2 position1 = Vector2.zero;
            Vector2 position2 = Vector2.left;
            Vector2 position3 = Vector2.one;
            Vector2 position4 = Vector2.up;
            m_SpriteMeshDataController.CreateVertex(position1);
            m_SpriteMeshDataController.CreateVertex(position2);
            m_SpriteMeshDataController.CreateVertex(position3);
            m_SpriteMeshDataController.CreateVertex(position4);
            m_SpriteMeshDataController.CreateEdge(0, 1);

            m_SelectedVertices.Add(0);
            m_SelectedVertices.Add(1);
            m_SelectedVertices.Add(2);
            m_SelectedVertices.Add(3);

            Vector2 deltaPosition = Vector2.one * 10f;
            Vector2 delta;
            m_View.DoMoveEdge(out delta).Returns(x =>
                {
                    x[0] = deltaPosition;
                    return true;
                });
            m_View.IsActionTriggered(MeshEditorAction.MoveEdge).Returns( x => true );

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(4, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");
            Assert.AreEqual(position1 + deltaPosition, m_SpriteMeshData.vertices[0].position, "Vertex position is incorrect");
            Assert.AreEqual(position2 + deltaPosition, m_SpriteMeshData.vertices[1].position, "Vertex position is incorrect");
            Assert.AreEqual(position3 + deltaPosition, m_SpriteMeshData.vertices[2].position, "Vertex position is incorrect");
            Assert.AreEqual(position4 + deltaPosition, m_SpriteMeshData.vertices[3].position, "Vertex position is incorrect");
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
            m_Triangulator.Received().Triangulate(Arg.Any<IList<Vector2>>(), Arg.Any<IList<Edge>>(), Arg.Any<IList<int>>());
        }

        [Test]
        public void Remove_WithVerticesSelected_RemovesSelectedVertices()
        {
            Vector2 position1 = Vector2.zero;
            Vector2 position2 = Vector2.left;
            Vector2 position3 = Vector2.one;
            Vector2 position4 = Vector2.up;
            m_SpriteMeshDataController.CreateVertex(position1);
            m_SpriteMeshDataController.CreateVertex(position2);
            m_SpriteMeshDataController.CreateVertex(position3);
            m_SpriteMeshDataController.CreateVertex(position4);

            m_SelectedVertices.Add(0);
            m_SelectedVertices.Add(1);
            m_SelectedVertices.Add(2);

            m_View.DoRemove().Returns(true);

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(1, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");
            Assert.AreEqual(position4, m_SpriteMeshData.vertices[0].position, "Vertex position is incorrect");
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
            m_Triangulator.Received().Triangulate(Arg.Any<IList<Vector2>>(), Arg.Any<IList<Edge>>(), Arg.Any<IList<int>>());
        }

        [Test]
        public void Remove_WithEdgeVerticesSelected_RemovesEdgeAndKeepsVertices()
        {
            Vector2 position1 = Vector2.zero;
            Vector2 position2 = Vector2.left;
            m_SpriteMeshDataController.CreateVertex(position1);
            m_SpriteMeshDataController.CreateVertex(position2);
            m_SpriteMeshDataController.CreateEdge(0, 1);

            m_SelectedVertices.Add(0);
            m_SelectedVertices.Add(1);

            //Skip Remove selected vertices
            int counter = 0;
            m_View.DoRemove().Returns(x =>
                {
                    counter++;
                    return counter == 1;
                });

            m_SpriteMeshController.OnGUI();

            Assert.AreEqual(2, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices");
            Assert.AreEqual(position1, m_SpriteMeshData.vertices[0].position, "Vertex position is incorrect");
            Assert.AreEqual(position2, m_SpriteMeshData.vertices[1].position, "Vertex position is incorrect");
            m_CacheUndo.Received().BeginUndoOperation(Arg.Any<string>());
            m_Triangulator.Received().Triangulate(Arg.Any<IList<Vector2>>(), Arg.Any<IList<Edge>>(), Arg.Any<IList<int>>());
        }
    }
}
