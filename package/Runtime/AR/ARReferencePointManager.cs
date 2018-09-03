using System;
using System.Collections.Generic;
using UnityEngine.Experimental.XR;
using UnityEngine.Assertions;

namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// Manages reference points (aka anchors).
    /// </summary>
    /// <remarks>
    /// Use this component to programmatically add, remove, or query for
    /// reference points. Reference points are <c>Pose</c>s in the world
    /// which will be periodically updated by an AR devices as its understanding
    /// of the world changes.
    /// </remarks>
    [RequireComponent(typeof(ARSessionOrigin))]
    public sealed class ARReferencePointManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("If not null, instantiates this prefab for each instantiated reference point.")]
        GameObject m_ReferencePointPrefab;

        /// <summary>
        /// Getter/setter for the Reference Point Prefab.
        /// </summary>
        public GameObject referencePointPrefab
        {
            get { return m_ReferencePointPrefab; }
            set { m_ReferencePointPrefab = value; }
        }

        /// <summary>
        /// Raised each time an <see cref="ARReferencePoint"/> is updated.
        /// </summary>
        /// <remarks>
        /// This can happen when the <see cref="ARReferencePoint"/>'s <c>Pose</c> changes, or
        /// when its <c>TrackingState</c> changes.
        /// </remarks>
        public event Action<ARReferencePointUpdatedEventArgs> referencePointUpdated;

        /// <summary>
        /// Attempts to add an <see cref="ARReferencePoint"/> with the given <c>Pose</c>.
        /// </summary>
        /// <remarks>
        /// If <see cref="referencePointPrefab"/> is not null, a new instance will be created. Otherwise, a
        /// new <c>GameObject</c> will be created. In either case, this method ensures that the resulting
        /// <c>GameObject</c> has a <see cref="ARReferencePoint"/> component on it.
        /// </remarks>
        /// <param name="pose">The pose, in Unity world space, of the <see cref="ARReferencePoint"/>.</param>
        /// <returns>If successful, a new <see cref="ARReferencePoint"/>. Otherwise, <c>null</c>.</returns>
        public ARReferencePoint TryAddReferencePoint(Pose pose)
        {
            return TryAddReferencePoint(pose.position, pose.rotation);
        }

        /// <summary>
        /// Attempts to add an <see cref="ARReferencePoint"/> with the given <paramref name="position"/> and <paramref name="rotation"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="referencePointPrefab"/> is not null, a new instance will be created. Otherwise, a
        /// new <c>GameObject</c> will be created. In either case, this method ensures that the resulting
        /// <c>GameObject</c> has a <see cref="ARReferencePoint"/> component on it.
        /// </remarks>
        /// <param name="position">The position, in Unity world space, of the <see cref="ARReferencePoint"/>.</param>
        /// <param name="rotation">The rotation, in Unity world space, of the <see cref="ARReferencePoint"/>.</param>
        /// <returns>If successful, a new <see cref="ARReferencePoint"/>. Otherwise, <c>null</c>.</returns>
        public ARReferencePoint TryAddReferencePoint(Vector3 position, Quaternion rotation)
        {
            var referencePointSubsystem = ARSubsystemManager.referencePointSubsystem;
            if (referencePointSubsystem == null)
                return null;

            // World space pose
            var pose = new Pose(position, rotation);

            // Session space pose
            var localPose = m_SessionOrigin.trackablesParent.InverseTransformPose(pose);

            // Add the reference point to the XRReferencePointSubsystem
            TrackableId referencePointId;
            if (!referencePointSubsystem.TryAddReferencePoint(localPose.position, localPose.rotation, out referencePointId))
                return null;

            // Get the data back of the XRReferencePointSubsystem
            ReferencePoint referencePointData;
            var gotReferencePoint = referencePointSubsystem.TryGetReferencePoint(referencePointId, out referencePointData);

            // We were able to add it, but not retrieve it -- this shouldn't happen.
            Assert.IsTrue(gotReferencePoint, string.Format(
                "Added a reference point {0}, but then could not retrieve it.", referencePointId));

            // Create a new GameObject for this point
            var parentTransform = m_SessionOrigin.trackablesParent;
            GameObject go;
            if (referencePointPrefab != null)
            {
                go = Instantiate(referencePointPrefab, parentTransform);
            }
            else
            {
                go = new GameObject(string.Format("ReferencePoint {0}", referencePointId));
                go.transform.SetParent(parentTransform, false);
            }

            go.layer = gameObject.layer;

            // Make sure it has an ARReferencePoint component.
            var referencePoint = go.GetComponent<ARReferencePoint>();
            if (referencePoint == null)
                referencePoint = go.AddComponent<ARReferencePoint>();

            m_ReferencePoints.Add(referencePointId, referencePoint);
            referencePoint.sessionRelativeData = referencePointData;

            return referencePoint;
        }

        /// <summary>
        /// Attempts to remove an <see cref="ARReferencePoint"/> by <c>TrackableId</c>
        /// </summary>
        /// <param name="referencePointId">The <c>TrackableId</c> associated with the <see cref="ARReferencePoint"/> you wish to remove.</param>
        /// <returns>True if the reference point was successfully removed.
        /// False usually means the reference point doesn't exist or isn't tracked by this manager.</returns>
        public bool TryRemoveReferencePoint(TrackableId referencePointId)
        {
            var referencePointSubsystem = ARSubsystemManager.referencePointSubsystem;
            if (referencePointSubsystem == null)
                return false;

            // Are we tracking it?
            var referencePoint = TryGetReferencePoint(referencePointId);
            if (referencePoint == null)
                return false;

            return TryRemoveReferencePointInternal(referencePointId, referencePoint);
        }

        /// <summary>
        /// Attempts to remove an <see cref="ARReferencePoint"/>.
        /// </summary>
        /// <param name="referencePoint">The reference point you wish to remove.</param>
        /// <returns>True if the reference point was successfully removed.
        /// False usually means the reference point doesn't exist or isn't tracked by this manager.</returns>
        public bool TryRemoveReferencePoint(ARReferencePoint referencePoint)
        {
            if (referencePoint == null)
                return false;

            TrackableId referencePointId = referencePoint.sessionRelativeData.Id;
            if (!m_ReferencePoints.ContainsKey(referencePointId))
                return false;

            return TryRemoveReferencePointInternal(referencePointId, referencePoint);
        }

        bool TryRemoveReferencePointInternal(TrackableId referencePointId, ARReferencePoint referencePoint)
        {
            var referencePointSubsystem = ARSubsystemManager.referencePointSubsystem;
            if (!referencePointSubsystem.TryRemoveReferencePoint(referencePointId))
                return false;

            Destroy(referencePoint.gameObject);
            m_ReferencePoints.Remove(referencePointId);
            return true;
        }

        /// <summary>
        /// Attempts to retrieve an existing <see cref="ARReferencePoint"/> previously added with <see cref="TryAddReferencePoint"/>.
        /// </summary>
        /// <param name="referencePointId">The <c>TrackableId</c> associated with the reference point.</param>
        /// <param name="referencePoint">Populated with the <see cref="ARReferencePoint"/> if successful.</param>
        /// <returns>The <see cref="ARReferencePoint"/> with <c>TrackableId</c> <paramref name="referencePointId"/>. Otherwise, <c>null</c>.</returns>
        public ARReferencePoint TryGetReferencePoint(TrackableId referencePointId)
        {
            ARReferencePoint referencePoint;
            m_ReferencePoints.TryGetValue(referencePointId, out referencePoint);
            return referencePoint;
        }

        /// <summary>
        /// Get all currently tracked <see cref="ARReferencePoint"/>s.
        /// </summary>
        /// <param name="referencePointsOut">Replaces the contents with the current list of reference points.</param>
        public void GetAllReferencePoints(List<ARReferencePoint> referencePointsOut)
        {
            if (referencePointsOut == null)
                throw new ArgumentNullException("referencePointsOut");

            referencePointsOut.Clear();
            foreach (var kvp in m_ReferencePoints)
            {
                referencePointsOut.Add(kvp.Value);
            }
        }

        void OnReferencePointUpdated(ReferencePointUpdatedEventArgs eventArgs)
        {
            var referencePointData = eventArgs.ReferencePoint;
            ARReferencePoint referencePoint = TryGetReferencePoint(referencePointData.Id);
            if (referencePoint == null)
            {
                // We aren't responsible for this reference point, so ignore it.
                return;
            }

            referencePoint.sessionRelativeData = referencePointData;

            // Notify event subscribers
            RaiseReferencePointUpdatedEvent(eventArgs, referencePoint);
        }

        void RaiseReferencePointUpdatedEvent(ReferencePointUpdatedEventArgs eventArgs, ARReferencePoint referencePoint)
        {
            if (referencePointUpdated == null)
                return;

            referencePointUpdated(new ARReferencePointUpdatedEventArgs(
                referencePoint,
                eventArgs.PreviousTrackingState,
                eventArgs.PreviousPose));
        }

        void Awake()
        {
            m_SessionOrigin = GetComponent<ARSessionOrigin>();
        }

        void OnEnable()
        {
            ARSubsystemManager.referencePointUpdated += OnReferencePointUpdated;
        }

        void OnDisable()
        {
            ARSubsystemManager.referencePointUpdated -= OnReferencePointUpdated;
        }

        ARSessionOrigin m_SessionOrigin;

        Dictionary<TrackableId, ARReferencePoint> m_ReferencePoints = new Dictionary<TrackableId, ARReferencePoint>();
    }
}
