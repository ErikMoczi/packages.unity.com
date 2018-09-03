using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using LibTessDotNet;
using ClipperLib;

namespace Unity.VectorGraphics
{
    public static partial class VectorUtils
    {
        const int kClipperScale = 100000;

        private static void ClipNodeHierarchy(SceneNode root, Dictionary<SceneNode, List<Geometry>> nodeGeoms, TessellationOptions tessellationOptions)
        {
            if (root.children != null)
            {
                foreach (var child in root.children)
                    ClipNodeHierarchy(child, nodeGeoms, tessellationOptions);
            }

            if (root.clipper != null)
            {
                // Tessellate the clipping geometry
                var clipperGeoms = TessellateNodeHierarchy(root.clipper, tessellationOptions);
                foreach (var geom in clipperGeoms)
                    geom.vertices = geom.vertices.Select(v => geom.worldTransform * v).ToArray();

                var clipperPaths = new List<List<IntPoint>>(100);
                foreach (var geom in clipperGeoms)
                {
                    clipperPaths.AddRange(BuildTriangleClipPaths(geom));
                }

                // Clip root and children
                ClipGeometryAndChildren(root, nodeGeoms, clipperPaths);
            }
        }

        private static void ClipGeometryAndChildren(SceneNode root, Dictionary<SceneNode, List<Geometry>> nodeGeoms, List<List<IntPoint>> clipperPaths)
        {
            foreach (var transformedNode in WorldTransformedSceneNodes(root, null))
            {
                var clipper = new Clipper();
                List<Geometry> geoms;
                if (nodeGeoms.TryGetValue(transformedNode.node, out geoms))
                {
                    foreach (var geom in geoms)
                    {
                        var paths = BuildTriangleClipPaths(geom);
                        var result = new List<List<IntPoint>>();
                        clipper.AddPaths(clipperPaths, PolyType.ptClip, true);
                        clipper.AddPaths(paths, PolyType.ptSubject, true);
                        clipper.Execute(ClipType.ctIntersection, result, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
                        clipper.Clear();
                        BuildGeometryFromClipPaths(geom,  result);
                    }            
                }
            }
        }

        private static List<List<IntPoint>> BuildTriangleClipPaths(Geometry geom)
        {
            var paths = new List<List<IntPoint>>(geom.indices.Length/3);
            var verts = geom.vertices;
            var inds = geom.indices;
            var indexCount = geom.indices.Length;
            for (int i = 0; i < indexCount; i += 3)
            {
                var v0 = verts[inds[i]];
                var v1 = verts[inds[i+1]];
                var v2 = verts[inds[i+2]];
                var tri = new List<IntPoint>(3);
                tri.Add(new IntPoint(v0.x * kClipperScale, v0.y * kClipperScale));
                tri.Add(new IntPoint(v1.x * kClipperScale, v1.y * kClipperScale));
                tri.Add(new IntPoint(v2.x * kClipperScale, v2.y * kClipperScale));
                paths.Add(tri);
            }
            return paths;
        }

        private static void BuildGeometryFromClipPaths(Geometry geom, List<List<IntPoint>> paths)
        {
            var vertices = new List<Vector2>(100);
            var indices = new List<UInt16>(vertices.Capacity*3);
            var vertexIndex = new Dictionary<IntPoint, UInt16>();

            UInt16 maxIndex = 0;
            foreach (var path in paths)
            {
                if (path.Count == 3)
                {
                    // Triangle case, no need to tessellate
                    foreach (var pt in path)
                        StoreClipVertex(vertexIndex, vertices, indices, pt, ref maxIndex);
                }
                else if (path.Count > 3)
                {
                    // Generic polygon case, we need to tessellate first
                    var tess = new Tess();
                    var contour = new ContourVertex[path.Count];
                    for (int i = 0; i < path.Count; ++i)
                        contour[i] = new ContourVertex() { Position = new Vec3() { X = path[i].X, Y = path[i].Y, Z = 0.0f }};
                    tess.AddContour(contour, ContourOrientation.Original);

                    var windingRule = WindingRule.NonZero; 
                    tess.Tessellate(windingRule, ElementType.Polygons, 3);

                    foreach (var e in tess.Elements)
                    {
                        var v = tess.Vertices[e];
                        var pt = new IntPoint(v.Position.X, v.Position.Y);
                        StoreClipVertex(vertexIndex, vertices, indices, pt, ref maxIndex);
                    }
                }                
            }

            geom.vertices = vertices.ToArray();
            geom.indices = indices.ToArray();
        }

        private static void StoreClipVertex(Dictionary<IntPoint, UInt16> vertexIndex, List<Vector2> vertices, List<UInt16> indices, IntPoint pt,  ref UInt16 index)
        {
            UInt16 storedIndex;
            if (vertexIndex.TryGetValue(pt, out storedIndex))
            {
                indices.Add(storedIndex);
            }
            else
            {
                vertices.Add(new Vector2(((float)pt.X) / kClipperScale, ((float)pt.Y) / kClipperScale));
                indices.Add(index);
                vertexIndex[pt] = index;
                ++index;
            }
        }
    }
}