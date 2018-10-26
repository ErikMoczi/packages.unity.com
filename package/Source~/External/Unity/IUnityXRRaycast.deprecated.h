#pragma once
#include "IUnityXRRaycast.h"

/// Provides an interface for allocating buffers to store raycast hit results.
/// Allocations are fast and Unity owns the memory.
struct IUnityXRRaycastAllocator
{
    /// Sets the number of raycast hits and returns a pointer to the beginning
    /// of an array of UnityXRRaycastHits.
    /// In the event this is called multiple times, existing data
    /// is copied into the new allocation. Unity owns the memory.
    ///
    /// @param numHits The number of UnityXRRaycastHits to allocate
    /// @return An array of UnityXRRaycastHit
    virtual UnityXRRaycastHit* UNITY_INTERFACE_API SetNumberOfHits(size_t numHits) = 0;

    /// Expands the array of raycast hits by numHits and returns a pointer
    /// to the first new element. Unity owns the memory.
    ///
    /// @param numHits The number of UnityXRRaycastHit to expand the array by.
    /// @return An array of UnityXRRaycastHit
    virtual UnityXRRaycastHit* UNITY_INTERFACE_API ExpandBy(size_t numHits = 1) = 0;
};

struct IUnityXRRaycastProvider
{
    /// Casts a ray from given screen coordinates
    ///
    /// @param screenX Normalized screen x - 0 is left, 1 is right
    /// @param screenY Normalized screen y - 0 is top, 1 is bottom
    /// @param hitFlags The types of trackables to cast against
    /// @param allocator An allocator used to allocate one hit result at a time.
    /// @return True if the ray hit a trackable, false otherwise.
    virtual bool UNITY_INTERFACE_API Raycast(
        float screenX,
        float screenY,
        UnityXRTrackableType hitFlags,
        IUnityXRRaycastAllocator& allocator) = 0;
};

struct IUnityXRRaycastSubsystem : public IUnitySubsystem
{
    /// Registers a raycast provider which Unity may invoke to service raycast requests.
    /// @param raycastProvider The IUnityXRRaycastProvider which will service raycast requests.
    virtual void UNITY_INTERFACE_API RegisterRaycastProvider(IUnityXRRaycastProvider* raycastProvider) = 0;
};

UNITY_XR_DECLARE_INTERFACE(IUnityXRRaycastInterface_Deprecated);
UNITY_REGISTER_INTERFACE_GUID(0x48FF9B218B63435FULL, 0xB823BE52477FB175ULL, IUnityXRRaycastInterface_Deprecated);
