using AOT;
using System;
using UnityEngine.XR.ARExtensions;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARCore
{
    /// <summary>
    /// For internal use. Provides ARCore-specific extensions to the XRCameraSubsystem.
    /// </summary>
    internal class ARCoreCameraExtension
    {
        static readonly string k_CameraPermissionName = "android.permission.CAMERA";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            Api.UnityARCore_setCameraPermissionProvider(CameraPermissionRequestProvider);
            XRCameraExtensions.RegisterIsPermissionGrantedHandler(k_SubsystemId, IsPermissionGranted);
        }

        static bool IsPermissionGranted(XRCameraSubsystem cameraSubsystem)
        {
            return ARCorePermissionManager.IsPermissionGranted(k_CameraPermissionName);
        }

        [MonoPInvokeCallback(typeof(Api.CameraPermissionRequestProvider))]
        static void CameraPermissionRequestProvider(Api.CameraPermissionsResultCallback callback, IntPtr context)
        {
            ARCorePermissionManager.RequestPermission(k_CameraPermissionName, (permissinName, granted) =>
            {
                callback(granted, context);
            });
        }

        static readonly string k_SubsystemId = "ARCore-Camera";
    }
}
