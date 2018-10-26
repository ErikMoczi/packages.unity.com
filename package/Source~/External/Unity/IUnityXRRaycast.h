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

/// The result of a raycast hit.
typedef struct UnityXRRaycastHit
{
    /// The unique (to this session) id of the trackable hit by the raycast.
    UnityXRTrackableId trackableId;

    /// The pose of the hit.
    UnityXRPose pose;

    /// The distance from the ray's origin.
    float distance;

    /// The type of trackable that was hit.
    UnityXRTrackableType hitType;
} UnityXRRaycastHit;

/// Handle struct for use during calls to Raycast. Pass back this
/// back to the Allocator_-prefixed functions on IUnityXRRaycastInterface.
/// Allocations are inexpensive to make, and Unity owns the memory.
typedef struct UnityXRRaycastDataAllocator UnityXRRaycastDataAllocator;

typedef struct UnityXRRaycastProvider
{
    /// Pointer which will be passed to every callback as the userData parameter.
    void* userData;

    /// Casts a ray from given screen coordinates
    ///
    /// @param handle    Handle that could instead be obtained from lifecycle
    ///                  events, pass this back as the first parameter to
    ///                  IUnityXRRaycastInterface functions that accept it.
    /// @param userData  User-specified data, supplied in this struct when
    ///                  passed to RegisterRaycasteProvider.
    /// @param screenX   Normalized screen x - 0 is left, 1 is right.
    /// @param screenY   Normalized screen y - 0 is top, 1 is bottom.
    /// @param hitFlags  The types of trackables to cast against.
    /// @param allocator An allocator used to allocate one hit result at a time.
    /// @return          Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * Raycast)(
        UnitySubsystemHandle handle,
        void* userData,
        float screenX,
        float screenY,
        UnityXRTrackableType hitFlags,
        UnityXRRaycastDataAllocator * allocator);
} UnityXRRaycastProvider;

// @brief XR interface for performing raycast queries
UNITY_DECLARE_INTERFACE(IUnityXRRaycastInterface)
{
    /// Entry-point for getting callbacks when the raycasting subsystem is initialized / started / stopped / shutdown.
    ///
    /// Example usage:
    /// @code
    /// #include "IUnityXRRaycast.h"
    ///
    /// static IUnityXRRaycastInterface* s_XrRaycast = NULL;
    ///
    /// extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
    /// UnityPluginLoad(IUnityInterfaces* unityInterfaces)
    /// {
    ///     s_XrRaycast = unityInterfaces->Get<IUnityXRRaycastInterface>();
    ///     UnityLifecycleProvider raycastLifecycleHandler =
    ///     {
    ///         NULL,  // This can be any object you want to be passed as userData to the following functions
    ///         &Lifecycle_Initialize,
    ///         &Lifecycle_Start,
    ///         &Lifecycle_Stop,
    ///         &Lifecycle_Shutdown
    ///     };
    ///     s_XrRaycast->RegisterLifecycleProvider("PluginName", "HitBasedRaycasting", &raycastLifecycleHandler);
    /// }
    /// @endcode
    ///
    /// @param[in] pluginName Name of the plugin which was listed in your UnitySubsystemsManifest.json.
    /// @param[in] id         ID of the subsystem that was listed in your UnitySubsystemsManifest.json.
    /// @param[in] provider   Callbacks to register.
    /// @return               Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * RegisterLifecycleProvider)(const char* pluginName, const char* id, const UnityLifecycleProvider * provider);

    /// Registers a raycast provider which Unity may invoke to service raycast requests.
    ///
    /// @param[in] handle   Handle that was obtained from lifecycle events.
    /// @param[in] provider The event handler which contains definitions for
    ///                     raycasting subsystem events.
    /// @return             Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * RegisterRaycastProvider)(UnitySubsystemHandle handle, const UnityXRRaycastProvider * provider);

    /// Sets the number of raycast hits and returns a pointer to the beginning
    /// of an array of UnityXRRaycastHits.
    /// In the event this is called multiple times, existing data
    /// is copied into the new allocation. Unity owns the memory.
    ///
    /// @param[in] allocator Allocator passed to the user in
    ///                      UnityXRRaycastProvider::Raycast, pass it
    ///                      back here for this call to succeed.
    /// @param numHits       The number of UnityXRRaycastHits to allocate
    /// @return              An array of UnityXRRaycastHit to be populated.
    UnityXRRaycastHit* (UNITY_INTERFACE_API * Allocator_SetNumberOfHits)(UnityXRRaycastDataAllocator * allocator, size_t numHits);

    /// Expands the array of raycast hits by numHits and returns a pointer
    /// to the first new element. Unity owns the memory.
    ///
    /// @param[in] allocator Allocator passed to the user in
    ///                      UnityXRRaycastProvider::Raycast, pass it
    ///                      back here for this call to succeed.
    /// @param numHits       The number of UnityXRRaycastHit to expand the array by.
    /// @return              An array of UnityXRRaycastHit to be populated.
    UnityXRRaycastHit* (UNITY_INTERFACE_API * Allocator_ExpandBy)(UnityXRRaycastDataAllocator * allocator, size_t numHits);
};
UNITY_REGISTER_INTERFACE_GUID(0xDD8B50408AFB42FFULL, 0x964BBBAE4AA9E383ULL, IUnityXRRaycastInterface);
