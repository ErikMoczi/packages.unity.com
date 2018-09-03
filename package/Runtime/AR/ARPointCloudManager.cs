using System;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// Creates and updates a <c>GameObject</c> under the <see cref="ARSessionOrigin"/>'s TrackablesParent
    /// to represent a point cloud.
    /// </summary>
    [RequireComponent(typeof(ARSessionOrigin))]
    [DisallowMultipleComponent]
    public sealed class ARPointCloudManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("If not null, instantiates this prefab for the created point cloud.")]
        GameObject m_PointCloudPrefab;

        /// <summary>
        /// Getter/setter for the Point Cloud Prefab.
        /// </summary>
        public GameObject pointCloudPrefab
        {
            get { return m_PointCloudPrefab; }
            set { m_PointCloudPrefab = value; }
        }

        /// <summary>
        /// Raised each time the <see cref="ARPointCloud"/> is updated.
        /// </summary>
        public event Action<ARPointCloudUpdatedEventArgs> pointCloudUpdated;

        void Awake()
        {
            m_SessionOrigin = GetComponent<ARSessionOrigin>();
        }

        void OnEnable()
        {
            ARSubsystemManager.pointCloudUpdated += OnPointCloudUpdated;
        }

        void OnDisable()
        {
            ARSubsystemManager.pointCloudUpdated -= OnPointCloudUpdated;
        }

        ARPointCloud GetOrCreatePointCloud()
        {
            if (m_PointCloud != null)
                return m_PointCloud;

            GameObject newGameObject;
            if (pointCloudPrefab != null)
            {
                newGameObject = Instantiate(pointCloudPrefab, m_SessionOrigin.trackablesParent);
            }
            else
            {
                newGameObject = new GameObject();
                newGameObject.transform.SetParent(m_SessionOrigin.trackablesParent, false);
            }

            newGameObject.layer = gameObject.layer;

            var pointCloud = newGameObject.GetComponent<ARPointCloud>();
            if (pointCloud == null)
                pointCloud = newGameObject.AddComponent<ARPointCloud>();

            return pointCloud;
        }

        void OnPointCloudUpdated(PointCloudUpdatedEventArgs eventArgs)
        {
            m_PointCloud = GetOrCreatePointCloud();
            m_PointCloud.OnUpdated();

            if (pointCloudUpdated != null)
                pointCloudUpdated(new ARPointCloudUpdatedEventArgs(m_PointCloud));
        }

        ARPointCloud m_PointCloud;

        ARSessionOrigin m_SessionOrigin;
    }
}
