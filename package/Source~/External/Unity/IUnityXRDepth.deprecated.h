#pragma once
#include "IUnityXRDepth.h"

/// Provides an interface for allocating buffers to store point cloud data.
/// Allocations are fast and Unity owns the memory.
struct IUnityXRDepthDataAllocator
{
    /// Sets the number of point cloud points
    /// @param numPoints The number of points to allocate.
    virtual void UNITY_INTERFACE_API SetNumberOfPoints(size_t numPoints) = 0;

    /// Get the buffer to an array of point cloud positions.
    /// @return An array of XRVector3
    virtual UnityXRVector3* UNITY_INTERFACE_API GetPointsBuffer() const = 0;

    /// Get the buffer to an array of point cloud confidence values.
    /// @return An array of floats
    virtual float* UNITY_INTERFACE_API GetConfidenceBuffer() const = 0;
};

/// An interface for providing depth data, i.e. point cloud.
struct IUnityXRDepthProvider
{
    /// Invoked by Unity to retrieve point cloud data.
    /// @param allocator An allocator to use to allocate data for the point cloud.
    /// @return True if point cloud data is available, false otherwise.
    virtual bool UNITY_INTERFACE_API GetPointCloud(IUnityXRDepthDataAllocator& allocator) = 0;
};

struct IUnityXRDepthSubsystem : public IUnitySubsystem
{
    /// (Optional) Registers a depth provider for your plugin to communicate depth data (aka feature points
    /// and point clouds) back to Unity.
    /// @param depthProvider The IUnityXRDepthProvider which will be invoked by Unity during frame updates
    virtual void UNITY_INTERFACE_API RegisterDepthProvider(IUnityXRDepthProvider* depthProvider) = 0;
};

UNITY_XR_DECLARE_INTERFACE(IUnityXRDepthInterface_Deprecated);
UNITY_REGISTER_INTERFACE_GUID(0xC79CD82CD2C141FFULL, 0x8A3914F99504446AULL, IUnityXRDepthInterface_Deprecated);
