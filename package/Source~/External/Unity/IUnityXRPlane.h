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

/// Represents a plane detected by the device in the user's environment.
/// The plugin should fill out an array of UnityXRPlane when Unity invokes
/// the GetAllPlanes method.
typedef struct UnityXRPlane
{
    /// The unique (to this session) id for the plane.
    UnityXRTrackableId id;

    /// The center of the plane in device space.
    UnityXRVector3 center;

    /// The pose (position and rotation) of the plane in device space.
    /// Position need not be the center; it typically
    /// does not change very often and may refer to some point
    /// at which the plane was initially detected.
    /// Note: Unity uses a left-handed coordinate system.
    /// If rotation is identity, then the plane looks like this:
    ///
    /// +Y is out of the screen
    ///
    ///      Z
    ///      ^
    ///      |
    ///      |
    /// +---------+
    /// |         |
    /// |         |
    /// |  Plane  | --> X  ("height")
    /// |         |
    /// |         |
    /// +---------+
    /// \- width -/
    UnityXRPose pose;

    /// The dimensions of the plane (see above picture)
    UnityXRVector2 bounds;

    /// True if the plane has changed since the last call to GetAllPlanes
    bool wasUpdated;

    /// True if the plane was merged into another plane. Setting this to
    /// true will also remove the plane from Unity's internal list of planes.
    /// Triggers the XREnvironment.PlaneRemoved event with
    /// PlaneRemovedEventArgs.SubsumedByPlane non null
    bool wasMerged;

    /// If wasMerged is true, then this is the id of the plane with which
    /// this plane has been combined.
    UnityXRTrackableId mergedInto;
} UnityXRPlane;

/// Handle struct for use during calls to GetAllPlanes. Pass back this
/// back to the Allocator_-prefixed functions on IUnityXRPlaneInterface.
/// supplied in the UnityXRPlaneAllocator struct to each of its functions.
/// Allocations are inexpensive to make, and Unity owns the memory.
typedef struct UnityXRPlaneDataAllocator UnityXRPlaneDataAllocator;

/// @brief Event handler implemented by a plugin for providing plane-detection subsystem data.
typedef struct UnityXRPlaneProvider
{
    /// Pointer which will be passed to every callback as the userData parameter.
    void* userData;

    /// Retrieve all the planes currently tracked by the system.
    /// Implementations should use the provided allocator to request
    /// buffers to write plane data to.
    /// @param[in] handle    Handle that could instead be obtained from lifecycle
    ///                      events, pass this back as the first parameter to
    ///                      IUnityXRPlaneInterface functions that accept it.
    /// @param[in] userData  User-specified data, supplied in this struct when
    ///                      passed to RegisterPlaneProvider.
    /// @param allocator     An allocator to use to allocate data for planes and
    ///                      other optional data.
    /// Use this to allocate data for planes and boundary points.
    /// @return              Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * GetAllPlanes)(
        UnitySubsystemHandle handle,
        void* userData,
        UnityXRPlaneDataAllocator * allocator);
} UnityXRPlaneProvider;

// @brief XR interface for supplying plane-detection data
UNITY_DECLARE_INTERFACE(IUnityXRPlaneInterface)
{
    /// Entry-point for getting callbacks when the plane-detection subsystem is initialized / started / stopped / shutdown.
    ///
    /// Example usage:
    /// @code
    /// #include "IUnityXRPlane.h"
    ///
    /// static IUnityXRPlaneInterface* s_XrPlane = NULL;
    ///
    /// extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
    /// UnityPluginLoad(IUnityInterfaces* unityInterfaces)
    /// {
    ///     s_XrPlane = unityInterfaces->Get<IUnityXRPlaneInterface>();
    ///     UnityLifecycleProvider planeLifecycleHandler =
    ///     {
    ///         NULL,  // This can be any object you want to be passed as userData to the following functions
    ///         &Lifecycle_Initialize,
    ///         &Lifecycle_Start,
    ///         &Lifecycle_Stop,
    ///         &Lifecycle_Shutdown
    ///     };
    ///     s_XrPlane->RegisterLifecycleProvider("PluginName", "HandheldPlaneDetection", &planeLifecycleHandler);
    /// }
    /// @endcode
    ///
    /// @param[in] pluginName Name of the plugin which was listed in your UnitySubsystemsManifest.json.
    /// @param[in] id         ID of the subsystem that was listed in your UnitySubsystemsManifest.json.
    /// @param[in] provider   Callbacks to register.
    /// @return               Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * RegisterLifecycleProvider)(const char* pluginName, const char* id, const UnityLifecycleProvider * provider);

    /// Registers your plugin for events that are specific to the Plane subsystem.
    ///
    /// @param[in] handle   Handle that was obtained from lifecycle events.
    /// @param[in] provider The event handler which contains definitions for plane
    ///                     subsystem events.
    /// @return             Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * RegisterPlaneProvider)(UnitySubsystemHandle handle, const UnityXRPlaneProvider * provider);

    /// Allocates an array of UnityXRPlanes which can be written to
    /// by the UnityXRPlaneProviderProvider::GetAllPlanes method.
    /// @param[in] allocator Allocator passed to the user in 
    ///                      UnityXRPlaneProvider::GetAllPlanes, pass it
    ///                      back here for this call to succeed.
    /// @param[in] numPlanes Number of planes to allocate.
    /// @return              An array of UnityXRPlane objects to populate.
    UnityXRPlane* (UNITY_INTERFACE_API * Allocator_AllocatePlaneData)(
        UnityXRPlaneDataAllocator* allocator,
        size_t numPlanes);

    /// Allocates an array of XRVector3s which can be written to
    /// during the UnityXRPlaneProviderProvider::GetAllPlanes callback.
    /// Boundary points are optional. If you do not allocate boundary
    /// points for a plane, or if you allocate fewer than 3, then Unity
    /// will create 4 boundary points, which match the 4 corners of the plane.
    /// @param[in] allocator Allocator passed to the user in 
    ///                      UnityXRPlaneProvider::GetAllPlanes, pass it
    ///                      back here for this call to succeed.
    /// @param[in] planeId   The UnityXRTrackableId associated with the plane to which this boundary belongs.
    /// @param[in] numPoints The number of XRVector3s to allocate.
    /// @return              An array of UnityXRVector3 to fill with plane boundary vertices.
    UnityXRVector3* (UNITY_INTERFACE_API * Allocator_AllocateBoundaryPoints)(
        UnityXRPlaneDataAllocator* allocator,
        const UnityXRTrackableId * planeId,
        size_t numPoints);
};
UNITY_REGISTER_INTERFACE_GUID(0x3A154DBCC01E4E5EULL, 0xA38870231CC9D80DULL, IUnityXRPlaneInterface);
