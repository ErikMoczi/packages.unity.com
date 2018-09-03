using AOT;
using System;
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
        }

        static TrackingState GetTrackingState(XRPlaneSubsystem planeSubsystem, TrackableId planeId)
        {
            return Api.UnityARKit_getAnchorTrackingState(planeId);
        }

        static readonly string k_SubsystemId = "ARKit-Plane";
    }
}
