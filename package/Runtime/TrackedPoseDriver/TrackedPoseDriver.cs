using System;
using UnityEngine.XR;
using System.Collections.Generic;
using UnityEngine.XR.Tango;
using UnityEngine.Experimental.XR.Interaction;

namespace UnityEngine.SpatialTracking
{
    public class TrackedPoseDriverDataDescription
    {
        public struct PoseData
        {
            public List<string> PoseNames;
            public List<TrackedPoseDriver.TrackedPose> Poses;
        }

        public static List<PoseData> DeviceData = new List<PoseData>
        {
            // Generic XR Device
            new PoseData
            {
                PoseNames = new List<string>
                {
                    "Left Eye", "Right Eye", "Center Eye", "Head", "Color Camera"
                },
                Poses = new List<TrackedPoseDriver.TrackedPose>
                {
                    TrackedPoseDriver.TrackedPose.LeftEye,
                    TrackedPoseDriver.TrackedPose.RightEye,
                    TrackedPoseDriver.TrackedPose.Center,
                    TrackedPoseDriver.TrackedPose.Head,
                    TrackedPoseDriver.TrackedPose.ColorCamera
                }
            },
            // generic controller
            new PoseData
            {
                PoseNames = new List<string>
                {
                    "Left Controller", "Right Controller"
                },
                Poses = new List<TrackedPoseDriver.TrackedPose>
                {
                    TrackedPoseDriver.TrackedPose.LeftPose,
                    TrackedPoseDriver.TrackedPose.RightPose
                }
            },
            // generic remote
            new PoseData
            {
                PoseNames = new List<string>
                {
                    "Device Pose"
                },
                Poses = new List<TrackedPoseDriver.TrackedPose>
                {
                    TrackedPoseDriver.TrackedPose.RemotePose,
                }
            },
        };
    }

    static public class PoseDataSource
    {
        static List<XR.XRNodeState> nodeStates = new List<XR.XRNodeState>();
        static internal bool TryGetNodePoseData(XR.XRNode node, out Pose resultPose)
        {
            XR.InputTracking.GetNodeStates(nodeStates);
            foreach (XR.XRNodeState nodeState in nodeStates)
            {
                if (nodeState.nodeType == node)
                {
                    bool result = nodeState.TryGetPosition(out resultPose.position);
                    result |= nodeState.TryGetRotation(out resultPose.rotation);
                    return result;
                }
            }
            resultPose = Pose.identity;
            return false;
        }

        static public bool TryGetDataFromSource(TrackedPoseDriver.TrackedPose poseSource, out Pose resultPose)
        {
            switch (poseSource)
            {
                case TrackedPoseDriver.TrackedPose.RemotePose:
                {
                    if (!TryGetNodePoseData(XR.XRNode.RightHand, out resultPose))
                        return TryGetNodePoseData(XR.XRNode.LeftHand, out resultPose);
                    return true;
                }
                case TrackedPoseDriver.TrackedPose.LeftEye:
                {
                    return TryGetNodePoseData(XR.XRNode.LeftEye, out resultPose);
                }
                case TrackedPoseDriver.TrackedPose.RightEye:
                {
                    return TryGetNodePoseData(XR.XRNode.RightEye, out resultPose);
                }
                case TrackedPoseDriver.TrackedPose.Head:
                {
                    return TryGetNodePoseData(XR.XRNode.Head, out resultPose);
                }
                case TrackedPoseDriver.TrackedPose.Center:
                {
                    return TryGetNodePoseData(XR.XRNode.CenterEye, out resultPose);
                }
                case TrackedPoseDriver.TrackedPose.LeftPose:
                {
                    return TryGetNodePoseData(XR.XRNode.LeftHand, out resultPose);
                }
                case TrackedPoseDriver.TrackedPose.RightPose:
                {
                    return TryGetNodePoseData(XR.XRNode.RightHand, out resultPose);
                }
                case TrackedPoseDriver.TrackedPose.ColorCamera:
                {
                    if (!TryGetTangoPose(out resultPose))
                    {
                        // We fall back to CenterEye because we can't currently extend the XRNode structure, nor are we ready to replace it.
                        return TryGetNodePoseData(XR.XRNode.CenterEye, out resultPose);
                    }
                    return true;
                }               
            }
            resultPose = Pose.identity;
            return false;
        }

        static bool TryGetTangoPose(out Pose pose)
        {
            PoseData poseOut;
            if (TangoInputTracking.TryGetPoseAtTime(out poseOut) && poseOut.statusCode == PoseStatus.Valid)
            {
                pose.position = poseOut.position;
                pose.rotation = poseOut.rotation;
                return true;
            }
            pose = Pose.identity;

            return false;
        }
    }

    // The DefaultExecutionOrder is needed because TrackedPoseDriver does some
    // of its work in regular Update and FixedUpdate calls, but this needs to
    // be done before regular user scripts have their own Update and
    // FixedUpdate calls, in order that they correctly get the values for this
    // frame and not the previous.
    // -32000 is the minimal possible execution order value; -30000 makes it
    // is unlikely users chose lower values for their scripts by accident, but
    // still makes it possible.
    [DefaultExecutionOrder(-30000)]
    [Serializable]
    [AddComponentMenu("XR/Tracked Pose Driver")]
    public class TrackedPoseDriver : MonoBehaviour
    {
        public enum DeviceType
        {
            GenericXRDevice = 0,
            GenericXRController = 1,
            GenericXRRemote = 2
        }

        public enum TrackedPose
        {
            LeftEye = 0,
            RightEye = 1,
            Center = 2,
            Head = 3,
            LeftPose = 4,
            RightPose = 5,
            ColorCamera = 6,
            RemotePose = 7,
        }

        [SerializeField]
        DeviceType m_Device;
        public DeviceType deviceType
        {
            get { return m_Device; }
            internal set { m_Device = value; }
        }

        [SerializeField]
        TrackedPose m_PoseSource;
        public TrackedPose poseSource
        {
            get { return m_PoseSource; }
            internal set { m_PoseSource = value; }
        }

        public bool SetPoseSource(DeviceType deviceType, TrackedPose pose)
        {
            if ((int)deviceType < TrackedPoseDriverDataDescription.DeviceData.Count)
            {
                TrackedPoseDriverDataDescription.PoseData val = TrackedPoseDriverDataDescription.DeviceData[(int)deviceType];
                for (int i = 0; i < val.Poses.Count; ++i)
                {
                    if (val.Poses[i] == pose)
                    {
                        this.deviceType = deviceType;
                        poseSource = pose;
                        return true;
                    }
                }
            }
            return false;
        }

        [SerializeField]
        BasePoseProvider m_PoseProviderComponent = null;
        public BasePoseProvider poseProviderComponent
        {
            get { return m_PoseProviderComponent; }
            set
            {
                m_PoseProviderComponent = value;
            }
        }

        bool TryGetPoseData(DeviceType device, TrackedPose poseSource, out Pose resultPose)
        {
            if (m_PoseProviderComponent != null)
            {
                return m_PoseProviderComponent.TryGetPoseFromProvider(out resultPose);
            }

            return PoseDataSource.TryGetDataFromSource(poseSource, out resultPose);
        }

        public enum TrackingType
        {
            RotationAndPosition,
            RotationOnly,
            PositionOnly
        }

        [SerializeField]
        TrackingType m_TrackingType;
        public TrackingType trackingType
        {
            get { return m_TrackingType; }
            set { m_TrackingType = value; }
        }

        public enum UpdateType
        {
            UpdateAndBeforeRender,
            Update,
            BeforeRender,
        }

        [SerializeField]
        UpdateType m_UpdateType = UpdateType.UpdateAndBeforeRender;
        public UpdateType updateType
        {
            get { return m_UpdateType; }
            set { m_UpdateType = value; }
        }

        [SerializeField]
        bool m_UseRelativeTransform = true;
        public bool UseRelativeTransform
        {
            get { return m_UseRelativeTransform; }
            set { m_UseRelativeTransform = value; }
        }

        protected Pose m_OriginPose;

        // originPose is an offset applied to any tracking data read from this object.
        // Setting this value should be reserved for dealing with edge-cases, such as
        // achieving parity between room-scale (floor centered) and stationary (head centered)
        // tracking - without having to alter the transform hierarchy.
        // For user locomotion and gameplay purposes you are usually better off just
        // moving the parent transform of this object.
        public Pose originPose
        {
            get { return m_OriginPose; }
            set { m_OriginPose = value; }
        }

        private void CacheLocalPosition()
        {
            m_OriginPose.position = transform.localPosition;
            m_OriginPose.rotation = transform.localRotation;
        }

        private void ResetToCachedLocalPosition()
        {
            SetLocalTransform(m_OriginPose.position, m_OriginPose.rotation);
        }

        protected virtual void Awake()
        {
            CacheLocalPosition();

            if (HasStereoCamera())
            {
                XRDevice.DisableAutoXRCameraTracking(GetComponent<Camera>(), true);
            }
        }

        protected virtual void OnDestroy()
        {
            if (HasStereoCamera())
            {
#if ENABLE_VR
                XRDevice.DisableAutoXRCameraTracking(GetComponent<Camera>(), false);
#endif
            }
        }

        protected virtual void OnEnable()
        {
            Application.onBeforeRender += OnBeforeRender;
        }

        protected virtual void OnDisable()
        {
            // remove delegate registration            
            ResetToCachedLocalPosition();
            Application.onBeforeRender -= OnBeforeRender;
        }

        protected virtual void FixedUpdate()
        {
            if (m_UpdateType == UpdateType.Update ||
                m_UpdateType == UpdateType.UpdateAndBeforeRender)
            {
                PerformUpdate();
            }
        }

        protected virtual void Update()
        {
            if (m_UpdateType == UpdateType.Update ||
                m_UpdateType == UpdateType.UpdateAndBeforeRender)
            {
                PerformUpdate();
            }
        }

        protected virtual void OnBeforeRender()
        {
            if (m_UpdateType == UpdateType.BeforeRender ||
                m_UpdateType == UpdateType.UpdateAndBeforeRender)
            {
                PerformUpdate();
            }
        }

        protected virtual void SetLocalTransform(Vector3 newPosition, Quaternion newRotation)
        {
            if (m_TrackingType == TrackingType.RotationAndPosition ||
                m_TrackingType == TrackingType.RotationOnly)
            {
                transform.localRotation = newRotation;
            }

            if (m_TrackingType == TrackingType.RotationAndPosition ||
                m_TrackingType == TrackingType.PositionOnly)
            {
                transform.localPosition = newPosition;
            }
        }

        protected Pose TransformPoseByOriginIfNeeded(Pose pose)
        {
            if (m_UseRelativeTransform)
            {
                return pose.GetTransformedBy(m_OriginPose);
            }
            else
            {
                return pose;
            }
        }

        private bool HasStereoCamera()
        {
            Camera camera = GetComponent<Camera>();
            return camera != null && camera.stereoEnabled;
        }

        protected virtual void PerformUpdate()
        {
            if (!enabled)
                return;
            Pose currentPose = new Pose();
            currentPose = Pose.identity;
            if (TryGetPoseData(m_Device, m_PoseSource, out currentPose))
            {
                Pose localPose = TransformPoseByOriginIfNeeded(currentPose);
                SetLocalTransform(localPose.position, localPose.rotation);
            }
        }
    }
}
