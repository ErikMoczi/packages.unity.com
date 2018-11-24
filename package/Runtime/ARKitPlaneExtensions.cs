using AOT;
using System;
using System.Runtime.InteropServices;
using UnityEngine.XR.ARExtensions;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARKit
{
    /// <summary>
    /// For internal use. Provides ARKit-specific extensions to the XRPlaneSubsystem.
    /// </summary>
    internal class ARKitPlaneExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            XRPlaneExtensions.RegisterGetTrackingStateHandler(k_SubsystemId, GetTrackingState);
            XRPlaneExtensions.RegisterGetNativePtrHandler(k_SubsystemId, GetNativePtr);
            XRPlaneExtensions.RegisterTrySetPlaneDetectionFlagsHandler(k_SubsystemId, TrySetPlaneDetectionFlags);
        }

        static bool TrySetPlaneDetectionFlags(XRPlaneSubsystem planeSubsystem, PlaneDetectionFlags flags)
        {
            return UnityARKit_trySetPlaneDetectionFlags(flags);
        }

        static TrackingState GetTrackingState(XRPlaneSubsystem planeSubsystem, TrackableId planeId)
        {
            return Api.UnityARKit_getAnchorTrackingState(planeId);
        }

        static IntPtr GetNativePtr(XRPlaneSubsystem planeSubsystem, TrackableId planeId)
        {
            return Api.UnityARKit_getNativePlanePtr(planeId);
        }

        static readonly string k_SubsystemId = "ARKit-Plane";

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern bool UnityARKit_trySetPlaneDetectionFlags(PlaneDetectionFlags flags);
#else
        static bool UnityARKit_trySetPlaneDetectionFlags(PlaneDetectionFlags flags)
        {
            return false;
        }
#endif
    }
}
