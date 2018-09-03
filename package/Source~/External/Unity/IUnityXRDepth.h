#pragma once
#if UNITY
#   include "Modules/XR/ProviderInterface/UnityXRTypes.h"
#   include "Modules/XR/ProviderInterface/IUnitySubsystem.h"
#else
#   include "UnityXRTypes.h"
#   include "IUnitySubsystem.h"
#endif

#include <stddef.h>
#include <stdint.h>

/// Handle struct for use during calls to GetPointCloud. Pass back this
/// supplied in the Allocator_-prefixed functions on IUnityXRDepthInterface.
/// Allocations are fast and Unity owns the memory.
typedef struct UnityXRDepthDataAllocator UnityXRDepthDataAllocator;

/// @brief Event handler implemented by a plugin for providing depth (i.e., point cloud) subsystem data.
typedef struct UnityXRDepthProvider
{
    /// Pointer which will be passed to every callback as the userData parameter.
    void* userData;

    /// Invoked by Unity to retrieve point cloud data.
    /// @param[in] handle    Handle that could instead be obtained from lifecycle
    ///                      events, pass this back as the first parameter to
    ///                      IUnityXRDepthInterface functions that accept it.
    /// @param[in] userData  User-specified data, supplied in this struct when
    ///                      passed to RegisterDepthProvider.
    /// @param[in] allocator An allocator to use to allocate data for the point cloud.
    /// @return              Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * GetPointCloud)(UnitySubsystemHandle handle, void* userData, const UnityXRDepthDataAllocator * allocator);
} UnityXRDepthProvider;

// @brief XR interface for supplying depth data
UNITY_DECLARE_INTERFACE(IUnityXRDepthInterface)
{
    /// Entry-point for getting callbacks when the depth subsystem is initialized / started / stopped / shutdown.
    ///
    /// Example usage:
    /// @code
    /// #include "IUnityXRDepth.h"
    ///
    /// static IUnityXRDepthInterface* s_XrDepth = NULL;
    ///
    /// extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
    /// UnityPluginLoad(IUnityInterfaces* unityInterfaces)
    /// {
    ///     s_XrDepth = unityInterfaces->Get<IUnityXRDepthInterface>();
    ///     UnityLifecycleProvider depthLifecycleHandler =
    ///     {
    ///         NULL,  // This can be any object you want to be passed as userData to the following functions
    ///         &Lifecycle_Initialize,
    ///         &Lifecycle_Start,
    ///         &Lifecycle_Stop,
    ///         &Lifecycle_Shutdown
    ///     };
    ///     s_XrDepth->RegisterLifecycleProvider("PluginName", "HandheldPointClouds", &depthLifecycleHandler);
    /// }
    /// @endcode
    ///
    /// @param[in] pluginName Name of the plugin which was listed in your UnitySubsystemsManifest.json.
    /// @param[in] id         ID of the subsystem that was listed in your UnitySubsystemsManifest.json.
    /// @param[in] provider   Callbacks to register.
    /// @return               Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * RegisterLifecycleProvider)(const char* pluginName, const char* id, const UnityLifecycleProvider * provider);

    /// Registers your plugin for events that are specific to the Depth subsystem.
    ///
    /// @param[in] handle   Handle that was obtained from lifecycle events.
    /// @param[in] provider The event handler which contains definitions for depth
    ///                     subsystem events.
    /// @return             Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * RegisterDepthProvider)(UnitySubsystemHandle handle, const UnityXRDepthProvider * provider);

    /// Sets the number of point cloud points
    /// @param[in] allocationKey Allocation key found in this struct, pass it
    ///                          back here for this call to succeed.
    /// @param[in] numPoints     The number of points to allocate.
    /// @return                  Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * Allocator_SetNumberOfPoints)(const UnityXRDepthDataAllocator * allocator, size_t numPoints);

    /// Get the buffer to an array of point cloud positions.
    /// @param[in] allocationKey Allocation key found in this struct, pass it
    ///                          back here for this call to succeed.
    /// @return                  An array of UnityXRVector3.
    UnityXRVector3* (UNITY_INTERFACE_API * Allocator_GetPointsBuffer)(const UnityXRDepthDataAllocator * allocator);

    /// Get the buffer to an array of point cloud confidence values.
    /// @param[in] allocationKey Allocation key found in this struct, pass it
    ///                          back here for this call to succeed.
    /// @return                  An array of floats.
    float* (UNITY_INTERFACE_API * Allocator_GetConfidenceBuffer)(const UnityXRDepthDataAllocator * allocator);
};
UNITY_REGISTER_INTERFACE_GUID(0x27DF4866C87A48ABULL, 0xAC8AAB5A62312CD2ULL, IUnityXRDepthInterface);
