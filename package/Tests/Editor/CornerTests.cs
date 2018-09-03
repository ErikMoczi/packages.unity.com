using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.VectorGraphics;
using Unity.VectorGraphics.Editor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.U2D;

public class CornerTests : MonoBehaviour
{
    public class Geometry
    {
        public Vector2[] vertices;
        public Vector2[] uvs;
        public UInt16[] indices;
        public Color color;
        public Matrix2D worldTransform = Matrix2D.identity;
        public IFill fill;
        public Matrix2D fillTransform;
    }


    void Start()
    {
        float x = -1.0f;
        float xStep = 0.48f;

        var options = new VectorUtils.TessellationOptions();
        options.maxCordDeviation = float.MaxValue;
        options.maxTanAngleDeviation = Mathf.PI * 0.5f;
        options.samplingStepSize = 1.0f / 100.0f;

        // Fine tessellation
        options.stepDistance = 1.0f;
        BuildTestColumnWithProperties(x, 1.0f, PathEnding.Chop, PathCorner.Tipped, options);  x += xStep;
        BuildTestColumnWithProperties(x, 2.0f, PathEnding.Chop, PathCorner.Tipped, options);  x += xStep;
        BuildTestColumnWithProperties(x, 4.0f, PathEnding.Chop, PathCorner.Beveled, options); x += xStep;
        BuildTestColumnWithProperties(x, 6.0f, PathEnding.Round, PathCorner.Round, options);  x += xStep;

        // Coarse tessellation
        options.stepDistance = 100.0f;
        BuildTestColumnWithProperties(x, 1.0f, PathEnding.Chop, PathCorner.Tipped, options);  x += xStep;
        BuildTestColumnWithProperties(x, 2.0f, PathEnding.Chop, PathCorner.Tipped, options);  x += xStep;
        BuildTestColumnWithProperties(x, 4.0f, PathEnding.Chop, PathCorner.Beveled, options); x += xStep;
        BuildTestColumnWithProperties(x, 6.0f, PathEnding.Round, PathCorner.Round, options);  x += xStep;
    }

    private static void BuildTestColumnWithProperties(float x, float width, PathEnding ending, PathCorner corners, VectorUtils.TessellationOptions options)
    {
        var sprites = new List<Sprite>();

        var angles = new float[] {
            -Mathf.PI + Mathf.PI / 8,
            -Mathf.PI + Mathf.PI / 4,
            Mathf.PI - Mathf.PI / 4,
            Mathf.PI - Mathf.PI / 8
        };

        foreach (var angle in angles)
        {
            var path = LinesWithAngle(angle, width);
            var pathProps = path.pathProps;
            pathProps.head = ending;
            pathProps.tail = ending;
            pathProps.corners = corners;
            path.pathProps = pathProps;

            var geoms = new List<Geometry>();
            TessellatePath(path.contour, path.pathProps, geoms, options);
            sprites.Add(SpriteFromGeometry(geoms));
        }

        var pos = new Vector2(x, 0.0f);
        foreach (var sprite in sprites)
        {
            var go = new GameObject("Path");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            pos.y += 0.25f;
        }
    }

    private static Path LinesWithAngle(float angle, float width)
    {
        var p = Vector2.zero;
        var q = new Vector2(20.0f, 0.0f) + p;
        var r = new Vector2(Mathf.Cos(angle) * 20.0f, Mathf.Sin(angle) * 20.0f) + q;
        var path = new Path() {
            contour = new BezierContour() {
                segments = new BezierPathSegment[] {
                    new BezierPathSegment() { p0 = p, p1 = p + (q - p) / 3.0f, p2 = p + (q - p) / 3.0f * 2.0f },
                    new BezierPathSegment() { p0 = q, p1 = q + (r - q) / 3.0f, p2 = q + (r - q) / 3.0f * 2.0f },
                    new BezierPathSegment() { p0 = r }
                }
            },
            pathProps = new PathProperties() {
                stroke = new Stroke() { color = Color.white, halfThickness = width / 2 }
            }
        };
        return path;
    }

    private static Sprite SpriteFromGeometry(List<Geometry> geoms)
    {
        var vertices = new List<Vector2>();
        var indices = new List<UInt16>();
        var colors = new List<Color>();

        foreach (var geom in geoms)
        {
            if (geom.indices.Length == 0)
                continue;

            indices.AddRange(geom.indices.Select(x => (UInt16)(x + vertices.Count)));
            vertices.AddRange(geom.vertices.Select(x => geom.worldTransform * x));
            colors.AddRange(Enumerable.Repeat(geom.color, geom.vertices.Length));
        }

        var bbox = VectorUtils.RealignVerticesInBounds(vertices, true);
        var rect = new Rect(0, 0, bbox.width, bbox.height);

        // The Sprite.Create(Rect, Vector2, float, Texture2D) method is internal. Using reflection
        // until it becomes public.
        var spriteCreateMethod = typeof(Sprite).GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder, new Type[] { typeof(Rect), typeof(Vector2), typeof(float), typeof(Texture2D) }, null);
        var sprite = spriteCreateMethod.Invoke(null, new object[] { rect, Vector2.zero, 100.0f, null }) as Sprite;

        sprite.OverrideGeometry(vertices.ToArray(), indices.ToArray());

        var colors32 = colors.Select(c => (Color32)c);
        using (var nativeColors = new NativeArray<Color32>(colors32.ToArray(), Allocator.Temp))
            sprite.SetVertexAttribute<Color32>(VertexAttribute.Color, nativeColors);

        return sprite;
    }

    private static void TessellatePath(BezierContour contour, PathProperties pathProps, List<Geometry> geoms, VectorUtils.TessellationOptions options)
    {
        if (pathProps.stroke != null)
        {
            Vector2[] vertices;
            UInt16[] indices;
            VectorUtils.TessellatePath(contour, pathProps, options, out vertices, out indices);

            var color = pathProps.stroke.color;
            geoms.Add(new Geometry() { vertices = vertices, indices = indices, color = color });
        }
    }
}
