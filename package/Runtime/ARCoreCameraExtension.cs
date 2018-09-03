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
            XRCameraExtensions.RegisterTryGetColorCorrectionHandler(k_SubsystemId, TryGetColorCorrection);
        }

        static bool IsPermissionGranted(XRCameraSubsystem cameraSubsystem)
        {
            return ARCorePermissionManager.IsPermissionGranted(k_CameraPermissionName);
        }

        static bool TryGetColorCorrection(XRCameraSubsystem cameraSubsystem, out Color color)
        {
            float r, g, b, a;
            if (Api.UnityARCore_tryGetColorCorrection(out r, out g, out b, out a))
            {
                color = new Color(r, g, b, a);
                return true;
            }
            else
            {
                color = default(Color);
                return false;
            }
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
