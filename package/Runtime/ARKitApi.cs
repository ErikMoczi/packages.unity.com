using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.XR;
using UnityEngine.XR.ARExtensions;

namespace UnityEngine.XR.ARKit
{
    internal static class Api
    {
        // Should match ARKitAvailability in ARKitXRSessionProvider.mm
        public enum Availability
        {
            None,
            Supported
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        internal static extern TrackableId UnityARKit_attachReferencePoint(TrackableId trackableId, Pose pose);

        [DllImport("__Internal")]
        internal static extern Availability UnityARKit_CheckAvailability();

        [DllImport("__Internal")]
        internal static extern bool UnityARKit_IsCameraPermissionGranted();

        [DllImport("__Internal")]
        internal static extern TrackingState UnityARKit_getAnchorTrackingState(TrackableId id);

        [DllImport("__Internal")]
        static internal extern bool UnityARKit_cameraImage_tryAcquireLatestImage(
            out int nativeHandle, out Vector2Int dimensions, out int planeCount, out double timestamp, out CameraImageFormat format);

        [DllImport("__Internal")]
        static internal extern bool UnityARKit_cameraImage_tryGetConvertedDataSize(
            int nativeHandle, Vector2Int dimensions, TextureFormat format, out int size);

        [DllImport("__Internal")]
        static internal extern bool UnityARKit_cameraImage_tryConvert(
            int nativeHandle, CameraImageConversionParams conversionParams,
            IntPtr buffer, int bufferLength);

        [DllImport("__Internal")]
        static internal extern bool UnityARKit_cameraImage_tryGetPlane(
            int nativeHandle, int planeIndex,
            out int rowStride, out int pixelStride, out IntPtr dataPtr, out int dataLength);

        [DllImport("__Internal")]
        static internal extern bool UnityARKit_cameraImage_handleValid(
            int nativeHandle);

        [DllImport("__Internal")]
        static internal extern void UnityARKit_cameraImage_disposeImage(
            int nativeHandle);

        [DllImport("__Internal")]
        static internal extern int UnityARKit_cameraImage_createAsyncConversionRequest(
            int nativeHandle, CameraImageConversionParams conversionParams);

        [DllImport("__Internal")]
        static internal extern void UnityARKit_cameraImage_createAsyncConversionRequestWithCallback(
            int nativeHandle, CameraImageConversionParams conversionParams,
            XRCameraExtensions.OnImageRequestCompleteDelegate callback, IntPtr context);

        [DllImport("__Internal")]
        static internal extern AsyncCameraImageConversionStatus
            UnityARKit_cameraImage_getAsyncRequestStatus(int requestId);

        [DllImport("__Internal")]
        static internal extern void UnityARKit_cameraImage_disposeAsyncRequest(
            int requestHandle);

        [DllImport("__Internal")]
        static internal extern bool UnityARKit_cameraImage_tryGetAsyncRequestData(
            int requestHandle, out IntPtr dataPtr, out int dataLength);

#else
        static internal bool UnityARKit_cameraImage_tryAcquireLatestImage(
            out int nativeHandle, out Vector2Int dimensions, out int planeCount, out double timestamp, out CameraImageFormat format)
        {
            nativeHandle = 0;
            dimensions = default(Vector2Int);
            planeCount = default(int);
            timestamp = default(double);
            format = default(CameraImageFormat);
            return false;
        }

        static internal bool UnityARKit_cameraImage_tryGetConvertedDataSize(
            int nativeHandle, Vector2Int dimensions, TextureFormat format, out int size)
        {
            size = default(int);
            return false;
        }

        static internal bool UnityARKit_cameraImage_tryConvert(
            int nativeHandle, CameraImageConversionParams conversionParams,
            IntPtr buffer, int bufferLength)
        {
            return false;
        }

        static internal bool UnityARKit_cameraImage_tryGetPlane(
            int nativeHandle, int planeIndex,
            out int rowStride, out int pixelStride, out IntPtr dataPtr, out int dataLength)
        {
            rowStride = default(int);
            pixelStride = default(int);
            dataPtr = default(IntPtr);
            dataLength = default(int);
            return false;
        }

        static internal bool UnityARKit_cameraImage_handleValid(
            int nativeHandle)
        {
            return false;
        }

        static internal void UnityARKit_cameraImage_disposeImage(
            int nativeHandle)
        { }

        static internal int UnityARKit_cameraImage_createAsyncConversionRequest(
            int nativeHandle, CameraImageConversionParams conversionParams)
        {
            return 0;
        }

        static internal void UnityARKit_cameraImage_createAsyncConversionRequestWithCallback(
            int nativeHandle, CameraImageConversionParams conversionParams,
            XRCameraExtensions.OnImageRequestCompleteDelegate callback, IntPtr context)
        {
            callback(AsyncCameraImageConversionStatus.Disposed, conversionParams, IntPtr.Zero, 0, context);
        }

        static internal AsyncCameraImageConversionStatus
            UnityARKit_cameraImage_getAsyncRequestStatus(int requestId)
        {
            return AsyncCameraImageConversionStatus.Disposed;
        }

        static internal void UnityARKit_cameraImage_disposeAsyncRequest(
            int requestHandle)
        { }

        static internal bool UnityARKit_cameraImage_tryGetAsyncRequestData(
            int requestHandle, out IntPtr dataPtr, out int dataLength)
        {
            dataPtr = default(IntPtr);
            dataLength = default(int);
            return false;
        }        internal static Availability UnityARKit_CheckAvailability() { return Availability.None; }

        internal static TrackableId UnityARKit_attachReferencePoint(TrackableId trackableId, Pose pose)
        {
            return TrackableId.InvalidId;
        }

        internal static bool UnityARKit_IsCameraPermissionGranted() { return false; }

        internal static TrackingState UnityARKit_getAnchorTrackingState(TrackableId id) { return TrackingState.Unavailable; }
#endif
    }
}
