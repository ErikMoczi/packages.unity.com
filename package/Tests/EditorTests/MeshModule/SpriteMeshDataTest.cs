using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NSubstitute;
using UnityEditor;
using UnityEngine;
using UnityEditor.U2D;
using UnityEditor.U2D.Interface;
using UnityEngine.U2D.Interface;

namespace UnityEditor.Experimental.U2D.Animation.Test.MeshModule.SpriteMeshDataTest
{
    [TestFixture]
    public class SpriteMeshDataTest
    {
        private SpriteMeshData m_SpriteMeshData;

        [SetUp]
        public void Setup()
        {
            m_SpriteMeshData = new SpriteMeshData();
        }

        private void CreateTwoVerticesAndEdge(Vector2 v1, Vector2 v2)
        {
            m_SpriteMeshData.CreateVertex(v1);
            m_SpriteMeshData.CreateVertex(v2);
            m_SpriteMeshData.CreateEdge(m_SpriteMeshData.vertices.Count - 2, m_SpriteMeshData.vertices.Count - 1);
        }

        private void SplitEdge(int edgeIndex)
        {
            m_SpriteMeshData.CreateVertex(Vector2.zero, edgeIndex);
        }

        private void CreateTenVertices()
        {
            for (int i = 0; i < 10; ++i)
                m_SpriteMeshData.CreateVertex(Vector2.one * i);
        }

        [Test]
        public void CreateVertex_NewVertexAddedToVertexList_IncrementsVertexCount()
        {
            m_SpriteMeshData.CreateVertex(Vector2.zero);
            Assert.AreEqual(1, m_SpriteMeshData.vertices.Count, "Should contain 1 vertex.");
            Assert.AreEqual(Vector2.zero, m_SpriteMeshData.vertices[0].position, "Should contain a Vector2.zero vertex.");
        }

        [Test]
        public void CreateVertexAndSplitEdge_RemoveFirstEdge_CreatesTwoEdgesConnectingTheThreeVertices()
        {
            CreateTwoVerticesAndEdge(Vector2.zero, Vector2.one);
            SplitEdge(0);
            Assert.AreEqual(3, m_SpriteMeshData.vertices.Count, "Vertex count after CreateVertex in edge should return 3");
            Assert.AreEqual(2, m_SpriteMeshData.edges.Count, "Edge count after CreateVertex in edge should return 2");
            Assert.IsFalse(m_SpriteMeshData.edges.Contains(new Edge(0, 1)), "MeshStorage should not contain an edge from the first to the second");
            Assert.IsTrue(m_SpriteMeshData.edges.Contains(new Edge(0, 2)), "MeshStorage should contain an edge from the first vertex to the last");
            Assert.IsTrue(m_SpriteMeshData.edges.Contains(new Edge(1, 2)), "MeshStorage should contain an edge from the second vertex to the last");
        }

        [Test]
        public void RemoveVertex_RemovesVertexFromVertexList_DecrementsVertexCount()
        {
            m_SpriteMeshData.CreateVertex(Vector2.zero);
            m_SpriteMeshData.RemoveVertex(0);
            Assert.AreEqual(0, m_SpriteMeshData.vertices.Count, "Should contain no vertices.");
        }

        [Test]
        public void RemoveCollectionOfVertices()
        {
            int[] vertIndices = new int[] {3, 8, 9, 5};

            CreateTenVertices();
            m_SpriteMeshData.CreateEdge(0, 1);
            m_SpriteMeshData.CreateEdge(1, 3);
            m_SpriteMeshData.CreateEdge(3, 4);
            m_SpriteMeshData.CreateEdge(2, 4);
            m_SpriteMeshData.CreateEdge(6, 7);
            m_SpriteMeshData.CreateEdge(8, 9);

            m_SpriteMeshData.RemoveVertex(vertIndices);

            Assert.True(m_SpriteMeshData.vertices.Count == 6);
            Assert.True(m_SpriteMeshData.vertices[0].position == Vector2.zero);
            Assert.True(m_SpriteMeshData.vertices[1].position == Vector2.one);
            Assert.True(m_SpriteMeshData.vertices[2].position == Vector2.one * 2f);
            Assert.True(m_SpriteMeshData.vertices[3].position == Vector2.one * 4f);
            Assert.True(m_SpriteMeshData.vertices[4].position == Vector2.one * 6f);
            Assert.True(m_SpriteMeshData.vertices[5].position == Vector2.one * 7f);

            Assert.True(m_SpriteMeshData.edges.Count == 4);
            Assert.True(m_SpriteMeshData.edges.Contains(new Edge(0, 1)));
            Assert.True(m_SpriteMeshData.edges.Contains(new Edge(1, 3)));
            Assert.True(m_SpriteMeshData.edges.Contains(new Edge(2, 3)));
            Assert.True(m_SpriteMeshData.edges.Contains(new Edge(4, 5)));
        }

        [Test]
        public void RemoveEmptyCollectionOfVertices()
        {
            int[] vertIndices = new int[0];

            CreateTenVertices();

            m_SpriteMeshData.RemoveVertex(vertIndices);

            Assert.True(m_SpriteMeshData.vertices.Count == 10);
        }

        [Test]
        public void RemoveVertex_EdgesHaveHigherIndices_DecrementEdgeIndicesHigherThanVertexIndex()
        {
            m_SpriteMeshData.CreateVertex(Vector2.zero);
            CreateTwoVerticesAndEdge(Vector2.zero, Vector2.one);
            m_SpriteMeshData.RemoveVertex(0);
            Assert.AreEqual(2, m_SpriteMeshData.vertices.Count, "Should contain 2 vertices after removal.");
            Assert.AreEqual(1, m_SpriteMeshData.edges.Count, "Edge should not be removed.");
            Assert.AreEqual(new Edge(0, 1), m_SpriteMeshData.edges[0], "Edge indices should have decremented.");
        }

        [Test]
        public void RemoveVertex_WhereEdgeContainsVertexIndex_RemovesTheEdge()
        {
            CreateTwoVerticesAndEdge(Vector2.zero, Vector2.one);
            m_SpriteMeshData.RemoveVertex(0);
            Assert.AreEqual(1, m_SpriteMeshData.vertices.Count, "Vertex count after RemoveVertex in edge should return 1.");
            Assert.AreEqual(0, m_SpriteMeshData.edges.Count, "Edge count after RemoveVertex should return 0.");
        }

        [Test]
        public void RemoveVertex_WhereTwoEdgesShareVertexIndex_CreatesEdgeConnectingEndpoints()
        {
            CreateTwoVerticesAndEdge(Vector2.zero, Vector2.one);
            SplitEdge(0);
            m_SpriteMeshData.RemoveVertex(2);
            Assert.AreEqual(2, m_SpriteMeshData.vertices.Count, "GetVertexCount after RemoveVertex in edge should return 2");
            Assert.AreEqual(1, m_SpriteMeshData.edges.Count, "GetEdgeCount after RemoveVertex should return 1.");
            Assert.AreEqual(new Edge(0, 1), m_SpriteMeshData.edges[0], "The remaining edge should connect the remaining 2 vertices.");
        }

        [Test]
        public void RemoveVertex_WhereMoreThanTwoEdgesShareVertexIndex_RemoveEdgesContainingVertexIndex()
        {
            CreateTwoVerticesAndEdge(Vector2.zero, Vector2.one);
            SplitEdge(0);
            m_SpriteMeshData.CreateVertex(Vector2.right);
            m_SpriteMeshData.CreateEdge(3, 2);
            Assert.AreEqual(4, m_SpriteMeshData.vertices.Count, "Vertex count should return 4.");
            Assert.AreEqual(3, m_SpriteMeshData.edges.Count, "Edge count should return 3.");
            Assert.IsTrue(m_SpriteMeshData.edges.Contains(new Edge(2, 3)), "Edges should contain an edge connecting the last 2 vertices.");
            m_SpriteMeshData.RemoveVertex(2);
            Assert.AreEqual(3, m_SpriteMeshData.vertices.Count, "Vertex count after RemoveVertex in edge should return 3");
            Assert.AreEqual(0, m_SpriteMeshData.edges.Count, "Edge count after RemoveVertex should return 0.");
        }

        [Test]
        public void CreateEdge_AddsEdgeToMeshStorage_IncrementsEdgeCount()
        {
            CreateTwoVerticesAndEdge(Vector2.zero, Vector2.one);
            Assert.AreEqual(1, m_SpriteMeshData.edges.Count, "Edge count should increment.");
            Assert.IsTrue(m_SpriteMeshData.edges.Contains(new Edge(0, 1)), "A Edge(0,1) should be created");
        }

        [Test]
        public void CreateEdge_CannotAddDuplicateEdge()
        {
            CreateTwoVerticesAndEdge(Vector2.zero, Vector2.one);
            m_SpriteMeshData.CreateEdge(0, 1);
            Assert.AreEqual(1, m_SpriteMeshData.edges.Count, "No duplicate edges should be allowed.");
        }

        [Test]
        public void CalculateWeights_FiltersSmallWeights()
        {
            float tolerance = 0.1f;

            IWeightsGenerator generator = Substitute.For<IWeightsGenerator>();

            m_SpriteMeshData.CreateVertex(Vector2.zero);

            BoneWeight[] weigts = new BoneWeight[]
            {
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    weight0 = 0.05f,
                    weight1 = 0.95f
                }
            };

            generator.Calculate(Arg.Any<Vector2[]>(), Arg.Any<Edge[]>(), Arg.Any<Vector2[]>(), Arg.Any<Edge[]>()).Returns(weigts);

            m_SpriteMeshData.CalculateWeights(generator, null, tolerance);

            BoneWeight result = m_SpriteMeshData.vertices[0].editableBoneWeight.ToBoneWeight(false);

            Assert.AreEqual(0, result.boneIndex0, "Incorrect bone index");
            Assert.AreEqual(1, result.boneIndex1, "Incorrect bone index");
            Assert.AreEqual(0f, result.weight0, "Incorrect bone weight");
            Assert.AreEqual(1f, result.weight1, "Incorrect bone weight");
        }

        [Test]
        public void CalculateWeightsSafe_SetWeightsOnlyToVerticesWithoutInfluences()
        {
            IWeightsGenerator generator = Substitute.For<IWeightsGenerator>();

            m_SpriteMeshData.CreateVertex(Vector2.zero);
            m_SpriteMeshData.CreateVertex(Vector2.one);

            m_SpriteMeshData.vertices[0].editableBoneWeight = EditableBoneWeightUtility.CreateFromBoneWeight(new BoneWeight());
            m_SpriteMeshData.vertices[1].editableBoneWeight = EditableBoneWeightUtility.CreateFromBoneWeight(new BoneWeight() { weight0 = 0.5f });

            BoneWeight[] weigts = new BoneWeight[]
            {
                new BoneWeight() { weight0 = 1f },
                new BoneWeight() { weight0 = 1f }
            };

            generator.Calculate(Arg.Any<Vector2[]>(), Arg.Any<Edge[]>(), Arg.Any<Vector2[]>(), Arg.Any<Edge[]>()).Returns(weigts);

            m_SpriteMeshData.CalculateWeightsSafe(generator, null, 0f);

            BoneWeight result1 = m_SpriteMeshData.vertices[0].editableBoneWeight.ToBoneWeight(false);
            BoneWeight result2 = m_SpriteMeshData.vertices[1].editableBoneWeight.ToBoneWeight(false);

            Assert.AreEqual(1f, result1.weight0, "Incorrect bone weight");
            Assert.AreEqual(0.5f, result2.weight0, "Incorrect bone weight");
        }
    }
}
