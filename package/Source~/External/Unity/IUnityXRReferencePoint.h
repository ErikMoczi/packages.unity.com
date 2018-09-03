#pragma once
#if UNITY
#   include "Modules/XR/ProviderInterface/UnityXRTypes.h"
#   include "Modules/XR/ProviderInterface/UnityXRTrackable.h"
#   include "Modules/XR/ProviderInterface/IUnitySubsystem.h"
#else
#   include "UnityXRTypes.h"
#   include "UnityXRTrackable.h"
#   include "IUnitySubsystem.h"
#endif

#include <stddef.h>
#include <stdint.h>

/// A reference point, aka "anchor".
typedef struct UnityXRReferencePoint
{
    /// The unique (to this session) id for the reference point.
    UnityXRTrackableId id;

    /// The pose (position and rotation) of the reference point.
    UnityXRPose pose;

    /// The current tracking state of the reference point.
    UnityXRTrackingState trackingState;
} UnityXRReferencePoint;

/// Handle struct for use during calls to Raycast. Pass back this
/// back to the Allocator_-prefixed functions on IUnityXRReferencePointInterface.
/// Allocations are inexpensive to make, and Unity owns the memory.
typedef struct UnityXRReferencePointDataAllocator UnityXRReferencePointDataAllocator;

/// @brief Event handler implemented by a plugin for providing a means to
///        stabilize virtual content around real-world points.
typedef struct UnityXRReferencePointProvider
{
    /// Pointer which will be passed to every callback as the userData parameter.
    void* userData;

    /// Invoked by Unity to add a reference point to the plugin's environment system.
    /// @param[in]  handle              Handle that could instead be obtained from lifecycle
    ///                                 events, pass this back as the first parameter to
    ///                                 IUnityXRReferencePointInterface functions that accept it.
    /// @param[in]  userData            User-specified data, supplied in this struct when
    ///                                 passed to RegisterReferencePointProvider.
    /// @param[in]  referencePointPose  The XRPose of the reference point
    /// @param[out] outReferencePointId The id of the newly added reference point
    /// @param[out] outTrackingState    The tracking state of the newly added point.
    /// @return                         Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * TryAddReferencePoint)(
        UnitySubsystemHandle handle,
        void* userData,
        const UnityXRPose * referencePointPose,
        UnityXRTrackableId * outReferencePointId,
        UnityXRTrackingState * outTrackingState);

    /// Invoked by Unity to remove a reference point
    /// @param[in] handle           Handle that could instead be obtained from lifecycle
    ///                             events, pass this back as the first parameter to
    ///                             IUnityXRReferencePointInterface functions that accept it.
    /// @param[in] userData         User-specified data, supplied in this struct when
    ///                             passed to RegisterReferencePointProvider.
    /// @param[in] referencePointId The id of the reference point to remove from the system
    /// @return                     Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * TryRemoveReferencePoint)(
        UnitySubsystemHandle handle,
        void* userData,
        const UnityXRTrackableId * referencePointId);

    /// Invoked by Unity to get all currently tracked reference points
    /// @param[in] handle    Handle that could instead be obtained from lifecycle
    ///                      events, pass this back as the first parameter to
    ///                      IUnityXRReferencePointInterface functions that accept it.
    /// @param[in] userData  User-specified data, supplied in this struct when
    ///                      passed to RegisterReferencePointProvider.
    /// @param[in] allocator An allocator that can be used to request a buffer to write reference point data into.
    /// @return              Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * GetAllReferencePoints)(
        UnitySubsystemHandle handle,
        void* userData,
        UnityXRReferencePointDataAllocator * allocator);
} UnityXRReferencePointProvider;

// @brief XR interface for supplying a means of stabilizing virtual content
//        real-world positions.
UNITY_DECLARE_INTERFACE(IUnityXRReferencePointInterface)
{
    /// Entry-point for getting callbacks when the reference-point subsystem is initialized / started / stopped / shutdown.
    ///
    /// Example usage:
    /// @code
    /// #include "IUnityXRReferencePoint.h"
    ///
    /// static IUnityXRReferencePointInterface* s_XrReferencePoint = NULL;
    ///
    /// extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
    /// UnityPluginLoad(IUnityInterfaces* unityInterfaces)
    /// {
    ///     s_XrReferencePoint = unityInterfaces->Get<IUnityXRReferencePointInterface>();
    ///     UnityLifecycleProvider referencePointLifecycleHandler =
    ///     {
    ///         NULL,  // This can be any object you want to be passed as userData to the following functions
    ///         &Lifecycle_Initialize,
    ///         &Lifecycle_Start,
    ///         &Lifecycle_Stop,
    ///         &Lifecycle_Shutdown
    ///     };
    ///     s_XrReferencePoint->RegisterLifecycleProvider("PluginName", "HandheldReferencePoints", &referencePointLifecycleHandler);
    /// }
    /// @endcode
    ///
    /// @param[in] pluginName Name of the plugin which was listed in your UnitySubsystemsManifest.json.
    /// @param[in] id         ID of the subsystem that was listed in your UnitySubsystemsManifest.json.
    /// @param[in] provider   Callbacks to register.
    /// @return               Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * RegisterLifecycleProvider)(const char* pluginName, const char* id, const UnityLifecycleProvider * provider);

    /// Registers your plugin for events that are specific to the ReferencePoint subsystem.
    ///
    /// @param[in] handle   Handle that was obtained from lifecycle events.
    /// @param[in] provider The event handler which contains definitions for
    ///                     reference point subsystem events.
    /// @return             Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * RegisterReferencePointProvider)(UnitySubsystemHandle handle, const UnityXRReferencePointProvider * provider);

    /// Allocates an array of UnityXRReferencePoints which can be written to
    /// by the UnityXRReferencePointProviderProvider::GetAllReferencePoints method.
    /// @param[in] allocator          Allocator passed to the user in
    ///                               UnityXRReferencePointProvider::GetAllReferencePoints,
    ///                               pass it back here for this call to succeed.
    /// @param[in] numReferencePoints Number of reference points to allocate.
    /// @return                       An array of UnityXRReferencePoint objects to populate.
    UnityXRReferencePoint* (UNITY_INTERFACE_API * Allocator_AllocateReferencePoints)(
        UnityXRReferencePointDataAllocator * allocator,
        size_t numReferencePoints);
};
UNITY_REGISTER_INTERFACE_GUID(0xEE7D348631FF4B54ULL, 0xB1216F4392461425ULL, IUnityXRReferencePointInterface);
