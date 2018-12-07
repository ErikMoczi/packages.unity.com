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
        public void NoBones_ReturnsDefaultBoneWeight()
        {
            m_SpriteMeshData.bones.Clear();

            BoneWeight[] boneWeights = CalculateWeights();

            BoneWeight[] expected = new BoneWeight[] {
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0f,
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
        public void AllVerticesZero_ReturnsDefaultBoneWeight()
        {
            SetAllVerticesToZero();

            BoneWeight[] boneWeights = CalculateWeights();

            BoneWeight[] expected = new BoneWeight[] {
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0f,
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
                    weight0 = 0.9956115f,
                    weight1 = 0.00438851f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.9934362f,
                    weight1 = 0.0065637636f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.509042561f,
                    weight1 = 0.490957439f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.515595f,
                    weight1 = 0.484404951f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.999267042f,
                    weight1 = 0.0007329605f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.9965891f,
                    weight1 = 0.00341090187f,
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
                    weight0 = 0.994602442f,
                    weight1 = 0.0053975787f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.99406594f,
                    weight1 = 0.00593405f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.518201768f,
                    weight1 = 0.481798232f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.546529949f,
                    weight1 = 0.453470081f,
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
                    weight0 = 0.9967752f,
                    weight1 = 0.0032248057f,
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
                    weight0 = 0.5409105f,
                    weight1 = 0.459089518f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.564954f,
                    weight1 = 0.435046f,
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
        public void TwoBonesOutsideMesh_ReturnsZeroInfluences()
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
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0f,
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
                    weight0 = 0f,
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
                    weight0 = 0f,
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
                    weight0 = 0f,
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
                    weight0 = 0f,
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
                    weight0 = 0f,
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
        public void MeshWithIsolatedRegion_CreatesInfluencesInMainRegion_IsolatedVerticesGetZeroWeights()
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
                    weight0 = 0.99503696f,
                    weight1 = 0.00496303709f,
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
                    weight0 = 0.534442127f,
                    weight1 = 0.465557873f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.584795415f,
                    weight1 = 0.415204585f,
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
                    weight0 = 0.990874708f,
                    weight1 = 0.00912532024f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0f,
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
                    weight0 = 0f,
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
                    weight0 = 0f,
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
                    weight0 = 0f,
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
                    weight0 = 0f,
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
                    weight0 = 0f,
                    weight1 = 0f,
                    weight2 = 0f,
                    weight3 = 0f
                }
            };

            Assert.AreEqual(m_SpriteMeshData.vertices.Count, boneWeights.Length, "Weights count don't match vertex count");

            for (int i = 0; i < boneWeights.Length; ++i)
                AssertBoneWeightContainsChannels(expected[i], boneWeights[i]);
        }
    }
}
