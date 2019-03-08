using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARKit
{
    /// <summary>
    /// The camera subsystem implementation for ARKit.
    /// </summary>
   [Preserve]
    public sealed class ARKitCameraSubsystem : XRCameraSubsystem
    {
        /// <summary>
        /// The identifying name for the camera-providing implementation.
        /// </summary>
        /// <value>
        /// The identifying name for the camera-providing implementation.
        /// </value>
        const string k_SubsystemId = "ARKit-Camera";

        /// <summary>
        /// Create and register the camera subsystem descriptor to advertise a providing implementation for camera
        /// functionality.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            XRCameraSubsystemCinfo cameraSubsystemCinfo = new XRCameraSubsystemCinfo();
            cameraSubsystemCinfo.id = k_SubsystemId;
            cameraSubsystemCinfo.implementationType = typeof(ARKitCameraSubsystem);
            cameraSubsystemCinfo.supportsAverageBrightness = true;
            cameraSubsystemCinfo.supportsAverageColorTemperature = true;
            cameraSubsystemCinfo.supportsDisplayMatrix = true;
            cameraSubsystemCinfo.supportsProjectionMatrix = true;
            cameraSubsystemCinfo.supportsTimestamp = true;

            if (!XRCameraSubsystem.Register(cameraSubsystemCinfo))
            {
                Debug.LogErrorFormat("Cannot register the {0} subsystem", k_SubsystemId);
            }
        }

        /// <summary>
        /// Create the ARKit camera functionality provider for the camera subsystem.
        /// </summary>
        /// <returns>
        /// The ARKit camera functionality provider for the camera subsystem.
        /// </returns>
        protected override IProvider CreateProvider()
        {
            return new Provider();
        }

        /// <summary>
        /// Provides the camera functionality for the ARKit implementation.
        /// </summary>
        class Provider : IProvider
        {
            /// <summary>
            /// The shader property name for the luminance component of the camera video frame.
            /// </summary>
            /// <value>
            /// The shader property name for the luminance component of the camera video frame.
            /// </value>
            const string k_TextureYPropertyName = "_textureY";

            /// <summary>
            /// The shader property name for the chrominance components of the camera video frame.
            /// </summary>
            /// <value>
            /// The shader property name for the chrominance components of the camera video frame.
            /// </value>
            const string k_TextureCbCrPropertyName = "_textureCbCr";

            /// <summary>
            /// The shader property name identifier for the luminance component of the camera video frame.
            /// </summary>
            /// <value>
            /// The shader property name identifier for the luminance component of the camera video frame.
            /// </value>
            static readonly int k_TextureYPropertyNameId = Shader.PropertyToID(k_TextureYPropertyName);

            /// <summary>
            /// The shader property name identifier for the chrominance components of the camera video frame.
            /// </summary>
            /// <value>
            /// The shader property name identifier for the chrominance components of the camera video frame.
            /// </value>
            static readonly int k_TextureCbCrPropertyNameId = Shader.PropertyToID(k_TextureCbCrPropertyName);

            /// <summary>
            /// Constructs the ARKit camera functionality provider.
            /// </summary>
            public Provider()
            {
                NativeApi.UnityARKit_Camera_Construct(k_TextureYPropertyNameId,
                                                      k_TextureCbCrPropertyNameId);
            }

            /// <summary>
            /// Start the camera functionality.
            /// </summary>
            public override void Start()
            {
                NativeApi.UnityARKit_Camera_Start();
            }

            /// <summary>
            /// Stop the camera functionality.
            /// </summary>
            public override void Stop()
            {
                NativeApi.UnityARKit_Camera_Stop();
            }

            /// <summary>
            /// Destroy any resources required for the camera functionality.
            /// </summary>
            public override void Destroy()
            {
                NativeApi.UnityARKit_Camera_Destruct();
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
                return NativeApi.UnityARKit_Camera_TryGetFrame(cameraParams, out cameraFrame);
            }

            /// <summary>
            /// Get the shader name used by <c>XRCameraSubsystem</c> to render texture.
            /// </summary>
            public override string shaderName
            {
                get { return "Unlit/ARKit"; }
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
                return NativeApi.UnityARKit_Camera_TrySetFocusMode(cameraFocusMode);
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
                return NativeApi.UnityARKit_Camera_TrySetLightEstimationMode(lightEstimationMode);
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
                return NativeApi.UnityARKit_Camera_TryGetIntrinsics(out cameraIntrinsics);
            }

            /// <summary>
            /// Determine whether camera permission has been granted.
            /// </summary>
            /// <returns>
            /// <c>true</c> if camera permission has been granted for this app. Otherwise, <c>false</c>.
            /// </returns>
            public override bool permissionGranted
            {
                get { return NativeApi.UnityARKit_Camera_IsCameraPermissionGranted(); }
            }

            /// <summary>
            /// Gets the texture descriptors associated with th current camera
            /// frame.
            /// </summary>
            /// <returns>The texture descriptors.</returns>
            /// <param name="defaultDescriptor">Default descriptor.</param>
            /// <param name="allocator">Allocator.</param>
            public unsafe override NativeArray<XRTextureDescriptor> GetTextureDescriptors(
                XRTextureDescriptor defaultDescriptor,
                Allocator allocator)
            {
                int length, elementSize;
                var textureDescriptors = NativeApi.UnityARKit_Camera_AcquireTextureDescriptors(
                    out length, out elementSize);

                try
                {
                    return NativeCopyUtility.PtrToNativeArrayWithDefault(
                        defaultDescriptor,
                        textureDescriptors, elementSize, length, allocator);
                }
                finally
                {
                    NativeApi.UnityARKit_Camera_ReleaseTextureDescriptors(textureDescriptors);
                }
            }
        }

        /// <summary>
        /// Container to wrap the native ARKit camera APIs.
        /// </summary>
        static class NativeApi
        {
#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal")]
            public static extern void UnityARKit_Camera_Construct(int textureYPropertyNameId,
                                                                  int textureCbCrPropertyNameId);

            [DllImport("__Internal")]
            public static extern void UnityARKit_Camera_Destruct();

            [DllImport("__Internal")]
            public static extern void UnityARKit_Camera_Start();

            [DllImport("__Internal")]
            public static extern void UnityARKit_Camera_Stop();

            [DllImport("__Internal")]
            public static extern bool UnityARKit_Camera_TryGetFrame(XRCameraParams cameraParams,
                                                                    out XRCameraFrame cameraFrame);

            [DllImport("__Internal")]
            public static extern bool UnityARKit_Camera_TrySetFocusMode(CameraFocusMode cameraFocusMode);

            [DllImport("__Internal")]
            public static extern bool UnityARKit_Camera_TrySetLightEstimationMode(LightEstimationMode lightEstimationMode);

            [DllImport("__Internal")]
            public static extern bool UnityARKit_Camera_TryGetIntrinsics(out XRCameraIntrinsics cameraIntrinsics);

            [DllImport("__Internal")]
            public static extern bool UnityARKit_Camera_IsCameraPermissionGranted();

            [DllImport("__Internal")]
            public static unsafe extern void* UnityARKit_Camera_AcquireTextureDescriptors(
                out int length, out int elementSize);

            [DllImport("__Internal")]
            public static unsafe extern void UnityARKit_Camera_ReleaseTextureDescriptors(
                void* descriptors);
#else
            public static void UnityARKit_Camera_Construct(int textureYPropertyNameId, int textureCbCrPropertyNameId) {}

            public static void UnityARKit_Camera_Destruct() {}

            public static void UnityARKit_Camera_Start() {}

            public static void UnityARKit_Camera_Stop() {}

            public static bool UnityARKit_Camera_TryGetFrame(XRCameraParams cameraParams, out XRCameraFrame cameraFrame)
            {
                cameraFrame = default(XRCameraFrame);
                return false;
            }

            public static bool UnityARKit_Camera_TrySetFocusMode(CameraFocusMode cameraFocusMode) { return false; }

            public static bool UnityARKit_Camera_TrySetLightEstimationMode(LightEstimationMode lightEstimationMode) { return false; }

            public static bool UnityARKit_Camera_TryGetIntrinsics(out XRCameraIntrinsics cameraIntrinsics)
            {
                cameraIntrinsics = default(XRCameraIntrinsics);
                return false;
            }

            public static bool UnityARKit_Camera_IsCameraPermissionGranted() { return true; }

            public static unsafe void* UnityARKit_Camera_AcquireTextureDescriptors(
                out int length, out int elementSize)
            {
                length = elementSize = 0;
                return null;
            }

            public static unsafe void UnityARKit_Camera_ReleaseTextureDescriptors(
                void* descriptors)
            { }
#endif
        }
    }
}
