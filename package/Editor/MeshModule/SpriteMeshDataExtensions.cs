using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal static class SpriteMeshDataExtensions
    {
        public static void Triangulate(this SpriteMeshData spriteMeshData, ITriangulator triangulator)
        {
            Debug.Assert(triangulator != null);

            m_VerticesTemp.Clear();

            for (int i = 0; i < spriteMeshData.vertices.Count; ++i)
                m_VerticesTemp.Add(spriteMeshData.vertices[i].position);

            triangulator.Triangulate(m_VerticesTemp, spriteMeshData.edges, spriteMeshData.indices);
        }

        public static void Subdivide(this SpriteMeshData spriteMeshData, ITriangulator triangulator, float largestAreaFactor)
        {
            Debug.Assert(triangulator != null);

            m_VerticesTemp.Clear();

            for (int i = 0; i < spriteMeshData.vertices.Count; ++i)
                m_VerticesTemp.Add(spriteMeshData.vertices[i].position);

            triangulator.Tessellate(0f, 0f, 0f, largestAreaFactor, 100, m_VerticesTemp, spriteMeshData.edges, spriteMeshData.indices);

            spriteMeshData.vertices.Clear();
            for (int i = 0; i < m_VerticesTemp.Count; ++i)
                spriteMeshData.vertices.Add(new Vertex2D(m_VerticesTemp[i]));
        }

        public static void ClearWeights(this SpriteMeshData spriteMeshData, ISelection selection)
        {
            for (int i = 0; i < spriteMeshData.vertices.Count; ++i)
                if (selection == null || (selection.Count == 0 || selection.IsSelected(i)))
                    spriteMeshData.vertices[i].editableBoneWeight.SetFromBoneWeight(default(BoneWeight));
        }

        public static void OutlineFromAlpha(this SpriteMeshData spriteMeshData, IOutlineGenerator outlineGenerator, ITextureDataProvider textureDataProvider, float outlineDetail, byte alphaTolerance)
        {
            Debug.Assert(textureDataProvider != null);
            Debug.Assert(textureDataProvider.texture != null);

            int width, height;
            textureDataProvider.GetTextureActualWidthAndHeight(out width, out height);

            Vector2 scale = new Vector2(textureDataProvider.texture.width / (float)width, textureDataProvider.texture.height / (float)height);
            Vector2 scaleInv = new Vector2(1f / scale.x, 1f / scale.y);
            Vector2 rectOffset = spriteMeshData.frame.size * 0.5f + spriteMeshData.frame.position;

            Rect scaledRect = spriteMeshData.frame;
            scaledRect.min = Vector2.Scale(scaledRect.min, scale);
            scaledRect.max = Vector2.Scale(scaledRect.max, scale);

            spriteMeshData.vertices.Clear();
            spriteMeshData.edges.Clear();

            Vector2[][] paths;
            outlineGenerator.GenerateOutline(textureDataProvider.texture, spriteMeshData.frame, outlineDetail, alphaTolerance, false, out paths);

            int vertexIndexBase = 0;
            for (int i = 0; i < paths.Length; ++i)
            {
                int numPathVertices = paths[i].Length;

                for (int j = 0; j <= numPathVertices; j++)
                {
                    if (j < numPathVertices)
                        spriteMeshData.vertices.Add(new Vertex2D(Vector2.Scale(paths[i][j], scaleInv) + rectOffset));

                    if (j > 0)
                        spriteMeshData.edges.Add(new Edge(vertexIndexBase + j - 1, vertexIndexBase + j % numPathVertices));
                }

                vertexIndexBase += numPathVertices;
            }
        }

        public static void NormalizeWeights(this SpriteMeshData spriteMeshData, ISelection selection)
        {
            for (int i = 0; i < spriteMeshData.vertices.Count; ++i)
                if (selection == null || (selection.Count == 0 || selection.IsSelected(i)))
                    spriteMeshData.vertices[i].editableBoneWeight.NormalizeChannels();
        }

        public static void CalculateWeightsSafe(this SpriteMeshData spriteMeshData, IWeightsGenerator weightsGenerator, ISelection selection, float filterTolerance)
        {
            SerializableSelection tempSelection = new SerializableSelection();
            GenericSelector<Vertex2D> vertexSelector = new GenericSelector<Vertex2D>();

            vertexSelector.items = spriteMeshData.vertices;
            vertexSelector.selection = tempSelection;
            vertexSelector.Filter = (int i) => {
                    return vertexSelector.items[i].editableBoneWeight.GetWeightSum() == 0f && (selection == null || selection.Count == 0 || selection.IsSelected(i));
                };
            vertexSelector.Select();

            if (tempSelection.Count > 0)
                spriteMeshData.CalculateWeights(weightsGenerator, tempSelection, filterTolerance);
        }

        public static void CalculateWeights(this SpriteMeshData spriteMeshData, IWeightsGenerator weightsGenerator, ISelection selection, float filterTolerance)
        {
            Vector2[] controlPoints;
            Edge[] controlPointEdges;

            spriteMeshData.GetControlPoints(out controlPoints, out controlPointEdges);

            Vector2[] vertices = new Vector2[spriteMeshData.vertices.Count];

            for (int i = 0; i < spriteMeshData.vertices.Count; ++i)
                vertices[i] = spriteMeshData.vertices[i].position;

            BoneWeight[] boneWeights = weightsGenerator.Calculate(vertices, spriteMeshData.edges.ToArray(), controlPoints, controlPointEdges);

            Debug.Assert(boneWeights.Length == spriteMeshData.vertices.Count);

            for (int i = 0; i < spriteMeshData.vertices.Count; ++i)
            {
                if (selection == null || (selection.Count == 0 || selection.IsSelected(i)))
                {
                    EditableBoneWeight editableBoneWeight = EditableBoneWeightUtility.CreateFromBoneWeight(boneWeights[i]);

                    if (filterTolerance > 0f)
                    {
                        editableBoneWeight.FilterChannels(filterTolerance);
                        editableBoneWeight.NormalizeChannels();
                    }

                    spriteMeshData.vertices[i].editableBoneWeight = editableBoneWeight;
                }
            }
        }

        public static bool FindTriangle(this SpriteMeshData spriteMeshData, Vector2 point, out Vector3Int indices, out Vector3 barycentricCoords)
        {
            indices = Vector3Int.zero;
            barycentricCoords = Vector3.zero;

            if (spriteMeshData.indices.Count < 3)
                return false;

            int triangleCount = spriteMeshData.indices.Count / 3;

            for (int i = 0; i < triangleCount; ++i)
            {
                indices.x = spriteMeshData.indices[i * 3];
                indices.y = spriteMeshData.indices[i * 3 + 1];
                indices.z = spriteMeshData.indices[i * 3 + 2];

                MeshModuleUtility.Barycentric(
                    point,
                    spriteMeshData.vertices[indices.x].position,
                    spriteMeshData.vertices[indices.y].position,
                    spriteMeshData.vertices[indices.z].position,
                    out barycentricCoords);

                if (barycentricCoords.x >= 0f && barycentricCoords.y >= 0f && barycentricCoords.z >= 0f)
                    return true;
            }

            return false;
        }

        public static void GetMultiEditChannelData(this SpriteMeshData spriteMeshData, ISelection selection, int channelIndex,
            out bool channelEnabled, out BoneWeightData boneWeightData,
            out bool isChannelEnabledMixed, out bool isBoneIndexMixed, out bool isWeightMixed)
        {
            if (selection == null)
                throw new ArgumentNullException("selection is null");

            bool first = true;
            channelEnabled = false;
            boneWeightData = new BoneWeightData();
            isChannelEnabledMixed = false;
            isBoneIndexMixed = false;
            isWeightMixed = false;

            foreach (int i in selection)
            {
                EditableBoneWeight editableBoneWeight = spriteMeshData.vertices[i].editableBoneWeight;

                BoneWeightData otherBoneWeightData = editableBoneWeight.GetBoneWeightData(channelIndex);

                if (first)
                {
                    channelEnabled = editableBoneWeight.IsChannelEnabled(channelIndex);
                    boneWeightData.boneIndex = otherBoneWeightData.boneIndex;
                    boneWeightData.weight = otherBoneWeightData.weight;

                    first = false;
                }
                else
                {
                    if (channelEnabled != editableBoneWeight.IsChannelEnabled(channelIndex))
                    {
                        isChannelEnabledMixed = true;
                        channelEnabled = false;
                    }

                    if (boneWeightData.boneIndex != otherBoneWeightData.boneIndex)
                    {
                        isBoneIndexMixed = true;
                        boneWeightData.boneIndex = -1;
                    }

                    if (boneWeightData.weight != otherBoneWeightData.weight)
                    {
                        isWeightMixed = true;
                        boneWeightData.weight = 0f;
                    }
                }
            }
        }

        public static void SetMultiEditChannelData(this SpriteMeshData spriteMeshData, ISelection selection, int channelIndex,
            bool referenceChannelEnabled, bool newChannelEnabled,  BoneWeightData referenceData, BoneWeightData newData)
        {
            if (selection == null)
                throw new ArgumentNullException("selection is null");

            bool channelEnabledChanged = referenceChannelEnabled != newChannelEnabled;
            bool boneIndexChanged = referenceData.boneIndex != newData.boneIndex;
            bool weightChanged = referenceData.weight != newData.weight;

            foreach (int i in selection)
            {
                EditableBoneWeight editableBoneWeight = spriteMeshData.vertices[i].editableBoneWeight;
                BoneWeightData data = editableBoneWeight.GetBoneWeightData(channelIndex);

                if (channelEnabledChanged)
                    editableBoneWeight.EnableChannel(channelIndex, newChannelEnabled);

                if (boneIndexChanged)
                    data.boneIndex = newData.boneIndex;

                if (weightChanged)
                    data.weight = newData.weight;

                editableBoneWeight.SetBoneWeightData(channelIndex, data);

                if (channelEnabledChanged || weightChanged)
                    editableBoneWeight.CompensateOtherChannels(channelIndex);
            }
        }

        public static void GetControlPoints(this SpriteMeshData spriteMeshData, out Vector2[] points, out Edge[] edges)
        {
            points = null;
            edges = null;

            List<Vector2> pointList = new List<Vector2>();
            List<Edge> edgeList = new List<Edge>();

            foreach (var bone in spriteMeshData.bones)
            {
                if (bone.length > 0f)
                {
                    Vector2 endPosition = bone.position + bone.rotation * Vector3.right * bone.length * 0.99f;
                    int index1 = FindPoint(pointList, bone.position, 0.01f);
                    int index2 = FindPoint(pointList, endPosition, 0.01f);

                    if (index1 == -1)
                    {
                        pointList.Add(bone.position);
                        index1 = pointList.Count - 1;
                    }

                    if (index2 == -1)
                    {
                        pointList.Add(endPosition);
                        index2 = pointList.Count - 1;
                    }

                    edgeList.Add(new Edge(index1, index2));
                }
                else
                {
                    pointList.Add(bone.position);
                }
            }

            points = pointList.ToArray();
            edges = edgeList.ToArray();
        }

        private static int FindPoint(List<Vector2> points, Vector2 point, float distanceTolerance)
        {
            float sqrTolerance = distanceTolerance * distanceTolerance;

            for (int i = 0; i < points.Count; ++i)
            {
                if ((points[i] - point).sqrMagnitude <= sqrTolerance)
                    return i;
            }

            return -1;
        }

        private static readonly List<Vector2> m_VerticesTemp = new List<Vector2>();
    }
}
