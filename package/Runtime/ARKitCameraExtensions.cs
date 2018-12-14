using System;
using System.Runtime.InteropServices;
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
            XRCameraExtensions.RegisterGetNativePtrHandler(k_SubsystemId, GetNativePtr);
            XRCameraExtensions.RegisterCameraImageApi(k_SubsystemId, s_CameraImageApi);
            XRCameraExtensions.RegisterCameraConfigApi(k_SubsystemId, s_CameraConfigApi);
            XRCameraExtensions.RegisterTrySetFocusModeHandler(k_SubsystemId, TrySetFocusMode);
        }

        static ARKitCameraExtension()
        {
            s_CameraImageApi = new ARKitCameraImageApi();
            s_CameraConfigApi = new ARKitCameraConfigApi();
        }

        static bool TrySetFocusMode(XRCameraSubsystem cameraSubsystem, CameraFocusMode focusMode)
        {
            return UnityARKit_trySetFocusMode(focusMode);
        }

        static bool IsPermissionGranted(XRCameraSubsystem cameraSubsystem)
        {
            return Api.UnityARKit_IsCameraPermissionGranted();
        }

        static IntPtr GetNativePtr(XRCameraSubsystem cameraSubsystem)
        {
            return Api.UnityARKit_getNativeFramePtr();
        }

        static readonly string k_SubsystemId = "ARKit-Camera";

        static readonly ARKitCameraImageApi s_CameraImageApi;

        static readonly ARKitCameraConfigApi s_CameraConfigApi;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern bool UnityARKit_trySetFocusMode(CameraFocusMode focusMode);
#else
        static bool UnityARKit_trySetFocusMode(CameraFocusMode focusMode)
        {
            return false;
        }
#endif
    }
}
