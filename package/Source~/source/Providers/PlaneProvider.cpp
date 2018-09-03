#include "PlaneProvider.h"
#include "Utility.h"
#include "Wrappers/WrappedPlaneList.h"
#include "SessionProvider.h"

#include <cstring>
#include <set>
#include <unordered_map>
#include <mutex>
#include <algorithm>

struct TrackableIdHasher
{
    std::size_t operator()(const UnityXRTrackableId& trackableId) const
    {
        return ((std::hash<uint64_t>()(trackableId.idPart[0]) ^
                (std::hash<uint64_t>()(trackableId.idPart[1]) << 1)));
    }
};

struct PlaneData
{
    UnityXRPlane plane;
    UnityXRTrackingState trackingState = kUnityXRTrackingStateUnknown;
};

typedef std::unordered_map<UnityXRTrackableId, PlaneData, TrackableIdHasher> PlaneIdToDataMap;

static std::mutex g_PlaneMutex;
static PlaneIdToDataMap g_Planes;

static UnityXRTrackingState ToUnityTrackingState(ArTrackingState googleTrackingState)
{
    switch (googleTrackingState)
    {
        case AR_TRACKING_STATE_TRACKING:
            return kUnityXRTrackingStateTracking;
        case AR_TRACKING_STATE_PAUSED:
            return kUnityXRTrackingStateUnavailable;
        case AR_TRACKING_STATE_STOPPED:
            return kUnityXRTrackingStateUnknown;
    }
}

extern "C" UnityXRTrackingState UnityARCore_getAnchorTrackingState(UnityXRTrackableId id)
{
    if (!IsArSessionEnabled())
        return kUnityXRTrackingStateUnavailable;

    std::lock_guard<std::mutex> lock(g_PlaneMutex);

    auto iter = g_Planes.find(id);
    if (iter == g_Planes.end())
        return kUnityXRTrackingStateUnknown;

    return iter->second.trackingState;
}

PlaneProvider::PlaneProvider(IUnityXRPlaneInterface*& unityInterface)
    : m_UnityInterface(unityInterface)
    , m_LastFrameTimestamp(0)
{ }

// TODO: return false under the right circumstances (I'm guessing at least during tracking loss... anything else?)
bool UNITY_INTERFACE_API PlaneProvider::GetAllPlanes(IUnityXRPlaneDataAllocator& allocator)
{
    auto session = GetArSession();
    if (session == nullptr)
        return false;

    auto frame = GetArFrame();
    if (frame == nullptr)
        return false;

    int64_t latestFrameTimestamp;
    ArFrame_getTimestamp(session, frame, &latestFrameTimestamp);
    if (latestFrameTimestamp == m_LastFrameTimestamp)
        return false;
	
    std::set<UnityXRTrackableId> updatedIds;
    {
        WrappedPlaneList updatedPlanes = eWrappedConstruction::Default;
        updatedPlanes.GetUpdatedPlanes();

        const int32_t numUpdatedPlanes = updatedPlanes.Size();
        for (int32_t planeIndex = 0; planeIndex < numUpdatedPlanes; ++planeIndex)
        {
            WrappedPlane updatedPlane;
            if (!updatedPlanes.TryAcquireAt(planeIndex, updatedPlane))
                continue;

            UnityXRTrackableId id;
            ConvertToTrackableId(id, updatedPlane.Get());
            updatedIds.insert(id);
        }
    }

    WrappedPlaneList detectedPlanes = eWrappedConstruction::Default;
    detectedPlanes.GetAllPlanes();

    std::lock_guard<std::mutex> lock(g_PlaneMutex);

    const int32_t numPlanes = detectedPlanes.Size();
    UnityXRPlane* currentUnityPlane = allocator.AllocatePlaneData(numPlanes);
    for (int32_t planeIndex = 0; planeIndex < numPlanes; ++planeIndex, ++currentUnityPlane)
    {
        WrappedPlane wrappedPlane;
        if (!detectedPlanes.TryAcquireAt(planeIndex, wrappedPlane))
            continue;

        wrappedPlane.ConvertToUnityXRPlane(*currentUnityPlane, allocator);
        currentUnityPlane->wasUpdated = updatedIds.find(currentUnityPlane->id) != updatedIds.end();

        auto& planeData = g_Planes[currentUnityPlane->id];
        
        const ArTrackable* trackable = ArAsTrackable(wrappedPlane.Get());
        ArTrackingState trackingState;
        ArTrackable_getTrackingState(session, trackable, &trackingState);
        auto newTrackingState = ToUnityTrackingState(trackingState);
        currentUnityPlane->wasUpdated |= (planeData.trackingState != newTrackingState);
        planeData.plane = *currentUnityPlane;
        planeData.trackingState = newTrackingState;
    }

    m_LastFrameTimestamp = latestFrameTimestamp;

    return true;
}

struct PlaneDataAllocatorWrapper : public IUnityXRPlaneDataAllocator
{
    PlaneDataAllocatorWrapper(IUnityXRPlaneInterface* unityInterface, UnityXRPlaneDataAllocator* allocator)
        : m_UnityInterface(unityInterface)
        , m_Allocator(allocator)
    {}

    virtual UnityXRPlane* AllocatePlaneData(
        size_t numPlanes) override
    {
        return m_UnityInterface->Allocator_AllocatePlaneData(m_Allocator, numPlanes);
    }

    virtual UnityXRVector3* AllocateBoundaryPoints(
        const UnityXRTrackableId& planeId,
        size_t numPoints) override
    {
        return m_UnityInterface->Allocator_AllocateBoundaryPoints(m_Allocator, &planeId, numPoints);
    }

private:
    IUnityXRPlaneInterface* m_UnityInterface;
    UnityXRPlaneDataAllocator* m_Allocator;
};

UnitySubsystemErrorCode UNITY_INTERFACE_API PlaneProvider::StaticGetAllPlanes(UnitySubsystemHandle handle, void* userData, UnityXRPlaneDataAllocator* allocator)
{
    PlaneProvider* thiz = static_cast<PlaneProvider*>(userData);
    if (thiz == nullptr || allocator == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    PlaneDataAllocatorWrapper wrapper(thiz->m_UnityInterface, allocator);
    return thiz->GetAllPlanes(wrapper) ? kUnitySubsystemErrorCodeSuccess : kUnitySubsystemErrorCodeFailure;
}

void PlaneProvider::PopulateCStyleProvider(UnityXRPlaneProvider& provider)
{
    std::memset(&provider, 0, sizeof(provider));
    provider.userData = this;
    provider.GetAllPlanes = &StaticGetAllPlanes;
}
