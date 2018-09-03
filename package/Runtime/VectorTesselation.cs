using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.VectorGraphics
{
    public static partial class VectorUtils
    {
        /// <summary>
        /// Structure to store the tessellation options.
        /// </summary>
        public struct TessellationOptions
        {
            float maxCordDev, maxCordDevSq, maxTanAngleDev, maxTanAngleDevCosine, stepSize;

            /// <summary>
            /// The uniform tessellation step distance.
            /// </summary>
            public float stepDistance { get; set; } // A split to happen uniformly at fixed distances

            /// <summary>
            /// The maximum distance on the cord to a straight line between to points after which more tessellation will be generated.
            /// To disable, specify float.MaxValue.
            /// </summary>
            public float maxCordDeviation  // Maximum distance allowed between a cord and its line projection
            {
                get { return maxCordDev; }
                set
                {
                    maxCordDev = Mathf.Max(value, 0.0f);
                    maxCordDevSq = (maxCordDev == float.MaxValue) ? float.MaxValue : maxCordDev * maxCordDev;
                }
            }
            internal float maxCordDeviationSquared { get { return maxCordDevSq; } }

            /// <summary>
            /// The maximum angle (in degrees) between the curve tangent and the next point after which more tessellation will be generated.
            /// To disable, specify float.MaxValue.
            /// </summary>
            public float maxTanAngleDeviation // The maximum angle allowed (in radians) between tangents before a split happens
            {
                get { return maxTanAngleDev; }
                set
                {
                    maxTanAngleDev = Mathf.Clamp(value, VectorUtils.Epsilon, Mathf.PI * 0.5f);
                    maxTanAngleDevCosine = Mathf.Cos(maxTanAngleDev);
                }
            }
            internal  float maxTanAngleDeviationCosine { get { return maxTanAngleDevCosine; } } // Cosine of the maximum angle allowed between tangents before a split happens

            /// <summary>
            /// The number of samples used internally to evaluate the curves. More samples = higher quality.
            /// Should be between 0 and 1 (inclusive).
            /// </summary>
            public float samplingStepSize
            {
                get { return stepSize; }
                set { stepSize = Mathf.Clamp(value, Epsilon, 1.0f); }
            }
        }

        /// <summary>
        /// Tessellates a path.
        /// </summary>
        /// <param name="contour">The path to tessellate</param>
        /// <param name="pathProps">The path properties</param>
        /// <param name="tessellateOptions">The tessellation options</param>
        /// <param name="vertices">The resulting vertices</param>
        /// <param name="indices">The resulting triangles</param>
        /// <remarks>
        /// The individual line segments generated during tessellation are made out of a set of ordered vertices. It is important
        /// to honor this ordering so joining and and capping connect properly with the existing vertices without generating dupes.
        /// The ordering assumed is as follows:
        /// The last two vertices of a piece must be such that the first is generated at the end with a positive half-thickness
        /// while the second vertex is at the end too but at a negative half-thickness.
        /// No assumptions are enforced for other vertices before the two last vertices.
        /// </remarks>
        public static void TessellatePath(BezierContour contour, PathProperties pathProps, TessellationOptions tessellateOptions, out Vector2[] vertices, out UInt16[] indices)
        {
            if (tessellateOptions.stepDistance < Epsilon)
                throw new Exception("stepDistance too small");

            UnityEngine.Profiling.Profiler.BeginSample("TessellatePath");

            float[] segmentLengths = VectorUtils.SegmentsLengths(contour.segments, contour.closed);

            // Approximate the number of vertices/indices we need to store the results so we reduce memory reallocations during work
            float approxTotalLength = 0.0f;
            foreach (var s in segmentLengths)
                approxTotalLength += s;

            int approxStepCount = Math.Max((int)(approxTotalLength / tessellateOptions.stepDistance + 0.5f), 2);
            if (pathProps.stroke.pattern != null)
                approxStepCount += pathProps.stroke.pattern.Length * 2;

            List<Vector2> verts = new List<Vector2>(approxStepCount * 2 + 32); // A little bit possibly for the endings
            List<UInt16> inds = new List<UInt16>((int)(verts.Capacity * 1.5f)); // Usually every 4 verts represent a quad that uses 6 indices

            var patternIt = new PathPatternIterator(pathProps.stroke.pattern, pathProps.stroke.patternOffset);
            var pathIt = new PathDistanceForwardIterator(contour.segments, contour.closed, tessellateOptions.maxCordDeviationSquared, tessellateOptions.maxTanAngleDeviationCosine, tessellateOptions.samplingStepSize);

            JoiningInfo[] joiningInfo = new JoiningInfo[2];
            HandleNewSegmentJoining(pathIt, patternIt, joiningInfo, pathProps.stroke.halfThickness, segmentLengths);

            int rangeIndex = 0;
            while (!pathIt.Ended)
            {
                if (patternIt.IsSolid)
                    TessellateRange(patternIt.SegmentLength, pathIt, patternIt, pathProps, tessellateOptions, joiningInfo, segmentLengths, approxTotalLength, rangeIndex++, verts, inds);
                else 
                    SkipRange(patternIt.SegmentLength, pathIt, patternIt, pathProps, joiningInfo, segmentLengths);
                patternIt.Advance();
            }

            vertices = verts.ToArray();
            indices = inds.ToArray();

            UnityEngine.Profiling.Profiler.EndSample();
        }

        static Vector2[] TraceShape(BezierContour contour, Stroke stroke, TessellationOptions tessellateOptions)
        {
            if (tessellateOptions.stepDistance < Epsilon)
                throw new Exception("stepDistance too small");

            float[] segmentLengths = VectorUtils.SegmentsLengths(contour.segments, contour.closed);

            // Approximate the number of vertices/indices we need to store the results so we reduce memory reallocations during work
            float approxTotalLength = 0.0f;
            foreach (var s in segmentLengths)
                approxTotalLength += s;

            int approxStepCount = Math.Max((int)(approxTotalLength / tessellateOptions.stepDistance + 0.5f), 2);
            var strokePattern = stroke != null ? stroke.pattern : null;
            var strokePatternOffset = stroke != null ? stroke.patternOffset : 0.0f;
            if (strokePattern != null)
                approxStepCount += strokePattern.Length * 2;

            List<Vector2> verts = new List<Vector2>(approxStepCount); // A little bit possibly for the endings

            var patternIt = new PathPatternIterator(strokePattern, strokePatternOffset);
            var pathIt = new PathDistanceForwardIterator(contour.segments, true, tessellateOptions.maxCordDeviationSquared, tessellateOptions.maxTanAngleDeviationCosine, tessellateOptions.samplingStepSize);
            verts.Add(pathIt.EvalCurrent());

            while (!pathIt.Ended)
            {
                float distance = patternIt.SegmentLength;
                float startingLength = pathIt.LengthSoFar;
                float unitsRemaining = Mathf.Min(tessellateOptions.stepDistance, distance);
                bool endedEntirePath = false;
                for (;;)
                {
                    var result = pathIt.AdvanceBy(unitsRemaining, out unitsRemaining);
                    if (result == PathDistanceForwardIterator.Result.Ended)
                    {
                        endedEntirePath = true;
                        break;
                    }
                    else if (result == PathDistanceForwardIterator.Result.NewSegment)
                        verts.Add(pathIt.EvalCurrent());

                    if ((unitsRemaining <= Epsilon) &&
                        !TryGetMoreRemainingUnits(ref unitsRemaining, pathIt, startingLength, distance, tessellateOptions.stepDistance))
                    {
                        break;
                    }

                    if (result == PathDistanceForwardIterator.Result.Stepped)
                        verts.Add(pathIt.EvalCurrent());
                }

                // Ending
                if (endedEntirePath)
                    break;
                else verts.Add(pathIt.EvalCurrent());
                patternIt.Advance();
            }

            if ((verts[0] - verts[verts.Count - 1]).sqrMagnitude < Epsilon)
                verts.RemoveAt(verts.Count - 1);
            return verts.ToArray(); // Why not return verts itself?
        }

        static bool TryGetMoreRemainingUnits(ref float unitsRemaining, PathDistanceForwardIterator pathIt, float startingLength, float distance, float stepDistance)
        {
            float distanceCrossedSoFar = pathIt.LengthSoFar - startingLength;
            if (distanceCrossedSoFar >= distance - Epsilon * 100.0f)
                return false;
            if (distanceCrossedSoFar + stepDistance > distance)
                unitsRemaining = distance - distanceCrossedSoFar;
            else unitsRemaining = stepDistance;
            return true;
        }

        static void HandleNewSegmentJoining(PathDistanceForwardIterator pathIt, PathPatternIterator patternIt, JoiningInfo[] joiningInfo, float halfThickness, float[] segmentLengths)
        {
            joiningInfo[0] = joiningInfo[1];
            joiningInfo[1] = null;

            if (!patternIt.IsSolidAt(pathIt.LengthSoFar + segmentLengths[pathIt.CurrentSegment]))
                return; // The joining center falls outside the pattern, so don't join... period

            if (pathIt.Closed)
            {
                JoiningInfo closing;
                if ((pathIt.CurrentSegment == 0) || (pathIt.CurrentSegment == pathIt.Segments.Count - 2))
                {
                    closing = ForeseeJoining(
                            VectorUtils.PathSegment(pathIt.Segments, pathIt.Segments.Count - 2),
                            VectorUtils.PathSegment(pathIt.Segments, 0),
                            halfThickness, segmentLengths[pathIt.Segments.Count - 2]);

                    if (pathIt.CurrentSegment == 0)
                        joiningInfo[0] = closing;
                    else
                    {
                        joiningInfo[1] = closing;
                        return;
                    }
                }
                else if (pathIt.CurrentSegment > pathIt.Segments.Count - 2)
                    return;
            }
            else if (pathIt.CurrentSegment >= pathIt.Segments.Count - 2)
                return;

            joiningInfo[1] = ForeseeJoining(
                    VectorUtils.PathSegment(pathIt.Segments, pathIt.CurrentSegment),
                    VectorUtils.PathSegment(pathIt.Segments, pathIt.CurrentSegment + 1),
                    halfThickness, segmentLengths[pathIt.CurrentSegment]);
        }

        static void SkipRange(
            float distance, PathDistanceForwardIterator pathIt, PathPatternIterator patternIt,
            PathProperties pathProps, JoiningInfo[] joiningInfo, float[] segmentLengths)
        {
            float unitsRemaining = distance;
            while (unitsRemaining > Epsilon)
            {
                var result = pathIt.AdvanceBy(unitsRemaining, out unitsRemaining);
                switch (result)
                {
                    case PathDistanceForwardIterator.Result.Ended:
                        return;
                    case PathDistanceForwardIterator.Result.Stepped:
                        if (unitsRemaining < Epsilon)
                            return;
                        break;
                    case PathDistanceForwardIterator.Result.NewSegment:
                        HandleNewSegmentJoining(pathIt, patternIt, joiningInfo, pathProps.stroke.halfThickness, segmentLengths);
                        break;
                }
            }
        }

        static void TessellateRange(
            float distance, PathDistanceForwardIterator pathIt, PathPatternIterator patternIt, PathProperties pathProps,
            TessellationOptions tessellateOptions, JoiningInfo[] joiningInfo, float[] segmentLengths, float totalLength, int rangeIndex, List<Vector2> verts, List<UInt16> inds)
        {
            bool startOfLoop = pathIt.Closed && (pathIt.CurrentSegment == 0) && (pathIt.CurrentT == 0.0f);
            if (startOfLoop && (joiningInfo[0] != null))
            {
                GenerateJoining(joiningInfo[0], pathProps.corners, pathProps.stroke.halfThickness, pathProps.stroke.tippedCornerLimit, tessellateOptions, verts, inds);
            }
            else
            {
                var pathEnding = pathProps.head;

                // If pattern at the end will overlap with beginning, use a chopped ending to allow merging
                if (pathIt.Closed && rangeIndex == 0 && patternIt.IsSolidAt(pathIt.CurrentT) && patternIt.IsSolidAt(totalLength))
                    pathEnding = PathEnding.Chop;

                GenerateTip(VectorUtils.PathSegment(pathIt.Segments, pathIt.CurrentSegment), true, pathIt.CurrentT, pathEnding, pathProps.stroke.halfThickness, tessellateOptions, verts, inds);
            }

            float startingLength = pathIt.LengthSoFar;
            float unitsRemaining = Mathf.Min(tessellateOptions.stepDistance, distance);
            bool endedEntirePath = false;
            for (;;)
            {
                var result = pathIt.AdvanceBy(unitsRemaining, out unitsRemaining);
                if (result == PathDistanceForwardIterator.Result.Ended)
                {
                    endedEntirePath = true;
                    break;
                }
                else if (result == PathDistanceForwardIterator.Result.NewSegment)
                {
                    if (joiningInfo[1] != null)
                        GenerateJoining(joiningInfo[1], pathProps.corners, pathProps.stroke.halfThickness, pathProps.stroke.tippedCornerLimit, tessellateOptions, verts, inds);
                    else AddSegment(VectorUtils.PathSegment(pathIt.Segments, pathIt.CurrentSegment), pathIt.CurrentT, pathProps.stroke.halfThickness, null, pathIt.SegmentLengthSoFar, verts, inds);
                    HandleNewSegmentJoining(pathIt, patternIt, joiningInfo, pathProps.stroke.halfThickness, segmentLengths);
                }

                if ((unitsRemaining <= Epsilon) &&
                    !TryGetMoreRemainingUnits(ref unitsRemaining, pathIt, startingLength, distance, tessellateOptions.stepDistance))
                {
                    break;
                }

                if (result == PathDistanceForwardIterator.Result.Stepped)
                    AddSegment(VectorUtils.PathSegment(pathIt.Segments, pathIt.CurrentSegment), pathIt.CurrentT, pathProps.stroke.halfThickness, joiningInfo, pathIt.SegmentLengthSoFar, verts, inds);
            }

            // Ending
            if (endedEntirePath && pathIt.Closed)
            {
                // No joining needed, the start and end of the path should just connect
                inds.Add(0);
                inds.Add(1);
                inds.Add((UInt16)(verts.Count - 2));
                inds.Add((UInt16)(verts.Count - 1));
                inds.Add((UInt16)(verts.Count - 2));
                inds.Add(1);
            }
            else
            {
                AddSegment(VectorUtils.PathSegment(pathIt.Segments, pathIt.CurrentSegment), pathIt.CurrentT, pathProps.stroke.halfThickness, joiningInfo, pathIt.SegmentLengthSoFar, verts, inds);
                GenerateTip(VectorUtils.PathSegment(pathIt.Segments, pathIt.CurrentSegment), false, pathIt.CurrentT, pathProps.tail, pathProps.stroke.halfThickness, tessellateOptions, verts, inds);
            }
        }

        static void AddSegment(BezierSegment segment, float toT, float halfThickness, JoiningInfo[] joinInfo, float segmentLengthSoFar, List<Vector2> verts, List<UInt16> inds)
        {
            Vector2 tanTo, normTo;
            Vector2 posTo = VectorUtils.EvalFull(segment, toT, out tanTo, out normTo);

            Vector2 posThickness = posTo + normTo * halfThickness;
            Vector2 negThickness = posTo + normTo * -halfThickness;

            if (joinInfo != null)
            {
                if ((joinInfo[0] != null) && (segmentLengthSoFar < joinInfo[0].innerCornerDistFromStart))
                {
                    if (joinInfo[0].roundPosThickness)
                        negThickness = joinInfo[0].innerCornerVertex;
                    else posThickness = joinInfo[0].innerCornerVertex;
                }

                if ((joinInfo[1] != null) && (segmentLengthSoFar > joinInfo[1].innerCornerDistToEnd))
                {
                    if (joinInfo[1].roundPosThickness)
                        negThickness = joinInfo[1].innerCornerVertex;
                    else posThickness = joinInfo[1].innerCornerVertex;
                }
            }

            System.Diagnostics.Debug.Assert(verts.Count >= 2);
            int indexStart = verts.Count - 2;
            verts.Add(posThickness);
            verts.Add(negThickness);
            inds.Add((UInt16)(indexStart + 0));
            inds.Add((UInt16)(indexStart + 3));
            inds.Add((UInt16)(indexStart + 1));
            inds.Add((UInt16)(indexStart + 0));
            inds.Add((UInt16)(indexStart + 2));
            inds.Add((UInt16)(indexStart + 3));
        }

        class JoiningInfo
        {
            public Vector2 joinPos;
            public Vector2 tanAtEnd, tanAtStart;
            public Vector2 normAtEnd, normAtStart;
            public Vector2 posThicknessStart, negThicknessStart;
            public Vector2 posThicknessEnd, negThicknessEnd;
            public Vector2 posThicknessClosingPoint, negThicknessClosingPoint;
            public bool roundPosThickness;
            public bool simpleJoin;
            public Vector2 innerCornerVertex;
            public float innerCornerDistToEnd, innerCornerDistFromStart;
        }

        static JoiningInfo ForeseeJoining(BezierSegment end, BezierSegment start, float halfThickness, float endSegmentLength)
        {
            JoiningInfo joinInfo = new JoiningInfo();

            // The joining generates the vertices at both ends as well as the joining itself
            joinInfo.joinPos = end.p3;
            joinInfo.tanAtEnd = VectorUtils.EvalTangent(end, 1.0f);
            joinInfo.normAtEnd = Vector2.Perpendicular(joinInfo.tanAtEnd);
            joinInfo.tanAtStart = VectorUtils.EvalTangent(start, 0.0f);
            joinInfo.normAtStart = Vector2.Perpendicular(joinInfo.tanAtStart);

            // If the tangents are continuous at the join location, we don't have
            // to generate a corner, we do a "simple" join by just connecting the vertices
            // from the two segments directly
            float cosAngleBetweenTans = Vector2.Dot(joinInfo.tanAtEnd, joinInfo.tanAtStart);
            joinInfo.simpleJoin = Mathf.Approximately(Mathf.Abs(cosAngleBetweenTans), 1.0f);
            if (joinInfo.simpleJoin)
                return null;

            joinInfo.posThicknessEnd = joinInfo.joinPos + joinInfo.normAtEnd * halfThickness;
            joinInfo.negThicknessEnd = joinInfo.joinPos - joinInfo.normAtEnd * halfThickness;
            joinInfo.posThicknessStart = joinInfo.joinPos + joinInfo.normAtStart * halfThickness;
            joinInfo.negThicknessStart = joinInfo.joinPos - joinInfo.normAtStart * halfThickness;

            if (joinInfo.simpleJoin)
            {
                joinInfo.posThicknessClosingPoint = Vector2.LerpUnclamped(joinInfo.posThicknessEnd, joinInfo.posThicknessStart, 0.5f);
                joinInfo.negThicknessClosingPoint = Vector2.LerpUnclamped(joinInfo.negThicknessEnd, joinInfo.negThicknessStart, 0.5f);
            }
            else
            {
                joinInfo.posThicknessClosingPoint = VectorUtils.IntersectLines(joinInfo.posThicknessEnd, joinInfo.posThicknessEnd + joinInfo.tanAtEnd, joinInfo.posThicknessStart, joinInfo.posThicknessStart + joinInfo.tanAtStart);
                joinInfo.negThicknessClosingPoint = VectorUtils.IntersectLines(joinInfo.negThicknessEnd, joinInfo.negThicknessEnd + joinInfo.tanAtEnd, joinInfo.negThicknessStart, joinInfo.negThicknessStart + joinInfo.tanAtStart);

                if (float.IsInfinity(joinInfo.posThicknessClosingPoint.x) || float.IsInfinity(joinInfo.posThicknessClosingPoint.y))
                    joinInfo.posThicknessClosingPoint = joinInfo.joinPos;
                if (float.IsInfinity(joinInfo.negThicknessClosingPoint.x) || float.IsInfinity(joinInfo.negThicknessClosingPoint.y))
                    joinInfo.negThicknessClosingPoint = joinInfo.joinPos;
            }

            // Should we round the positive thickness side or the negative thickness side?
            joinInfo.roundPosThickness = PointOnTheLeftOfLine(Vector2.zero, joinInfo.tanAtEnd, joinInfo.tanAtStart);

            // Inner corner vertex should be calculated by intersection of the inner segments
            Vector2[] startTrail = null, endTrail = null;
            Vector2 intersectionOnStart = Vector2.zero, intersectionOnEnd = Vector2.zero;
            if (!joinInfo.simpleJoin)
            {
                BezierSegment endFlipped = VectorUtils.FlipSegment(end);
                Vector2 thicknessClosingPoint = joinInfo.roundPosThickness ? joinInfo.posThicknessClosingPoint : joinInfo.negThicknessClosingPoint;
                Vector2 meetingPoint = end.p3;
                Vector2 thicknessDiagonalEnd = meetingPoint + (thicknessClosingPoint - meetingPoint) * 10.0f;
                startTrail = LineBezierThicknessIntersect(
                        start, joinInfo.roundPosThickness ? -halfThickness : halfThickness, meetingPoint, thicknessDiagonalEnd,
                        out joinInfo.innerCornerDistFromStart, out intersectionOnStart);
                endTrail = LineBezierThicknessIntersect(
                        endFlipped, joinInfo.roundPosThickness ? halfThickness : -halfThickness, meetingPoint, thicknessDiagonalEnd,
                        out joinInfo.innerCornerDistToEnd, out intersectionOnEnd);
            }

            bool intersectionFound = false;
            if ((startTrail != null) && (endTrail != null))
            {
                var intersect = VectorUtils.IntersectLines(startTrail[0], startTrail[1], endTrail[0], endTrail[1]);
                var isOnStartTrail = PointOnLineIsWithinSegment(startTrail[0], startTrail[1], intersect);
                var isOnEndTrail = PointOnLineIsWithinSegment(endTrail[0], endTrail[1], intersect);
                if (!float.IsInfinity(intersect.x) && isOnStartTrail && isOnEndTrail)
                {
                    var vStart = intersectionOnStart - intersect;
                    var vEnd = intersectionOnEnd - intersect;
                    joinInfo.innerCornerDistFromStart += (vStart == Vector2.zero) ? 0.0f : vStart.magnitude;
                    joinInfo.innerCornerDistToEnd += (vEnd == Vector2.zero) ? 0.0f : vEnd.magnitude;
                    joinInfo.innerCornerDistToEnd = endSegmentLength - joinInfo.innerCornerDistToEnd;
                    joinInfo.innerCornerVertex = intersect; // Found it!
                    intersectionFound = true;
                }
            }

            if (!intersectionFound)
            {
                joinInfo.innerCornerVertex = joinInfo.joinPos + ((joinInfo.tanAtStart - joinInfo.tanAtEnd) / 2.0f).normalized * halfThickness;
                joinInfo.innerCornerDistFromStart = 0;
                joinInfo.innerCornerDistToEnd = endSegmentLength;
            }
            return joinInfo;
        }

        static Vector2[] LineBezierThicknessIntersect(BezierSegment seg, float thickness, Vector2 lineFrom, Vector2 lineTo, out float distanceToIntersection, out Vector2 intersection)
        {
            Vector2 tan = VectorUtils.EvalTangent(seg, 0.0f);
            Vector2 nrm = Vector2.Perpendicular(tan);
            Vector2 lastPoint = seg.p0 + nrm * thickness;
            distanceToIntersection = 0.0f;
            intersection = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            float stepT = 0.01f;
            float t = 0;
            while (t < 1.0f)
            {
                t += stepT;
                var point = VectorUtils.EvalFull(seg, t, out tan, out nrm) + nrm * thickness;
                intersection = VectorUtils.IntersectLines(lineFrom, lineTo, lastPoint, point);
                if (PointOnLineIsWithinSegment(lastPoint, point, intersection))
                {
                    distanceToIntersection += (lastPoint - intersection).magnitude;
                    return new Vector2[] { lastPoint, point };
                }
                distanceToIntersection += (lastPoint - point).magnitude;
                lastPoint = point;
            }
            return null;
        }

        static bool PointOnLineIsWithinSegment(Vector2 lineFrom, Vector2 lineTo, Vector2 point)
        {
            // Point is assumed to be already on the line, but we would like to know if it is within the segment specified
            var v = (lineTo - lineFrom).normalized;
            if (Vector2.Dot(point - lineFrom,  v) < -Epsilon)
                return false;
            if (Vector2.Dot(point - lineTo, v) > Epsilon)
                return false;
            return true;
        }

        static void GenerateJoining(JoiningInfo joinInfo, PathCorner corner, float halfThickness, float tippedCornerLimit, TessellationOptions tessellateOptions, List<Vector2> verts, List<UInt16> inds)
        {
            // The joining generates the vertices at both ends as well as the joining itself
            if (verts.Count == 0)
            {
                // Starting a path with a joining (meaning a loop)
                verts.Add(joinInfo.roundPosThickness ? joinInfo.posThicknessEnd : joinInfo.innerCornerVertex);
                verts.Add(joinInfo.roundPosThickness ? joinInfo.innerCornerVertex : joinInfo.negThicknessEnd);
            }

            System.Diagnostics.Debug.Assert(verts.Count >= 2);
            int indexStart = verts.Count - 2; // Using the last two vertices

            // Convert a tipped corner to a beveled one if tippedCornerLimit ratio is reached
            if (corner == PathCorner.Tipped && tippedCornerLimit >= 1.0f)
            {
                var theta = Vector2.Angle(-joinInfo.tanAtEnd, joinInfo.tanAtStart) * Mathf.Deg2Rad;
                var ratio = 1.0f / Mathf.Sin(theta / 2.0f);
                if (ratio > tippedCornerLimit)
                    corner = PathCorner.Beveled;
            }

            if (joinInfo.simpleJoin)
            {
                // TODO
            }
            else if (corner == PathCorner.Tipped)
            {
                verts.Add(joinInfo.posThicknessClosingPoint);
                verts.Add(joinInfo.negThicknessClosingPoint);
                verts.Add(joinInfo.roundPosThickness ? joinInfo.posThicknessStart : joinInfo.innerCornerVertex);
                verts.Add(joinInfo.roundPosThickness ? joinInfo.innerCornerVertex : joinInfo.negThicknessStart);

                // Ending to tip
                inds.Add((UInt16)(indexStart + 0));
                inds.Add((UInt16)(indexStart + 3));
                inds.Add((UInt16)(indexStart + 1));
                inds.Add((UInt16)(indexStart + 0));
                inds.Add((UInt16)(indexStart + 2));
                inds.Add((UInt16)(indexStart + 3));

                // Tip to starting
                inds.Add((UInt16)(indexStart + 4));
                inds.Add((UInt16)(indexStart + 3));
                inds.Add((UInt16)(indexStart + 2));
                inds.Add((UInt16)(indexStart + 4));
                inds.Add((UInt16)(indexStart + 5));
                inds.Add((UInt16)(indexStart + 3));

                return;
            }
            else if (corner == PathCorner.Beveled)
            {
                verts.Add(joinInfo.roundPosThickness ? joinInfo.posThicknessEnd : joinInfo.innerCornerVertex); // 2
                verts.Add(joinInfo.roundPosThickness ? joinInfo.innerCornerVertex : joinInfo.negThicknessEnd); // 3
                verts.Add(joinInfo.roundPosThickness ? joinInfo.posThicknessStart : joinInfo.innerCornerVertex); // 4
                verts.Add(joinInfo.roundPosThickness ? joinInfo.innerCornerVertex : joinInfo.negThicknessStart); // 5

                // Ending to tip
                inds.Add((UInt16)(indexStart + 0));
                inds.Add((UInt16)(indexStart + 2));
                inds.Add((UInt16)(indexStart + 1));
                inds.Add((UInt16)(indexStart + 1));
                inds.Add((UInt16)(indexStart + 2));
                inds.Add((UInt16)(indexStart + 3));

                // Bevel
                if (joinInfo.roundPosThickness)
                {
                    inds.Add((UInt16)(indexStart + 2));
                    inds.Add((UInt16)(indexStart + 4));
                    inds.Add((UInt16)(indexStart + 3));
                }
                else
                {
                    inds.Add((UInt16)(indexStart + 3));
                    inds.Add((UInt16)(indexStart + 2));
                    inds.Add((UInt16)(indexStart + 5));
                }

                return;
            }

            if (corner == PathCorner.Round)
            {
                float sweepAngle = Mathf.Acos(Vector2.Dot(joinInfo.normAtEnd, joinInfo.normAtStart));
                bool flipArc = false;
                if (!PointOnTheLeftOfLine(Vector2.zero, joinInfo.normAtEnd, joinInfo.normAtStart))
                {
                    sweepAngle = -sweepAngle;
                    flipArc = true;
                }

                UInt16 innerCornerVertexIndex = (UInt16)verts.Count;
                verts.Add(joinInfo.innerCornerVertex);

                int arcSegments = CalculateArcSteps(halfThickness, 0, sweepAngle, tessellateOptions);
                for (int i = 0; i <= arcSegments; i++)
                {
                    float angle = sweepAngle * (i / (float)arcSegments);
                    Vector2 nrm = Matrix2D.Rotate(angle) * joinInfo.normAtEnd;
                    if (flipArc) nrm = -nrm;
                    verts.Add(nrm * halfThickness + joinInfo.joinPos);

                    if (i == 0)
                    {
                        inds.Add((UInt16)(indexStart + 0));
                        inds.Add((UInt16)(indexStart + 3));
                        inds.Add((UInt16)(indexStart + (joinInfo.roundPosThickness ? 2 : 1)));

                        inds.Add((UInt16)(indexStart + 0));
                        inds.Add((UInt16)(indexStart + 2));
                        inds.Add((UInt16)(indexStart + (joinInfo.roundPosThickness ? 1 : 3)));
                    }
                    else
                    {
                        if (joinInfo.roundPosThickness)
                        {
                            inds.Add((UInt16)(indexStart + i + (flipArc ? 3 : 2)));
                            inds.Add((UInt16)(indexStart + i + (flipArc ? 2 : 3)));
                            inds.Add(innerCornerVertexIndex);
                        }
                        else
                        {
                            inds.Add((UInt16)(indexStart + i + (flipArc ? 3 : 2)));
                            inds.Add((UInt16)(indexStart + i + (flipArc ? 2 : 3)));
                            inds.Add(innerCornerVertexIndex);
                        }
                    }
                }

                // Manually add the last segment, maintain the expected vertex positioning
                int endingVerticesIndex = verts.Count;
                if (joinInfo.roundPosThickness)
                {
                    verts.Add(joinInfo.posThicknessStart);
                    verts.Add(joinInfo.innerCornerVertex);
                }
                else
                {
                    verts.Add(joinInfo.innerCornerVertex);
                    verts.Add(joinInfo.negThicknessStart);
                }
                inds.Add((UInt16)(endingVerticesIndex - 1));
                inds.Add((UInt16)(endingVerticesIndex + 0));
                inds.Add(innerCornerVertexIndex);
            }
        }

        static void GenerateTip(BezierSegment segment, bool atStart, float t, PathEnding ending, float halfThickness, TessellationOptions tessellateOptions, List<Vector2> verts, List<UInt16> inds)
        {
            // The tip includes the vertices at the end itself
            Vector2 tan, nrm;
            var pos = VectorUtils.EvalFull(segment, t, out tan, out nrm);
            int indexStart = verts.Count;

            switch (ending)
            {
                case PathEnding.Chop:
                    if (atStart)
                    {
                        verts.Add(pos + nrm * halfThickness);
                        verts.Add(pos - nrm * halfThickness);
                    }
                    else
                    {
                        // Not much, path segments are always expected to be generated perpendicular to the path
                        // at the segment point location, so we don't have to do anything for the ending
                    }
                    break;

                case PathEnding.Square:
                    if (atStart)
                    {
                        verts.Add(pos + nrm * halfThickness - tan * halfThickness);
                        verts.Add(pos - nrm * halfThickness - tan * halfThickness);
                        verts.Add(pos + nrm * halfThickness);
                        verts.Add(pos - nrm * halfThickness);

                        inds.Add((UInt16)(indexStart + 0));
                        inds.Add((UInt16)(indexStart + 3));
                        inds.Add((UInt16)(indexStart + 1));
                        inds.Add((UInt16)(indexStart + 0));
                        inds.Add((UInt16)(indexStart + 2));
                        inds.Add((UInt16)(indexStart + 3));
                    }
                    else
                    {
                        // Relying on the last two vertices, and just adding two of our own here
                        verts.Add(pos + nrm * halfThickness + tan * halfThickness);
                        verts.Add(pos - nrm * halfThickness + tan * halfThickness);

                        inds.Add((UInt16)(indexStart + 0 - 2));
                        inds.Add((UInt16)(indexStart + 3 - 2));
                        inds.Add((UInt16)(indexStart + 1 - 2));
                        inds.Add((UInt16)(indexStart + 0 - 2));
                        inds.Add((UInt16)(indexStart + 2 - 2));
                        inds.Add((UInt16)(indexStart + 3 - 2));
                    }
                    break;

                case PathEnding.Round:
                    float arcSign = atStart ? -1 : 1;
                    int arcSegments = CalculateArcSteps(halfThickness, 0, Mathf.PI, tessellateOptions);
                    for (int i = 1; i < arcSegments; i++)
                    {
                        float angle = Mathf.PI * (i / (float)arcSegments);
                        verts.Add(pos + Matrix2D.Rotate(angle) * nrm * halfThickness * arcSign);
                    }

                    if (atStart)
                    {
                        // Note how we maintain the last two vertices being setup for connection by the rest of the path vertices
                        int indexTipStart = verts.Count;
                        verts.Add(pos + nrm * halfThickness);
                        verts.Add(pos - nrm * halfThickness);

                        for (int i = 1; i < arcSegments; i++)
                        {
                            inds.Add((UInt16)(indexTipStart + 1));
                            inds.Add((UInt16)(indexStart + i - 1));
                            inds.Add((UInt16)(indexStart + i));
                        }
                    }
                    else
                    {
                        inds.Add((UInt16)(indexStart - 1));
                        inds.Add((UInt16)(indexStart - 2));
                        inds.Add((UInt16)(indexStart + 0));
                        for (int i = 1; i < arcSegments - 1; i++)
                        {
                            inds.Add((UInt16)(indexStart - 1));
                            inds.Add((UInt16)(indexStart + i - 1));
                            inds.Add((UInt16)(indexStart + i));
                        }
                    }
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false); // Joining has its own function
                    break;
            }
        }

        static int CalculateArcSteps(float radius, float fromAngle, float toAngle, TessellationOptions tessellateOptions)
        {
            float stepDivisor = float.MaxValue;

            if (tessellateOptions.stepDistance != float.MaxValue)
                stepDivisor = tessellateOptions.stepDistance / radius;

            if (tessellateOptions.maxCordDeviation != float.MaxValue)
            {
                float y = radius - tessellateOptions.maxCordDeviation;
                float cordHalfLength = Mathf.Sqrt(radius * radius - y * y);
                float div = Mathf.Min(stepDivisor, Mathf.Asin(cordHalfLength / radius));
                if (div > VectorUtils.Epsilon)
                    stepDivisor = div;
            }

            if (tessellateOptions.maxTanAngleDeviation < Mathf.PI * 0.5f)
                stepDivisor = Mathf.Min(stepDivisor, tessellateOptions.maxTanAngleDeviation * 2.0f);

            float stepsInFullCircle = (Mathf.PI * 2.0f) / stepDivisor;
            float arcPercentage = Mathf.Abs(fromAngle - toAngle) / (Mathf.PI * 2.0f);
            return (int)Mathf.Max(stepsInFullCircle * arcPercentage + 0.5f, 3); // Never less than 3 segments
        }

        /// <summary>Tessellates a rectangle.</summary>
        /// <param name="rect">Rectangle to tessellate</param>
        /// <param name="vertices">The output vertices</param>
        /// <param name="indices">The output triangles</param>
        public static void TessellateRect(Rect rect, out Vector2[] vertices, out UInt16[] indices)
        {
            vertices = new Vector2[] {
                new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMax, rect.yMax),
                new Vector2(rect.xMin, rect.yMax)
            };
            indices = new UInt16[] {
                1, 0, 2, 2, 0, 3
            };
        }

        /// <summary>Tessellates a rectangle border.</summary>
        /// <param name="rect">Rectangle to tessellate</param>
        /// <param name="halfThickness">The half-thickness of the border</param>
        /// <param name="vertices">The output vertices</param>
        /// <param name="indices">The output triangles</param>
        public static void TessellateRectBorder(Rect rect, float halfThickness, out Vector2[] vertices, out UInt16[] indices)
        {
            var verts = new List<Vector2>(16);
            var inds = new List<UInt16>(24);

            // Left edge
            var p0 = new Vector2(rect.x, rect.y + rect.height);
            var p1 = new Vector2(rect.x, rect.y);

            var q0 = p0 + new Vector2(-halfThickness, halfThickness);
            var q1 = p1 + new Vector2(-halfThickness, -halfThickness);
            var q2 = p1 + new Vector2(halfThickness, halfThickness);
            var q3 = p0 + new Vector2(halfThickness, -halfThickness);

            verts.Add(q0); verts.Add(q1); verts.Add(q2); verts.Add(q3);
            inds.Add(0); inds.Add(3); inds.Add(2); inds.Add(2); inds.Add(1); inds.Add(0);

            // Top edge
            p0 = new Vector2(rect.x, rect.y);
            p1 = new Vector2(rect.x + rect.width, rect.y);

            q0 = p0 + new Vector2(-halfThickness, -halfThickness);
            q1 = p1 + new Vector2(halfThickness, -halfThickness);
            q2 = p1 + new Vector2(-halfThickness, halfThickness);
            q3 = p0 + new Vector2(halfThickness, halfThickness);

            verts.Add(q0); verts.Add(q1); verts.Add(q2); verts.Add(q3);
            inds.Add(4); inds.Add(7); inds.Add(6); inds.Add(6); inds.Add(5); inds.Add(4);

            // Right edge
            p0 = new Vector2(rect.x + rect.width, rect.y);
            p1 = new Vector2(rect.x + rect.width, rect.y + rect.height);

            q0 = p0 + new Vector2(halfThickness, -halfThickness);
            q1 = p1 + new Vector2(halfThickness, halfThickness);
            q2 = p1 + new Vector2(-halfThickness, -halfThickness);
            q3 = p0 + new Vector2(-halfThickness, halfThickness);

            verts.Add(q0); verts.Add(q1); verts.Add(q2); verts.Add(q3);
            inds.Add(8); inds.Add(11); inds.Add(10); inds.Add(10); inds.Add(9); inds.Add(8);

            // Bottom edge
            p0 = new Vector2(rect.x + rect.width, rect.y + rect.height);
            p1 = new Vector2(rect.x, rect.y + rect.height);

            q0 = p0 + new Vector2(halfThickness, halfThickness);
            q1 = p1 + new Vector2(-halfThickness, halfThickness);
            q2 = p1 + new Vector2(halfThickness, -halfThickness);
            q3 = p0 + new Vector2(-halfThickness, -halfThickness);

            verts.Add(q0); verts.Add(q1); verts.Add(q2); verts.Add(q3);
            inds.Add(12); inds.Add(15); inds.Add(14); inds.Add(14); inds.Add(13); inds.Add(12);

            vertices = verts.ToArray();
            indices = inds.ToArray();
        }
    }
}
