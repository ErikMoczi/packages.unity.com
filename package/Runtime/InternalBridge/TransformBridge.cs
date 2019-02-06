using UnityEngine;

namespace Unity.Tiny
{
    internal static class TransformBridge
    {
        public static Vector3 GetLocalEulerAngles(this Transform transform)
        {
            return transform.GetLocalEulerAngles(transform.rotationOrder);
        }

        public static void SetLocalEulerAngles(this Transform transform, Vector3 eulerAngles)
        {
            transform.SetLocalEulerAngles(eulerAngles, transform.rotationOrder);
        }
    }
}