namespace UnityEngine.Experimental.U2D.IK
{
    public static class Limb
    {
        public static bool Solve(Vector3 targetPosition, float[] lengths, Vector3[] positions, ref float[] angles)
        {
            angles[0] = 0f;
            angles[1] = 0f;

            if (lengths[0] == 0f || lengths[1] == 0f)
                return false;

            Vector3 startToEnd = targetPosition - positions[0];
            float distanceMagnitude = startToEnd.magnitude;
            float sqrDistance = startToEnd.sqrMagnitude;

            float sqrParentLength = (lengths[0] * lengths[0]);
            float sqrTargetLength = (lengths[1] * lengths[1]);

            float angle0Cos = (sqrDistance + sqrParentLength - sqrTargetLength) / (2f * lengths[0] * distanceMagnitude);
            float angle1Cos = (sqrDistance - sqrParentLength - sqrTargetLength) / (2f * lengths[0] * lengths[1]);

            if ((angle0Cos >= -1f && angle0Cos <= 1f) && (angle1Cos >= -1f && angle1Cos <= 1f))
            {
                angles[0] = Mathf.Acos(angle0Cos) * Mathf.Rad2Deg;
                angles[1] = Mathf.Acos(angle1Cos) * Mathf.Rad2Deg;
            }

            return true;
        }
    }
}
