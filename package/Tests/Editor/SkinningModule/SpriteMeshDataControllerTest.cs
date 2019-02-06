using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NSubstitute;
using UnityEditor;
using UnityEngine;
using UnityEditor.U2D;
using UnityEditor.U2D.Interface;
using UnityEngine.U2D.Interface;
using UnityEngine.Experimental.U2D;

namespace UnityEditor.Experimental.U2D.Animation.Test.SpriteMesh
{
    [TestFixture]
    public class SpriteMeshDataControllerTest
    {
        private SpriteMeshData m_SpriteMeshData;
        private SpriteMeshDataController m_SpriteMeshDataController = new SpriteMeshDataController();
        private List<int> m_SelectedVertices;
        private ISelection<int> m_Selection;

        [SetUp]
        public void Setup()
        {
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

            m_SpriteMeshData = new SpriteMeshData();
            m_SpriteMeshDataController.spriteMeshData = m_SpriteMeshData;
        }

        private void CreateTwoVerticesAndEdge(Vector2 v1, Vector2 v2)
        {
            m_SpriteMeshDataController.CreateVertex(v1);
            m_SpriteMeshDataController.CreateVertex(v2);
            m_SpriteMeshDataController.CreateEdge(m_SpriteMeshData.vertices.Count - 2, m_SpriteMeshData.vertices.Count - 1);
        }

        private void SplitEdge(int edgeIndex)
        {
            m_SpriteMeshDataController.CreateVertex(Vector2.zero, edgeIndex);
        }

        private void CreateTenVertices()
        {
            for (int i = 0; i < 10; ++i)
                m_SpriteMeshDataController.CreateVertex(Vector2.one * i);
        }

        [Test]
        public void CreateVertex_NewVertexAddedToVertexList_IncrementsVertexCount()
        {
            m_SpriteMeshDataController.CreateVertex(Vector2.zero);
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
            m_SpriteMeshDataController.CreateVertex(Vector2.zero);
            m_SpriteMeshDataController.RemoveVertex(0);
            Assert.AreEqual(0, m_SpriteMeshData.vertices.Count, "Should contain no vertices.");
        }

        [Test]
        public void RemoveCollectionOfVertices()
        {
            int[] vertIndices = new int[] {3, 8, 9, 5};

            CreateTenVertices();
            m_SpriteMeshDataController.CreateEdge(0, 1);
            m_SpriteMeshDataController.CreateEdge(1, 3);
            m_SpriteMeshDataController.CreateEdge(3, 4);
            m_SpriteMeshDataController.CreateEdge(2, 4);
            m_SpriteMeshDataController.CreateEdge(6, 7);
            m_SpriteMeshDataController.CreateEdge(8, 9);

            m_SpriteMeshDataController.RemoveVertex(vertIndices);

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

            m_SpriteMeshDataController.RemoveVertex(vertIndices);

            Assert.True(m_SpriteMeshData.vertices.Count == 10);
        }

        [Test]
        public void RemoveVertex_EdgesHaveHigherIndices_DecrementEdgeIndicesHigherThanVertexIndex()
        {
            m_SpriteMeshDataController.CreateVertex(Vector2.zero);
            CreateTwoVerticesAndEdge(Vector2.zero, Vector2.one);
            m_SpriteMeshDataController.RemoveVertex(0);
            Assert.AreEqual(2, m_SpriteMeshData.vertices.Count, "Should contain 2 vertices after removal.");
            Assert.AreEqual(1, m_SpriteMeshData.edges.Count, "Edge should not be removed.");
            Assert.AreEqual(new Edge(0, 1), m_SpriteMeshData.edges[0], "Edge indices should have decremented.");
        }

        [Test]
        public void RemoveVertex_WhereEdgeContainsVertexIndex_RemovesTheEdge()
        {
            CreateTwoVerticesAndEdge(Vector2.zero, Vector2.one);
            m_SpriteMeshDataController.RemoveVertex(0);
            Assert.AreEqual(1, m_SpriteMeshData.vertices.Count, "Vertex count after RemoveVertex in edge should return 1.");
            Assert.AreEqual(0, m_SpriteMeshData.edges.Count, "Edge count after RemoveVertex should return 0.");
        }

        [Test]
        public void RemoveVertex_WhereTwoEdgesShareVertexIndex_CreatesEdgeConnectingEndpoints()
        {
            CreateTwoVerticesAndEdge(Vector2.zero, Vector2.one);
            SplitEdge(0);
            m_SpriteMeshDataController.RemoveVertex(2);
            Assert.AreEqual(2, m_SpriteMeshData.vertices.Count, "GetVertexCount after RemoveVertex in edge should return 2");
            Assert.AreEqual(1, m_SpriteMeshData.edges.Count, "GetEdgeCount after RemoveVertex should return 1.");
            Assert.AreEqual(new Edge(0, 1), m_SpriteMeshData.edges[0], "The remaining edge should connect the remaining 2 vertices.");
        }

        [Test]
        public void RemoveVertex_WhereMoreThanTwoEdgesShareVertexIndex_RemoveEdgesContainingVertexIndex()
        {
            CreateTwoVerticesAndEdge(Vector2.zero, Vector2.one);
            SplitEdge(0);
            m_SpriteMeshDataController.CreateVertex(Vector2.right);
            m_SpriteMeshDataController.CreateEdge(3, 2);
            Assert.AreEqual(4, m_SpriteMeshData.vertices.Count, "Vertex count should return 4.");
            Assert.AreEqual(3, m_SpriteMeshData.edges.Count, "Edge count should return 3.");
            Assert.IsTrue(m_SpriteMeshData.edges.Contains(new Edge(2, 3)), "Edges should contain an edge connecting the last 2 vertices.");
            m_SpriteMeshDataController.RemoveVertex(2);
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
            m_SpriteMeshDataController.CreateEdge(0, 1);
            Assert.AreEqual(1, m_SpriteMeshData.edges.Count, "No duplicate edges should be allowed.");
        }

        [Test]
        public void CalculateWeights_FiltersSmallWeights()
        {
            float tolerance = 0.1f;

            IWeightsGenerator generator = Substitute.For<IWeightsGenerator>();

            m_SpriteMeshDataController.CreateVertex(Vector2.zero);

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

            generator.Calculate(Arg.Any<Vector2[]>(), Arg.Any<Edge[]>(), Arg.Any<Vector2[]>(), Arg.Any<Edge[]>(), Arg.Any<int[]>()).Returns(weigts);

            m_SpriteMeshDataController.CalculateWeights(generator, null, tolerance);

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

            m_SpriteMeshDataController.CreateVertex(Vector2.zero);
            m_SpriteMeshDataController.CreateVertex(Vector2.one);

            m_SpriteMeshData.vertices[0].editableBoneWeight = EditableBoneWeightUtility.CreateFromBoneWeight(new BoneWeight());
            m_SpriteMeshData.vertices[1].editableBoneWeight = EditableBoneWeightUtility.CreateFromBoneWeight(new BoneWeight() { weight0 = 0.5f });

            BoneWeight[] weigts = new BoneWeight[]
            {
                new BoneWeight() { weight0 = 1f },
                new BoneWeight() { weight0 = 1f }
            };

            generator.Calculate(Arg.Any<Vector2[]>(), Arg.Any<Edge[]>(), Arg.Any<Vector2[]>(), Arg.Any<Edge[]>(), Arg.Any<int[]>()).Returns(weigts);

            m_SpriteMeshDataController.CalculateWeightsSafe(generator, null, 0f);

            BoneWeight result1 = m_SpriteMeshData.vertices[0].editableBoneWeight.ToBoneWeight(false);
            BoneWeight result2 = m_SpriteMeshData.vertices[1].editableBoneWeight.ToBoneWeight(false);

            Assert.AreEqual(1f, result1.weight0, "Incorrect bone weight");
            Assert.AreEqual(0.5f, result2.weight0, "Incorrect bone weight");
        }

        [Test]
        public void NormalizeWeights()
        {
            m_SpriteMeshDataController.CreateVertex(Vector2.zero);

            m_SpriteMeshData.vertices[0].editableBoneWeight = EditableBoneWeightUtility.CreateFromBoneWeight(new BoneWeight() { weight0 = 0.1f });

            m_SpriteMeshDataController.NormalizeWeights(null);

            BoneWeight result = m_SpriteMeshData.vertices[0].editableBoneWeight.ToBoneWeight(false);

            Assert.AreEqual(1f, result.weight0, "Incorrect bone weight");
        }

        [Test]
        public void NormalizeWeights_WithSelection_NormalizeSelectedVertices()
        {
            m_SelectedVertices.Add(1);

            m_SpriteMeshDataController.CreateVertex(Vector2.zero);
            m_SpriteMeshDataController.CreateVertex(Vector2.zero);

            m_SpriteMeshData.vertices[0].editableBoneWeight = EditableBoneWeightUtility.CreateFromBoneWeight(new BoneWeight() { weight0 = 0.1f });
            m_SpriteMeshData.vertices[1].editableBoneWeight = EditableBoneWeightUtility.CreateFromBoneWeight(new BoneWeight() { weight0 = 0.1f });

            m_SpriteMeshDataController.NormalizeWeights(m_Selection);

            BoneWeight result0 = m_SpriteMeshData.vertices[0].editableBoneWeight.ToBoneWeight(false);
            BoneWeight result1 = m_SpriteMeshData.vertices[1].editableBoneWeight.ToBoneWeight(false);

            Assert.AreEqual(0.1f, result0.weight0, "Incorrect bone weight");
            Assert.AreEqual(1f, result1.weight0, "Incorrect bone weight");
        }

        [Test]
        public void GetMultiEditChannelData_WithNullSelection_TrowsException()
        {
            int boneIndex;
            float weight;
            bool channelEnabled, mixedChannelEnabled, mixedBoneIndex, mixedWeight;

            Assert.Throws<ArgumentNullException>(() => m_SpriteMeshDataController.GetMultiEditChannelData(null, 0, out channelEnabled, out boneIndex, out weight, out mixedChannelEnabled, out mixedBoneIndex, out mixedWeight));
        }

        [Test]
        public void GetMultiEditChannelData_WithNoMixedData_ReturnsFalseToMixedValues_ReturnsCommonBoneWeightData_ReturnsCommonChannelEnabledState()
        {
            m_SelectedVertices.Add(0);
            m_SelectedVertices.Add(1);

            m_SpriteMeshDataController.CreateVertex(Vector2.zero);
            m_SpriteMeshDataController.CreateVertex(Vector2.zero);

            m_SpriteMeshData.vertices[0].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { boneIndex0 = 0, boneIndex1 = 1, boneIndex2 = 2, boneIndex3 =  3, weight0 = 0.1f, weight1 = 0.2f, weight2 = 0.3f, weight3 = 0.4f });
            m_SpriteMeshData.vertices[1].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { boneIndex0 = 0, boneIndex1 = 1, boneIndex2 = 2, boneIndex3 =  3, weight0 = 0.1f, weight1 = 0.2f, weight2 = 0.3f, weight3 = 0.4f });

            int boneIndex;
            float weight;
            bool channelEnabled, mixedChannelEnabled, mixedBoneIndex, mixedWeight;

            m_SpriteMeshDataController.GetMultiEditChannelData(m_Selection, 0, out channelEnabled, out boneIndex, out weight, out mixedChannelEnabled, out mixedBoneIndex, out mixedWeight);

            Assert.True(channelEnabled, "Incorrect channel enabled state");
            Assert.False(mixedChannelEnabled, "Incorrect mixed value state");
            Assert.False(mixedBoneIndex, "Incorrect mixed value state");
            Assert.False(mixedWeight, "Incorrect mixed value state");
            Assert.AreEqual(0, boneIndex, "Incorrect mixed boneIndex");
            Assert.AreEqual(0.1f, weight, "Incorrect mixed boneWeight");
        }

        [Test]
        public void GetMultiEditChannelData_WithMixedChannelEnabled_ReturnsTrueToMixedChannelEnabled_ReturnsFalseChannelEnabledState()
        {
            m_SelectedVertices.Add(0);
            m_SelectedVertices.Add(1);

            m_SpriteMeshDataController.CreateVertex(Vector2.zero);
            m_SpriteMeshDataController.CreateVertex(Vector2.zero);

            m_SpriteMeshData.vertices[0].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { boneIndex0 = 0, boneIndex1 = 1, boneIndex2 = 2, boneIndex3 =  3, weight0 = 0.1f, weight1 = 0.2f, weight2 = 0.3f, weight3 = 0.4f });
            m_SpriteMeshData.vertices[1].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { boneIndex0 = 0, boneIndex1 = 1, boneIndex2 = 2, boneIndex3 =  3, weight0 = 0.1f, weight1 = 0.2f, weight2 = 0.3f, weight3 = 0.4f });
            m_SpriteMeshData.vertices[0].editableBoneWeight[0].enabled = false;

            int boneIndex;
            float weight;
            bool channelEnabled, mixedChannelEnabled, mixedBoneIndex, mixedWeight;

            m_SpriteMeshDataController.GetMultiEditChannelData(m_Selection, 0, out channelEnabled, out boneIndex, out weight, out mixedChannelEnabled, out mixedBoneIndex, out mixedWeight);

            Assert.False(channelEnabled, "Incorrect channel enabled state");
            Assert.True(mixedChannelEnabled, "Incorrect mixed value state");
        }

        [Test]
        public void GetMultiEditChannelData_WithMixedWeight_ReturnstrueToMixedWeight_ReturnsZeroWeight()
        {
            m_SelectedVertices.Add(0);
            m_SelectedVertices.Add(1);

            m_SpriteMeshDataController.CreateVertex(Vector2.zero);
            m_SpriteMeshDataController.CreateVertex(Vector2.zero);

            m_SpriteMeshData.vertices[0].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { boneIndex0 = 0, boneIndex1 = 1, boneIndex2 = 2, boneIndex3 =  3, weight0 = 0.1f, weight1 = 0.2f, weight2 = 0.3f, weight3 = 0.4f });
            m_SpriteMeshData.vertices[1].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { boneIndex0 = 0, boneIndex1 = 1, boneIndex2 = 2, boneIndex3 =  3, weight0 = 0f, weight1 = 0.2f, weight2 = 0.3f, weight3 = 0.4f });

            int boneIndex;
            float weight;
            bool channelEnabled, mixedChannelEnabled, mixedBoneIndex, mixedWeight;

            m_SpriteMeshDataController.GetMultiEditChannelData(m_Selection, 0, out channelEnabled, out boneIndex, out weight, out mixedChannelEnabled, out mixedBoneIndex, out mixedWeight);

            Assert.True(mixedWeight, "Incorrect mixed value state");
            Assert.AreEqual(0f, weight, "Incorrect mixed boneWeight");
        }

        [Test]
        public void GetMultiEditChannelData_WithMixedBoneIndex_ReturnsTrueToMixedBoneIndex_ReturnsInvalidBoneIndex()
        {
            m_SelectedVertices.Add(0);
            m_SelectedVertices.Add(1);

            m_SpriteMeshDataController.CreateVertex(Vector2.zero);
            m_SpriteMeshDataController.CreateVertex(Vector2.zero);

            m_SpriteMeshData.vertices[0].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { boneIndex0 = 0, boneIndex1 = 1, boneIndex2 = 2, boneIndex3 =  3, weight0 = 0.1f, weight1 = 0.2f, weight2 = 0.3f, weight3 = 0.4f });
            m_SpriteMeshData.vertices[1].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { boneIndex0 = 1, boneIndex1 = 1, boneIndex2 = 2, boneIndex3 =  3, weight0 = 0.1f, weight1 = 0.2f, weight2 = 0.3f, weight3 = 0.4f });

            int boneIndex;
            float weight;
            bool channelEnabled, mixedChannelEnabled, mixedBoneIndex, mixedWeight;

            m_SpriteMeshDataController.GetMultiEditChannelData(m_Selection, 0, out channelEnabled, out boneIndex, out weight, out mixedChannelEnabled, out mixedBoneIndex, out mixedWeight);

            Assert.True(mixedBoneIndex, "Incorrect mixed value state");
            Assert.AreEqual(-1, boneIndex, "Incorrect mixed boneIndex");
        }

        [Test]
        public void SetMultiEditChannelData_WithNullSelection_TrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => m_SpriteMeshDataController.SetMultiEditChannelData(null, 0, false, false, 0, 0, 1f, 1f));
        }

        [Test]
        public void SetMultiEditChannelData_SetsValuesToSelectedVertices_CompensatesOtherChannels()
        {
            m_SelectedVertices.Add(0);

            m_SpriteMeshDataController.CreateVertex(Vector2.zero);

            m_SpriteMeshData.vertices[0].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { boneIndex0 = 0, weight0 = 0f, weight1 = 1f });

            m_SpriteMeshDataController.SetMultiEditChannelData(m_Selection, 0, false, true, 0, 1, 0f, 1f);

            Assert.True(m_SpriteMeshData.vertices[0].editableBoneWeight[0].enabled, "Incorrect channel enabled state");
            Assert.AreEqual(1, m_SpriteMeshData.vertices[0].editableBoneWeight[0].boneIndex, "Incorrect bone index");
            Assert.AreEqual(1f, m_SpriteMeshData.vertices[0].editableBoneWeight[0].weight, "Incorrect weight");
            Assert.AreEqual(0f, m_SpriteMeshData.vertices[0].editableBoneWeight[1].weight, "Incorrect weight");
        }

        [Test]
        public void SortTrianglesByDept_TrianglesAreSortedBySpriteBoneDepth()
        {
            m_SpriteMeshDataController.CreateVertex(Vector2.zero);
            m_SpriteMeshDataController.CreateVertex(Vector2.zero);
            m_SpriteMeshData.vertices[0].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { weight0 = 1f });
            m_SpriteMeshData.vertices[1].editableBoneWeight.SetFromBoneWeight(new BoneWeight() { boneIndex0 = 1, weight0 = 1f });
            m_SpriteMeshData.indices.AddRange(new int[] { 0, 0, 0, 1, 1, 1 });
            m_SpriteMeshData.bones.Add(new SpriteBoneData() { depth = 1 });
            m_SpriteMeshData.bones.Add(new SpriteBoneData() { depth = -1 });

            m_SpriteMeshDataController.SortTrianglesByDepth();

            Assert.AreEqual(new int[] { 1, 1, 1, 0, 0, 0 }, m_SpriteMeshData.indices.ToArray(), "Incorrect triangles");
        }

        private static IEnumerable<TestCaseData> BoneMetadataCases()
        {
            var originalMetadata = new SpriteBone[5]
            {
                new SpriteBone()
                {
                    name = "root",
                    parentId = -1,
                    position = Vector2.one,
                    rotation = Quaternion.Euler(0.0f, 0.0f, 30.0f),
                    length = 1.0f
                },
                new SpriteBone()
                {
                    name = "child1",
                    parentId = 0,
                    position = Vector3.up,
                    rotation = Quaternion.Euler(0.0f, 0.0f, 60.0f),
                    length = 1.5f
                },
                new SpriteBone()
                {
                    name = "child2",
                    parentId = 1,
                    position = Vector3.right,
                    rotation = Quaternion.identity,
                    length = 1.5f
                },
                new SpriteBone()
                {
                    name = "child3",
                    parentId = 1,
                    position = Vector3.left,
                    rotation = Quaternion.Euler(0.0f, 0.0f, 120.0f),
                    length = 2.5f
                },
                new SpriteBone()
                {
                    name = "child4",
                    parentId = 3,
                    position = Vector3.up,
                    rotation = Quaternion.identity,
                    length = 1.0f
                }
            };

            var expectedPositions = new List<Vector2>();
            expectedPositions.Add(new Vector2(1f, 1f));
            expectedPositions.Add(new Vector2(0.5f, 1.8660f));
            expectedPositions.Add(new Vector2(0.5f, 2.8660f));
            expectedPositions.Add(new Vector2(0.5f, 0.8660f));
            expectedPositions.Add(new Vector2(1f, 0f));

            var expectedEndPositions = new List<Vector2>();
            expectedEndPositions.Add(new Vector2(1.8660f, 1.5f));
            expectedEndPositions.Add(new Vector2(0.5f, 3.3660f));
            expectedEndPositions.Add(new Vector2(0.5f, 4.3660f));
            expectedEndPositions.Add(new Vector2(-1.6650f, -0.383974f));
            expectedEndPositions.Add(new Vector2(0.133974f, -0.5f));

            var testcase = new TestCaseData(originalMetadata, expectedPositions, expectedEndPositions);
            testcase.SetName("Normal Hierarchical order");
            yield return testcase;

            originalMetadata = new SpriteBone[5]
            {
                new SpriteBone()
                {
                    name = "child4",
                    parentId = 1,
                    position = Vector3.up,
                    rotation = Quaternion.identity,
                    length = 1.0f
                },
                new SpriteBone()
                {
                    name = "child3",
                    parentId = 3,
                    position = Vector3.left,
                    rotation = Quaternion.Euler(0.0f, 0.0f, 120.0f),
                    length = 2.5f
                },
                new SpriteBone()
                {
                    name = "child2",
                    parentId = 3,
                    position = Vector3.right,
                    rotation = Quaternion.identity,
                    length = 1.5f
                },
                new SpriteBone()
                {
                    name = "child1",
                    parentId = 4,
                    position = Vector3.up,
                    rotation = Quaternion.Euler(0.0f, 0.0f, 60.0f),
                    length = 1.5f
                },
                new SpriteBone()
                {
                    name = "root",
                    parentId = -1,
                    position = Vector2.one,
                    rotation = Quaternion.Euler(0.0f, 0.0f, 30.0f),
                    length = 1.0f
                }
            };

            expectedPositions = new List<Vector2>();
            expectedPositions.Add(new Vector2(1f, 0f));
            expectedPositions.Add(new Vector2(0.5f, 0.8660f));
            expectedPositions.Add(new Vector2(0.5f, 2.8660f));
            expectedPositions.Add(new Vector2(0.5f, 1.8660f));
            expectedPositions.Add(new Vector2(1f, 1f));

            expectedEndPositions = new List<Vector2>();
            expectedEndPositions.Add(new Vector2(0.133974f, -0.5f));
            expectedEndPositions.Add(new Vector2(-1.6650f, -0.383974f));
            expectedEndPositions.Add(new Vector2(0.5f, 4.3660f));
            expectedEndPositions.Add(new Vector2(0.5f, 3.3660f));
            expectedEndPositions.Add(new Vector2(1.8660f, 1.5f));

            testcase = new TestCaseData(originalMetadata, expectedPositions, expectedEndPositions);
            testcase.SetName("Reversed Hierarchical order");
            yield return testcase;
        }

        [Test, TestCaseSource("BoneMetadataCases")]
        public void ConvertBoneMetadataToTextureSpace_RegardlessOfOrder(SpriteBone[] original, List<Vector2> expectedPositions, List<Vector2> expectedEndPosition)
        {
            List<SpriteBoneData> spriteBoneData = ModuleUtility.CreateSpriteBoneData(original, Matrix4x4.identity);

            VerifyApproximatedSpriteBones(spriteBoneData, expectedPositions, expectedEndPosition);
        }

        private static void VerifyApproximatedSpriteBones(List<SpriteBoneData> spriteBoneData, List<Vector2> expectedPosition, List<Vector2> expectedEndPosition)
        {
            const double kLooseEqual = 0.001;
            Assert.AreEqual(spriteBoneData.Count, expectedPosition.Count);
            Assert.AreEqual(spriteBoneData.Count, expectedEndPosition.Count);

            for (var i = 0; i < spriteBoneData.Count; ++i)
            {
                var boneData = spriteBoneData[i];
                var position = expectedPosition[i];
                var endPosition = expectedEndPosition[i];

                Assert.AreEqual(position.x, boneData.position.x, kLooseEqual, "Position X not matched at #{0}", i);
                Assert.AreEqual(position.y, boneData.position.y, kLooseEqual, "Position Y not matched at #{0}", i);
                Assert.AreEqual(endPosition.x, boneData.endPosition.x, kLooseEqual, "EndPosition X not matched at #{0}", i);
                Assert.AreEqual(endPosition.y, boneData.endPosition.y, kLooseEqual, "EndPosition Y not matched at #{0}", i);
            }
        }

        [Test]
        public void GenerateOutline_WithScaledDownTexture_ProducesVerticesAndEdges()
        {
            var path = "OutlineTests/scaled-down-square";

            var textureAsset = Resources.Load<Texture2D>(path);

            Assert.IsNotNull(textureAsset, "texture asset not found");

            var spriteEditorDataProvider = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(textureAsset)) as ISpriteEditorDataProvider;
            spriteEditorDataProvider.InitSpriteEditorDataProvider();

            var textureDataProvider = spriteEditorDataProvider.GetDataProvider<ITextureDataProvider>();
            var rects = spriteEditorDataProvider.GetSpriteRects();
            
            Assert.AreEqual(1, rects.Length, "Sprite rect not found");

            var spriteRect = rects[0];

            m_SpriteMeshData.frame = spriteRect.rect;
            m_SpriteMeshDataController.OutlineFromAlpha(new OutlineGenerator(), textureDataProvider, 0f, 0);

            Assert.AreEqual(27, m_SpriteMeshData.vertices.Count, "Incorrect number of vertices generated");
            Assert.AreEqual(27, m_SpriteMeshData.edges.Count, "Incorrect number of edges generated");
        }

        [Test]
        public void GetControlPoints_CreatesUniquePoints_CreatesBoneEdges_CreatesPinIndices()
        {
            m_SpriteMeshData.bones.Add( new SpriteBoneData()
            {
                length = 1f,
                localPosition = Vector2.zero,
                position = Vector2.zero,
                endPosition = Vector2.right
            });

            m_SpriteMeshData.bones.Add( new SpriteBoneData()
            {
                length = 1f,
                localPosition = Vector2.right,
                position = Vector2.right,
                endPosition = Vector2.right * 2f
            });

            m_SpriteMeshData.bones.Add( new SpriteBoneData()
            {
                length = 0f,
                localPosition = Vector2.right,
                position = Vector2.right * 3,
                endPosition = Vector2.right * 3f
            });

            Vector2[] vertices;
            Edge[] edges;
            int[] pins;

            m_SpriteMeshDataController.GetControlPoints(out vertices, out edges, out pins);

            Assert.AreEqual(4, vertices.Length, "Incorrect number of points");
            Assert.AreEqual(Vector2.zero, vertices[0], "Incorrect points");
            Assert.AreEqual(Vector2.right, vertices[1], "Incorrect points");
            Assert.AreEqual(Vector2.right * 2f, vertices[2], "Incorrect points");
            Assert.AreEqual(Vector2.right * 3f, vertices[3], "Incorrect points");
            Assert.AreEqual(2, edges.Length, "Incorrect number of edges");
            Assert.AreEqual(1, pins.Length, "Incorrect number of pins");
        }
    }
}
