using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// Represents a detected point cloud, aka feature points.
    /// </summary>
    [DefaultExecutionOrder(ARUpdateOrder.k_PointCloud)]
    [DisallowMultipleComponent]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@1.0/api/UnityEngine.XR.ARFoundation.ARPointCloud.html")]
    public class ARPointCloud : ARTrackable<XRPointCloud, ARPointCloud>
    {
        /// <summary>
        /// Invoked whenenver the point cloud is updated.
        /// </summary>
        public event Action<ARPointCloudUpdatedEventArgs> updated;

        /// <summary>
        /// Replaces the contents of <paramref name="points"/> with the feature points in Unity world space.
        /// </summary>
        /// <param name="points">A <c>List</c> of <c>Vector3</c>s. The contents are replaced with the current point cloud.</param>
        /// <param name="space">Which coordinate system to use. <c>Space.Self</c> refers to point cloud space (i.e., relative to
        /// the local transform), while <c>Space.World</c> refers to Unity world space. The default is <c>Space.World</c>.</param>
        public void GetPoints(List<Vector3> featurePoints, Space space = Space.World)
        {
            featurePoints.Clear();
            featurePoints.AddRange(m_Points);

            if (space == Space.World)
                transform.parent.TransformPointList(featurePoints);
        }

        /// <summary>
        /// Get the feature points as a new <c>NativeArray</c>. The caller owns the memory and is responsible for calling
        /// <c>Dispose</c> on the returned array. The points are relative to this point cloud's local transform.
        /// </summary>
        /// <param name="allocator">The allocator to use when creating the returned <c>NativeArray</c>.</param>
        /// <returns>A new <c>NativeArray</c> containing all the points in this point cloud relative to the point
        /// cloud's local transform.</returns>
        public NativeArray<Vector3> GetPoints(Allocator allocator)
        {
            var points = new NativeArray<Vector3>(m_Points.Count, allocator);
            for (int i = 0; i < points.Length; ++i)
                points[i] = m_Points[i];
            return points;
        }

        /// <summary>
        /// Gets the confidence values for each point in the point cloud.
        /// </summary>
        /// <param name="confidence">A <c>List</c> of <c>float</c>s representing the confidence values for each point
        /// in the point cloud. The contents are replaced with the current confidence values.</param>
        public void GetConfidenceValues(List<float> confidence)
        {
            confidence.Clear();
            confidence.AddRange(m_Confidence);
        }

        /// <summary>
        /// Gets the unique IDs for each point in the point cloud.
        /// </summary>
        /// <param name="ids">A <c>List</c> of <c>ulong</c>s representing the unique ID for each point
        /// in the point cloud. The contents are replaced with the current confidence values.</param>
        public void GetIds(List<ulong> ids)
        {
            ids.Clear();
            ids.AddRange(m_Ids);
        }

        void Awake()
        {
            m_Points = new List<Vector3>();
            m_Confidence = new List<float>();
            m_Ids = new List<ulong>();
        }

        void Update()
        {
            if (m_PointsUpdated && updated != null)
            {
                m_PointsUpdated = false;
                updated(new ARPointCloudUpdatedEventArgs());
            }
        }

        internal void SetPoints(NativeArray<Vector3> points)
        {
            m_Points.Clear();
            foreach (var position in points)
                m_Points.Add(position);

            m_PointsUpdated = true;
        }

        internal void SetConfidence(NativeArray<float> confidenceValues)
        {
            m_Confidence.Clear();
            foreach (var confidenceValue in confidenceValues)
                m_Confidence.Add(confidenceValue);
        }

        internal void SetIdentifiers(NativeArray<ulong> identifiers)
        {
            m_Ids.Clear();
            foreach (var id in identifiers)
                m_Ids.Add(id);
        }

        internal void Clear()
        {
            m_Points.Clear();
            m_Confidence.Clear();
            m_Ids.Clear();
        }

        List<Vector3> m_Points;

        List<float> m_Confidence;

        List<ulong> m_Ids;

        bool m_PointsUpdated = false;
    }
}
