#pragma once
#if UNITY
#   include "Modules/XR/ProviderInterface/UnityXRTypes.h"
#   include "Modules/XR/ProviderInterface/IUnitySubsystem.h"
#   include "Runtime/PluginInterface/Headers/IUnityRenderingExtensions.h"
#else
#   include "UnityXRTypes.h"
#   include "IUnitySubsystem.h"
#   include "IUnityRenderingExtensions.h"
#endif

#include <stddef.h>
#include <stdint.h>

/// Describes the orientation of the screen
typedef enum UnityXRScreenOrientation
{
    kUnityXRScreenOrientationUnknown,               ///< The orientation is not known
    kUnityXRScreenOrientationPortrait,              ///< Portrait orientation, usually the default vertical orientation for mobile devices.
    kUnityXRScreenOrientationPortraitUpsideDown,    ///< Portrait orientation, upside down.
    kUnityXRScreenOrientationLandscapeLeft,         ///< Landscape orientation, counter-clockwise from the portrait orientation.
    kUnityXRScreenOrientationLandscapeRight,        ///< Landscape orientation, clockwise from the portrait orientation.

    /// Used to ensure this enum is represented by a 32 bit integral type
    kUnityXRScreenOrientationEnsure32Bits = 0xffffffff
} UnityXRScreenOrientation;

/// A texture descriptor, used to describe metadata concerning the
/// camera textures used natively by the device.
typedef struct UnityXRTextureDescriptor
{
    /// A pointer to the native texture id
    intptr_t nativeId;

    /// The width of the texture.
    /// Note: in many cases Unity can retrieve this data from the native texture,
    /// so it may be ignored. However, it is considered good practice to set this correctly.
    size_t width;

    /// The height of the texture.
    /// Note: in many cases Unity can retrieve this data from the native texture,
    /// so it may be ignored. However, it is considered good practice to set this correctly.
    size_t height;

    /// The format used by the texture.
    /// Note: in many cases Unity can retrieve this data from the native texture,
    /// so it may be ignored. However, it is considered good practice to set this correctly.
    UnityRenderingExtTextureFormat format;

    /// The name to assign the texture in its associated Material. If you plan
    /// to use this texture in a shader, the name should match that used in the shader.
    char name[kUnityXRStringSize];
} UnityXRTextureDescriptor;

/// The maximum number of texture descriptors
enum { kUnityXRMaxTextureDescriptors = 8 };

/// Flags representing fields in the UnityXRCameraFrame
typedef enum UnityXRCameraFramePropertyFlags
{
    /// Refers to the UnityXRCameraFrame::timestampNs
    kUnityXRCameraFramePropertiesTimestamp = 1 << 0,

    /// Refers to the UnityXRCameraFrame::averageBrightness
    kUnityXRCameraFramePropertiesAverageBrightness = 1 << 1,

    /// Refers to the UnityXRCameraFrame::averageColorTemperature
    kUnityXRCameraFramePropertiesAverageColorTemperature = 1 << 2,

    /// Refers to the UnityXRCameraFrame::projectionMatrix
    kUnityXRCameraFramePropertiesProjectionMatrix = 1 << 3,

    /// Refers to the UnityXRCameraFrame::displayMatrix
    kUnityXRCameraFramePropertiesDisplayMatrix = 1 << 4
} UnityXRCameraFramePropertyFlags;

/// Describes a camera "frame", that is all camera data associated with a single
/// snapshot in time.
typedef struct UnityXRCameraFrame
{
    /// The timestamp, in nanoseconds, associated with this frame
    int64_t timestampNs;

    /// If available, the estimated brightness of the scene.
    float averageBrightness;

    /// If available, the estimated color temperature of the scene.
    float averageColorTemperature;

    /// A 4x4 projection matrix that will be assigned to the Unity Camera
    /// associated with this XRCameraSubsystem.
    UnityXRMatrix4x4 projectionMatrix;

    /// A 4x4 matrix representing a display transform for use in the shader associated
    /// with the Unity Material. The shader parameter is "_UnityDisplayTransform".
    UnityXRMatrix4x4 displayMatrix;

    /// An array of texture descriptors describing the textures Unity should create
    /// and maintain. This should be populated at every request to
    /// IUnityXRCameraProvider::GetFrame. Unity will dynamically create or destroy
    /// textures, and recreate them if the parameters in a UnityXRTextureDescriptor
    /// changes.
    UnityXRTextureDescriptor textureDescriptors[kUnityXRMaxTextureDescriptors];

    /// The number of textures provided in the array of textureDescriptors
    size_t numTextures;

    /// A bitfield representing the fields of this struct which have been filled out.
    /// Unity will ignore any fields whose bit has not been set, except for textureDescriptors.
    /// numTextures is used to determine the validity of textureDescriptors in this case.
    UnityXRCameraFramePropertyFlags providedFields;
} UnityXRCameraFrame;

/// Parameters of the Unity Camera that may be necessary or useful to the plugin.
typedef struct UnityXRCameraParams
{
    /// The distance from the camera to the near plane
    float zNear;

    /// The distance from the camera to the far plane
    float zFar;

    /// The width of the screen resolution, in pixels
    float screenWidth;

    /// The height of the screen resolution, in pixels
    float screenHeight;

    /// The orientation of the screen
    UnityXRScreenOrientation orientation;
} UnityXRCameraParams;

/// @brief Event handler implemented by a plugin for camera subsystem main-thread specific events.
///
/// All event callbacks will be executed on the main thread.
typedef struct UnityXRCameraProvider
{
    /// Pointer which will be passed to every callback as the userData parameter.
    void* userData;

    /// Get the rendering data associated with the most recent AR frame.
    ///
    /// @param[in] handle    Handle that could instead be obtained from lifecycle
    ///                      events, pass this back as the first parameter to
    ///                      IUnityXRCameraInterface functions that accept it.
    /// @param[in] userData  User-specified data, supplied in this struct when
    ///                      passed to RegisterCameraProvider.
    /// @param[in] paramsIn  Input parameters about the Unity camera which may
    ///                      be necessary or helpful generating e.g., the projection
    ///                      matrix.
    /// @param[out] frameOut The output frame, including lighting estimation,
    ///                      projection matrix, and texture information.
    /// @return              Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * GetFrame)(UnitySubsystemHandle handle, void* userData, const UnityXRCameraParams * paramsIn, UnityXRCameraFrame * frameOut);

    /// Invoked by Unity to request that light estimation be enabled or disabled
    /// whenever the session is active. This method allows the Unity developer
    /// to specify intent. Whether light estimation is actually available may
    /// then be queried with GetEnableLightEstimationAvailable below.
    ///
    /// @param[in] handle    Handle that could instead be obtained from lifecycle
    ///                      events, pass this back as the first parameter to
    ///                      IUnityXRCameraInterface functions that accept it.
    /// @param[in] userData  User-specified data, supplied in this struct when
    ///                      passed to RegisterCameraProvider.
    /// @param[in] requested True if light estimation is requested, otherwise false.
    /// @return              Error code describing the success or failure of the operation.
    void(UNITY_INTERFACE_API * SetLightEstimationRequested)(UnitySubsystemHandle handle, void* userData, bool requested);

    /// Invoked by Unity to retrieve the name of the shader that should be used
    /// during background rendering (video overlay)
    ///
    /// @param[in] handle      Handle that could instead be obtained from lifecycle
    ///                        events, pass this back as the first parameter to
    ///                        IUnityXRCameraInterface functions that accept it.
    /// @param[in] userData    User-specified data, supplied in this struct when
    ///                        passed to RegisterCameraProvider.
    /// @param[out] shaderName The name of the shader. Max string length is kUnityXRStringSize.
    /// @return                Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * GetShaderName)(UnitySubsystemHandle handle, void* userData, char shaderName[kUnityXRStringSize]);
} UnityXRCameraProvider;

// @brief XR interface for pass-through video cameras
UNITY_DECLARE_INTERFACE(IUnityXRCameraInterface)
{
    /// Entry-point for getting callbacks when the display subsystem is initialized / started / stopped / shutdown.
    ///
    /// Example usage:
    /// @code
    /// #include "IUnityXRCamera.h"
    ///
    /// static IUnityXRCameraInterface* s_XrCamera = NULL;
    ///
    /// extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
    /// UnityPluginLoad(IUnityInterfaces* unityInterfaces)
    /// {
    ///     s_XrCamera = unityInterfaces->Get<IUnityXRCameraInterface>();
    ///     UnityLifecycleProvider cameraLifecycleHandler =
    ///     {
    ///         NULL,  // This can be any object you want to be passed as userData to the following functions
    ///         &Lifecycle_Initialize,
    ///         &Lifecycle_Start,
    ///         &Lifecycle_Stop,
    ///         &Lifecycle_Shutdown
    ///     };
    ///     s_XrCamera->RegisterLifecycleProvider("PluginName", "HandheldCamera", &cameraLifecycleHandler);
    /// }
    /// @endcode
    ///
    /// @param[in] pluginName Name of the plugin which was listed in your UnitySubsystemsManifest.json.
    /// @param[in] id         ID of the subsystem that was listed in your UnitySubsystemsManifest.json.
    /// @param[in] provider   Callbacks to register.
    /// @return               Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * RegisterLifecycleProvider)(const char* pluginName, const char* id, const UnityLifecycleProvider * provider);

    /// Registers your plugin for events that are specific to the Camera subsystem.
    ///
    /// @param[in] handle   Handle that was obtained from lifecycle events.
    /// @param[in] provider The event handler which contains definitions for camera
    ///                     subsystem events.
    /// @return             Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * RegisterCameraProvider)(UnitySubsystemHandle handle, const UnityXRCameraProvider * provider);
};
UNITY_REGISTER_INTERFACE_GUID(0x18284232876F4701ULL, 0x9FEFF8589F5F5CF4ULL, IUnityXRCameraInterface);
