using System;
using System.Collections.Generic;

namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// A cache of resuable data containers. Useful for avoiding excessive garbage collection.
    /// </summary>
    /// <remarks>
    /// To avoid garbage collected allocations, you should reuse
    /// the same <c>List</c> object for any AR API that accepts a <c>List</c>. You may
    /// use these to avoid creating several such objects. Beware that the
    /// results are ephemeral, and this cache may be overwritten by other
    /// systems.
    /// 
    /// The list grows (reallocates) when necessary (that is, its capacity is
    /// insufficient for new results).
    /// </remarks>
    internal static class ARDataCache
    {
        /// <summary>
        /// A reuable cache of <c>Vector4</c>s.
        /// </summary>
        public static List<Vector4> vector4List
        {
            get { return s_Vector4s; }
        }

        /// <summary>
        /// A reuable cache of <c>Vector3</c>s.
        /// </summary>
        public static List<Vector3> vector3List
        {
            get { return s_Vector3s; }
        }

        /// <summary>
        /// A reusable cache of <c>Vector2</c>s.
        /// </summary>
        public static List<Vector2> vector2List
        {
            get { return s_Vector2s; }
        }

        /// <summary>
        /// A resuable cache of <c>int</c>s.
        /// </summary>
        public static List<int> intList
        {
            get { return s_Ints; }
        }

        /// <summary>
        /// A reusable cache of <see cref="ARRaycastHit" />s.
        /// </summary>
        public static List<ARRaycastHit> raycastHitList
        {
            get { return s_RaycastHits; }
        }

        /// <summary>
        /// Copies each element of <paramref name="src"/> to <paramref name="dst"/>.
        /// </summary>
        /// <remarks>
        /// If <paramref name="src"/> or <paramref name="dst"/> are <c>null</c>,
        /// this method throws <c>ArgumentNullException</c>.
        /// </remarks>
        /// <typeparam name="T">The type of <c>List</c></typeparam>
        /// <param name="src">The source <c>List</c></param>
        /// <param name="dst">The destination <c>List</c></param>
        public static void CopyList<T>(List<T> src, List<T> dst)
        {
            if (src == null)
                throw new ArgumentNullException("src");

            if (dst == null)
                throw new ArgumentNullException("dst");

            if (dst.Capacity < src.Capacity)
                dst.Capacity = src.Capacity;

            dst.Clear();

            for (int i = 0; i < src.Count; ++i)
                dst.Add(src[i]);
        }

        static List<Vector4> s_Vector4s = new List<Vector4>();

        static List<Vector3> s_Vector3s = new List<Vector3>();

        static List<Vector2> s_Vector2s = new List<Vector2>();

        static List<int> s_Ints = new List<int>();

        static List<ARRaycastHit> s_RaycastHits = new List<ARRaycastHit>();
    }
}