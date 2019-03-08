using AOT;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARCore
{
    /// <summary>
    /// ARCore implementation of the <c>XRSessionSubsystem</c>. Do not create this directly. Use the <c>SubsystemManager</c> instead.
    /// </summary>
    [Preserve]
    public sealed class ARCoreSessionSubsystem : XRSessionSubsystem
    {
        /// <summary>
        /// Creates the provider interface.
        /// </summary>
        /// <returns>The provider interface for ARCore</returns>
        protected override IProvider CreateProvider()
        {
            return new Provider();
        }

        class Provider : IProvider
        {
            public Provider()
            {
                NativeApi.UnityARCore_session_construct(CameraPermissionRequestProvider);
            }

            public override void Resume()
            {
                NativeApi.UnityARCore_session_resume();
            }

            public override void Pause()
            {
                NativeApi.UnityARCore_session_pause();
            }

            public override void Update(XRSessionUpdateParams updateParams)
            {
                NativeApi.UnityARCore_session_update(
                    updateParams.screenOrientation,
                    updateParams.screenDimensions);
            }

            public override void Destroy()
            {
                NativeApi.UnityARCore_session_destroy();
            }

            public override void Reset()
            {
                NativeApi.UnityARCore_session_reset();
            }

            public override void OnApplicationPause()
            {
                NativeApi.UnityARCore_session_onApplicationPause();
            }

            public override void OnApplicationResume()
            {
                NativeApi.UnityARCore_session_onApplicationResume();
            }

            public override Promise<SessionAvailability> GetAvailabilityAsync()
            {
                return ExecuteAsync<SessionAvailability>((context) =>
                {
                    NativeApi.ArPresto_checkApkAvailability(OnCheckApkAvailability, context);
                });
            }

            public override Promise<SessionInstallationStatus> InstallAsync()
            {
                return ExecuteAsync<SessionInstallationStatus>((context) =>
                {
                    NativeApi.ArPresto_requestApkInstallation(true, OnApkInstallation, context);
                });
            }

            public override IntPtr nativePtr
            {
                get
                {
                    return NativeApi.UnityARCore_session_getNativePtr();
                }
            }

            public override TrackingState trackingState
            {
                get
                {
                    return NativeApi.UnityARCore_session_getTrackingState();
                }
            }

            static Promise<T> ExecuteAsync<T>(Action<IntPtr> apiMethod)
            {
                var promise = new ARCorePromise<T>();
                GCHandle gch = GCHandle.Alloc(promise);
                apiMethod(GCHandle.ToIntPtr(gch));
                return promise;
            }

            [MonoPInvokeCallback(typeof(Action<NativeApi.ArPrestoApkInstallStatus, IntPtr>))]
            static void OnApkInstallation(NativeApi.ArPrestoApkInstallStatus status, IntPtr context)
            {
                var sessionInstallation = SessionInstallationStatus.None;
                switch (status)
                {
                    case NativeApi.ArPrestoApkInstallStatus.ErrorDeviceNotCompatible:
                        sessionInstallation = SessionInstallationStatus.ErrorDeviceNotCompatible;
                        break;

                    case NativeApi.ArPrestoApkInstallStatus.ErrorUserDeclined:
                        sessionInstallation = SessionInstallationStatus.ErrorUserDeclined;
                        break;

                    case NativeApi.ArPrestoApkInstallStatus.Requested:
                        // This shouldn't happen
                        sessionInstallation = SessionInstallationStatus.Error;
                        break;

                    case NativeApi.ArPrestoApkInstallStatus.Success:
                        sessionInstallation = SessionInstallationStatus.Success;
                        break;

                    case NativeApi.ArPrestoApkInstallStatus.Error:
                    default:
                        sessionInstallation = SessionInstallationStatus.Error;
                        break;
                }

                ResolvePromise(context, sessionInstallation);
            }

            [MonoPInvokeCallback(typeof(Action<NativeApi.ArAvailability, IntPtr>))]
            static void OnCheckApkAvailability(NativeApi.ArAvailability availability, IntPtr context)
            {
                var sessionAvailability = SessionAvailability.None;
                switch (availability)
                {
                    case NativeApi.ArAvailability.SupportedNotInstalled:
                    case NativeApi.ArAvailability.SupportedApkTooOld:
                        sessionAvailability = SessionAvailability.Supported;
                        break;

                    case NativeApi.ArAvailability.SupportedInstalled:
                        sessionAvailability = SessionAvailability.Supported | SessionAvailability.Installed;
                        break;

                    default:
                        sessionAvailability = SessionAvailability.None;
                        break;
                }

                ResolvePromise(context, sessionAvailability);
            }

            [MonoPInvokeCallback(typeof(NativeApi.CameraPermissionRequestProviderDelegate))]
            static void CameraPermissionRequestProvider(NativeApi.CameraPermissionsResultCallbackDelegate callback, IntPtr context)
            {
                ARCorePermissionManager.RequestPermission(k_CameraPermissionName, (permissinName, granted) =>
                {
                    callback(granted, context);
                });
            }

            static void ResolvePromise<T>(IntPtr context, T arg) where T : struct
            {
                GCHandle gch = GCHandle.FromIntPtr(context);
                var promise = (ARCorePromise<T>)gch.Target;
                if (promise != null)
                    promise.Resolve(arg);
                gch.Free();
            }

            const string k_CameraPermissionName = "android.permission.CAMERA";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterDescriptor()
        {
            XRSessionSubsystemDescriptor.RegisterDescriptor(new XRSessionSubsystemDescriptor.Cinfo
            {
                id = "ARCore-Session",
                subsystemImplementationType = typeof(ARCoreSessionSubsystem),
                supportsInstall = false
            });
        }

        static class NativeApi
        {
            public enum ArPrestoApkInstallStatus
            {
                Uninitialized = 0,
                Requested = 1,
                Success = 100,
                Error = 200,
                ErrorDeviceNotCompatible = 201,
                ErrorUserDeclined = 203,
            }

            public enum ArAvailability
            {
                UnknownError = 0,
                UnknownChecking = 1,
                UnknownTimedOut = 2,
                UnsupportedDeviceNotCapable = 100,
                SupportedNotInstalled = 201,
                SupportedApkTooOld = 202,
                SupportedInstalled = 203
            }

            public delegate void CameraPermissionRequestProviderDelegate(
                CameraPermissionsResultCallbackDelegate resultCallback,
                IntPtr context);

            public delegate void CameraPermissionsResultCallbackDelegate(
                bool granted,
                IntPtr context);

#if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport("UnityARCore")]
            public static extern IntPtr UnityARCore_session_getNativePtr();

            [DllImport("UnityARCore")]
            public static extern void ArPresto_checkApkAvailability(
                Action<ArAvailability, IntPtr> onResult, IntPtr context);

            [DllImport("UnityARCore")]
            public static extern void ArPresto_requestApkInstallation(
                bool userRequested, Action<ArPrestoApkInstallStatus, IntPtr> onResult, IntPtr context);

            [DllImport("UnityARCore")]
            public static extern void UnityARCore_session_update(
                ScreenOrientation orientation,
                Vector2Int screenDimensions);

            [DllImport("UnityARCore")]
            public static extern void UnityARCore_session_construct(
                CameraPermissionRequestProviderDelegate cameraPermissionRequestProvider);

            [DllImport("UnityARCore")]
            public static extern void UnityARCore_session_destroy();

            [DllImport("UnityARCore")]
            public static extern void UnityARCore_session_resume();

            [DllImport("UnityARCore")]
            public static extern void UnityARCore_session_pause();

            [DllImport("UnityARCore")]
            public static extern void UnityARCore_session_onApplicationResume();

            [DllImport("UnityARCore")]
            public static extern void UnityARCore_session_onApplicationPause();

            [DllImport("UnityARCore")]
            public static extern void UnityARCore_session_reset();

            [DllImport("UnityARCore")]
            public static extern TrackingState UnityARCore_session_getTrackingState();
#else
            public static IntPtr UnityARCore_session_getNativePtr()
            {
                return IntPtr.Zero;
            }

            public static void ArPresto_checkApkAvailability(
                Action<ArAvailability, IntPtr> onResult, IntPtr context)
            {
                onResult(ArAvailability.UnsupportedDeviceNotCapable, context);
            }

            public static void ArPresto_requestApkInstallation(
                bool userRequested, Action<ArPrestoApkInstallStatus, IntPtr> onResult, IntPtr context)
            {
                onResult(ArPrestoApkInstallStatus.ErrorDeviceNotCompatible, context);
            }

            public static void UnityARCore_session_update(
                ScreenOrientation orientation,
                Vector2Int screenDimensions)
            { }

            public static void UnityARCore_session_construct(
                CameraPermissionRequestProviderDelegate cameraPermissionRequestProvider)
            { }

            public static void UnityARCore_session_destroy()
            { }

            public static void UnityARCore_session_resume()
            { }

            public static void UnityARCore_session_pause()
            { }

            public static void UnityARCore_session_onApplicationResume()
            { }

            public static void UnityARCore_session_onApplicationPause()
            { }

            public static void UnityARCore_session_reset()
            { }

            public static TrackingState UnityARCore_session_getTrackingState()
            {
                return TrackingState.None;
            }
#endif
        }
    }
}
