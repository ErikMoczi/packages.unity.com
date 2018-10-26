#pragma once

#include <cstdint>
#include "IUnityInterface.h"
#include "Handle.h"

namespace CameraImageApi
{

// UnityEngine.TextureFormat
enum TextureFormat
{
    kTextureFormatAlpha8 = 1,
    kTextureFormatRGB24 = 3,
    kTextureFormatRGBA32 = 4,
    kTextureFormatARGB32 = 5,
    kTextureFormatBGRA32 = 14,
    kTextureFormatYUY2 = 21,
    kTextureFormatR8 = 63
};

inline bool IsColorFormat(TextureFormat format)
{
    return
        kTextureFormatRGB24 ||
        kTextureFormatRGBA32 ||
        kTextureFormatARGB32 ||
        kTextureFormatBGRA32;
}

inline bool IsGrayscaleFormat(TextureFormat format)
{
    return
        kTextureFormatAlpha8 ||
        kTextureFormatR8;
}

// UnityEngine.XR.ARExtensions.ImageTransformation
enum ImageTransformationFlags
{
    kImageTransformationFlagsNone = 0,
    kImageTransformationFlagsMirrorX = 1 << 0,
    kImageTransformationFlagsMirrorY = 1 << 1
};

// UnityEngine.XR.ARExtensions.CameraImageRequestStatus
enum AsyncRequestStatus
{
    kAsyncStatusDisposed,
    kAsyncStatusPending,
    kAsyncStatusProcessing,
    kAsyncStatusCompletedSuccess,
    kAsyncStatusCompletedFailure,
};

// UnityEngine.XR.ARExtensions.CameraImageFormat
enum ImageFormat
{
    kImageFormatUnknown,
    kImageFormatAndroidYuv420_888,
    kImageFormatIosYpCbCr420_8BiPlanarFullRange
};

// Must match UnityEngine.RectInt
struct Recti
{
    int x;
    int y;
    int width;
    int height;
};

// Must match UnityEngine.Vector2Int
struct Vector2i
{
    int x;
    int y;
};

// Must match UnityEngine.XR.ARExtensions.CameraImageConversionParams
struct ConversionParams
{
    Recti inputRect;
    Vector2i outputDimensions;
    TextureFormat format;
    ImageTransformationFlags transformationFlags;
};

// A handle to an asychronous request. Distinct from the ImageHandle it converts
typedef Handle<int, struct AsyncRequest> AsyncRequestHandle;

// A handle to a ref counted image
typedef Handle<int, class Image> ImageHandle;

// A handle to the platform-specific native image
typedef Handle<const void*, struct NativeImage> NativeHandle;

typedef void(*AsyncCallback)(AsyncRequestStatus status, ConversionParams conversionParams, void* dataPtr, int size, void* context);

enum PlaneIndex
{
    kPlaneIndexY = 0,
    kPlaneIndexUv = 1,
    kPlaneIndexU = 1,
    kPlaneIndexV = 2
};

struct Plane
{
    int rowStride;
    int pixelStride;
    uint8_t* dataPtr;
    int dataLength;
};

struct ImageData
{
    int width;
    int height;
    int planeCount;
    ImageFormat format;
    Plane planes[3];
};

bool UNITY_INTERFACE_EXPORT TryAcquireLatestImage(
    int* handle,
    Vector2i* dimensions,
    int* planeCount,
    double* timestamp,
    ImageFormat* format);

bool UNITY_INTERFACE_EXPORT IsImageHandleValidImage(
    ImageHandle imageHandle);

void UNITY_INTERFACE_EXPORT DisposeImage(
    ImageHandle handle);

bool UNITY_INTERFACE_EXPORT TryGetPlane(
    ImageHandle imageHandle,
    int planeIndex,
    int* rowStride,
    int* pixelStride,
    void** dataPtr,
    int* dataLength);

bool UNITY_INTERFACE_EXPORT TryGetConvertedDataSize(
    ImageHandle imageHandle,
    const Vector2i& dimensions,
    TextureFormat format,
    int* size);

bool UNITY_INTERFACE_EXPORT TryConvertImage(
    ImageHandle imageHandle,
    const ConversionParams& conversionParams,
    uint8_t* buffer,
    int bufferSize);

bool UNITY_INTERFACE_EXPORT TryConvertImage(
    const ImageData& imageData,
    const ConversionParams& conversionParams,
    uint8_t* buffer,
    int bufferSize,
    bool multithreaded);

AsyncRequestHandle UNITY_INTERFACE_EXPORT CreateAsyncConversionRequest(
    ImageHandle imageHandle,
    const ConversionParams& params,
    AsyncCallback callback = nullptr,
    void* context = nullptr);

AsyncRequestStatus UNITY_INTERFACE_EXPORT GetAsyncRequestStatus(
    AsyncRequestHandle handle);

bool UNITY_INTERFACE_EXPORT TryGetAsyncRequestData(
    AsyncRequestHandle handle,
    void** dataPtr,
    int* dataLength);

void UNITY_INTERFACE_EXPORT DisposeAsyncRequest(AsyncRequestHandle handle);

void UNITY_INTERFACE_EXPORT Create();
void UNITY_INTERFACE_EXPORT Update();
void UNITY_INTERFACE_EXPORT Destroy();

// Platform specific functions to implement
struct PlatformInterface
{
    bool(*TryGetImageData)(NativeHandle nativeHandle, ImageData* imageOut);
    NativeHandle(*AcquireImage)(double* timestamp);
    void(*ReleaseImage)(NativeHandle nativeHandle);
    bool uvInterleaved;
    void(*Log)(const char* msg);
};

void UNITY_INTERFACE_EXPORT RegisterPlatformInterface(const PlatformInterface& platformImpl);

} // namespace CameraImageApi
