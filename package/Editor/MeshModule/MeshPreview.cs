using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.U2D;
using UnityEngine.Experimental.U2D;
using UnityEditor.Experimental.U2D;
using UnityEditor.Experimental.U2D.Animation;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class MeshPreview
    {
        private const int kNiceColorCount = 6;

        public SpriteMeshData spriteMeshData { get; set; }
        public ITextureDataProvider textureDataProvider { get; set; }
        public Matrix4x4[] localToWorldMatrices { get; set; }
        public float opacity { get; set; }
        public Mesh mesh { get { return m_Mesh; } }
        public Material meterial { get { return m_Material; } }
        public bool enableSkinning
        {
            get
            {
                return m_EnableSkinning;
            }

            set
            {
                if (m_EnableSkinning != value)
                {
                    m_EnableSkinning = value;
                    SetSkinningDirty();
                }
            }
        }

        private Mesh m_Mesh;
        private Material m_Material;
        private List<Vector3> m_Vertices = new List<Vector3>();
        private List<Vector3> m_SkinnedVertices = new List<Vector3>();
        private List<BoneWeight> m_Weights = new List<BoneWeight>();
        private List<Vector2> m_TexCoords = new List<Vector2>();
        private List<int> m_Indices = new List<int>();
        private List<Color> m_Colors = new List<Color>();
        private Matrix4x4[] m_BindPose;
        private List<Matrix4x4> m_SkinningMatrices = new List<Matrix4x4>();
        private bool m_MeshDirty;
        private bool m_VerticesDirty;
        private bool m_SkinningDirty;
        private bool m_WeightsDirty;
        private bool m_IndicesDirty;
        private bool m_BindPoseDirty;
        private bool m_EnableSkinning;
        private List<Vector3> vertices
        {
            get
            {
                if (enableSkinning)
                    return m_SkinnedVertices;

                return m_Vertices;
            }
        }

        public MeshPreview()
        {
            InitializeMesh();
            InitializeMaterials();
        }

        public void Dispose()
        {
            InvalidateMesh();
            InvalidateMaterial();
        }

        public void SetMeshDirty()
        {
            m_MeshDirty = true;
        }

        public void SetVerticesDirty()
        {
            m_VerticesDirty = true;
        }

        public void SetSkinningDirty()
        {
            m_SkinningDirty = true;
        }

        public void SetWeightsDirty()
        {
            m_WeightsDirty = true;
        }

        public void SetIndicesDirty()
        {
            m_IndicesDirty = true;
        }

        public void Prepare()
        {
            Debug.Assert(spriteMeshData != null);
            Debug.Assert(textureDataProvider != null);
            Debug.Assert(m_Material != null);

            m_IndicesDirty |= m_WeightsDirty;
            m_MeshDirty |= m_Vertices.Count != spriteMeshData.vertices.Count;

            if (m_MeshDirty)
            {
                m_Mesh.Clear();
                m_VerticesDirty = true;
                m_WeightsDirty = true;
                m_IndicesDirty = true;
                m_BindPoseDirty = true;
                m_MeshDirty = false;
            }

            if (m_BindPoseDirty)
            {
                UpdateBindPose();
                m_BindPoseDirty = false;
            }

            if (m_VerticesDirty)
            {
                m_Vertices.Clear();
                m_TexCoords.Clear();

                int width, height;
                textureDataProvider.GetTextureActualWidthAndHeight(out width, out height);

                var uvScale = new Vector2(1f / width, 1f / height);

                for (int i = 0; i < spriteMeshData.vertices.Count; ++i)
                {
                    var position = spriteMeshData.vertices[i].position;
                    m_Vertices.Add(position);
                    m_TexCoords.Add(Vector2.Scale(position, uvScale));
                }

                m_Mesh.SetVertices(m_Vertices);
                m_Mesh.SetUVs(0, m_TexCoords);
                m_VerticesDirty = false;
            }

            if (m_SkinningDirty)
            {
                if (enableSkinning)
                {
                    SkinVertices();
                    m_Mesh.SetVertices(m_SkinnedVertices);
                }

                m_SkinningDirty = false;
            }

            if (m_WeightsDirty)
            {
                m_Weights.Clear();

                for (int i = 0; i < spriteMeshData.vertices.Count; ++i)
                {
                    var vertex = spriteMeshData.vertices[i];
                    m_Weights.Add(vertex.editableBoneWeight.ToBoneWeight(true));
                }

                PrepareColors();

                m_Mesh.SetColors(m_Colors);
                m_WeightsDirty = false;
            }

            if (m_IndicesDirty)
            {
                spriteMeshData.SortTrianglesByDepth();

                m_Indices.Clear();

                for (int i = 0; i < spriteMeshData.indices.Count; ++i)
                {
                    int index = spriteMeshData.indices[i];
                    m_Indices.Add(index);
                }

                m_Mesh.SetTriangles(m_Indices, 0);
                m_IndicesDirty = false;
            }

            m_Material.mainTexture = textureDataProvider.texture;
            m_Material.SetFloat("_ColorOpacity", opacity);
        }

        private void InitializeMesh()
        {
            InvalidateMesh();

            m_Mesh = new Mesh();
            m_Mesh.MarkDynamic();
            m_Mesh.hideFlags = HideFlags.DontSave;
        }

        private void InitializeMaterials()
        {
            m_Material = new Material(Shader.Find("Hidden/MeshModule-GUITextureClip"));
            m_Material.hideFlags = HideFlags.DontSave;
        }

        public void InvalidateMesh()
        {
            if (m_Mesh)
                UnityEngine.Object.DestroyImmediate(m_Mesh);
        }

        private void InvalidateMaterial()
        {
            if (m_Material)
                UnityEngine.Object.DestroyImmediate(m_Material);
        }

        public void DrawTriangles()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            CommonDrawingUtility.DrawTriangleLines(vertices, m_Indices, 1f, Color.white.AlphaMultiplied(0.35f));
        }

        private void PrepareColors()
        {
            Debug.Assert(spriteMeshData != null);

            m_Colors.Clear();

            for (int i = 0; i < spriteMeshData.vertices.Count; ++i)
            {
                BoneWeight boneWeight = m_Weights[i];
                float weightSum = boneWeight.weight0 + boneWeight.weight1 + boneWeight.weight2 + boneWeight.weight3;

                var color = CommonDrawingUtility.CalculateNiceColor(boneWeight.boneIndex0, kNiceColorCount) * boneWeight.weight0 +
                    CommonDrawingUtility.CalculateNiceColor(boneWeight.boneIndex1, kNiceColorCount) * boneWeight.weight1 +
                    CommonDrawingUtility.CalculateNiceColor(boneWeight.boneIndex2, kNiceColorCount) * boneWeight.weight2 +
                    CommonDrawingUtility.CalculateNiceColor(boneWeight.boneIndex3, kNiceColorCount) * boneWeight.weight3;

                color.a = 1f;

                m_Colors.Add(Color.Lerp(Color.black, color, weightSum));
            }
        }

        private void UpdateBindPose()
        {
            MeshModuleUtility.UpdateLocalToWorldMatrices(spriteMeshData.bones, spriteMeshData.CalculateRootMatrix(), ref m_BindPose);

            for (int i = 0; i < m_BindPose.Length; ++i)
                m_BindPose[i] = m_BindPose[i].inverse;
        }

        private void SkinVertices()
        {
            Debug.Assert(localToWorldMatrices != null);
            Debug.Assert(m_BindPose != null);
            Debug.Assert(localToWorldMatrices.Length == m_BindPose.Length);

            var originMatrix = new Matrix4x4();
            originMatrix.SetTRS(spriteMeshData.pivot, Quaternion.identity, Vector3.one);
            var originInverseMatrix = originMatrix.inverse;

            m_SkinnedVertices.Clear();
            m_SkinningMatrices.Clear();

            for (int i = 0; i < localToWorldMatrices.Length; ++i)
                m_SkinningMatrices.Add(originInverseMatrix * localToWorldMatrices[i] * m_BindPose[i]);

            for (int i = 0; i < m_Vertices.Count; ++i)
            {
                var position = m_Vertices[i];
                BoneWeight boneWeight = m_Weights[i];
                float weightSum = boneWeight.weight0 + boneWeight.weight1 + boneWeight.weight2 + boneWeight.weight3;

                if (weightSum > 0f)
                {
                    position = m_SkinningMatrices[boneWeight.boneIndex0].MultiplyPoint3x4(position) * boneWeight.weight0 +
                        m_SkinningMatrices[boneWeight.boneIndex1].MultiplyPoint3x4(position) * boneWeight.weight1 +
                        m_SkinningMatrices[boneWeight.boneIndex2].MultiplyPoint3x4(position) * boneWeight.weight2 +
                        m_SkinningMatrices[boneWeight.boneIndex3].MultiplyPoint3x4(position) * boneWeight.weight3;

                    position = originMatrix.MultiplyPoint3x4(position);
                }

                m_SkinnedVertices.Add(position);
            }
        }
    }
}
