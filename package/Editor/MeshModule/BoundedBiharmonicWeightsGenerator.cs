using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class BoundedBiharmonicWeightsGenerator : IWeightsGenerator
    {
        private const int kNumIterations = -1;
        private const float kDistancePerSample = 5f;
        private const int kMinSamples = 10;
        private const float kMeshAreaFactor = 0.0005f;

        [DllImport("BoundedBiharmonicWeightsModule")]
        private static extern int Bbw(int iterations,
            [In, Out] IntPtr vertices, int vertexCount, int originalVertexCount,
            [In, Out] IntPtr indices, int indexCount,
            [In, Out] IntPtr controlPoints, int controlPointsCount,
            [In, Out] IntPtr boneEdges, int boneEdgesCount,
            [In, Out] IntPtr weights
            );

        public BoneWeight[] Calculate(Vector2[] vertices, Edge[] edges, Vector2[] controlPoints, Edge[] controlPointEdges)
        {
            Vector2[] sampledEdges = SampleEdges(controlPoints, controlPointEdges, kDistancePerSample, kMinSamples);

            List<Vector2> verticesList = new List<Vector2>(vertices.Length + controlPoints.Length + sampledEdges.Length);
            List<Edge> edgesList = new List<Edge>(edges);
            List<int> indicesList = new List<int>();

            verticesList.AddRange(vertices);
            verticesList.AddRange(controlPoints);
            verticesList.AddRange(sampledEdges);

            TriangulationUtility.Tessellate(0f, 0f, kMeshAreaFactor, 0f, 0, verticesList, edgesList, indicesList);

            Vector2[] tessellatedVertices = verticesList.ToArray();
            int[] tessellatedIndices = indicesList.ToArray();

            BoneWeight[] weights = new BoneWeight[vertices.Length];

            GCHandle verticesHandle = GCHandle.Alloc(tessellatedVertices, GCHandleType.Pinned);
            GCHandle indicesHandle = GCHandle.Alloc(tessellatedIndices, GCHandleType.Pinned);
            GCHandle controlPointsHandle = GCHandle.Alloc(controlPoints, GCHandleType.Pinned);
            GCHandle boneEdgesHandle = GCHandle.Alloc(controlPointEdges, GCHandleType.Pinned);
            GCHandle weightsHandle = GCHandle.Alloc(weights, GCHandleType.Pinned);

            Bbw(kNumIterations,
                verticesHandle.AddrOfPinnedObject(), tessellatedVertices.Length, vertices.Length,
                indicesHandle.AddrOfPinnedObject(), tessellatedIndices.Length,
                controlPointsHandle.AddrOfPinnedObject(), controlPoints.Length,
                boneEdgesHandle.AddrOfPinnedObject(), controlPointEdges.Length,
                weightsHandle.AddrOfPinnedObject());

            verticesHandle.Free();
            indicesHandle.Free();
            controlPointsHandle.Free();
            boneEdgesHandle.Free();
            weightsHandle.Free();

            return weights;
        }

        private Vector2[] SampleEdges(Vector2[] points, Edge[] edges, float distancePerSample, int minSamples)
        {
            Debug.Assert(distancePerSample > 0f);

            List<Vector2> sampledEdges = new List<Vector2>();

            sampledEdges.AddRange(points);

            for (int i = 0; i < edges.Length; i++)
            {
                Edge edge = edges[i];

                Vector2 tip = points[edge.index1];
                Vector2 tail = points[edge.index2];
                float length = (tip - tail).magnitude;
                int samplesPerEdge = Mathf.Min((int)(length / distancePerSample), minSamples);

                for (int s = 0; s < samplesPerEdge; s++)
                {
                    float f = (s + 1f) / (float)(samplesPerEdge + 1f);
                    sampledEdges.Add(f * tail + (1f - f) * tip);
                }
            }

            return sampledEdges.ToArray();
        }
    }
}
