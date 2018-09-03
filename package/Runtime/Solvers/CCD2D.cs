namespace UnityEngine.Experimental.U2D.IK
{
    public static class CCD2D
    {
        public static bool Solve(Vector3 targetPosition, Vector3 forward, int solverLimit, float tolerance, float velocity, ref Vector3[] positions)
        {
            int last = positions.Length - 1;
            int iterations = 0;
            float sqrTolerance = tolerance * tolerance;
            float sqrDistanceToTarget = (targetPosition - positions[last]).sqrMagnitude;
            while (sqrDistanceToTarget > sqrTolerance)
            {
                DoIteration(targetPosition, forward, last, velocity, ref positions);
                sqrDistanceToTarget = (targetPosition - positions[last]).sqrMagnitude;
                if (++iterations >= solverLimit)
                    break;
            }
            return iterations != 0;
        }

        static void DoIteration(Vector3 targetPosition, Vector3 forward, int last, float velocity, ref Vector3[] positions)
        {
            for (int i = last - 1; i >= 0; --i)
            {
                Vector3 toTarget = targetPosition - positions[i];
                Vector3 toLast = positions[last] - positions[i];

                float angle = Vector3.SignedAngle(toLast, toTarget, forward);
                angle = Mathf.Lerp(0f, angle, velocity);

                Quaternion deltaRotation = Quaternion.AngleAxis(angle, forward);
                for (int j = last; j > i; --j)
                    positions[j] = RotatePositionFrom(positions[j], positions[i], deltaRotation);
            }
        }

        static Vector3 RotatePositionFrom(Vector3 position, Vector3 pivot, Quaternion rotation)
        {
            Vector3 v = position - pivot;
            v = rotation * v;
            return pivot + v;
        }
    }
}
