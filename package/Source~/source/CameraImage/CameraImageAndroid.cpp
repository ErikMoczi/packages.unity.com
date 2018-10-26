#include <cstdint>
#include <media/NdkImage.h>
#include <mutex>

#include "Unity/IUnityInterface.h"

#include "CameraImageApi.h"
#include "Utility.h"

#define UNITY_UBER_VERBOSE 0
#if UNITY_UBER_VERBOSE
#   define LOG_UBER_VERBOSE(...) DEBUG_LOG_VERBOSE(__VA_ARGS__)
#else
#   define LOG_UBER_VERBOSE(...)
#endif

namespace CameraImageApi
{

// Note: The ArImage's acquire, release & getNdk methods
// should be thread safe, but without a mutex
// we get deadlocks in the ARCore API. A future
// release may negate the need for this.
static std::mutex s_ArImageMutex;

static ArImage* AcquireArImage(double* timestampOut)
{
    auto session = GetArSessionMutable();
    if (session == nullptr)
    {
        LOG_UBER_VERBOSE("session was null");
        return nullptr;
    }

    auto frame = GetArFrameMutable();
    if (frame == nullptr)
    {
        LOG_UBER_VERBOSE("frame was null");
        return nullptr;
    }

    std::lock_guard<std::mutex> lock(s_ArImageMutex);

    ArImage* arImage = nullptr;
    if (ArFrame_acquireCameraImage(session, frame, &arImage) != AR_SUCCESS)
    {
        LOG_UBER_VERBOSE("ArFrame_acquireCameraImage failed");
        return nullptr;
    }

    int64_t timestampNs;
    ArFrame_getTimestamp(session, frame, &timestampNs);
    *timestampOut = timestampNs * 1e-9;

    return arImage;
}

static void ReleaseArImage(const ArImage* arImage)
{
    if (arImage == nullptr)
        return;

    std::lock_guard<std::mutex> lock(s_ArImageMutex);

    // It's the NDK Image which is const
    ArImage_release(const_cast<ArImage*>(arImage));
}

static const AImage* GetAImage(const void* image)
{
    std::lock_guard<std::mutex> lock(s_ArImageMutex);

    auto arImage = static_cast<const ArImage*>(image);
    const AImage* aImage = nullptr;
    ArImage_getNdkImage(arImage, &aImage);
    return aImage;
}

static NativeHandle AcquireImage(double* timestampOut)
{
    return NativeHandle(AcquireArImage(timestampOut));
}

static void ReleaseImage(NativeHandle handle)
{
    auto image = static_cast<const ArImage*>(handle.Value());
    ReleaseArImage(image);
}

static void Log(const char* msg)
{
    DEBUG_LOG_VERBOSE("%s", msg);
}

#define AIMAGE_API_CALL(API_FUNC, ...) \
do { \
    auto status = API_FUNC(__VA_ARGS__); \
    if (status != AMEDIA_OK) { \
        DEBUG_LOG_VERBOSE(#API_FUNC " failed with status %d", (int)status); \
        return false; \
    } \
} while(0)

static bool TryGetImageData(NativeHandle nativeHandle, ImageData* imageData)
{
    auto ndkImage = GetAImage(nativeHandle.Value());

    int imageFormat;
    AIMAGE_API_CALL(AImage_getFormat, ndkImage, &imageFormat);
    if (imageFormat != AIMAGE_FORMAT_YUV_420_888)
    {
        DEBUG_LOG_VERBOSE("AImage_getFormat returned unexpected format %d", (int)imageFormat);
        return false;
    }
    imageData->format = kImageFormatAndroidYuv420_888;

    AIMAGE_API_CALL(AImage_getWidth, ndkImage, &imageData->width);
    AIMAGE_API_CALL(AImage_getHeight, ndkImage, &imageData->height);
    AIMAGE_API_CALL(AImage_getNumberOfPlanes, ndkImage, &imageData->planeCount);
    if (imageData->planeCount != 3)
    {
        DEBUG_LOG_VERBOSE("AImage_getNumberOfPlanes returned an unexpected number of planes: %d", imageData->planeCount);
        return false;
    }

    for (int planeIndex = 0; planeIndex < imageData->planeCount; ++planeIndex)
    {
        auto& plane = imageData->planes[planeIndex];
        AIMAGE_API_CALL(AImage_getPlaneData, ndkImage, planeIndex, &plane.dataPtr, &plane.dataLength);
        AIMAGE_API_CALL(AImage_getPlaneRowStride, ndkImage, planeIndex, &plane.rowStride);
        AIMAGE_API_CALL(AImage_getPlanePixelStride, ndkImage, planeIndex, &plane.pixelStride);
    }

    return true;
}

#undef AIMAGE_API_CALL

extern "C" bool UNITY_INTERFACE_API UNITY_INTERFACE_EXPORT
UnityARCore_cameraImage_tryAcquireLatestImage(
    int* imageHandle, Vector2i* dimensions, int* planeCount, double* timestamp, ImageFormat* format)
{
    LOG_UBER_VERBOSE("UnityARCore_cameraImage_tryAcquireLatestImage");
    return TryAcquireLatestImage(imageHandle, dimensions, planeCount, timestamp, format);
}

extern "C" bool UNITY_INTERFACE_API UNITY_INTERFACE_EXPORT UnityARCore_cameraImage_tryGetConvertedDataSize(
    int imageHandle, Vector2i dimensions, TextureFormat format, int* size)
{
    LOG_UBER_VERBOSE("UnityARCore_cameraImage_tryGetConvertedDataSize");
    return TryGetConvertedDataSize(
        ImageHandle(imageHandle), dimensions, format, size);
}

extern "C" bool UNITY_INTERFACE_API UNITY_INTERFACE_EXPORT UnityARCore_cameraImage_tryConvert(
    int imageHandle, 
    ConversionParams params,
    uint8_t* buffer, int bufferSize)
{
    LOG_UBER_VERBOSE("UnityARCore_cameraImage_tryConvert %d", imageHandle);
    return TryConvertImage(ImageHandle(imageHandle), params, buffer, bufferSize);
}

extern "C" bool UNITY_INTERFACE_API UNITY_INTERFACE_EXPORT UnityARCore_cameraImage_tryGetPlane(
    int imageHandle, int planeIndex, int* rowStride, int* pixelStride, void** dataPtr, int* dataLength)
{
    LOG_UBER_VERBOSE("UnityARCore_cameraImage_tryGetPlane");
    return TryGetPlane(
        ImageHandle(imageHandle),
        planeIndex,
        rowStride,
        pixelStride,
        dataPtr,
        dataLength);
}

extern "C" bool UNITY_INTERFACE_API UNITY_INTERFACE_EXPORT UnityARCore_cameraImage_handleValid(
    int imageHandle)
{
    LOG_UBER_VERBOSE("UnityARCore_cameraImage_handleValid");
    return IsImageHandleValidImage(ImageHandle(imageHandle));
}

extern "C" void UNITY_INTERFACE_API UNITY_INTERFACE_EXPORT UnityARCore_cameraImage_disposeImage(
    int imageHandle)
{
    LOG_UBER_VERBOSE("UnityARCore_cameraImage_disposeImage");
    DisposeImage(ImageHandle(imageHandle));
}

extern "C" int UNITY_INTERFACE_API UNITY_INTERFACE_EXPORT
UnityARCore_cameraImage_createAsyncConversionRequest(
    int imageHandle, ConversionParams params)
{
    LOG_UBER_VERBOSE("UnityARCore_cameraImage_createAsyncConversionRequest");
    return CreateAsyncConversionRequest(
        ImageHandle(imageHandle), params).Value();
}

extern "C" void UNITY_INTERFACE_API UNITY_INTERFACE_EXPORT
UnityARCore_cameraImage_createAsyncConversionRequestWithCallback(
    int imageHandle, ConversionParams params, AsyncCallback callback, void* context)
{
    LOG_UBER_VERBOSE("UnityARCore_cameraImage_createAsyncConversionRequestWithCallback");
    CreateAsyncConversionRequest(
        ImageHandle(imageHandle), params, callback, context);
}

extern "C" AsyncRequestStatus UNITY_INTERFACE_API UNITY_INTERFACE_EXPORT
UnityARCore_cameraImage_getAsyncRequestStatus(int handle)
{
    LOG_UBER_VERBOSE("UnityARCore_cameraImage_getAsyncRequestStatus");
    return GetAsyncRequestStatus(AsyncRequestHandle(handle));
}

extern "C" void UNITY_INTERFACE_API UNITY_INTERFACE_EXPORT
UnityARCore_cameraImage_disposeAsyncRequest(int handle)
{
    LOG_UBER_VERBOSE("UnityARCore_cameraImage_disposeAsyncRequest");
    DisposeAsyncRequest(AsyncRequestHandle(handle));
}

extern "C" bool UNITY_INTERFACE_API UNITY_INTERFACE_EXPORT
UnityARCore_cameraImage_tryGetAsyncRequestData(
    int handle, void** dataPtr, int* dataLength)
{
    LOG_UBER_VERBOSE("UnityARCore_cameraImage_tryGetAsyncRequestData");
    return TryGetAsyncRequestData(AsyncRequestHandle(handle), dataPtr, dataLength);
}

void RegisterInterface()
{
    const PlatformInterface platformInterface =
    {
        &TryGetImageData,
        &AcquireImage,
        &ReleaseImage,
        false,
        &Log
    };

    RegisterPlatformInterface(platformInterface);
}

} // namespace CameraImageApi
