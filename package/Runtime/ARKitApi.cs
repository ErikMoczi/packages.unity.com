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
        internal static extern int UnityARKit_createWorldMapRequest();

        [DllImport("__Internal")]
        internal static extern void UnityARKit_createWorldMapRequestWithCallback(
            ARWorldMapSessionExtensions.OnAsyncConversionCompleteDelegate callback,
            IntPtr context);

        [DllImport("__Internal")]
        internal static extern ARWorldMapRequestStatus UnityARKit_getWorldMapRequestStatus(int worldMapId);

        [DllImport("__Internal")]
        internal static extern void UnityARKit_disposeWorldMap(int worldMapId);

        [DllImport("__Internal")]
        internal static extern void UnityARKit_disposeWorldMapRequest(int worldMapId);

        [DllImport("__Internal")]
        internal static extern bool UnityARKit_worldMapSupported();

        [DllImport("__Internal")]
        internal static extern ARWorldMappingStatus UnityARKit_getWorldMappingStatus();

        [DllImport("__Internal")]
        internal static extern void UnityARKit_applyWorldMap(int worldMapId);

        [DllImport("__Internal")]
        internal static extern int UnityARKit_getWorldMapIdFromRequestId(int requestId);

        [DllImport("__Internal")]
        internal static extern bool UnityARKit_isWorldMapValid(int nativeHandle);

        [DllImport("__Internal")]
        internal static extern bool UnityARKit_trySerializeWorldMap(
            int nativeHandle, out IntPtr nsdata, out int length);

        [DllImport("__Internal")]
        internal static extern int UnityARKit_copyAndReleaseNsData(
            IntPtr destination, IntPtr sourceNsData, int length);

        [DllImport("__Internal")]
        internal static extern int UnityARKit_deserializeWorldMap(
            IntPtr buffer, int bufferLength);
		
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

        [DllImport("__Internal")]
        static internal extern IntPtr UnityARKit_getNativePlanePtr(TrackableId trackableId);

        [DllImport("__Internal")]
        static internal extern IntPtr UnityARKit_getNativeReferencePointPtr(TrackableId trackableId);

        [DllImport("__Internal")]
        static internal extern IntPtr UnityARKit_getNativeSessionPtr();

        [DllImport("__Internal")]
        static internal extern IntPtr UnityARKit_getNativeFramePtr();

        [DllImport("__Internal")]
        static internal extern int UnityARKit_cameraImage_getConfigurationCount();

        [DllImport("__Internal")]
        static internal extern bool UnityARKit_cameraImage_tryGetConfiguration(
            int index,
            out CameraConfiguration configuration);

        [DllImport("__Internal")]
        static internal extern bool UnityARKit_cameraImage_trySetConfiguration(
            CameraConfiguration configuration);

        [DllImport("__Internal")]
        static internal extern bool UnityARKit_cameraImage_tryGetCurrentConfiguration(
            out CameraConfiguration configuration);
#else
        static internal bool UnityARKit_cameraImage_tryGetCurrentConfiguration(
            out CameraConfiguration configuration)
        {
            configuration = default(CameraConfiguration);
            return false;
        }

        static internal bool UnityARKit_cameraImage_trySetConfiguration(
            CameraConfiguration configuration)
        {
            return false;
        }

        static internal bool UnityARKit_cameraImage_tryGetConfiguration(
            int index,
            out CameraConfiguration configuration)
        {
            configuration = default(CameraConfiguration);
            return false;
        }

        static internal int UnityARKit_cameraImage_getConfigurationCount()
        {
            return 0;
        }

        internal static bool UnityARKit_worldMapSupported()
        {
            return false;
        }

        internal static int UnityARKit_createWorldMapRequest()
        {
            return default(int);
        }

        internal static void UnityARKit_createWorldMapRequestWithCallback(
            ARWorldMapSessionExtensions.OnAsyncConversionCompleteDelegate callback,
            IntPtr context)
        {
            callback(ARWorldMapRequestStatus.ErrorNotSupported, ARWorldMap.k_InvalidHandle, context);
        }


        internal static ARWorldMapRequestStatus UnityARKit_getWorldMapRequestStatus(int worldMapId)
        {
            return default(ARWorldMapRequestStatus);
        }

        internal static void UnityARKit_disposeWorldMap(int worldMapId)
        { }

        internal static void UnityARKit_disposeWorldMapRequest(int worldMapId)
        { }

        internal static void UnityARKit_applyWorldMap(int worldMapId)
        { }

        internal static int UnityARKit_getWorldMapIdFromRequestId(int requestId)
        {
            return default(int);
        }

        internal static ARWorldMappingStatus UnityARKit_getWorldMappingStatus()
        {
            return ARWorldMappingStatus.NotAvailable;
        }

        internal static int UnityARKit_deserializeWorldMap(IntPtr buffer, int bufferLength)
        {
            return ARWorldMap.k_InvalidHandle;
        }

        internal static bool UnityARKit_isWorldMapValid(int nativeHandle)
        {
            return false;
        }

        internal static bool UnityARKit_trySerializeWorldMap(
            int nativeHandle, out IntPtr nsdata, out int length)
        {
            nsdata = default(IntPtr);
            length = default(int);
            return false;
        }

        internal static int UnityARKit_copyAndReleaseNsData(
            IntPtr destination, IntPtr sourceNsData, int length)
        {
            return 0;
        }

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
        }

        internal static Availability UnityARKit_CheckAvailability() { return Availability.None; }

        internal static TrackableId UnityARKit_attachReferencePoint(TrackableId trackableId, Pose pose)
        {
            return TrackableId.InvalidId;
        }

        internal static bool UnityARKit_IsCameraPermissionGranted() { return false; }

        internal static TrackingState UnityARKit_getAnchorTrackingState(TrackableId id) { return TrackingState.Unavailable; }

        static internal IntPtr UnityARKit_getNativePlanePtr(TrackableId trackableId) { return IntPtr.Zero; }

        static internal IntPtr UnityARKit_getNativeReferencePointPtr(TrackableId trackableId) { return IntPtr.Zero; }

        static internal IntPtr UnityARKit_getNativeSessionPtr() { return IntPtr.Zero; }

        static internal IntPtr UnityARKit_getNativeFramePtr() { return IntPtr.Zero; }
#endif
    }
}
