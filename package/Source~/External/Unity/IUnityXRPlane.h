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

#include <cstddef>
#include <stdint.h>

/// Represents a plane detected by the device in the user's environment.
/// The plugin should fill out an array of UnityXRPlane when Unity invokes
/// the GetAllPlanes method.
struct UnityXRPlane
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
};

/// Interface for allocating plane-related data
/// Allocations are inexpensive to make, and Unity owns the memory.
struct IUnityXRPlaneDataAllocator
{
    /// Allocates an array of UnityXRPlanes which can be written to
    /// by the IUnityXRPlaneProviderProvider::GetAllPlanes method.
    /// @param numPlanes Number of planes to allocate
    /// @return An array of UnityXRPlane
    virtual UnityXRPlane* UNITY_INTERFACE_API AllocatePlaneData(
        size_t numPlanes) = 0;

    /// Allocates an array of XRVector3s which can be written to
    /// by the IUnityXRPlaneProviderProvider::GetAllPlanes method.
    /// Boundary points are optional. If you do not allocate boundary
    /// points for a plane, or if you allocate fewer than 3, then Unity
    /// will create 4 boundary points, which match the 4 corners of the plane.
    /// @param planeId The UnityXRTrackableId associated with the plane to which this boundary belongs.
    /// @param numPoints The number of XRVector3s to allocate
    /// @return An array of XRVector3
    virtual UnityXRVector3* UNITY_INTERFACE_API AllocateBoundaryPoints(
        const UnityXRTrackableId& planeId,
        size_t numPoints) = 0;
};

/// Event handler implemented by the plugin for Plane Provider specific events.
struct IUnityXRPlaneProvider
{
    /// Retrieve all the planes currently tracked by the system.
    /// Implementations should use the provided allocator to request
    /// buffers to write plane data to.
    /// @param allocator An object which implements the IUnityXRPlaneDataAllocator interface.
    /// Use this to allocate data for planes and boundary points.
    /// @return True if successful, false otherwise
    virtual bool UNITY_INTERFACE_API GetAllPlanes(IUnityXRPlaneDataAllocator& allocator) = 0;
};

/// When IUnityXRPlaneInstance is initialized, you will get an access to plane data.
struct IUnityXRPlaneSubsystem : public IUnitySubsystem
{
    /// Registers a plane provider for your plugin to communicate plane data back to Unity
    /// @param planeProvider The IUnityXRPlaneProvider which will be invoked by Unity during frame updates
    virtual void UNITY_INTERFACE_API RegisterPlaneProvider(IUnityXRPlaneProvider* planeProvider) = 0;
};

UNITY_XR_DECLARE_INTERFACE(IUnityXRPlaneInterface);
UNITY_REGISTER_INTERFACE_GUID(0xCD4E6F6F67504AFDULL, 0x9A5C36B44314B707ULL, IUnityXRPlaneInterface);
