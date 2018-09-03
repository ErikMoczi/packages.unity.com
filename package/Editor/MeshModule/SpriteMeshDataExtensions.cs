using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    public class WeightedTriangle
    {
        int m_P1;
        int m_P2;
        int m_P3;
        float m_Weight;

        public int p1 { get { return m_P1; } }
        public int p2 { get { return m_P2; } }
        public int p3 { get { return m_P3; } }
        public float weight { get { return m_Weight; } }

        public WeightedTriangle(int _p1, int _p2, int _p3, float _weight)
        {
            m_P1 = _p1;
            m_P2 = _p2;
            m_P3 = _p3;
            m_Weight = _weight;
        }
    }

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
            outlineGenerator.GenerateOutline(textureDataProvider.texture, scaledRect, outlineDetail, alphaTolerance, false, out paths);

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
            Edge[] bones;
            int[] pins;

            spriteMeshData.GetControlPoints(out controlPoints, out bones, out pins);

            Vector2[] vertices = new Vector2[spriteMeshData.vertices.Count];

            for (int i = 0; i < spriteMeshData.vertices.Count; ++i)
                vertices[i] = spriteMeshData.vertices[i].position;

            BoneWeight[] boneWeights = weightsGenerator.Calculate(vertices, spriteMeshData.edges.ToArray(), controlPoints, bones, pins);

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

        public static Matrix4x4 CalculateRootMatrix(this SpriteMeshData spriteMeshData)
        {
            Matrix4x4 rootMatrix = new Matrix4x4();
            rootMatrix.SetTRS(spriteMeshData.frame.position, Quaternion.identity, Vector3.one);
            return rootMatrix;
        }

        public static void UpdateSpriteBoneDataWorldPosition(this SpriteMeshData spriteMeshData, Matrix4x4[] localToWorldMatrices)
        {
            Debug.Assert(spriteMeshData.bones.Count == localToWorldMatrices.Length);

            for (int i = 0; i < spriteMeshData.bones.Count; ++i)
            {
                var spriteBoneData = spriteMeshData.bones[i];
                spriteBoneData.position = localToWorldMatrices[i].MultiplyPoint(Vector2.zero);
                spriteBoneData.endPosition = localToWorldMatrices[i].MultiplyPoint(Vector2.right * spriteBoneData.length);
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

        public static void GetControlPoints(this SpriteMeshData spriteMeshData, out Vector2[] points, out Edge[] edges, out int[] pins)
        {
            points = null;
            edges = null;

            List<Vector2> pointList = new List<Vector2>();
            List<Edge> edgeList = new List<Edge>();
            List<int> pinList = new List<int>();

            foreach (var bone in spriteMeshData.bones)
            {
                if (bone.length > 0f)
                {
                    Vector2 endPosition = bone.position + (bone.endPosition - bone.position).normalized * bone.length * 0.99f;

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
                else if (bone.length == 0f)
                {
                    pointList.Add(bone.position);
                    pinList.Add(pointList.Count - 1);
                }
            }

            points = pointList.ToArray();
            edges = edgeList.ToArray();
            pins = pinList.ToArray();
        }

        public static void SortTrianglesByDepth(this SpriteMeshData spriteMeshData)
        {
            List<float> vertexOrderList = new List<float>(spriteMeshData.vertices.Count);

            for (int i = 0; i < spriteMeshData.vertices.Count; i++)
            {
                float vertexOrder = 0;

                if (spriteMeshData.bones.Count > 0)
                {
                    BoneWeight boneWeight = spriteMeshData.vertices[i].editableBoneWeight.ToBoneWeight(false);
                    float orderBone0 = spriteMeshData.bones[boneWeight.boneIndex0].depth;
                    float orderBone1 = spriteMeshData.bones[boneWeight.boneIndex1].depth;
                    float orderBone2 = spriteMeshData.bones[boneWeight.boneIndex2].depth;
                    float orderBone3 = spriteMeshData.bones[boneWeight.boneIndex3].depth;

                    vertexOrder = orderBone0 * boneWeight.weight0 + orderBone1 * boneWeight.weight1 + orderBone2 * boneWeight.weight2 + orderBone3 * boneWeight.weight3;
                }

                vertexOrderList.Add(vertexOrder);
            }

            List<WeightedTriangle> weightedTriangles = new List<WeightedTriangle>(spriteMeshData.indices.Count / 3);

            for (int i = 0; i < spriteMeshData.indices.Count; i += 3)
            {
                int p1 = spriteMeshData.indices[i];
                int p2 = spriteMeshData.indices[i + 1];
                int p3 = spriteMeshData.indices[i + 2];
                float weight = (vertexOrderList[p1] + vertexOrderList[p2] + vertexOrderList[p3]) / 3f;

                weightedTriangles.Add(new WeightedTriangle(p1, p2, p3, weight));
            }

            weightedTriangles = weightedTriangles.OrderBy(t => t.weight).ToList();

            spriteMeshData.indices.Clear();

            for (int i = 0; i < weightedTriangles.Count; ++i)
            {
                WeightedTriangle triangle = weightedTriangles[i];
                spriteMeshData.indices.Add(triangle.p1);
                spriteMeshData.indices.Add(triangle.p2);
                spriteMeshData.indices.Add(triangle.p3);
            }
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
