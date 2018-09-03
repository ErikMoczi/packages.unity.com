using UnityEditorInternal;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D.Interface;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Experimental.U2D.TriangleNet;
using UnityEngine.Experimental.U2D.TriangleNet.Geometry;
using UnityEngine.Experimental.U2D.TriangleNet.Meshing;
using UnityEngine.Experimental.U2D.TriangleNet.Topology;
using UnityEngine.Experimental.U2D.TriangleNet.Tools;
using UnityEngine.Experimental.U2D.TriangleNet.Smoothing;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class TriangulationUtility
    {
        public static void Triangulate(IList<Vector2> vertices, IList<Edge> edges, IList<int> indices)
        {
            indices.Clear();

            if (vertices.Count < 3)
                return;

            var polygon = new Polygon(vertices.Count);

            for (int i = 0; i < vertices.Count; ++i)
            {
                Vector2 position = vertices[i];
                polygon.Add(new Vertex(position.x, position.y));
            }

            for (int i = 0; i < edges.Count; ++i)
            {
                Edge edge = edges[i];
                polygon.Add(new Segment(polygon.Points[edge.index1], polygon.Points[edge.index2]));
            }

            var mesh = polygon.Triangulate();

            foreach (ITriangle triangle in mesh.Triangles)
            {
                int id0 = triangle.GetVertexID(0);
                int id1 = triangle.GetVertexID(1);
                int id2 = triangle.GetVertexID(2);

                if (id0 < 0 || id1 < 0 || id2 < 0 ||  id0 >= vertices.Count || id1 >= vertices.Count || id2 >= vertices.Count)
                    continue;

                indices.Add(id0);
                indices.Add(id2);
                indices.Add(id1);
            }
        }

        public static void Tessellate(float minAngle, float maxAngle, float meshAreaFactor, float largestTriangleAreaFactor, int smoothIterations, IList<Vector2> vertices, IList<Edge> edges, IList<int> indices)
        {
            if (vertices.Count < 3)
                return;

            largestTriangleAreaFactor = Mathf.Clamp01(largestTriangleAreaFactor);

            var polygon = new Polygon(vertices.Count);

            for (int i = 0; i < vertices.Count; ++i)
            {
                Vector2 position = vertices[i];
                polygon.Add(new Vertex(position.x, position.y));
            }

            for (int i = 0; i < edges.Count; ++i)
            {
                Edge edge = edges[i];
                polygon.Add(new Segment(polygon.Points[edge.index1], polygon.Points[edge.index2]));
            }

            var mesh = polygon.Triangulate();

            var angleQuality = new QualityOptions();

            angleQuality.SteinerPoints = 0;
            angleQuality.MinimumAngle = minAngle;
            angleQuality.MaximumAngle = maxAngle;

            mesh.Refine(angleQuality, false);

            var statistic = new Statistic();
            statistic.Update((UnityEngine.Experimental.U2D.TriangleNet.Mesh)mesh, 1);

            double maxAreaToApply = statistic.MeshArea * meshAreaFactor;

            maxAreaToApply = (double)Mathf.Max((float)statistic.LargestArea * largestTriangleAreaFactor, (float)maxAreaToApply);

            if (maxAreaToApply > 0f)
            {
                var areaQuality = new QualityOptions();
                areaQuality.SteinerPoints = 0;
                areaQuality.MaximumArea = maxAreaToApply;

                mesh.Refine(areaQuality, false);
            }

            mesh.Renumber();

            if (smoothIterations > 0)
            {
                var smoother = new SimpleSmoother();
                smoother.Smooth(mesh, smoothIterations);
            }

            vertices.Clear();
            edges.Clear();
            indices.Clear();

            foreach (Vertex vertex in mesh.Vertices)
            {
                vertices.Add(new Vector2((float)vertex.X, (float)vertex.Y));
            }

            foreach (ISegment segment in mesh.Segments)
            {
                edges.Add(new Edge(segment.P0, segment.P1));
            }

            foreach (ITriangle triangle in mesh.Triangles)
            {
                int id0 = triangle.GetVertexID(0);
                int id1 = triangle.GetVertexID(1);
                int id2 = triangle.GetVertexID(2);

                if (id0 < 0 || id1 < 0 || id2 < 0 ||  id0 >= vertices.Count || id1 >= vertices.Count || id2 >= vertices.Count)
                    continue;

                indices.Add(id0);
                indices.Add(id2);
                indices.Add(id1);
            }
        }
    }
}
