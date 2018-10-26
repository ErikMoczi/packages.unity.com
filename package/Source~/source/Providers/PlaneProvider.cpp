#include "PlaneProvider.h"
#include "SessionProvider.h"
#include "Utility.h"
#include "Wrappers/WrappedPlane.h"
#include "Wrappers/WrappedTrackable.h"
#include "Wrappers/WrappedTrackableList.h"

#include <algorithm>
#include <cstring>
#include <mutex>
#include <set>
#include <unordered_map>

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
bool UNITY_INTERFACE_API PlaneProvider::GetAllPlanes(IUnityXRPlaneDataAllocator& xrAllocator)
{
    if (GetArSession() == nullptr || GetArFrame() == nullptr)
        return false;

    int64_t latestFrameTimestamp = GetLatestTimestamp();
    if (latestFrameTimestamp == m_LastFrameTimestamp)
        return false;
	
    std::set<UnityXRTrackableId> updatedIds;
    {
        WrappedTrackableListRaii updatedPlanes;
        updatedPlanes.PopulateList_UpdatedOnly(AR_TRACKABLE_PLANE);

        const int32_t numUpdatedPlanes = updatedPlanes.Size();
        for (int32_t planeIndex = 0; planeIndex < numUpdatedPlanes; ++planeIndex)
        {
            WrappedTrackableRaii wrappedTrackable;
            wrappedTrackable.AcquireFromList(updatedPlanes, planeIndex);

            UnityXRTrackableId id;
            ConvertToTrackableId(id, wrappedTrackable.Get());
            updatedIds.insert(id);
        }
    }

    WrappedTrackableListRaii detectedPlanes;
    detectedPlanes.PopulateList_All(AR_TRACKABLE_PLANE);

    std::lock_guard<std::mutex> lock(g_PlaneMutex);

    const int32_t numPlanes = detectedPlanes.Size();
    UnityXRPlane* currentXrPlane = xrAllocator.AllocatePlaneData(numPlanes);
    for (int32_t planeIndex = 0; planeIndex < numPlanes; ++planeIndex, ++currentXrPlane)
    {
        WrappedTrackableRaii wrappedTrackable;
        wrappedTrackable.AcquireFromList(detectedPlanes, planeIndex);

        WrappedPlane wrappedPlane = ArAsPlane(wrappedTrackable);
        wrappedPlane.ConvertToXRPlane(*currentXrPlane, xrAllocator);
        currentXrPlane->wasUpdated = updatedIds.find(currentXrPlane->id) != updatedIds.end();

        auto& planeData = g_Planes[currentXrPlane->id];

        auto xrTrackingState = wrappedTrackable.GetTrackingState();
        currentXrPlane->wasUpdated = currentXrPlane->wasUpdated || (planeData.trackingState != xrTrackingState);
        planeData.plane = *currentXrPlane;
        planeData.trackingState = xrTrackingState;
    }

    m_LastFrameTimestamp = latestFrameTimestamp;
    return true;
}

void PlaneProvider::PopulateCStyleProvider(UnityXRPlaneProvider& provider)
{
    std::memset(&provider, 0, sizeof(provider));
    provider.userData = this;
    provider.GetAllPlanes = &StaticGetAllPlanes;
}

struct PlaneDataAllocatorRedirect : public IUnityXRPlaneDataAllocator
{
    PlaneDataAllocatorRedirect(IUnityXRPlaneInterface* unityInterface, UnityXRPlaneDataAllocator* xrAllocator)
        : m_UnityInterface(unityInterface)
        , m_XrAllocator(xrAllocator)
    {}

    virtual UnityXRPlane* AllocatePlaneData(
        size_t numPlanes) override
    {
        return m_UnityInterface->Allocator_AllocatePlaneData(m_XrAllocator, numPlanes);
    }

    virtual UnityXRVector3* AllocateBoundaryPoints(
        const UnityXRTrackableId& xrPlaneId,
        size_t numPoints) override
    {
        return m_UnityInterface->Allocator_AllocateBoundaryPoints(m_XrAllocator, &xrPlaneId, numPoints);
    }

private:
    IUnityXRPlaneInterface* m_UnityInterface;
    UnityXRPlaneDataAllocator* m_XrAllocator;
};

UnitySubsystemErrorCode UNITY_INTERFACE_API PlaneProvider::StaticGetAllPlanes(UnitySubsystemHandle handle, void* userData, UnityXRPlaneDataAllocator* xrAllocator)
{
    PlaneProvider* thiz = static_cast<PlaneProvider*>(userData);
    if (thiz == nullptr || xrAllocator == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    PlaneDataAllocatorRedirect redirect(thiz->m_UnityInterface, xrAllocator);
    return thiz->GetAllPlanes(redirect) ? kUnitySubsystemErrorCodeSuccess : kUnitySubsystemErrorCodeFailure;
}
