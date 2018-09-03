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

namespace UnityEditor.Experimental.U2D.Animation.Test.MeshModule.Weights
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

            m_SpriteMeshData.Triangulate(m_Triangulator);
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

            m_SpriteMeshData.GetControlPoints(out controlPoints, out bones, out pins);

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
                AssertBoneWeight(expected[i], boneWeights[i]);
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
                AssertBoneWeight(expected[i], boneWeights[i]);
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
                    weight0 = 0.969298065f,
                    weight1 = 0.0307019185f,
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
                    weight0 = 0.5343099f,
                    weight1 = 0.465690076f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.5561863f,
                    weight1 = 0.443813682f,
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
                AssertBoneWeight(expected[i], boneWeights[i]);
        }

        [Test]
        public void TwoPinsInsideMesh_CreatesInfluencesForBothBones()
        {
            m_SpriteMeshData.bones[0].length = 0f;
            m_SpriteMeshData.bones[1].length = 0f;

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
                AssertBoneWeight(expected[i], boneWeights[i]);
        }

        [Test]
        public void VertexCloseToBone_ProducesWeights()
        {
            var bonePos = m_SpriteMeshData.bones[0].position;
            m_SpriteMeshData.CreateVertex(bonePos + Vector2.one * 0.001f);

            BoneWeight[] boneWeights = CalculateWeights();

            BoneWeight[] expected = new BoneWeight[] {
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.988497853f,
                    weight1 = 0.0115021653f,
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
                    weight0 = 0.527753f,
                    weight1 = 0.472246975f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.517832756f,
                    weight1 = 0.482167244f,
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
                    weight0 = 0.9999997f,
                    weight1 = 2.8157632E-07f,
                    weight2 = 0f,
                    weight3 = 0f
                }
            };

            Assert.AreEqual(m_SpriteMeshData.vertices.Count, boneWeights.Length, "Weights count don't match vertex count");

            for (int i = 0; i < boneWeights.Length; ++i)
                AssertBoneWeight(expected[i], boneWeights[i]);
        }

        [Test]
        public void OneBoneOutsideMesh_CreatesInfluencesOfTheOtherBones()
        {
            SpriteBoneData spriteBone = m_SpriteMeshData.bones[0];
            spriteBone.position += Vector2.right * 10f;
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
                AssertBoneWeight(expected[i], boneWeights[i]);
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
                    weight0 = 0.9719688f,
                    weight1 = 0.0280311815f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.908445239f,
                    weight1 = 0.09155474f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.684992f,
                    weight1 = 0.315007955f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.739078164f,
                    weight1 = 0.260921866f,
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
                AssertBoneWeight(expected[i], boneWeights[i]);
        }

        [Test]
        public void TwoBonesOutsideMesh_ReturnsZeroInfluences()
        {
            SpriteBoneData spriteBone = m_SpriteMeshData.bones[0];
            spriteBone.position += Vector2.right * 10f;
            m_SpriteMeshData.bones[0] = spriteBone;

            spriteBone = m_SpriteMeshData.bones[1];
            spriteBone.position += Vector2.right * 10f;
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
                AssertBoneWeight(expected[i], boneWeights[i]);
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
                    weight0 = 0.9643655f,
                    weight1 = 0.03563449f,
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
                    weight0 = 0.624806464f,
                    weight1 = 0.375193536f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.5487788f,
                    weight1 = 0.4512212f,
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
                AssertBoneWeight(expected[i], boneWeights[i]);
        }
    }
}
