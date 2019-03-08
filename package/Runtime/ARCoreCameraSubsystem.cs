using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARCore
{
    /// <summary>
    /// The camera subsystem implementation for ARCore.
    /// </summary>
   [Preserve]
    public sealed class ARCoreCameraSubsystem : XRCameraSubsystem
    {
        /// <summary>
        /// The identifying name for the camera-providing implementation.
        /// </summary>
        /// <value>
        /// The identifying name for the camera-providing implementation.
        /// </value>
        const string k_SubsystemId = "ARCore-Camera";

        /// <summary>
        /// Create and register the camera subsystem descriptor to advertise a providing implementation for camera
        /// functionality.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            XRCameraSubsystemCinfo cameraSubsystemCinfo = new XRCameraSubsystemCinfo
            {
                id = k_SubsystemId,
                implementationType = typeof(ARCoreCameraSubsystem),
                supportsAverageBrightness = true,
                supportsAverageColorTemperature = false,
                supportsColorCorrection = true,
                supportsDisplayMatrix = true,
                supportsProjectionMatrix = true,
                supportsTimestamp = true
            };

            if (!XRCameraSubsystem.Register(cameraSubsystemCinfo))
            {
                Debug.LogErrorFormat("Cannot register the {0} subsystem", k_SubsystemId);
            }
        }

        /// <summary>
        /// Create the ARCore camera functionality provider for the camera subsystem.
        /// </summary>
        /// <returns>
        /// The ARCore camera functionality provider for the camera subsystem.
        /// </returns>
        protected override IProvider CreateProvider()
        {
            return new Provider();
        }

        /// <summary>
        /// Provides the camera functionality for the ARCore implementation.
        /// </summary>
        class Provider : IProvider
        {
            /// <summary>
            /// The shader property name for the main texture of the camera video frame.
            /// </summary>
            /// <value>
            /// The shader property name for the main texture of the camera video frame.
            /// </value>
            const string k_MainTexPropertyName = "_MainTex";

            /// <summary>
            /// The name of the camera permission for Android.
            /// </summary>
            /// <value>
            /// The name of the camera permission for Android.
            /// </value>
            const string k_CameraPermissionName = "android.permission.CAMERA";

            /// <summary>
            /// The shader property name identifier for the main texture of the camera video frame.
            /// </summary>
            /// <value>
            /// The shader property name identifier for the main texture of the camera video frame.
            /// </value>
            static readonly int k_MainTexPropertyNameId = Shader.PropertyToID(k_MainTexPropertyName);

            /// <summary>
            /// Construct the camera functionality provider for ARCore.
            /// </summary>
            /// <param name="preUpdateCallback">The callback for the preupdate event.</param>
            public Provider()
            {
                NativeApi.UnityARCore_Camera_Construct(k_MainTexPropertyNameId);
            }

            /// <summary>
            /// Start the camera functionality.
            /// </summary>
            public override void Start()
            {
                NativeApi.UnityARCore_Camera_Start();
            }

            /// <summary>
            /// Stop the camera functionality.
            /// </summary>
            public override void Stop()
            {
                NativeApi.UnityARCore_Camera_Stop();
            }

            /// <summary>
            /// Destroy any resources required for the camera functionality.
            /// </summary>
            public override void Destroy()
            {
                NativeApi.UnityARCore_Camera_Destruct();
            }

            /// <summary>
            /// Get the current camera frame for the subsystem.
            /// </summary>
            /// <param name="cameraParams">The current Unity <c>Camera</c> parameters.</param>
            /// <param name="cameraFrame">The current camera frame returned by the method.</param>
            /// <returns>
            /// <c>true</c> if the method successfully got a frame. Otherwise, <c>false</c>.
            /// </returns>
            public override bool TryGetFrame(XRCameraParams cameraParams, out XRCameraFrame cameraFrame)
            {
                return NativeApi.UnityARCore_Camera_TryGetFrame(cameraParams, out cameraFrame);
            }

            /// <summary>
            /// Get the shader name used by <c>XRCameraSubsystem</c> to render texture.
            /// </summary>
            public override string shaderName
            {
                get { return "Unlit/ARCoreBackground"; }
            }

            /// <summary>
            /// Set the focus mode for the camera.
            /// </summary>
            /// <param name="cameraFocusMode">The focus mode to set for the camera.</param>
            /// <returns>
            /// <c>true</c> if the method successfully set the focus mode for the camera. Otherwise, <c>false</c>.
            /// </returns>
            public override bool TrySetFocusMode(CameraFocusMode cameraFocusMode)
            {
                return NativeApi.UnityARCore_Camera_TrySetFocusMode(cameraFocusMode);
            }

            /// <summary>
            /// Set the light estimation mode.
            /// </summary>
            /// <param name="lightEstimationMode">The light estimation mode to set.</param>
            /// <returns>
            /// <c>true</c> if the method successfully set the light estimation mode. Otherwise, <c>false</c>.
            /// </returns>
            public override bool TrySetLightEstimationMode(LightEstimationMode lightEstimationMode)
            {
                return NativeApi.UnityARCore_Camera_TrySetLightEstimationMode(lightEstimationMode);
            }

            /// <summary>
            /// Get the camera intrinisics information.
            /// </summary>
            /// <param name="cameraIntrinsics">The camera intrinsics information returned from the method.</param>
            /// <returns>
            /// <c>true</c> if the method successfully gets the camera intrinsics information. Otherwise, <c>false</c>.
            /// </returns>
            public override bool TryGetIntrinsics(out XRCameraIntrinsics cameraIntrinsics)
            {
                return NativeApi.UnityARCore_Camera_TryGetIntrinsics(out cameraIntrinsics);
            }

            /// <summary>
            /// Determine whether camera permission has been granted.
            /// </summary>
            /// <returns>
            /// <c>true</c> if camera permission has been granted for this app. Otherwise, <c>false</c>.
            /// </returns>
            public override bool permissionGranted
            {
                get
                {
                    return ARCorePermissionManager.IsPermissionGranted(k_CameraPermissionName);
                }
            }

            /// <summary>
            /// Gets the texture descriptors associated with the camera image.
            /// </summary>
            /// <returns>The texture descriptors.</returns>
            /// <param name="defaultDescriptor">Default descriptor.</param>
            /// <param name="allocator">Allocator.</param>
            public unsafe override NativeArray<XRTextureDescriptor> GetTextureDescriptors(
                XRTextureDescriptor defaultDescriptor,
                Allocator allocator)
            {
                int length, elementSize;
                var textureDescriptors = NativeApi.UnityARCore_Camera_AcquireTextureDescriptors(
                    out length, out elementSize);

                try
                {
                    return NativeCopyUtility.PtrToNativeArrayWithDefault(
                        defaultDescriptor,
                        textureDescriptors, elementSize, length, allocator);
                }
                finally
                {
                    NativeApi.UnityARCore_Camera_ReleaseTextureDescriptors(textureDescriptors);
                }
            }
        }

        /// <summary>
        /// Container to wrap the native ARCore camera APIs.
        /// </summary>
        static class NativeApi
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport("UnityARCore")]
            public static extern void UnityARCore_Camera_Construct(int mainTexPropertyNameId);

            [DllImport("UnityARCore")]
            public static extern void UnityARCore_Camera_Destruct();

            [DllImport("UnityARCore")]
            public static extern void UnityARCore_Camera_Start();

            [DllImport("UnityARCore")]
            public static extern void UnityARCore_Camera_Stop();

            [DllImport("UnityARCore")]
            public static extern bool UnityARCore_Camera_TryGetFrame(XRCameraParams cameraParams,
                                                                    out XRCameraFrame cameraFrame);

            [DllImport("UnityARCore")]
            public static extern bool UnityARCore_Camera_TrySetFocusMode(CameraFocusMode cameraFocusMode);

            [DllImport("UnityARCore")]
            public static extern bool UnityARCore_Camera_TrySetLightEstimationMode(LightEstimationMode lightEstimationMode);

            [DllImport("UnityARCore")]
            public static extern bool UnityARCore_Camera_TryGetIntrinsics(out XRCameraIntrinsics cameraIntrinsics);

            [DllImport("UnityARCore")]
            public static unsafe extern void* UnityARCore_Camera_AcquireTextureDescriptors(
                out int length, out int elementSize);

            [DllImport("UnityARCore")]
            public static unsafe extern void UnityARCore_Camera_ReleaseTextureDescriptors(
                void* descriptors);
#else
            public static void UnityARCore_Camera_Construct(int mainTexPropertyNameId) {}

            public static void UnityARCore_Camera_Destruct() {}

            public static void UnityARCore_Camera_Start() {}

            public static void UnityARCore_Camera_Stop() {}

            public static bool UnityARCore_Camera_TryGetFrame(XRCameraParams cameraParams, out XRCameraFrame cameraFrame)
            {
                cameraFrame = default(XRCameraFrame);
                return false;
            }

            public static bool UnityARCore_Camera_TrySetFocusMode(CameraFocusMode cameraFocusMode) { return false; }

            public static bool UnityARCore_Camera_TrySetLightEstimationMode(LightEstimationMode lightEstimationMode) { return false; }

            public static bool UnityARCore_Camera_TryGetIntrinsics(out XRCameraIntrinsics cameraIntrinsics)
            {
                cameraIntrinsics = default(XRCameraIntrinsics);
                return false;
            }

            public static unsafe void* UnityARCore_Camera_AcquireTextureDescriptors(
                out int length, out int elementSize)
            {
                length = elementSize = 0;
                return null;
            }

            public static unsafe void UnityARCore_Camera_ReleaseTextureDescriptors(
                void* descriptors)
            { }
#endif
        }
    }
}
