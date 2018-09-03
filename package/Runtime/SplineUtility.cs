using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.U2D
{
    public class SplineUtility
    {
        public static void CalculateTangents(Vector3 point, Vector3 prevPoint, Vector3 nextPoint, Vector3 forward, float scale, out Vector3 rightTangent, out Vector3 leftTangent)
        {
            Vector3 v1 = (prevPoint - point).normalized;
            Vector3 v2 = (nextPoint - point).normalized;
            Vector3 v3 = v1 + v2;
            Vector3 cross = forward;

            if (prevPoint != nextPoint)
            {
                bool colinear = Mathf.Abs(v1.x * v2.y - v1.y * v2.x + v1.x * v2.z - v1.z * v2.x + v1.y * v2.z - v1.z * v2.y) < 0.01f;

                if (colinear)
                {
                    rightTangent = v2 * scale;
                    leftTangent = v1 * scale;
                    return;
                }

                cross = Vector3.Cross(v1, v2);
            }

            rightTangent = Vector3.Cross(cross, v3).normalized * scale;
            leftTangent = -rightTangent;
        }

        public static int NextIndex(int index, int pointCount)
        {
            return Mod(index + 1, pointCount);
        }

        public static int PreviousIndex(int index, int pointCount)
        {
            return Mod(index - 1, pointCount);
        }

        private static int Mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }
    }
}
