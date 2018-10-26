using System;
using System.Collections.Generic;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARExtensions
{
    /// <summary>
    /// Provides extensions to the <c>XRCameraSubsystem</c>.
    /// </summary>
    public static class XRCameraExtensions
    {
        /// <summary>
        /// For internal use. Defines a delegate that a platform-specific camera provider can implement
        /// to provide color correction data.
        /// </summary>
        /// <param name="cameraSubsystem">The <c>XRCameraSubsystem</c> being extended.</param>
        /// <param name="color">A <c>Color</c> representing the color correction data.</param>
        /// <returns><c>True</c> if the color correction was retrieved, otherwise, <c>False</c>.</returns>
        public delegate bool TryGetColorCorrectionDelegate(
            XRCameraSubsystem cameraSubsystems,
            out Color color);

        /// <summary>
        /// A delegate which implementors of <see cref="AsyncCameraImageConversion"/> must invoke when
        /// asynchronous camera image requests are made using the callback variant. See <see cref="CameraImage.ConvertAsync(CameraImageConversionParams, Action{AsyncCameraImageConversionStatus, CameraImageConversionParams, Unity.Collections.NativeArray{byte}})"/>.
        /// Consumers of the <c>XRCameraSubsystem</c> API should use <see cref="CameraImage.ConvertAsync(CameraImageConversionParams, Action{AsyncCameraImageConversionStatus, CameraImageConversionParams, Unity.Collections.NativeArray{byte}})"/>.
        /// </summary>
        /// <param name="status">The status of the request.</param>
        /// <param name="dataPtr">A pointer to the image data. Must be valid for the duration of the invocation, but may be destroyed immediately afterwards.</param>
        /// <param name="dataLength">The number of bytes pointed to by <paramref name="dataPtr"/>.</param>
        /// <param name="context">An <c>IntPtr</c> which is supplied to the API and must be passed back unaltered to this delegate.</param>
        public delegate void OnImageRequestCompleteDelegate(
            AsyncCameraImageConversionStatus status,
            CameraImageConversionParams conversionParams,
            IntPtr dataPtr,
            int dataLength,
            IntPtr context);

        /// <summary>
        /// For internal use. Allows a camera provider to register for the IsPermissionGranted extension.
        /// </summary>
        /// <param name="subsystemId">The string name associated with the camera provider to extend.</param>
        /// <param name="handler">A method that returns true if permission is granted.</param>
        public static void RegisterIsPermissionGrantedHandler(string subsystemId, Func<XRCameraSubsystem, bool> handler)
        {
            s_IsPermissionGrantedDelegates[subsystemId] = handler;
        }

        /// <summary>
        /// For internal use. Allows a camera provider to register for the TryGetColorCorrection extension.
        /// </summary>
        /// <param name="subsystemId">The string name associated with the camera provider to extend.</param>
        /// <param name="handler">A method that returns true if color correction is available.</param>
        public static void RegisterTryGetColorCorrectionHandler(string subsystemId, TryGetColorCorrectionDelegate handler)
        {
            s_TryGetColorCorrectionDelegates[subsystemId] = handler;
        }

        /// <summary>
        /// Allows a camera provider to register support for <see cref="ICameraImageApi"/>.
        /// This is typically only used by platform-specific packages.
        /// </summary>
        /// <param name="subsystemId"></param>
        /// <param name="api"></param>
        public static void RegisterCameraImageApi(string subsystemId, ICameraImageApi api)
        {
            s_CameraImageApis[subsystemId] = api;
        }

        /// <summary>
        /// Attempts to retrieve color correction data for the extended <c>XRCameraSubsystem</c>.
        /// The color correction data represents the scaling factors used for color correction.
        /// The RGB scale factors are used to match the color of the light
        /// in the scene. The alpha channel value is platform-specific.
        /// </summary>
        /// <param name="cameraSubsystem">The <c>XRCameraSubsystem</c> being extended.</param>
        /// <param name="color">The <c>Color</c> representing the color correction value.</param>
        /// <returns><c>True</c> if the data is available, otherwise <c>False</c>.</returns>
        public static bool TryGetColorCorrection(this XRCameraSubsystem cameraSubsystem, out Color color)
        {
            if (cameraSubsystem == null)
                throw new ArgumentNullException("cameraSubsystem");

            return s_TryGetColorCorrectionDelegate(cameraSubsystem, out color);
        }

        /// <summary>
        /// Allows you to determine whether camera permission has been granted.
        /// </summary>
        /// <param name="cameraSubsystem">The <c>XRCameraSubsystem</c> being extended.</param>
        /// <returns>True if camera permission has been granted for this app, false otherwise.</returns>
        public static bool IsPermissionGranted(this XRCameraSubsystem cameraSubsystem)
        {
            if (cameraSubsystem == null)
                throw new ArgumentNullException("cameraSubsystem");

            return s_IsPermissionGrantedDelegate(cameraSubsystem);
        }

        /// <summary>
        /// Attempt to get the latest camera image. This provides directly access to the raw
        /// pixel data, as well as utilities to convert to RGB and Grayscale formats.
        /// The <see cref="CameraImage"/> must be disposed to avoid resource leaks.
        /// </summary>
        /// <param name="cameraSubsystem">The <c>XRCameraSubsystem</c> being extended.</param>
        /// <param name="cameraImage"></param>
        /// <returns></returns>
        public static bool TryGetLatestImage(
            this XRCameraSubsystem cameraSubsystem,
            out CameraImage cameraImage)
        {
            if (cameraSubsystem == null)
                throw new ArgumentNullException("cameraSubsystem");

            int nativeHandle;
            Vector2Int dimensions;
            int planeCount;
            double timestamp;
            CameraImageFormat format;
            if (s_AsyncCameraImageApi.TryAcquireLatestImage(out nativeHandle, out dimensions, out planeCount, out timestamp, out format))
            {
                cameraImage = new CameraImage(s_AsyncCameraImageApi, nativeHandle, dimensions, planeCount, timestamp, format);
                return true;
            }
            else
            {
                cameraImage = default(CameraImage);
                return false;
            }
        }

        /// <summary>
        /// For internal use. Sets the active subsystem whose extension methods should be used.
        /// </summary>
        /// <param name="cameraSubsystem">The <c>XRCameraSubsystem</c> being extended.</param>
        public static void ActivateExtensions(this XRCameraSubsystem cameraSubsystem)
        {
            if (cameraSubsystem == null)
            {
                SetDefaultDelegates();
            }
            else
            {
                var id = cameraSubsystem.SubsystemDescriptor.id;
                s_IsPermissionGrantedDelegate = RegistrationHelper.GetValueOrDefault(s_IsPermissionGrantedDelegates, id, DefaultIsPermissionGranted);
                s_TryGetColorCorrectionDelegate = RegistrationHelper.GetValueOrDefault(s_TryGetColorCorrectionDelegates, id, DefaultTryGetColorCorrection);
                s_AsyncCameraImageApi = RegistrationHelper.GetValueOrDefault(s_CameraImageApis, id, s_DefaultAsyncCameraImageApi);
            }
        }

        static bool DefaultIsPermissionGranted(XRCameraSubsystem cameraSubsystem)
        {
            return true;
        }

        static bool DefaultTryGetColorCorrection(XRCameraSubsystem cameraSubsystem, out Color color)
        {
            color = default(Color);
            return false;
        }

        class DefaultCameraImageApi : ICameraImageApi
        {
            public AsyncCameraImageConversionStatus GetAsyncRequestStatus(int requestId)
            {
                return AsyncCameraImageConversionStatus.Disposed;
            }

            public void DisposeImage(int nativeHandle)
            { }

            public void DisposeAsyncRequest(int requestId)
            { }

            public bool TryGetPlane(int nativeHandle, int planeIndex, out int rowStride, out int pixelStride, out IntPtr dataPtr, out int dataLength)
            {
                rowStride = default(int);
                pixelStride = default(int);
                dataPtr = default(IntPtr);
                dataLength = default(int);
                return false;
            }

            public bool TryAcquireLatestImage(out int nativeHandle, out Vector2Int dimensions, out int planeCount, out double timestamp, out CameraImageFormat format)
            {
                nativeHandle = default(int);
                dimensions = default(Vector2Int);
                planeCount = default(int);
                timestamp = default(double);
                format = default(CameraImageFormat);
                return false;
            }

            public bool NativeHandleValid(int nativeHandle)
            {
                return false;
            }

            public bool TryGetConvertedDataSize(int nativeHandle, Vector2Int dimensions, TextureFormat format, out int size)
            {
                size = default(int);
                return false;
            }

            public bool TryConvert(int nativeHandle, CameraImageConversionParams conversionParams, IntPtr destinationBuffer, int bufferLength)
            {
                return false;
            }

            public int ConvertAsync(int nativeHandle, CameraImageConversionParams conversionParams)
            {
                return 0;
            }

            public bool TryGetAsyncRequestData(int requestId, out IntPtr dataPtr, out int dataLength)
            {
                dataPtr = default(IntPtr);
                dataLength = default(int);
                return false;
            }

            public void ConvertAsync(int nativeHandle, CameraImageConversionParams conversionParams, OnImageRequestCompleteDelegate callback, IntPtr context)
            {
                callback(AsyncCameraImageConversionStatus.Disposed, conversionParams, IntPtr.Zero, 0, context);
            }
        }

        static void SetDefaultDelegates()
        {
            s_IsPermissionGrantedDelegate = DefaultIsPermissionGranted;
            s_TryGetColorCorrectionDelegate = DefaultTryGetColorCorrection;
            s_AsyncCameraImageApi = s_DefaultAsyncCameraImageApi;
        }

        static XRCameraExtensions()
        {
            s_DefaultAsyncCameraImageApi = new DefaultCameraImageApi();
            SetDefaultDelegates();
        }

        static Func<XRCameraSubsystem, bool> s_IsPermissionGrantedDelegate;
        static TryGetColorCorrectionDelegate s_TryGetColorCorrectionDelegate;
        static ICameraImageApi s_AsyncCameraImageApi;
        static DefaultCameraImageApi s_DefaultAsyncCameraImageApi;

        static Dictionary<string, Func<XRCameraSubsystem, bool>> s_IsPermissionGrantedDelegates =
            new Dictionary<string, Func<XRCameraSubsystem, bool>>();

        static Dictionary<string, TryGetColorCorrectionDelegate> s_TryGetColorCorrectionDelegates =
            new Dictionary<string, TryGetColorCorrectionDelegate>();

        static Dictionary<string, ICameraImageApi> s_CameraImageApis =
            new Dictionary<string, ICameraImageApi>();
    }
}
