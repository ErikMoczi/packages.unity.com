using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using UnityEngine.Experimental.U2D;
using UnityEditor.Experimental.U2D;
using UnityEditor.Experimental.U2D.Animation;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation.Test.Weights
{
    [TestFixture]
    internal class BoundedBiharmonicWeightsTest : WeightEditorTestBase
    {
        private void AppendIsolatedRegion()
        {
            BoneWeight boneWeight = default(BoneWeight);

            m_SpriteMeshData.vertices.Add(new Vertex2D(new Vector2(0f, -4f), boneWeight));
            m_SpriteMeshData.vertices.Add(new Vertex2D(new Vector2(0f, -2f), boneWeight));
            m_SpriteMeshData.vertices.Add(new Vertex2D(new Vector2(1f, -4f), boneWeight));
            m_SpriteMeshData.vertices.Add(new Vertex2D(new Vector2(1f, -2f), boneWeight));
            m_SpriteMeshData.vertices.Add(new Vertex2D(new Vector2(2f, -4f), boneWeight));
            m_SpriteMeshData.vertices.Add(new Vertex2D(new Vector2(2f, -2f), boneWeight));

            m_SpriteMeshData.edges.Add(new Edge(6, 7));
            m_SpriteMeshData.edges.Add(new Edge(7, 9));
            m_SpriteMeshData.edges.Add(new Edge(9, 11));
            m_SpriteMeshData.edges.Add(new Edge(11, 10));
            m_SpriteMeshData.edges.Add(new Edge(10, 8));
            m_SpriteMeshData.edges.Add(new Edge(8, 6));

            Triangulate();
        }

        private void SetAllVerticesToZero()
        {
            foreach (Vertex2D v in m_SpriteMeshData.vertices)
                v.position = Vector2.zero;
        }

        private BoneWeight[] CalculateWeights()
        {
            Vector2[] controlPoints;
            Edge[] bones;
            int[] pins;

            m_SpriteMeshDataController.GetControlPoints(out controlPoints, out bones, out pins);

            Vector2[] vertices = new Vector2[m_SpriteMeshData.vertices.Count];

            for (int i = 0; i < m_SpriteMeshData.vertices.Count; ++i)
                vertices[i] = m_SpriteMeshData.vertices[i].position;

            BoundedBiharmonicWeightsGenerator generator = new BoundedBiharmonicWeightsGenerator();
            return generator.Calculate(vertices, m_SpriteMeshData.edges.ToArray(), controlPoints, bones, pins);
        }

        [Test]
        public void NoVertices_ReturnsEmptyWeightsArray()
        {
            m_SpriteMeshData.vertices.Clear();

            BoneWeight[] boneWeights = CalculateWeights();

            Assert.AreEqual(0, boneWeights.Length, "Weights count don't match vertex count");
        }

        [Test]
        public void NoTriangles_ReturnsDefaultWeights()
        {
            var v0 = m_SpriteMeshData.vertices[0];
            var v1 = m_SpriteMeshData.vertices[1];
            m_SpriteMeshData.vertices.Clear();
            m_SpriteMeshData.vertices.Add(v0);
            m_SpriteMeshData.edges.Clear();
            m_SpriteMeshData.indices.Clear();

            BoneWeight[] boneWeights = CalculateWeights();

            Assert.AreEqual(1, boneWeights.Length, "Weights count don't match vertex count");
            AssertBoneWeightContainsChannels(BoundedBiharmonicWeightsGenerator.defaultWeight, boneWeights[0]);

            m_SpriteMeshData.vertices.Add(v1);

            boneWeights = CalculateWeights();

            Assert.AreEqual(2, boneWeights.Length, "Weights count don't match vertex count");
            AssertBoneWeightContainsChannels(BoundedBiharmonicWeightsGenerator.defaultWeight, boneWeights[0]);
            AssertBoneWeightContainsChannels(BoundedBiharmonicWeightsGenerator.defaultWeight, boneWeights[1]);
        }

        [Test]
        public void NoBones_ReturnsDefaultBoneWeight()
        {
            m_SpriteMeshData.bones.Clear();

            BoneWeight[] boneWeights = CalculateWeights();

            BoneWeight[] expected = new BoneWeight[] {
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight
            };

            Assert.AreEqual(m_SpriteMeshData.vertices.Count, boneWeights.Length, "Weights count don't match vertex count");

            for (int i = 0; i < boneWeights.Length; ++i)
                AssertBoneWeightContainsChannels(expected[i], boneWeights[i]);
        }

        [Test]
        [Ignore("TriangleNET will enter in infinite recursion")]
        public void AllVerticesZero_ReturnsDefaultBoneWeight()
        {
            SetAllVerticesToZero();

            BoneWeight[] boneWeights = CalculateWeights();

            BoneWeight[] expected = new BoneWeight[] {
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight
            };

            Assert.AreEqual(m_SpriteMeshData.vertices.Count, boneWeights.Length, "Weights count don't match vertex count");

            for (int i = 0; i < boneWeights.Length; ++i)
                AssertBoneWeightContainsChannels(expected[i], boneWeights[i]);
        }

        [Test]
        public void TwoBonesInsideMesh_CreatesInfluencesForBothBones()
        {
            BoneWeight[] boneWeights = CalculateWeights();
            
            BoneWeight[] expected = new BoneWeight[] {
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.9802052f,
                    weight1 = 0.01979485f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 1f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.53958863f,
                    weight1 = 0.46041137f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.51283294f,
                    weight1 = 0.48716706f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 1f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.9899667f,
                    weight1 = 0.0100333253f,
                    weight2 = 0f,
                    weight3 = 0f
                }
            };

            Assert.AreEqual(m_SpriteMeshData.vertices.Count, boneWeights.Length, "Weights count don't match vertex count");

            for (int i = 0; i < boneWeights.Length; ++i)
                AssertBoneWeightContainsChannels(expected[i], boneWeights[i]);
        }

        [Test]
        public void TwoPinsInsideMesh_CreatesInfluencesForBothBones()
        {
            m_SpriteMeshData.bones[0].length = 0f;
            m_SpriteMeshData.bones[0].endPosition = m_SpriteMeshData.bones[0].position;
            m_SpriteMeshData.bones[1].length = 0f;
            m_SpriteMeshData.bones[1].endPosition = m_SpriteMeshData.bones[1].position;

            BoneWeight[] boneWeights = CalculateWeights();

            BoneWeight[] expected = new BoneWeight[] {
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.6f,
                    weight1 = 0.4f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.6f,
                    weight1 = 0.4f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.733333349f,
                    weight1 = 0.266666681f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.733333349f,
                    weight1 = 0.266666681f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 1f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 1f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                }
            };

            Assert.AreEqual(m_SpriteMeshData.vertices.Count, boneWeights.Length, "Weights count don't match vertex count");

            for (int i = 0; i < boneWeights.Length; ++i)
                AssertBoneWeightContainsChannels(expected[i], boneWeights[i]);
        }

        [Test]
        public void VertexCloseToBone_ProducesWeights()
        {
            var bonePos = m_SpriteMeshData.bones[0].position;
            m_SpriteMeshData.vertices.Add(new Vertex2D(bonePos + Vector2.one * 0.001f, new BoneWeight()));

            BoneWeight[] boneWeights = CalculateWeights();

            BoneWeight[] expected = new BoneWeight[] {
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.983741939f,
                    weight1 = 0.0162580889f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 1f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.5396806f,
                    weight1 = 0.4603194f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.5133232f,
                    weight1 = 0.486676782f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 1f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.990049f,
                    weight1 = 0.009950991f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 1f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                }
            };

            Assert.AreEqual(m_SpriteMeshData.vertices.Count, boneWeights.Length, "Weights count don't match vertex count");

            for (int i = 0; i < boneWeights.Length; ++i)
                AssertBoneWeightContainsChannels(expected[i], boneWeights[i]);
        }

        [Test]
        public void OneBoneOutsideMesh_CreatesInfluencesOfTheOtherBones()
        {
            SpriteBoneData spriteBone = m_SpriteMeshData.bones[0];
            spriteBone.position += Vector2.right * 10f;
            spriteBone.endPosition += Vector2.right * 10f;
            m_SpriteMeshData.bones[0] = spriteBone;

            BoneWeight[] boneWeights = CalculateWeights();

            BoneWeight[] expected = new BoneWeight[] {
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 1f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 1f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 1f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 1f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 1f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 1f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                }
            };

            Assert.AreEqual(m_SpriteMeshData.vertices.Count, boneWeights.Length, "Weights count don't match vertex count");

            for (int i = 0; i < boneWeights.Length; ++i)
                AssertBoneWeightContainsChannels(expected[i], boneWeights[i]);
        }

        [Test]
        public void OneBonePartiallyOutsideMesh_CreatesInfluencesForBothBones()
        {
            SpriteBoneData spriteBone = m_SpriteMeshData.bones[0];
            spriteBone.position += Vector2.left * 0.5f;
            m_SpriteMeshData.bones[0] = spriteBone;

            BoneWeight[] boneWeights = CalculateWeights();
            
            BoneWeight[] expected = new BoneWeight[] {
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.9966453f,
                    weight1 = 0.00335471169f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.983102858f,
                    weight1 = 0.0168971363f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.5451388f,
                    weight1 = 0.454861224f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.5641289f,
                    weight1 = 0.435871124f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 1f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 1f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                }
            };

            Assert.AreEqual(m_SpriteMeshData.vertices.Count, boneWeights.Length, "Weights count don't match vertex count");

            for (int i = 0; i < boneWeights.Length; ++i)
                AssertBoneWeightContainsChannels(expected[i], boneWeights[i]);
        }

        [Test]
        public void TwoBonesOutsideMesh_ReturnsDefaultWeight()
        {
            SpriteBoneData spriteBone = m_SpriteMeshData.bones[0];
            spriteBone.position += Vector2.right * 10f;
            spriteBone.endPosition += Vector2.right * 10f;
            m_SpriteMeshData.bones[0] = spriteBone;

            spriteBone = m_SpriteMeshData.bones[1];
            spriteBone.position += Vector2.right * 10f;
            spriteBone.endPosition += Vector2.right * 10f;
            m_SpriteMeshData.bones[1] = spriteBone;

            BoneWeight[] boneWeights = CalculateWeights();

            BoneWeight[] expected = new BoneWeight[] {
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight
            };

            Assert.AreEqual(m_SpriteMeshData.vertices.Count, boneWeights.Length, "Weights count don't match vertex count");

            for (int i = 0; i < boneWeights.Length; ++i)
                AssertBoneWeightContainsChannels(expected[i], boneWeights[i]);
        }

        [Test]
        public void MeshWithIsolatedRegion_CreatesInfluencesInMainRegion_IsolatedVerticesGetDefaultWeights()
        {
            AppendIsolatedRegion();

            BoneWeight[] boneWeights = CalculateWeights();

            BoneWeight[] expected = new BoneWeight[] {
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.9763962f,
                    weight1 = 0.02360377f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 1f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.532606363f,
                    weight1 = 0.467393637f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.5407073f,
                    weight1 = 0.4592927f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.986137748f,
                    weight1 = 0.0138622392f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.971780956f,
                    weight1 = 0.0282190144f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight,
                BoundedBiharmonicWeightsGenerator.defaultWeight
            };

            Assert.AreEqual(m_SpriteMeshData.vertices.Count, boneWeights.Length, "Weights count don't match vertex count");

            for (int i = 0; i < boneWeights.Length; ++i)
                AssertBoneWeightContainsChannels(expected[i], boneWeights[i]);
        }
    }
}
