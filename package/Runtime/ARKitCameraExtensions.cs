using AOT;
using System;
using UnityEngine.XR.ARExtensions;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARKit
{
    /// <summary>
    /// For internal use. Provides ARKit-specific extensions to the XRCameraSubsystem.
    /// </summary>
    internal class ARKitCameraExtension
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            XRCameraExtensions.RegisterIsPermissionGrantedHandler(k_SubsystemId, IsPermissionGranted);
        }

        static bool IsPermissionGranted(XRCameraSubsystem cameraSubsystem)
        {
            return Api.UnityARKit_IsCameraPermissionGranted();
        }

        static readonly string k_SubsystemId = "ARKit-Camera";
    }
}
