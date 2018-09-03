using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Unity.VectorGraphics
{
    public partial class VectorUtils
    {
        internal static void TessellateRectangle(Rectangle rect, List<Geometry> geoms, TessellationOptions tessellationOptions)
        {
            var width = rect.size.x;
            var height = rect.size.y;

            if (width <= VectorUtils.Epsilon || height <= VectorUtils.Epsilon)
                return;

            if (IsSimpleRectangle(rect))
            {
                // Fast path, square corners, no patterns
                TessellateRectangleSquareCorners(rect, geoms);
            }
            else
            {
                TessellateRectangleRoundedCorners(rect, geoms, tessellationOptions);
            }
        }

        internal static Vector2[] TraceRectangle(Rectangle rect, Stroke stroke, TessellationOptions tessellationOptions)
        {
            var width = rect.size.x;
            var height = rect.size.y;

            if (width <= VectorUtils.Epsilon || height <= VectorUtils.Epsilon)
                return null;

            if (IsSimpleRectangle(rect))
            {
                // Fast path, square corners, no patterns
                var r = new Rect(rect.position, rect.size);
                return new Vector2[] {
                    new Vector2(r.xMin, r.yMin),
                    new Vector2(r.xMax, r.yMin),
                    new Vector2(r.xMax, r.yMax),
                    new Vector2(r.xMin, r.yMax)
                };
            }
            else
            {
                var contour = BuildRoundedRectangleContour(rect);
                return VectorUtils.TraceShape(contour, stroke, tessellationOptions);    
            }
        }

        private static void TessellateRectangleSquareCorners(Rectangle rect, List<Geometry> geoms)
        {
            var x = rect.position.x;
            var y = rect.position.y;
            var width = rect.size.x;
            var height = rect.size.y;

            // Don't generate any geometry for pattern fills since these are generated from another SceneNode
            if (rect.fill != null && !(rect.fill is PatternFill))
            {
                Vector2[] vertices;
                UInt16[] indices;
                VectorUtils.TessellateRect(new Rect(x, y, width, height), out vertices, out indices);

                var solidFill = rect.fill as SolidFill;
                var color = solidFill != null ? solidFill.color : Color.white;
                color.a *= rect.fill.opacity;

                geoms.Add(new Geometry() { vertices = vertices, indices = indices, color = color, fill = rect.fill, fillTransform = rect.fillTransform });
            }

            if (rect.pathProps.stroke != null)
            {
                Vector2[] vertices;
                UInt16[] indices;
                VectorUtils.TessellateRectBorder(new Rect(x, y, width, height), rect.pathProps.stroke.halfThickness, out vertices, out indices);

                geoms.Add(new Geometry() { vertices = vertices, indices = indices, color = rect.pathProps.stroke.color });
            }
        }

        private static void TessellateRectangleRoundedCorners(Rectangle rect, List<Geometry> geoms, TessellationOptions tessellationOptions)
        {
            var contour = BuildRoundedRectangleContour(rect);
            var shape = new Shape() {
                contours = new BezierContour[] { contour },
                pathProps = rect.pathProps,
                fill = rect.fill,
                fillTransform = rect.fillTransform
            };
            VectorUtils.TessellateShape(shape, geoms, tessellationOptions);
        }

        private static BezierContour BuildRoundedRectangleContour(Rectangle rect)
        {
            var width = rect.size.x;
            var height = rect.size.y;

            var halfSize = new Vector2(width / 2.0f, height / 2.0f);
            var radiusTL = Vector2.Max(Vector2.Min(rect.radiusTL, halfSize), Vector2.zero);
            var radiusTR = Vector2.Max(Vector2.Min(rect.radiusTR, halfSize), Vector2.zero);
            var radiusBR = Vector2.Max(Vector2.Min(rect.radiusBR, halfSize), Vector2.zero);
            var radiusBL = Vector2.Max(Vector2.Min(rect.radiusBL, halfSize), Vector2.zero);

            var leftSegmentSize = height - (radiusBL.y + radiusTL.y);
            var topSegmentSize = width - (radiusTL.x + radiusTR.x);
            var rightSegmentSize = height - (radiusBR.y + radiusTR.y);
            var bottomSegmentSize = width - (radiusBL.x + radiusBR.x);

            var segments = new List<BezierPathSegment>(8);
            BezierPathSegment seg;

            if (leftSegmentSize > VectorUtils.Epsilon)
            {
                seg = MakePathLine(new Vector2(0.0f, radiusTL.y + leftSegmentSize), new Vector2(0.0f, radiusTL.y))[0];
                segments.Add(seg);
            }

            if (radiusTL.magnitude > VectorUtils.Epsilon)
            {
                var circleArc = VectorUtils.ArcSegmentForQuadrant(2);
                circleArc = VectorUtils.TransformSegment(circleArc, radiusTL, 0.0f, radiusTL);
                seg = new BezierPathSegment() { p0 = circleArc.p0, p1 = circleArc.p1, p2 = circleArc.p2 };
                segments.Add(seg);
            }

            if (topSegmentSize > VectorUtils.Epsilon)
            {
                seg = MakePathLine(new Vector2(radiusTL.x, 0.0f), new Vector2(radiusTL.x + topSegmentSize, 0.0f))[0];
                segments.Add(seg);
            }

            if (radiusTR.magnitude > VectorUtils.Epsilon)
            {
                var topRight = new Vector2(width - radiusTR.x, radiusTR.y);
                var circleArc = VectorUtils.ArcSegmentForQuadrant(3);
                circleArc = VectorUtils.TransformSegment(circleArc, topRight, 0.0f, radiusTR);
                seg = new BezierPathSegment() { p0 = circleArc.p0, p1 = circleArc.p1, p2 = circleArc.p2 };
                segments.Add(seg);
            }

            if (rightSegmentSize > VectorUtils.Epsilon)
            {
                seg = MakePathLine(new Vector2(width, radiusTR.y), new Vector2(width, radiusTR.y + rightSegmentSize))[0];
                segments.Add(seg);
            }

            if (radiusBR.magnitude > VectorUtils.Epsilon)
            {
                var bottomRight = new Vector2(width - radiusBR.x, height - radiusBR.y);
                var circleArc = VectorUtils.ArcSegmentForQuadrant(0);
                circleArc = VectorUtils.TransformSegment(circleArc, bottomRight, 0.0f, radiusBR);
                seg = new BezierPathSegment() { p0 = circleArc.p0, p1 = circleArc.p1, p2 = circleArc.p2 };
                segments.Add(seg);
            }

            if (bottomSegmentSize > VectorUtils.Epsilon)
            {
                seg = MakePathLine(new Vector2(width - radiusBR.x, height), new Vector2(width - (radiusBR.x + bottomSegmentSize), height))[0];
                segments.Add(seg);
            }

            if (radiusBL.magnitude > VectorUtils.Epsilon)
            {
                var bottomLeft = new Vector2(radiusBL.x, height - radiusBL.y);
                var circleArc = VectorUtils.ArcSegmentForQuadrant(1);
                circleArc = VectorUtils.TransformSegment(circleArc, bottomLeft, 0.0f, radiusBL);
                seg = new BezierPathSegment() { p0 = circleArc.p0, p1 = circleArc.p1, p2 = circleArc.p2 };
                segments.Add(seg);

                var end = new Vector2(0.0f, height - radiusBL.y);
                segments.Add(new BezierPathSegment() { p0 = end });
            }

            // Offset segments to position
            for (int i = 0; i < segments.Count; ++i)
            {
                var s = segments[i];
                s.p0 += rect.position;
                s.p1 += rect.position;
                s.p2 += rect.position;
                segments[i] = s;
            }

            return new BezierContour() { segments = segments.ToArray(), closed = true };
        }

        private static bool IsSimpleRectangle(Rectangle rect)
        {
            if (rect.radiusTL.magnitude > VectorUtils.Epsilon ||
                rect.radiusTR.magnitude > VectorUtils.Epsilon ||
                rect.radiusBL.magnitude > VectorUtils.Epsilon ||
                rect.radiusBR.magnitude > VectorUtils.Epsilon)
            {
                // Not simple if any corner is rounded
                return false;
            }

            if (rect.pathProps.stroke != null && rect.pathProps.stroke.pattern != null && rect.pathProps.stroke.pattern.Length > 0)
            {
                // Not simple if there's a pattern
                return false;
            }

            if (rect.pathProps.corners != PathCorner.Tipped)
            {
                // Not simple if corners aren't tipped
                return false;
            }

            // We can ignore the pathProps head/tail properties, they should have
            // no impact on the closed rectangle outline
            return true;
        }
    }
}
