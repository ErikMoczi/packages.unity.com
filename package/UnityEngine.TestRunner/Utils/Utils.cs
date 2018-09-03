using System;

namespace UnityEngine.TestTools.Utils
{
    public static class Utils
    {
        private const float k_FEpsilon = 0.0001f;

        public static bool AreFloatsEqual(float expected, float actual, float allowedRelativeError)
        {
            if (IsFloatCloseToZero(actual) && IsFloatCloseToZero(expected)
                || (expected == Mathf.Infinity && actual == Mathf.Infinity)
                || (expected == Mathf.NegativeInfinity && actual == Mathf.NegativeInfinity)
                || (Math.Abs(actual - expected) < k_FEpsilon))
            {
                return true;
            }

            return Math.Abs((actual - expected) / expected) <= allowedRelativeError;
        }

        private static bool IsFloatCloseToZero(float a)
        {
            return Math.Abs(a) < k_FEpsilon;
        }

        public static bool AreFloatsEqualAbsoluteError(float expected, float actual, float allowedAbsoluteError)
        {
            return Math.Abs(actual - expected) <= allowedAbsoluteError;
        }

        /// <summary>
        /// Analogous to GameObject.CreatePrimitive, but creates a primitive mesh renderer with fast shader instead of a default builtin shader.
        /// Optimized for testing performance.
        /// </summary>
        /// <returns>A GameObject with primitive mesh renderer and collider.</returns>
        public static GameObject CreatePrimitive(PrimitiveType type)
        {
            var prim = GameObject.CreatePrimitive(type);
            var renderer = prim.GetComponent<Renderer>();
            if (renderer)
                renderer.sharedMaterial = new Material(Shader.Find("VertexLit"));
            return prim;
        }
    }
}
