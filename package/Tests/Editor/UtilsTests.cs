using System;
using System.Linq;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using Unity.VectorGraphics;

public class UtilsTests
{
    [Test]
    public void PathSegments_WithBezierSegment_ReturnsBezierPathSegment()
    {
        var points = new Vector2[4];
        for (int i = 0; i < points.Length; ++i)
            points[i] = Vector2.one * i;

        var seg = new BezierSegment() { p0 = points[0], p1 = points[1], p2 = points[2], p3 = points[3] };
        var pathSeg = VectorUtils.PathSegments(seg);

        Assert.AreEqual(2, pathSeg.Length);
        Assert.AreEqual(points[0], pathSeg[0].p0);
        Assert.AreEqual(points[1], pathSeg[0].p1);
        Assert.AreEqual(points[2], pathSeg[0].p2);
        Assert.AreEqual(points[3], pathSeg[1].p0);
    }

    [Test]
    public void PathSegment_WithBezierPathSegmentList_ReturnsBezierSegment()
    {
        var points = new Vector2[7];
        for (int i = 0; i < points.Length; ++i)
            points[i] = Vector2.one * i;

        var path = new BezierPathSegment[] {
            new BezierPathSegment() { p0 = points[0], p1 = points[1], p2 = points[2] },
            new BezierPathSegment() { p0 = points[3], p1 = points[4], p2 = points[5] },
            new BezierPathSegment() { p0 = points[6] }
        };

        var seg0 = VectorUtils.PathSegment(path, 0);
        Assert.AreEqual(points[0], seg0.p0);
        Assert.AreEqual(points[1], seg0.p1);
        Assert.AreEqual(points[2], seg0.p2);
        Assert.AreEqual(points[3], seg0.p3);

        var seg1 = VectorUtils.PathSegment(path, 1);
        Assert.AreEqual(points[3], seg1.p0);
        Assert.AreEqual(points[4], seg1.p1);
        Assert.AreEqual(points[5], seg1.p2);
        Assert.AreEqual(points[6], seg1.p3);
    }

    [Test]
    public void SegmentsInPath_ReturnsAllSegmentsInPath()
    {
        var points = new Vector2[7];
        for (int i = 0; i < points.Length; ++i)
            points[i] = Vector2.one * i;

        var path = new BezierPathSegment[] {
            new BezierPathSegment() { p0 = points[0], p1 = points[1], p2 = points[2] },
            new BezierPathSegment() { p0 = points[3], p1 = points[4], p2 = points[5] },
            new BezierPathSegment() { p0 = points[6] }
        };

        var segs = VectorUtils.SegmentsInPath(path).ToList();
        Assert.AreEqual(2, segs.Count);

        Assert.AreEqual(points[0], segs[0].p0);
        Assert.AreEqual(points[1], segs[0].p1);
        Assert.AreEqual(points[2], segs[0].p2);
        Assert.AreEqual(points[3], segs[0].p3);

        Assert.AreEqual(points[3], segs[1].p0);
        Assert.AreEqual(points[4], segs[1].p1);
        Assert.AreEqual(points[5], segs[1].p2);
        Assert.AreEqual(points[6], segs[1].p3);
    }

    [Test]
    public void PathEndsPerfectlyMatch_ReturnsTrue_WhenEndsPerfectlyMatches()
    {
        // Should return false if there's less than 2 segments
        Assert.IsFalse(VectorUtils.PathEndsPerfectlyMatch(new BezierPathSegment[0]));

        var path = new BezierPathSegment[] {
            new BezierPathSegment() { p0 = Vector2.zero, p1 = new Vector2(-10, 10), p2 = new Vector2(10, 10) },
            new BezierPathSegment() { p0 = Vector2.zero }
        };

        Assert.IsTrue(VectorUtils.PathEndsPerfectlyMatch(path));

        path[1].p0 = Vector2.one;

        Assert.IsFalse(VectorUtils.PathEndsPerfectlyMatch(path));
    }

    [Test]
    public void MakeEllipse_MakesRoundedRectWithEllipseShape()
    {
        var radiuses = new Vector2(10.0f, 20.0f);
        var ellipse = VectorUtils.MakeEllipse(Vector2.zero, radiuses.x, radiuses.y);

        Assert.AreEqual(20.0f, ellipse.size.x);
        Assert.AreEqual(40.0f, ellipse.size.y);

        Assert.AreEqual(radiuses, ellipse.radiusTL);
        Assert.AreEqual(radiuses, ellipse.radiusTR);
        Assert.AreEqual(radiuses, ellipse.radiusBR);
        Assert.AreEqual(radiuses, ellipse.radiusBL);
    }

    [Test]
    public void MakeCircle_MakesRoundedRectWithCircleShape()
    {
        var radius = 10.0f;
        var circle = VectorUtils.MakeCircle(Vector2.zero, radius);

        Assert.AreEqual(20.0f, circle.size.x);
        Assert.AreEqual(20.0f, circle.size.y);

        var v = Vector2.one * 10.0f;
        Assert.AreEqual(v, circle.radiusTL);
        Assert.AreEqual(v, circle.radiusTR);
        Assert.AreEqual(v, circle.radiusBR);
        Assert.AreEqual(v, circle.radiusBL);
    }

    [Test]
    public void Bounds_ComputesBezierContoursBounds()
    {
        var seg = VectorUtils.MakeArc(Vector2.zero, 0.0f, Mathf.PI / 2);
        var contour = new BezierContour() {
            segments = VectorUtils.PathSegments(seg),
            closed = true
        };

        var bbox = VectorUtils.Bounds(contour);

        Assert.AreEqual(0.0f, bbox.min.x, VectorUtils.Epsilon);
        Assert.AreEqual(0.0f, bbox.min.y, VectorUtils.Epsilon);
        Assert.AreEqual(1.0f, bbox.max.x, VectorUtils.Epsilon);
        Assert.AreEqual(1.0f, bbox.max.y, VectorUtils.Epsilon);
    }

    [Test]
    public void Bounds_ReturnsVerticesBoundingBox()
    {
        var bbox = VectorUtils.Bounds(new Vector2[] { Vector2.zero, Vector2.right, Vector2.one });
        Assert.AreEqual(Vector2.zero, bbox.min);
        Assert.AreEqual(Vector2.one, bbox.max);
    }

    [Test]
    public void MakeLine_ReturnsLineSegment()
    {
        var from = Vector2.zero;
        var to = Vector2.one;
        var seg = VectorUtils.MakeLine(from, to);

        // All segment points should lie on the same line
        var v = (to - from).normalized;
        Assert.AreEqual(1.0f, Vector2.Dot((seg.p1 - from).normalized, v), VectorUtils.Epsilon);
        Assert.AreEqual(1.0f, Vector2.Dot((seg.p2 - from).normalized, v), VectorUtils.Epsilon);
    }

    [Test]
    public void MakePathLine_ReturnsLinePathSegment()
    {
        var from = Vector2.zero;
        var to = Vector2.one;
        var pathSeg = VectorUtils.MakePathLine(from, to);

        // All segment points should lie on the same line
        var v = (to - from).normalized;
        Assert.AreEqual(1.0f, Vector2.Dot((pathSeg[0].p1 - from).normalized, v), VectorUtils.Epsilon);
        Assert.AreEqual(1.0f, Vector2.Dot((pathSeg[0].p2 - from).normalized, v), VectorUtils.Epsilon);
    }

    [Test]
    public void MakeArc_ReturnsArcSegment()
    {
        var seg = VectorUtils.MakeArc(Vector2.zero, 0.0f, Mathf.PI / 2);
        for (var t = 0.0f; t <= 1.0f; t += 0.1f)
        {
            Assert.AreEqual(1.0f, VectorUtils.Eval(seg, t).magnitude, 0.001f);
        }
    }

    [Test]
    public void MakeArcSegments_ReturnsMultipleSegmentsWhenArcSpansMoreThanOneQuadrant()
    {
        var segs = VectorUtils.MakeArcSegments(Vector2.zero, 0.0f, Mathf.PI);
        Assert.AreEqual(2, segs.Length);

        foreach (var seg in segs)
        {
            for (var t = 0.0f; t <= 1.0f; t += 0.1f)
            {
                Assert.AreEqual(1.0f, VectorUtils.Eval(seg, t).magnitude, 0.001f);
            }
        }
    }

    [Test]
    public void FlipSegment_FlipsPointsOrder()
    {
        var points = new Vector2[4];
        for (int i = 0; i < points.Length; ++i)
            points[i] = Vector2.one * i;

        var seg = new BezierSegment() { p0 = points[0], p1 = points[1], p2 = points[2], p3 = points[3] };
        var flipped = VectorUtils.FlipSegment(seg);

        Assert.AreEqual(points[3], flipped.p0);
        Assert.AreEqual(points[2], flipped.p1);
        Assert.AreEqual(points[1], flipped.p2);
        Assert.AreEqual(points[0], flipped.p3);
    }

    [Test]
    public void Eval_EvaluatesPointOnBezierSegment()
    {
        var seg = VectorUtils.MakeLine(Vector2.zero, Vector2.one);
        var p = VectorUtils.Eval(seg, 0.5f);
        Assert.AreEqual(0.5f, p.x, VectorUtils.Epsilon);
        Assert.AreEqual(0.5f, p.y, VectorUtils.Epsilon);
    }

    [Test]
    public void EvalNormal_EvaluatesNormalOnBezierSegment()
    {
        var seg = VectorUtils.MakeLine(Vector2.zero, Vector2.right);
        var n = VectorUtils.EvalNormal(seg, 0.5f);
        Assert.AreEqual(0.0f, n.x, VectorUtils.Epsilon);
        Assert.AreEqual(1.0f, n.y, VectorUtils.Epsilon);
    }

    [Test]
    public void SegmentLength_ReturnSegmentLength()
    {
        var seg = VectorUtils.MakeLine(Vector2.zero, Vector2.one);
        Assert.AreEqual(Mathf.Sqrt(2), VectorUtils.SegmentLength(seg), VectorUtils.Epsilon);
    }

    [Test]
    public void SplitSegment_ReturnsTwoSegmentsSeparatedAtParameterT()
    {
        var seg = VectorUtils.MakeLine(Vector2.zero, Vector2.right);

        BezierSegment first;
        BezierSegment second;
        VectorUtils.SplitSegment(seg, 0.5f, out first, out second);

        Assert.AreEqual(Vector2.zero, first.p0);
        Assert.AreEqual(Vector2.right * 0.5f, first.p3);

        Assert.AreEqual(Vector2.right * 0.5f, second.p0);
        Assert.AreEqual(Vector2.right, second.p3);
    }

    [Test]
    public void SceneNodes_ReturnsAllSceneNodes()
    {
        var child1 = new SceneNode();
        var child2 = new SceneNode();
        var root = new SceneNode();
        root.children = new System.Collections.Generic.List<SceneNode>(2);
        root.children.Add(child1);
        root.children.Add(child2);

        var nodes = VectorUtils.SceneNodes(root).ToList();
        Assert.IsTrue(nodes.Contains(child1));
        Assert.IsTrue(nodes.Contains(child2));
        Assert.IsTrue(nodes.Contains(root));
    }

    [Test]
    public void WorldTransformedSceneNodes_ReturnsTransformedSceneNodes()
    {
        var child = new SceneNode();
        child.transform = Matrix2D.identity;

        var root = new SceneNode();
        root.children = new System.Collections.Generic.List<SceneNode>(2);
        root.children.Add(child);

        var transform = Matrix2D.Translate(new Vector2(1, 2));
        root.transform = transform;

        var nodes = VectorUtils.WorldTransformedSceneNodes(root, null);
        foreach (var nodeWithTransform in nodes)
        {
            Assert.AreEqual(1.0f, nodeWithTransform.worldTransform.m02);
            Assert.AreEqual(2.0f, nodeWithTransform.worldTransform.m12);
        }
    }

    [Test]
    public void WorldTransformedSceneNodes_ComputesParent()
    {
        var child = new SceneNode();
        var parent = new SceneNode() { children = new List<SceneNode> { child } };

        bool childFound = false;

        var nodes = VectorUtils.WorldTransformedSceneNodes(parent, null);
        foreach (var nodeWithTransform in nodes)
        {
            if (nodeWithTransform.node == child)
            {
                childFound = true;
                Assert.AreEqual(parent, nodeWithTransform.parent);
                break;
            }
        }

        Assert.IsTrue(childFound);
    }

    [Test]
    public void IntersectLines_ReturnsLineIntersection()
    {
        var intersect0 = VectorUtils.IntersectLines(Vector2.zero, Vector2.one, new Vector2(0, 1), new Vector2(1, 0));
        Assert.AreEqual(Vector2.one * 0.5f, intersect0);

        var intersect1 = VectorUtils.IntersectLines(Vector2.zero, Vector2.one, Vector2.zero, Vector2.one);
        Assert.IsTrue(float.IsInfinity(intersect1.x));
        Assert.IsTrue(float.IsInfinity(intersect1.y));
    }

    [Test]
    public void IntersectLineSegments_ReturnsLineIntersection_OnlyWhenTouching()
    {
        var intersect0 = VectorUtils.IntersectLineSegments(Vector2.zero, Vector2.one, new Vector2(0, 1), new Vector2(1, 0));
        Assert.AreEqual(Vector2.one * 0.5f, intersect0);

        var intersect1 = VectorUtils.IntersectLineSegments(Vector2.zero, Vector2.one * 0.25f, new Vector2(0, 1), new Vector2(1, 0));
        Assert.IsTrue(float.IsInfinity(intersect1.x));
        Assert.IsTrue(float.IsInfinity(intersect1.y));
    }

    [Test]
    public void FindBezierLineIntersections_ReturnsBezierLineIntersections()
    {
        var seg = VectorUtils.MakeArc(Vector2.zero, 0.0f, Mathf.PI / 2);
        var ts = VectorUtils.FindBezierLineIntersections(seg, new Vector2(0.0f , 1.1f), new Vector2(1.1f, 0.0f));

        Assert.AreEqual(2, ts.Length);

        var p = VectorUtils.Eval(seg, ts[0]);
        Assert.AreEqual(0.995f, p.x, 0.001f);
        Assert.AreEqual(0.096f, p.y, 0.001f);

        var q = VectorUtils.Eval(seg, ts[1]);
        Assert.AreEqual(0.096f, q.x, 0.001f);
        Assert.AreEqual(0.995f, q.y, 0.001f);
    }
}
