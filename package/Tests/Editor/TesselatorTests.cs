using System;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using Unity.VectorGraphics;

public class TessellatorTests
{
    static VectorUtils.TessellationOptions MakeTessOptions(float stepDistance)
    {
        var tessOptions = new VectorUtils.TessellationOptions();
        tessOptions.MaxCordDeviation = float.MaxValue;
        tessOptions.MaxTanAngleDeviation = Mathf.PI * 0.5f;
        tessOptions.SamplingStepSize = 1.0f / 100.0f;
        tessOptions.StepDistance = stepDistance;
        return tessOptions;
    }

    [Test]
    public void TessellatePathUniform_GeneratesPathWithRequestedWidth()
    {
        for (int width = 1; width <= 128; width *= 2)
        {
            var path = MakeLine(Vector2.zero, Vector2.right * width, width);

            Vector2[] vertices;
            UInt16[] indices;
            VectorUtils.TessellatePath(path.Contour, path.PathProps, MakeTessOptions(10.0f), out vertices, out indices);

            float targetWidth = (float)width;

            // Each triangle should have one edge of magnitude "targetWidth"
            float pathWidth = (vertices[1] - vertices[0]).magnitude;
            for (int i = 0; i < indices.Length; i += 3)
            {
                bool found = false;
                for (int e = 0; e < 3; ++e)
                {
                    var v0 = vertices[indices[i + e]];
                    var v1 = vertices[indices[i + ((e + 1) % 3)]];
                    found = Mathf.Abs((v1 - v0).magnitude - targetWidth) < VectorUtils.Epsilon;
                    if (found) break;
                }

                Assert.AreEqual(true, found);
            }
        }
    }

    [Test]
    public void TessellatePathUniform_GeneratesLineQuadAtLargeStepDistance()
    {
        var path = MakeLine(Vector2.zero, Vector2.right * 10.0f, 10.0f);

        Vector2[] vertices;
        UInt16[] indices;
        VectorUtils.TessellatePath(path.Contour, path.PathProps, MakeTessOptions(100.0f), out vertices, out indices);

        // There should be 2 triangles to generate a quad
        Assert.AreEqual(2, indices.Length / 3);
    }

    [Test]
    public void TessellatePathUniform_GeneratesTwoTrianglesForTippedCorner()
    {
        var path = MakeCorner(Vector2.zero, Vector2.right * 20.0f, Vector2.one * 20.0f, 2.0f);
        var pathProps = path.PathProps;
        pathProps.Corners = PathCorner.Tipped;
        path.PathProps = pathProps;

        Vector2[] vertices;
        UInt16[] indices;
        VectorUtils.TessellatePath(path.Contour, path.PathProps, MakeTessOptions(100.0f), out vertices, out indices);

        // Tipped joins generates 2 triangles, plus 4 for the straight line quads
        Assert.AreEqual(6, indices.Length / 3);
    }

    [Test]
    public void TessellatePathUniform_GeneratesFourTrianglesForRoundCorner()
    {
        var path = MakeCorner(Vector2.zero, Vector2.right * 20.0f, Vector2.one * 20.0f, 2.0f);
        var pathProps = path.PathProps;
        pathProps.Corners = PathCorner.Round;
        path.PathProps = pathProps;

        Vector2[] vertices;
        UInt16[] indices;
        VectorUtils.TessellatePath(path.Contour, path.PathProps, MakeTessOptions(100.0f), out vertices, out indices);

        // At coarse step distance, round joins generates 4 triangles, plus 4 for the straight line quads
        Assert.AreEqual(8, indices.Length / 3);
    }

    [Test]
    public void TessellatePathUniform_GeneratesOneTriangleForBeveledCorner()
    {
        var path = MakeCorner(Vector2.zero, Vector2.right * 20.0f, Vector2.one * 20.0f, 2.0f);
        var pathProps = path.PathProps;
        pathProps.Corners = PathCorner.Beveled;
        path.PathProps = pathProps;

        Vector2[] vertices;
        UInt16[] indices;
        VectorUtils.TessellatePath(path.Contour, path.PathProps, MakeTessOptions(100.0f), out vertices, out indices);

        // Beveled joins generates 1 triangle, plus 4 for the straight line quads
        Assert.AreEqual(5, indices.Length / 3);
    }

    [Test]
    public void TessellatePathUniform_GeneratesDashesAtLargeStepDistance()
    {
        var path = MakeLine(Vector2.zero, Vector2.right * 10.0f, 10.0f);
        path.PathProps.Stroke.Pattern = new float[] { 5.0f, 2.0f };

        Vector2[] vertices;
        UInt16[] indices;
        VectorUtils.TessellatePath(path.Contour, path.PathProps, MakeTessOptions(100.0f), out vertices, out indices);

        // There should be 2 quads for 2 dashes
        Assert.AreEqual(4, indices.Length / 3);
    }

    [Test]
    public void TessellateRect_GeneratesQuad()
    {
        Vector2[] vertices;
        UInt16[] indices;
        VectorUtils.TessellateRect(new Rect(0, 0, 100, 50), out vertices, out indices);

        Assert.AreEqual(2, indices.Length / 3);
    }

    [Test]
    public void TessellateRectBorder_GeneratesFourQuadsForBorder()
    {
        Vector2[] vertices;
        UInt16[] indices;
        VectorUtils.TessellateRectBorder(new Rect(0, 0, 100, 50), 2.0f, out vertices, out indices);

        Assert.AreEqual(8, indices.Length / 3);
    }

    private Path MakeLine(Vector2 from, Vector2 to, float width)
    {
        return new Path() {
            Contour = new BezierContour() { Segments = VectorUtils.MakePathLine(from, to) },
            PathProps = new PathProperties() {
                Stroke = new Stroke() { HalfThickness = width / 2.0f },
                Head = PathEnding.Chop,
                Tail = PathEnding.Chop,
                Corners = PathCorner.Tipped
            }
        };
    }

    private Path MakeCorner(Vector2 p0, Vector2 p1, Vector2 p2, float width)
    {
        return new Path() {
            Contour = new BezierContour() {
                Segments = new BezierPathSegment[] {
                    new BezierPathSegment() { P0 = p0, P1 = p0 + (p1 - p0) / 3.0f, P2 = p0 + (p1 - p0) / 3.0f * 2.0f },
                    new BezierPathSegment() { P0 = p1, P1 = p1 + (p2 - p1) / 3.0f, P2 = p1 + (p2 - p1) / 3.0f * 2.0f },
                    new BezierPathSegment() { P0 = p2 }
                }
            },
            PathProps = new PathProperties() {
                Stroke = new Stroke() { HalfThickness = width / 2.0f },
                Head = PathEnding.Chop,
                Tail = PathEnding.Chop,
                Corners = PathCorner.Tipped
            }
        };
    }
}
