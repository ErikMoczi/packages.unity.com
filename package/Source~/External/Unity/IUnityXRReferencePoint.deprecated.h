#pragma once
#include "IUnityXRReferencePoint.h"

/// Interface for allocating reference points.
/// Allocations are inexpensive to make, and Unity owns the memory.
struct IUnityXRReferencePointAllocator
{
    /// Allocate a buffer of UnityXRReferencePoint which Unity then owns
    /// @param numReferencePoints The number of UnityXRReferencePoints to allocate
    /// @return A pointer to the first element of an array of size numReferencePoints
    virtual UnityXRReferencePoint* UNITY_INTERFACE_API AllocateReferencePoints(size_t numReferencePoints) = 0;
};

struct IUnityXRReferencePointProvider
{
    /// Invoked by Unity to add a reference point to the plugin's environment system.
    /// @param referencePointPose The XRPose of the reference point
    /// @param[out] outReferencePointId The id of the newly added reference point
    /// @return True if the reference point was added, false otherwise.
    virtual bool UNITY_INTERFACE_API TryAddReferencePoint(const UnityXRPose& referencePointPose, UnityXRTrackableId& outReferencePointId, UnityXRTrackingState& outTrackingState) = 0;

    /// Invoked by Unity to remove a reference point
    /// @param referencePointId The id of the reference point to remove from the system
    /// @return True if successfully removed, false othewise.
    virtual bool UNITY_INTERFACE_API TryRemoveReferencePoint(const UnityXRTrackableId& referencePointId) = 0;

    /// Invoked by Unity to get all currently tracked reference points
    /// @param allocator An allocator that can be used to request a buffer to write reference point data into.
    /// @return True if successful, false otherwise
    virtual bool UNITY_INTERFACE_API GetAllReferencePoints(IUnityXRReferencePointAllocator& allocator) = 0;
};

struct IUnityXRReferencePointSubsystem : public IUnitySubsystem
{
    /// Registers a reference point provider which Unity may invoke to add or remove
    /// reference points (aka anchors).
    /// @param referencePointProvider The IUnityXRReferencePointProvider which will service reference point requests.
    virtual void UNITY_INTERFACE_API RegisterReferencePointProvider(IUnityXRReferencePointProvider* referencePointProvider) = 0;
};

UNITY_XR_DECLARE_INTERFACE(IUnityXRReferencePointInterface_Deprecated);
UNITY_REGISTER_INTERFACE_GUID(0xCF036ECE8A1C4AEFULL, 0x83C958D11771B1B8ULL, IUnityXRReferencePointInterface_Deprecated);
