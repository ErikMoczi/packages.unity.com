#pragma once
#include "IUnityXRPlane.h"

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

UNITY_XR_DECLARE_INTERFACE(IUnityXRPlaneInterface_Deprecated);
UNITY_REGISTER_INTERFACE_GUID(0xCD4E6F6F67504AFDULL, 0x9A5C36B44314B707ULL, IUnityXRPlaneInterface_Deprecated);
