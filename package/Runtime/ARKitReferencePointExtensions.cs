using System;
using UnityEngine.XR.ARExtensions;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARKit
{
    /// <summary>
    /// For internal use. Provides ARKit-specific extensions to the XRReferencePointSubsystem.
    /// </summary>
    internal class ARKitReferencePointExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            XRReferencePointExtensions.RegisterAttachReferencePointHandler(k_SubsystemId, AttachReferencePoint);
            XRReferencePointExtensions.RegisterGetNativePtrHandler(k_SubsystemId, GetNativePtr);
        }

        static IntPtr GetNativePtr(XRReferencePointSubsystem referencePointSubsystem, TrackableId trackableId)
        {
            return Api.UnityARKit_getNativeReferencePointPtr(trackableId);
        }

        static TrackableId AttachReferencePoint(XRReferencePointSubsystem referencePointSubsystem,
            TrackableId trackableId, Pose pose)
        {
            return Api.UnityARKit_attachReferencePoint(trackableId, pose);
        }

        static readonly string k_SubsystemId = "ARKit-ReferencePoint";
    }
}
