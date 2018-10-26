using System;
using UnityEngine.Experimental.XR;
using UnityEngine.XR.ARExtensions;

namespace UnityEngine.XR.ARKit
{
    /// <summary>
    /// For internal use. Provides ARKit-specific extensions to the XRCameraSubsystem.
    /// </summary>
    internal static class ARKitCameraExtension
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            XRCameraExtensions.RegisterIsPermissionGrantedHandler(k_SubsystemId, IsPermissionGranted);
            XRCameraExtensions.RegisterCameraImageApi(k_SubsystemId, s_CameraImageApi);
        }

        static ARKitCameraExtension()
        {
            s_CameraImageApi = new ARKitCameraImageApi();
        }

        static bool IsPermissionGranted(XRCameraSubsystem cameraSubsystem)
        {
            return Api.UnityARKit_IsCameraPermissionGranted();
        }

        static readonly string k_SubsystemId = "ARKit-Camera";

        static readonly ARKitCameraImageApi s_CameraImageApi;
    }
}
