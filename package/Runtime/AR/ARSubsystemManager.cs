using System;
using UnityEngine.Experimental;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// Manages the lifecycle of multiple XR Subsystems specific to AR.
    /// </summary>
    /// <remarks>
    /// The XR Subsystems provide direct access to the underlying data providers for a specific device.
    /// ARFoundation provides higher level abstractions and utilities on top of the low-level XR Subsystems,
    /// so, in general, you don't need to interact directly with the XR Subsystems.
    /// 
    /// A typical AR session may involve the following subsystems:
    /// <list type="number">
    /// <item><description><c>XRSessionSubsystem</c></description></item>
    /// <item><description><c>XRInputSubsystem</c></description></item>
    /// <item><description><c>XRCameraSubsystem</c></description></item>
    /// <item><description><c>XRDepthSubsystem</c></description></item>
    /// <item><description><c>XRPlaneSubsystem</c></description></item>
    /// <item><description><c>XRReferencePointSubsystem</c></description></item>
    /// <item><description><c>XRRaycastSubsystem</c></description></item>
    /// </list>
    /// Since there can only be a single AR session (and usually the associated
    /// subsystems), this class is a singleton.
    /// </remarks>
    public static class ARSubsystemManager
    {
        /// <summary>
        /// Gets the <c>XRSessionSubsystem</c>. This controls the lifecycle of the AR Session.
        /// </summary>
        public static XRSessionSubsystem sessionSubsystem { get; private set; }

        /// <summary>
        /// Gets the <c>XRInputSubsystem</c>. This allows <c>Pose</c> data from the device to be fed to the <c>TrackedPoseDriver</c>.
        /// </summary>
        public static XRInputSubsystem inputSubsystem { get; private set; }

        /// <summary>
        /// Gets the <c>XRCameraSubsystem</c>. This subsystem provides access to camera data, such as ligt estimation information and the camera texture.
        /// </summary>
        public static XRCameraSubsystem cameraSubsystem { get; private set; }

        /// <summary>
        /// Gets the <c>XRDepthSubsystem</c>. This subsystem provides access to depth data, such as features points (aka point cloud).
        /// </summary>
        public static XRDepthSubsystem depthSubsystem { get; private set; }

        /// <summary>
        /// Gets the <c>XRPlaneSubsystem</c>. This subsystem provides access to plane data, such as horizontal surfaces.
        /// </summary>
        public static XRPlaneSubsystem planeSubsystem { get; private set; }

        /// <summary>
        /// Gets the <c>XRReferencePointSubsystem</c>. This subystem provides access to reference points, aka anchors.
        /// </summary>
        public static XRReferencePointSubsystem referencePointSubsystem { get; private set; }

        /// <summary>
        /// Gets the <c>XRRaycastSubsystem</c>. This subsystem provides access to the raycast interface.
        /// </summary>
        public static XRRaycastSubsystem raycastSubsystem { get; private set; }

        /// <summary>
        /// This event is invoked whenever a new camera frame is provided by the device.
        /// </summary>
        public static event Action<ARCameraFrameEventArgs> cameraFrameReceived
        {
            add
            {
                s_CameraFrameReceived += value;
                SetRunning(cameraSubsystem, cameraSubsystemRequested);
            }

            remove
            {
                s_CameraFrameReceived -= value;
                SetRunning(cameraSubsystem, cameraSubsystemRequested);
            }
        }

        /// <summary>
        /// This event is invoked whenever the <c>TrackingState</c> changes.
        /// </summary>
        public static event Action<ARTrackingStateChangedEventArgs> trackingStateChanged;

        /// <summary>
        /// This event is invoked whenever the <see cref="ARSessionState"/> changes.
        /// </summary>
        public static event Action<ARSessionStateChangedEventArgs> sessionStateChanged;

        /// <summary>
        /// This event is invoked whenever a plane is added.
        /// </summary>
        /// <remarks>
        /// This is the low-level XR interface, and the data is in session space.
        /// Consider instead subscribing to the more useful <see cref="ARPlaneManager.planeAdded"/>.
        /// </remarks>
        public static event Action<PlaneAddedEventArgs> planeAdded
        {
            add
            {
                s_PlaneAdded += value;
                SetRunning(planeSubsystem, planeDetectionRequested);
            }

            remove
            {
                s_PlaneAdded -= value;
                SetRunning(planeSubsystem, planeDetectionRequested);
            }
        }

        /// <summary>
        /// This event is invoked whenever an existing plane is updated.
        /// </summary>
        /// <remarks>
        /// This is the low-level XR interface, and the data is in session space.
        /// Consider instead subscribing to the more useful <see cref="ARPlaneManager.planeUpdated"/>.
        /// </remarks>
        public static event Action<PlaneUpdatedEventArgs> planeUpdated
        {
            add
            {
                s_PlaneUpdated += value;
                SetRunning(planeSubsystem, planeDetectionRequested);
            }

            remove
            {
                s_PlaneUpdated -= value;
                SetRunning(planeSubsystem, planeDetectionRequested);
            }
        }

        /// <summary>
        /// This event is invoked whenever an existing plane is removed.
        /// </summary>
        /// <remarks>
        /// This is the low-level XR interface, and the data is in session space.
        /// Consider instead subscribing to the more useful <see cref="ARPlaneManager.planeRemoved"/>.
        /// </remarks>
        public static event Action<PlaneRemovedEventArgs> planeRemoved
        {
            add
            {
                s_PlaneRemoved += value;
                SetRunning(planeSubsystem, planeDetectionRequested);
            }

            remove
            {
                s_PlaneRemoved -= value;
                SetRunning(planeSubsystem, planeDetectionRequested);
            }
        }

        /// <summary>
        /// This event is invoked whenever the point cloud has changed.
        /// </summary>
        /// <remarks>
        /// This is the low-level XR interface, and the data is in session space.
        /// Consider instead subscribing to the more useful <see cref="ARPointCloudManager.pointCloudUpdated"/>.
        /// </remarks>
        public static event Action<PointCloudUpdatedEventArgs> pointCloudUpdated
        {
            add
            {
                s_PointCloudUpdated += value;
                SetRunning(depthSubsystem, depthDataRequested);
            }

            remove
            {
                s_PointCloudUpdated -= value;
                SetRunning(depthSubsystem, depthDataRequested);
            }
        }

        /// <summary>
        /// This event is invoked whenever a reference point changes.
        /// </summary>
        /// <remarks>
        /// This is the low-level XR interface, and the data is in session space.
        /// Consider instead subscribing to the more useful <see cref="ARReferencePointManager.referencePointUpdated"/>.
        /// </remarks>
        public static event Action<ReferencePointUpdatedEventArgs> referencePointUpdated;

        /// <summary>
        /// The current state of the session. Use to query whether the session is running.
        /// </summary>
        public static ARSessionState sessionState
        {
            get { return s_SessionState; }
            private set
            {
                if (s_SessionState == value)
                    return;

                s_SessionState = value;
                RaiseSessionStateChangedEvent();
            }
        }

        /// <summary>
        /// Get or set whether light estimation should be enabled.
        /// </summary>
        /// <remarks>
        /// Note: You can only request light estimation. The underlying provider may not support light estimation.
        /// </remarks>
        public static bool lightEstimationRequested
        {
            get { return s_LightEstimationRequested; }
            set
            {
                if (lightEstimationRequested == value)
                    return;

                s_LightEstimationRequested = value;

                if (cameraSubsystem != null)
                    cameraSubsystem.LightEstimationRequested = value;

                SetRunning(cameraSubsystem, cameraSubsystemRequested);
            }
        }

        /// <summary>
        /// Static constructor for this static class. Creates the subsystems.
        /// </summary>
        public static void ARSubsystemsManager()
        {
            s_SessionState = ARSessionState.NotRunning;
            CreateSubsystems();
        }

        /// <summary>
        /// Creates each XR Subsystem associated with an AR session and registers their callbacks.
        /// </summary>
        /// <remarks>
        /// "Creating" a subsystem means finding and initializing a plugin. It does not begin doing
        /// work until you call <see cref="StartSubsystems"/>.
        /// </remarks>
        public static void CreateSubsystems()
        {
            // Find and initialize each subsystem
            sessionSubsystem = ARSubsystemUtil.CreateSessionSubsystem();
            cameraSubsystem = ARSubsystemUtil.CreateCameraSubsystem();
            inputSubsystem = ARSubsystemUtil.CreateInputSubsystem();
            depthSubsystem = ARSubsystemUtil.CreateDepthSubsystem();
            planeSubsystem = ARSubsystemUtil.CreatePlaneSubsystem();
            referencePointSubsystem = ARSubsystemUtil.CreateReferencePointSubsystem();
            raycastSubsystem = ARSubsystemUtil.CreateRaycastSubsystem();

            if (planeSubsystem != null)
            {
                planeSubsystem.PlaneAdded -= OnPlaneAdded;
                planeSubsystem.PlaneAdded += OnPlaneAdded;
                planeSubsystem.PlaneUpdated -= OnPlaneUpdated;
                planeSubsystem.PlaneUpdated += OnPlaneUpdated;
                planeSubsystem.PlaneRemoved -= OnPlaneRemoved;
                planeSubsystem.PlaneRemoved += OnPlaneRemoved;
            }

            if (depthSubsystem != null)
            {
                depthSubsystem.PointCloudUpdated -= OnPointCloudUpdated;
                depthSubsystem.PointCloudUpdated += OnPointCloudUpdated;
            }

            if (referencePointSubsystem != null)
            {
                referencePointSubsystem.ReferencePointUpdated -= OnReferencePointUpdated;
                referencePointSubsystem.ReferencePointUpdated += OnReferencePointUpdated;
            }

            if (cameraSubsystem != null)
            {
                cameraSubsystem.FrameReceived -= OnFrameReceived;
                cameraSubsystem.FrameReceived += OnFrameReceived;
                cameraSubsystem.LightEstimationRequested = lightEstimationRequested;
            }

            if (sessionSubsystem != null)
            {
                sessionSubsystem.TrackingStateChanged -= OnTrackingStateChanged;
                sessionSubsystem.TrackingStateChanged += OnTrackingStateChanged;
            }
        }

        /// <summary>
        /// Destroys each XR Subsystem associated with an AR session.
        /// </summary>
        public static void DestroySubsystems()
        {
            DestroySubsystem(sessionSubsystem);
            DestroySubsystem(cameraSubsystem);
            DestroySubsystem(inputSubsystem);
            DestroySubsystem(depthSubsystem);
            DestroySubsystem(planeSubsystem);
            DestroySubsystem(referencePointSubsystem);
            DestroySubsystem(raycastSubsystem);

            sessionSubsystem = null;
            cameraSubsystem = null;
            inputSubsystem = null;
            depthSubsystem = null;
            planeSubsystem = null;
            referencePointSubsystem = null;
            raycastSubsystem = null;

            sessionState = ARSessionState.NotRunning;
        }

        static void OnTimeout()
        {
            sessionState = ARSessionState.TimedOut;
            StopSubsystems();
        }

        /// <summary>
        /// Starts each of the XR Subsystems associated with AR session according to the requested options.
        /// </summary>
        /// <remarks>
        /// Throws <c>InvalidOperationException</c> if there is no <c>XRSessionSubsystem</c>.
        /// </remarks>
        /// <returns>A timeout callback which can be invoked to indicate too much time has passed since requesting start.</returns>
        public static Action StartSubsystems()
        {
            // Early out if we're already running.
            if ((sessionState == ARSessionState.Initializing) || (sessionState == ARSessionState.Running))
                return null;

            if (sessionSubsystem == null)
                throw new InvalidOperationException("Cannot start AR session because there is no session subsystem.");

            SetRunning(raycastSubsystem, true);
            SetRunning(referencePointSubsystem, true);
            SetRunning(inputSubsystem, true);
            SetRunning(planeSubsystem, planeDetectionRequested);
            SetRunning(depthSubsystem, depthDataRequested);
            SetRunning(cameraSubsystem, cameraSubsystemRequested);
            sessionSubsystem.Start();
            sessionState = ARSessionState.Initializing;

            return OnTimeout;
        }

        /// <summary>
        /// Stops all the XR Subsystems associated with the AR session.
        /// </summary>
        /// <remarks>
        /// "Stopping" an AR session does not destroy the session. A call to <see cref="StopSubsystems"/> followed by <see cref="StartSubsystems"/c>
        /// is similar to "pause" and "resume". To completely destroy the current AR session and begin a new one, you must first
        /// <see cref="DestroySubsystems"/>.
        /// </remarks>
        public static void StopSubsystems()
        {
            if (sessionState == ARSessionState.NotRunning)
                return;

            SetRunning(raycastSubsystem, false);
            SetRunning(referencePointSubsystem, false);
            SetRunning(inputSubsystem, false);
            SetRunning(planeSubsystem, false);
            SetRunning(depthSubsystem, false);
            SetRunning(cameraSubsystem, false);
            sessionSubsystem.Stop();
            sessionState = ARSessionState.NotRunning;
        }

        /// <summary>
        /// Gets the current <c>TrackingState</c>.
        /// </summary>
        public static TrackingState TrackingState
        {
            get
            {
                if (sessionSubsystem == null)
                    return TrackingState.Unknown;

                return sessionSubsystem.TrackingState;
            }
        }

        static void RaiseSessionStateChangedEvent()
        {
            if (sessionStateChanged != null)
                sessionStateChanged(new ARSessionStateChangedEventArgs(sessionState));
        }

        static void OnTrackingStateChanged(SessionTrackingStateChangedEventArgs eventArgs)
        {
            if (eventArgs.NewState == TrackingState.Tracking)
                sessionState = ARSessionState.Running;

            if (trackingStateChanged != null)
                trackingStateChanged(new ARTrackingStateChangedEventArgs(eventArgs.NewState));
        }

        static void OnFrameReceived(FrameReceivedEventArgs eventArgs)
        {
            sessionState = ARSessionState.Running;

            if (s_CameraFrameReceived != null)
                RaiseFrameReceivedEvent();
        }

        static void RaiseFrameReceivedEvent()
        {
            var lightEstimation = new LightEstimationData();

            float data = 0F;
            if (cameraSubsystem.TryGetAverageBrightness(ref data))
                lightEstimation.averageBrightness = data;
            if (cameraSubsystem.TryGetAverageColorTemperature(ref data))
                lightEstimation.averageColorTemperature = data;

            float? timestampSeconds = null;
            Int64 timestampNs = 0;
            if (cameraSubsystem.TryGetTimestamp(ref timestampNs))
                timestampSeconds = timestampNs * 1e-9F;

            s_CameraFrameReceived(new ARCameraFrameEventArgs(lightEstimation, timestampSeconds));
        }

        static void OnPlaneAdded(PlaneAddedEventArgs eventArgs)
        {
            sessionState = ARSessionState.Running;

            if (s_PlaneAdded != null)
                s_PlaneAdded(eventArgs);
        }

        static void OnPlaneUpdated(PlaneUpdatedEventArgs eventArgs)
        {
            sessionState = ARSessionState.Running;

            if (s_PlaneUpdated != null)
                s_PlaneUpdated(eventArgs);
        }

        static void OnPlaneRemoved(PlaneRemovedEventArgs eventArgs)
        {
            sessionState = ARSessionState.Running;

            if (s_PlaneRemoved != null)
                s_PlaneRemoved(eventArgs);
        }

        static void OnPointCloudUpdated(PointCloudUpdatedEventArgs eventArgs)
        {
            sessionState = ARSessionState.Running;

            if (s_PointCloudUpdated != null)
                s_PointCloudUpdated(eventArgs);
        }

        static void OnReferencePointUpdated(ReferencePointUpdatedEventArgs eventArgs)
        {
            sessionState = ARSessionState.Running;

            if (referencePointUpdated != null)
                referencePointUpdated(eventArgs);
        }

        static void DestroySubsystem(Subsystem subsystem)
        {
            if (subsystem != null)
                subsystem.Destroy();
        }

        static void SetRunning(Subsystem subsystem, bool shouldBeRunning)
        {
            if (subsystem == null)
                return;

            if (shouldBeRunning)
                subsystem.Start();
            else
                subsystem.Stop();
        }

        // We don't expose this publicly because it is inferred from
        // 1. lighting estimation requested
        // 2. camera event subscriptions
        static bool cameraSubsystemRequested
        {
            get
            {
                return
                    lightEstimationRequested ||
                    (s_CameraFrameReceived != null);
            }
        }

        static Action<PlaneAddedEventArgs> s_PlaneAdded;

        static Action<PlaneUpdatedEventArgs> s_PlaneUpdated;

        static Action<PlaneRemovedEventArgs> s_PlaneRemoved;

        static bool planeDetectionRequested
        {
            get
            {
                return
                    (s_PlaneAdded != null) ||
                    (s_PlaneUpdated != null) ||
                    (s_PlaneRemoved != null);
            }
        }

        static Action<PointCloudUpdatedEventArgs> s_PointCloudUpdated;

        static Action<ARCameraFrameEventArgs> s_CameraFrameReceived;

        static ARSessionState s_SessionState;

        static bool depthDataRequested
        {
            get
            {
                return (s_PointCloudUpdated != null);
            }
        }

        static bool s_LightEstimationRequested;
    }
}
