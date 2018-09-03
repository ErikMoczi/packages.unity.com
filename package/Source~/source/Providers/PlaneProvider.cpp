#include "PlaneProvider.h"
#include "Utility.h"
#include "Wrappers/WrappedPlaneList.h"

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
    std::lock_guard<std::mutex> lock(g_PlaneMutex);

    auto iter = g_Planes.find(id);
    if (iter == g_Planes.end())
        return kUnityXRTrackingStateUnknown;

    return iter->second.trackingState;
}

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
