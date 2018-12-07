using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public static class AnimationRuntimeUtils
    {
        public static void SolveTwoBoneIK(
            AnimationStream stream,
            TransformHandle root,
            TransformHandle mid,
            TransformHandle tip,
            TransformHandle target,
            TransformHandle hint,
            float posWeight,
            float rotWeight,
            float hintWeight,
            Vector2 limbLengths
            )
        {
            Vector3 aPosition = root.GetPosition(stream);
            Vector3 bPosition = mid.GetPosition(stream);
            Vector3 cPosition = tip.GetPosition(stream);
            Vector3 tPosition = Vector3.Lerp(cPosition, target.GetPosition(stream), posWeight);
            Quaternion tRotation = Quaternion.Lerp(tip.GetRotation(stream), target.GetRotation(stream), rotWeight);

            Vector3 ab = bPosition - aPosition;
            Vector3 bc = cPosition - bPosition;
            Vector3 ac = cPosition - aPosition;
            Vector3 at = tPosition - aPosition;

            float oldAbcAngle = TriangleAngle(ac.magnitude, limbLengths[0], limbLengths[1]);
            float newAbcAngle = TriangleAngle(at.magnitude, limbLengths[0], limbLengths[1]);

            Vector3 axis = Vector3.Cross(ab, bc).normalized;
            if (Vector3.Dot(axis, axis) < Vector3.kEpsilon)
                axis = Vector3.up;

            float a = 0.5f * (oldAbcAngle - newAbcAngle);
            float sin = Mathf.Sin(a);
            float cos = Mathf.Cos(a);
            Quaternion deltaR = new Quaternion(axis.x * sin, axis.y * sin, axis.z * sin, cos);
            mid.SetRotation(stream, deltaR * mid.GetRotation(stream));

            cPosition = tip.GetPosition(stream);
            ac = cPosition - aPosition;
            root.SetRotation(stream, Quaternion.FromToRotation(ac, at) * root.GetRotation(stream));

            if (hint.IsValid(stream) && hintWeight > 0f)
            {
                float acSqrMag = ac.sqrMagnitude;
                if (acSqrMag > 0f)
                {
                    bPosition = mid.GetPosition(stream);
                    cPosition = tip.GetPosition(stream);
                    ab = bPosition - aPosition;
                    ac = cPosition - aPosition;

                    Vector3 acNorm = ac / Mathf.Sqrt(acSqrMag);
                    Vector3 ah = hint.GetPosition(stream) - aPosition;
                    Vector3 abProj = ab - acNorm * Vector3.Dot(ab, acNorm);
                    Vector3 ahProj = ah - acNorm * Vector3.Dot(ah, acNorm);

                    float maxReach = limbLengths[0] + limbLengths[1];
                    if (abProj.sqrMagnitude > (maxReach * maxReach * 0.001f) && ahProj.sqrMagnitude > 0f)
                    {
                        Quaternion hintR = Quaternion.FromToRotation(abProj, ahProj);
                        hintR.x *= hintWeight;
                        hintR.y *= hintWeight;
                        hintR.z *= hintWeight;
                        root.SetRotation(stream, hintR * root.GetRotation(stream));
                    }
                }
            }

            tip.SetRotation(stream, tRotation);
        }

        static float TriangleAngle(float aLen, float aLen1, float aLen2)
        {
            float c = Mathf.Clamp((aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) / 2.0f, -1.0f, 1.0f);
            return Mathf.Acos(c);
        }

        public static void SolveLookAt(
            Vector3 jointPosition,
            Quaternion jointRotation,
            Vector3 target,
            Quaternion offset,
            Vector3 lookAtAxis,
            Vector2 limits,
            out Quaternion outRotation
            )
        {
            var fromDir = jointRotation * lookAtAxis;
            var toDir = offset * (target - jointPosition);

            var axis = Vector3.Cross(fromDir, toDir).normalized;
            var angle = Vector3.Angle(fromDir, toDir);
            angle = Mathf.Clamp(angle, limits.x, limits.y);
            var boneToTargetRotation = Quaternion.AngleAxis(angle, axis);
        
            outRotation = boneToTargetRotation * jointRotation;
        }

        // Implementation of unconstrained FABRIK solver : Forward and Backward Reaching Inverse Kinematic
        // Aristidou A, Lasenby J. FABRIK: a fast, iterative solver for the inverse kinematics problem. Graphical Models 2011; 73(5): 243–260.
        public static bool SolveFABRIK(
            NativeArray<Vector3> linkPositions,
            NativeArray<float> linkLengths,
            Vector3 target,
            float tolerance,
            float maxReach,
            int maxIterations
            )
        {
            // If the target is unreachable
            var rootToTargetDir = target - linkPositions[0];
            if (rootToTargetDir.sqrMagnitude > (maxReach * maxReach))
            {
                // Line up chain towards target
                var dir = rootToTargetDir.normalized;
                for (int i = 1; i < linkPositions.Length; ++i)
                    linkPositions[i] = linkPositions[i - 1] + dir * linkLengths[i - 1];

                return true;
            }
            else
            {
                int tipIndex = linkPositions.Length - 1;
                float sqrTolerance = tolerance * tolerance;
                if (SqrDistance(linkPositions[tipIndex], target) > sqrTolerance)
                {
                    var rootPos = linkPositions[0];
                    int iteration = 0;

                    do
                    {
                        // Forward reaching phase
                        // Set tip to target and propagate displacement to rest of chain
                        linkPositions[tipIndex] = target;
                        for (int i = tipIndex - 1; i > -1; --i)
                            linkPositions[i] = linkPositions[i + 1] + ((linkPositions[i] - linkPositions[i + 1]).normalized * linkLengths[i]);

                        // Backward reaching phase
                        // Set root back at it's original position and propagate displacement to rest of chain
                        linkPositions[0] = rootPos;
                        for (int i = 1; i < linkPositions.Length; ++i)
                            linkPositions[i] = linkPositions[i - 1] + ((linkPositions[i] - linkPositions[i - 1]).normalized * linkLengths[i - 1]);
                    }
                    while ((SqrDistance(linkPositions[tipIndex], target) > sqrTolerance) && (++iteration < maxIterations));

                    return true;
                }
            }

            return false;
        }

        public static float SqrDistance(Vector3 p0, Vector3 p1)
        {
            return (p1 - p0).sqrMagnitude;
        }

        public static Vector3 Lerp(Vector3 a, Vector3 b, Vector3 t)
        {
            return Vector3.Scale(a, Vector3.one - t) + Vector3.Scale(b, t);
        }

        public static float Sum(AnimationJobCache.Cache cache, NativeArray<AnimationJobCache.Index> indices)
        {
            if (!indices.IsCreated || indices.Length == 0)
                return 0f;

            float sum = 0f;
            for (int i = 0; i < indices.Length; ++i)
                sum += cache.GetFloat(indices[i]);

            return sum;
        }
    }
}