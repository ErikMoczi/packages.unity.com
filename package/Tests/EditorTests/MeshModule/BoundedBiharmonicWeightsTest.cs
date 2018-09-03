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
            Edge[] controlPointEdges;

            m_SpriteMeshData.GetControlPoints(out controlPoints, out controlPointEdges);

            Vector2[] vertices = new Vector2[m_SpriteMeshData.vertices.Count];

            for (int i = 0; i < m_SpriteMeshData.vertices.Count; ++i)
                vertices[i] = m_SpriteMeshData.vertices[i].position;

            BoundedBiharmonicWeightsGenerator generator = new BoundedBiharmonicWeightsGenerator();
            return generator.Calculate(vertices, m_SpriteMeshData.edges.ToArray(), controlPoints, controlPointEdges);
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
                    weight0 = 0.9069023f,
                    weight1 = 0.0930977f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.910151362f,
                    weight1 = 0.08984863f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.506713152f,
                    weight1 = 0.493286848f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.5069977f,
                    weight1 = 0.4930023f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.918600857f,
                    weight1 = 0.08139912f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.917430043f,
                    weight1 = 0.08256998f,
                    weight2 = 0f,
                    weight3 = 0f
                }
            };

            Assert.AreEqual(m_SpriteMeshData.vertices.Count, boneWeights.Length, "Weights count don't match vertex count");

            for (int i = 0; i < boneWeights.Length; ++i)
                AssertBoneWeight(expected[i], boneWeights[i]);
        }

        [Test]
        public void OneBoneOutsideMesh_ReturnZeroInfluences()
        {
            SpriteBoneData spriteBone = m_SpriteMeshData.bones[0];
            spriteBone.position += Vector2.right * 10f;
            m_SpriteMeshData.bones[0] = spriteBone;

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
                    weight0 = 0.91450727f,
                    weight1 = 0.0854927152f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.9148939f,
                    weight1 = 0.08510605f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.6955761f,
                    weight1 = 0.3044239f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.6959888f,
                    weight1 = 0.304011256f,
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
                    weight0 = 0.901687145f,
                    weight1 = 0.09831287f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 0,
                    boneIndex1 = 1,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.9040701f,
                    weight1 = 0.09592994f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.509135067f,
                    weight1 = 0.4908649f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.5067605f,
                    weight1 = 0.493239522f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.9211994f,
                    weight1 = 0.07880062f,
                    weight2 = 0f,
                    weight3 = 0f
                },
                new BoneWeight()
                {
                    boneIndex0 = 1,
                    boneIndex1 = 0,
                    boneIndex2 = 0,
                    boneIndex3 = 0,
                    weight0 = 0.9180592f,
                    weight1 = 0.08194084f,
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
